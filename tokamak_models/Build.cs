// Builds smooth plasma-boundary profiles (cubic B-splines with sharp X-point corners) from digitised
// figures or from the parametric model, and writes STEP solids of revolution, STLs, CSVs and an SVG.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public class Seg
{
    public List<double[]> Ctrl = new List<double[]>(); // control points (R,Z) in metres
    public double[] Knots;                              // full clamped knot vector
    public int Deg = 3;
    public double RmsMm, MaxMm;                         // fit residuals vs digitised points (real scale, mm)
    public double Lambda;                               // smoothing used

    public double[] Eval(double t)
    {
        int n = Ctrl.Count - 1; var N = BS.Basis(n, Deg, Knots, t);
        double r = 0, z = 0;
        for (int i = 0; i <= n; i++) { r += N[i] * Ctrl[i][0]; z += N[i] * Ctrl[i][1]; }
        return new[] { r, z };
    }
}

public class Profile
{
    public string Name, Source, Note;
    public List<Seg> Segs = new List<Seg>();
    public List<double[]> Data;      // digitised points (R,Z) used for the fit, if any
    public List<double[]> XPts = new List<double[]>();
    public double MaxDentMm;         // deepest inward dent of any segment (real scale, mm)

    public List<double[]> Dense(int perSeg)
    {
        var o = new List<double[]>();
        foreach (var s in Segs) for (int i = 0; i < perSeg; i++) o.Add(s.Eval((double)i / perSeg));
        return o;
    }
}

public static class BS
{
    public static double[] Basis(int n, int p, double[] U, double t)
    {
        var N = new double[n + 1];
        if (t >= U[n + 1]) { N[n] = 1; return N; }
        // degree 0
        var Nb = new double[U.Length - 1];
        for (int i = 0; i < U.Length - 1; i++) Nb[i] = (t >= U[i] && t < U[i + 1]) ? 1 : 0;
        for (int k = 1; k <= p; k++)
            for (int i = 0; i < U.Length - 1 - k; i++)
            {
                double a = 0, b = 0;
                if (U[i + k] - U[i] > 0) a = (t - U[i]) / (U[i + k] - U[i]) * Nb[i];
                if (U[i + k + 1] - U[i + 1] > 0) b = (U[i + k + 1] - t) / (U[i + k + 1] - U[i + 1]) * Nb[i + 1];
                Nb[i] = a + b;
            }
        for (int i = 0; i <= n; i++) N[i] = Nb[i];
        return N;
    }

    public static double[] Knots(int n, int p)
    {
        var U = new double[n + p + 2]; int spans = n - p + 1;
        for (int i = 0; i < U.Length; i++)
        {
            if (i <= p) U[i] = 0; else if (i >= n + 1) U[i] = 1; else U[i] = (double)(i - p) / spans;
        }
        return U;
    }

    static double[] Solve(double[,] A, double[] b)
    {
        int n = b.Length; var M = (double[,])A.Clone(); var x = (double[])b.Clone();
        for (int c = 0; c < n; c++)
        {
            int piv = c; for (int r = c + 1; r < n; r++) if (Math.Abs(M[r, c]) > Math.Abs(M[piv, c])) piv = r;
            for (int k = 0; k < n; k++) { double t = M[c, k]; M[c, k] = M[piv, k]; M[piv, k] = t; }
            { double t = x[c]; x[c] = x[piv]; x[piv] = t; }
            for (int r = c + 1; r < n; r++)
            {
                double f = M[r, c] / M[c, c];
                for (int k = c; k < n; k++) M[r, k] -= f * M[c, k];
                x[r] -= f * x[c];
            }
        }
        for (int r = n - 1; r >= 0; r--) { double s = x[r]; for (int k = r + 1; k < n; k++) s -= M[r, k] * x[k]; x[r] = s / M[r, r]; }
        return x;
    }

    // Least-squares cubic fit through fixed end points; optional vertical tangent (dR = 0) at either end.
    public static Seg Fit(List<double[]> Q, int nCtrl, bool startVert, bool endVert, double lambda)
    {
        int n = nCtrl - 1, m = Q.Count, p = 3;
        var seg = new Seg { Knots = Knots(n, p) };
        var t = new double[m]; double L = 0; t[0] = 0;
        for (int j = 1; j < m; j++) { L += Dist(Q[j], Q[j - 1]); t[j] = L; }
        for (int j = 0; j < m; j++) t[j] /= L;
        var P = new double[n + 1, 2];
        for (int iter = 0; iter < 4; iter++)
        {
            for (int c = 0; c < 2; c++)
            {
                var fixedIdx = new Dictionary<int, double>();
                fixedIdx[0] = Q[0][c]; fixedIdx[n] = Q[m - 1][c];
                if (c == 0 && startVert) fixedIdx[1] = Q[0][0];
                if (c == 0 && endVert) fixedIdx[n - 1] = Q[m - 1][0];
                var free = new List<int>(); var col = new int[n + 1];
                for (int i = 0; i <= n; i++) { col[i] = -1; if (!fixedIdx.ContainsKey(i)) { col[i] = free.Count; free.Add(i); } }
                int F = free.Count; var A = new double[F, F]; var b = new double[F];
                Action<double[], double, double> addRow = (row, rhs, w) =>
                {
                    double r = rhs;
                    for (int i = 0; i <= n; i++) if (row[i] != 0 && col[i] < 0) r -= row[i] * fixedIdx[i];
                    for (int i = 0; i <= n; i++)
                    {
                        if (row[i] == 0 || col[i] < 0) continue;
                        b[col[i]] += w * row[i] * r;
                        for (int k = 0; k <= n; k++) if (row[k] != 0 && col[k] >= 0) A[col[i], col[k]] += w * row[i] * row[k];
                    }
                };
                for (int j = 0; j < m; j++) addRow(Basis(n, p, seg.Knots, t[j]), Q[j][c], 1.0);
                for (int i = 1; i < n; i++)
                {
                    var row = new double[n + 1]; row[i - 1] = 1; row[i] = -2; row[i + 1] = 1;
                    addRow(row, 0, lambda * m / n);
                }
                var x = F > 0 ? Solve(A, b) : new double[0];
                for (int i = 0; i <= n; i++) P[i, c] = col[i] >= 0 ? x[col[i]] : fixedIdx[i];
            }
            seg.Ctrl.Clear();
            for (int i = 0; i <= n; i++) seg.Ctrl.Add(new[] { P[i, 0], P[i, 1] });
            // re-parameterise: project each data point onto the current curve (local search)
            for (int j = 1; j < m - 1; j++)
            {
                double best = t[j], bd = Dist(seg.Eval(t[j]), Q[j]);
                for (double h = 0.02; h > 1e-6; h *= 0.5)
                    for (int s = -1; s <= 1; s += 2)
                    {
                        double tt = Math.Max(0, Math.Min(1, best + s * h)), d = Dist(seg.Eval(tt), Q[j]);
                        if (d < bd) { bd = d; best = tt; }
                    }
                t[j] = best;
            }
        }
        double ss = 0, mx = 0;
        for (int j = 0; j < m; j++) { double d = Dist(seg.Eval(t[j]), Q[j]); ss += d * d; mx = Math.Max(mx, d); }
        seg.RmsMm = 1000 * Math.Sqrt(ss / m); seg.MaxMm = 1000 * mx;
        return seg;
    }

    public static double Dist(double[] a, double[] b) { double dx = a[0] - b[0], dy = a[1] - b[1]; return Math.Sqrt(dx * dx + dy * dy); }
}

public static class Build
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    const double Scale = 10.0; // mm per metre at 1:100

    // Least-squares line through points: returns point and unit direction.
    static void LineFit(List<double[]> pts, out double[] c, out double[] d)
    {
        double mx = 0, my = 0; foreach (var p in pts) { mx += p[0]; my += p[1]; } mx /= pts.Count; my /= pts.Count;
        double sxx = 0, sxy = 0, syy = 0;
        foreach (var p in pts) { double dx = p[0] - mx, dy = p[1] - my; sxx += dx * dx; sxy += dx * dy; syy += dy * dy; }
        double ang = 0.5 * Math.Atan2(2 * sxy, sxx - syy);
        c = new[] { mx, my }; d = new[] { Math.Cos(ang), Math.Sin(ang) };
    }

    // Build a profile from ordered CCW points (R,Z) and approximate X-point locations.
    public static Profile FromPoints(string name, List<double[]> pts, List<double[]> xHints, bool refineX, string source, string note, int ctrlPerTurn)
    {
        var prof = new Profile { Name = name, Source = source, Note = note, Data = pts };
        double rmin = 1e9, rmax = -1e9, zmin = 1e9, zmax = -1e9;
        foreach (var p in pts) { rmin = Math.Min(rmin, p[0]); rmax = Math.Max(rmax, p[0]); zmin = Math.Min(zmin, p[1]); zmax = Math.Max(zmax, p[1]); }
        double a = 0.5 * (rmax - rmin);
        var work = new List<double[]>(pts);
        var xs = new List<double[]>();
        foreach (var h in xHints)
        {
            double[] X = h;
            if (refineX)
            {
                // nearest traced point, then fit a line to each branch 0.05a..0.25a away and intersect
                int k = 0; double bd = 1e9;
                for (int i = 0; i < work.Count; i++) { double d = BS.Dist(work[i], h); if (d < bd) { bd = d; k = i; } }
                var br1 = new List<double[]>(); var br2 = new List<double[]>();
                for (int s = 1; s < work.Count / 4; s++)
                {
                    var p1 = work[(k + s) % work.Count]; double d1 = BS.Dist(p1, work[k]);
                    if (d1 > 0.05 * a && d1 < 0.25 * a) br1.Add(p1);
                    var p2 = work[(k - s + work.Count) % work.Count]; double d2 = BS.Dist(p2, work[k]);
                    if (d2 > 0.05 * a && d2 < 0.25 * a) br2.Add(p2);
                }
                double[] c1, d1v, c2, d2v; LineFit(br1, out c1, out d1v); LineFit(br2, out c2, out d2v);
                double den = d1v[0] * d2v[1] - d1v[1] * d2v[0];
                double tt = ((c2[0] - c1[0]) * d2v[1] - (c2[1] - c1[1]) * d2v[0]) / den;
                X = new[] { c1[0] + tt * d1v[0], c1[1] + tt * d1v[1] };
                prof.Note += string.Format(Inv, " X({0:0.000},{1:0.000}) corner {2:0}deg;", X[0], X[1],
                    180 / Math.PI * Math.Acos(Math.Abs(d1v[0] * d2v[0] + d1v[1] * d2v[1])));
            }
            // drop traced points within 0.05a of the X-point, insert the X-point itself
            int ins = -1; double bestD = 1e9;
            for (int i = work.Count - 1; i >= 0; i--)
            {
                double d = BS.Dist(work[i], X);
                if (d < bestD) { bestD = d; ins = i; }
            }
            var nw = new List<double[]>(); int insAt = -1;
            for (int i = 0; i < work.Count; i++)
            {
                if (BS.Dist(work[i], X) < 0.05 * a) { if (insAt < 0) { insAt = nw.Count; nw.Add(X); } continue; }
                nw.Add(work[i]);
            }
            if (insAt < 0) nw.Insert(ins, X);
            work = nw; xs.Add(X);
        }
        prof.XPts = xs;
        // split indices: outboard Rmax, inboard Rmin (vertical tangents), X-points (corners)
        // (use a +-10 sample moving average of R so a single noisy point cannot become the split)
        int iMax = 0, iMin = 0; var rs = new double[work.Count];
        for (int i = 0; i < work.Count; i++)
        {
            double s = 0; for (int j = -10; j <= 10; j++) s += work[(i + j + work.Count) % work.Count][0];
            rs[i] = s / 21;
        }
        for (int i = 0; i < work.Count; i++) { if (rs[i] > rs[iMax]) iMax = i; if (rs[i] < rs[iMin]) iMin = i; }
        var splits = new List<KeyValuePair<int, bool>>(); // index, isVerticalTangent
        splits.Add(new KeyValuePair<int, bool>(iMax, true)); splits.Add(new KeyValuePair<int, bool>(iMin, true));
        foreach (var X in xs) { int k = work.FindIndex(q => q == X); splits.Add(new KeyValuePair<int, bool>(k, false)); }
        // order splits CCW starting at iMax
        splits.Sort((u, v) => ((u.Key - iMax + work.Count) % work.Count).CompareTo((v.Key - iMax + work.Count) % work.Count));
        double perim = 0; for (int i = 0; i < work.Count; i++) perim += BS.Dist(work[i], work[(i + 1) % work.Count]);
        for (int s = 0; s < splits.Count; s++)
        {
            int i0 = splits[s].Key, i1 = splits[(s + 1) % splits.Count].Key;
            var Q = new List<double[]>();
            for (int i = i0; ; i = (i + 1) % work.Count) { Q.Add(work[i]); if (i == i1) break; }
            double len = 0; for (int i = 1; i < Q.Count; i++) len += BS.Dist(Q[i], Q[i - 1]);
            int nc0 = Math.Max(5, (int)Math.Round(ctrlPerTurn * len / perim) + 3);
            // smallest smoothing (then fewest control points) whose deepest inward dent is < 0.5 mm (real scale)
            Seg best = null; double bestDepth = 1e9;
            double[] lams = { 1e-5, 1e-4, 1e-3, 3e-3, 1e-2, 3e-2, 0.1, 0.3, 1, 3 };
            for (int nc = nc0; nc >= 5 && bestDepth > 5e-4; nc -= 2)
                foreach (var lam in lams)
                {
                    var seg = BS.Fit(Q, nc, splits[s].Value, splits[(s + 1) % splits.Count].Value, lam);
                    double dep = SegDepth(seg);
                    if (dep < bestDepth) { best = seg; bestDepth = dep; best.Lambda = lam; }
                    if (dep <= 5e-4) break;
                }
            prof.Segs.Add(best);
            prof.MaxDentMm = Math.Max(prof.MaxDentMm, 1000 * bestDepth);
            prof.Note += string.Format(Inv, " s{0}[n{1} lam{2} rms{3:0.0} max{4:0.0} dent{5:0.00}]", s, best.Ctrl.Count, best.Lambda, best.RmsMm, best.MaxMm, 1000 * bestDepth);
        }
        return prof;
    }

    // Depth (m) of the deepest inward dent: max distance of the segment from its own convex hull.
    public static double SegDepth(Seg s)
    {
        var p = new List<double[]>(); for (int i = 0; i <= 400; i++) p.Add(s.Eval(i / 400.0));
        var pts = new List<double[]>(p); pts.Sort((u, v) => u[0] != v[0] ? u[0].CompareTo(v[0]) : u[1].CompareTo(v[1]));
        Func<double[], double[], double[], double> cross = (o, a, b) => (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0]);
        var hull = new List<double[]>();
        for (int pass = 0; pass < 2; pass++)
        {
            int start = hull.Count;
            for (int ii = 0; ii < pts.Count; ii++)
            {
                var q = pass == 0 ? pts[ii] : pts[pts.Count - 1 - ii];
                while (hull.Count >= start + 2 && cross(hull[hull.Count - 2], hull[hull.Count - 1], q) <= 0) hull.RemoveAt(hull.Count - 1);
                hull.Add(q);
            }
            hull.RemoveAt(hull.Count - 1);
        }
        double depth = 0;
        foreach (var q in p)
        {
            double dmin = 1e9;
            for (int i = 0; i < hull.Count; i++)
            {
                var a = hull[i]; var b = hull[(i + 1) % hull.Count];
                double dx = b[0] - a[0], dz = b[1] - a[1], L2 = dx * dx + dz * dz;
                double t = L2 > 0 ? Math.Max(0, Math.Min(1, ((q[0] - a[0]) * dx + (q[1] - a[1]) * dz) / L2)) : 0;
                double ex = a[0] + t * dx - q[0], ez = a[1] + t * dz - q[1];
                dmin = Math.Min(dmin, Math.Sqrt(ex * ex + ez * ez));
            }
            depth = Math.Max(depth, dmin);
        }
        return depth;
    }

    // Number of sample points where the segment bends the wrong way (clockwise) - 0 means convex.
    public static int SegConcave(Seg s)
    {
        var p = new List<double[]>(); for (int i = 0; i <= 400; i++) p.Add(s.Eval(i / 400.0));
        int c = 0;
        for (int i = 1; i < p.Count - 1; i++)
        {
            double ax = p[i][0] - p[i - 1][0], az = p[i][1] - p[i - 1][1], bx = p[i + 1][0] - p[i][0], bz = p[i + 1][1] - p[i][1];
            double cr = ax * bz - az * bx, l = Math.Sqrt((ax * ax + az * az) * (bx * bx + bz * bz));
            if (cr < -1e-7 * l) c++;
        }
        return c;
    }

    // ---------------- STEP (AP214) solid of revolution about Z ----------------
    public static string Step(Profile pr, string fileName)
    {
        var sb = new StringBuilder(); int id = 0;
        Func<string, int> E = s => { id++; sb.Append('#').Append(id).Append('=').Append(s).Append(";\n"); return id; };
        Func<double, string> F = v => { var s = v.ToString("0.0#########", Inv); return s; };
        Func<double, double, double, int> Pt = (x, y, z) => E("CARTESIAN_POINT('',(" + F(x) + "," + F(y) + "," + F(z) + "))");
        string head = "ISO-10303-21;\nHEADER;\nFILE_DESCRIPTION(('" + pr.Name + " plasma boundary (LCFS) solid of revolution, 1:100 scale, mm'),'2;1');\n" +
            "FILE_NAME('" + fileName + "','" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", Inv) + "',(''),(''),'TokamakGen','TokamakGen','');\n" +
            "FILE_SCHEMA(('AUTOMOTIVE_DESIGN { 1 0 10303 214 1 1 1 1 }'));\nENDSEC;\nDATA;\n";
        int appCtx = E("APPLICATION_CONTEXT('automotive design')");
        E("APPLICATION_PROTOCOL_DEFINITION('international standard','automotive_design',2000,#" + appCtx + ")");
        int prodCtx = E("PRODUCT_CONTEXT('',#" + appCtx + ",'mechanical')");
        string pname = pr.Name + "_plasma";
        int prod = E("PRODUCT('" + pname + "','" + pname + "','',(#" + prodCtx + "))");
        E("PRODUCT_RELATED_PRODUCT_CATEGORY('part',$,(#" + prod + "))");
        int pdf = E("PRODUCT_DEFINITION_FORMATION('','',#" + prod + ")");
        int pdc = E("PRODUCT_DEFINITION_CONTEXT('part definition',#" + appCtx + ",'design')");
        int pd = E("PRODUCT_DEFINITION('design','',#" + pdf + ",#" + pdc + ")");
        int pds = E("PRODUCT_DEFINITION_SHAPE('','',#" + pd + ")");
        int lu = E("(LENGTH_UNIT()NAMED_UNIT(*)SI_UNIT(.MILLI.,.METRE.))");
        int au = E("(NAMED_UNIT(*)PLANE_ANGLE_UNIT()SI_UNIT($,.RADIAN.))");
        int su = E("(NAMED_UNIT(*)SI_UNIT($,.STERADIAN.)SOLID_ANGLE_UNIT())");
        int unc = E("UNCERTAINTY_MEASURE_WITH_UNIT(LENGTH_MEASURE(1.E-06),#" + lu + ",'distance_accuracy_value','confusion accuracy')");
        int ctx = E("(GEOMETRIC_REPRESENTATION_CONTEXT(3)GLOBAL_UNCERTAINTY_ASSIGNED_CONTEXT((#" + unc + "))GLOBAL_UNIT_ASSIGNED_CONTEXT((#" + lu + ",#" + au + ",#" + su + "))REPRESENTATION_CONTEXT('Context #1','3D Context with UNIT and UNCERTAINTY'))");
        int o = Pt(0, 0, 0);
        int dz = E("DIRECTION('',(0.0,0.0,1.0))"), dx = E("DIRECTION('',(1.0,0.0,0.0))");
        int world = E("AXIS2_PLACEMENT_3D('',#" + o + ",#" + dz + ",#" + dx + ")");
        int axis = E("AXIS1_PLACEMENT('',#" + o + ",#" + dz + ")");

        int K = pr.Segs.Count;
        var vA = new int[K]; var vB = new int[K]; var circ = new int[K];
        for (int k = 0; k < K; k++)
        {
            var P0 = pr.Segs[k].Ctrl[0]; double R = P0[0] * Scale, Z = P0[1] * Scale;
            vA[k] = E("VERTEX_POINT('',#" + Pt(R, 0, Z) + ")");
            vB[k] = E("VERTEX_POINT('',#" + Pt(-R, 0, Z) + ")");
            int c = Pt(0, 0, Z);
            int ax = E("AXIS2_PLACEMENT_3D('',#" + c + ",#" + dz + ",#" + dx + ")");
            circ[k] = E("CIRCLE('',#" + ax + "," + F(R) + ")");
        }
        var c0 = new int[K]; var cp = new int[K]; var surf = new int[K];
        for (int k = 0; k < K; k++)
        {
            var s = pr.Segs[k]; int n = s.Ctrl.Count - 1;
            var ids0 = new List<string>(); var idsP = new List<string>();
            foreach (var q in s.Ctrl) { ids0.Add("#" + Pt(q[0] * Scale, 0, q[1] * Scale)); }
            foreach (var q in s.Ctrl) { idsP.Add("#" + Pt(-q[0] * Scale, 0, q[1] * Scale)); }
            // knot vector in compressed form
            var kv = new List<double>(); var mult = new List<int>();
            foreach (var u in s.Knots) { if (kv.Count > 0 && Math.Abs(kv[kv.Count - 1] - u) < 1e-12) mult[mult.Count - 1]++; else { kv.Add(u); mult.Add(1); } }
            string ks = "(" + string.Join(",", mult) + "),(" + string.Join(",", kv.ConvertAll<string>(v => F(v))) + ")";
            c0[k] = E("B_SPLINE_CURVE_WITH_KNOTS('',3,(" + string.Join(",", ids0) + "),.UNSPECIFIED.,.F.,.F.," + ks + ",.UNSPECIFIED.)");
            cp[k] = E("B_SPLINE_CURVE_WITH_KNOTS('',3,(" + string.Join(",", idsP) + "),.UNSPECIFIED.,.F.,.F.," + ks + ",.UNSPECIFIED.)");
            surf[k] = E("SURFACE_OF_REVOLUTION('',#" + c0[k] + ",#" + axis + ")");
        }
        var e0 = new int[K]; var ep = new int[K]; var a1 = new int[K]; var a2 = new int[K];
        for (int k = 0; k < K; k++)
        {
            int k1 = (k + 1) % K;
            e0[k] = E("EDGE_CURVE('',#" + vA[k] + ",#" + vA[k1] + ",#" + c0[k] + ",.T.)");
            ep[k] = E("EDGE_CURVE('',#" + vB[k] + ",#" + vB[k1] + ",#" + cp[k] + ",.T.)");
            a1[k] = E("EDGE_CURVE('',#" + vA[k] + ",#" + vB[k] + ",#" + circ[k] + ",.T.)");
            a2[k] = E("EDGE_CURVE('',#" + vB[k] + ",#" + vA[k] + ",#" + circ[k] + ",.T.)");
        }
        Func<int, bool, int> OE = (e, sense) => E("ORIENTED_EDGE('',*,*,#" + e + "," + (sense ? ".T." : ".F.") + ")");
        var faces = new List<string>();
        for (int k = 0; k < K; k++)
        {
            int k1 = (k + 1) % K;
            // half 1: phi 0..pi ; loop (u,v) CCW: arc1_k, Epi_k, -arc1_k1, -E0_k
            int l1 = E("EDGE_LOOP('',(#" + OE(a1[k], true) + ",#" + OE(ep[k], true) + ",#" + OE(a1[k1], false) + ",#" + OE(e0[k], false) + "))");
            int b1 = E("FACE_OUTER_BOUND('',#" + l1 + ",.T.)");
            faces.Add("#" + E("ADVANCED_FACE('',(#" + b1 + "),#" + surf[k] + ",.T.)"));
            // half 2: phi pi..2pi ; loop: arc2_k, E0_k, -arc2_k1, -Epi_k
            int l2 = E("EDGE_LOOP('',(#" + OE(a2[k], true) + ",#" + OE(e0[k], true) + ",#" + OE(a2[k1], false) + ",#" + OE(ep[k], false) + "))");
            int b2 = E("FACE_OUTER_BOUND('',#" + l2 + ",.T.)");
            faces.Add("#" + E("ADVANCED_FACE('',(#" + b2 + "),#" + surf[k] + ",.T.)"));
        }
        int shell = E("CLOSED_SHELL('',(" + string.Join(",", faces) + "))");
        int brep = E("MANIFOLD_SOLID_BREP('" + pname + "',#" + shell + ")");
        int rep = E("ADVANCED_BREP_SHAPE_REPRESENTATION('" + pname + "',(#" + world + ",#" + brep + "),#" + ctx + ")");
        E("SHAPE_DEFINITION_REPRESENTATION(#" + pds + ",#" + rep + ")");
        return head + sb.ToString() + "ENDSEC;\nEND-ISO-10303-21;\n";
    }

    // ---------------- everything ----------------
    public static string Run(string root)
    {
        string outDir = Path.Combine(root, "tokamak_models");
        string stepDir = Path.Combine(outDir, "step"), stlDir = Path.Combine(outDir, "stl"), csvDir = Path.Combine(outDir, "csv");
        Directory.CreateDirectory(stepDir); Directory.CreateDirectory(stlDir); Directory.CreateDirectory(csvDir);
        var profiles = new List<Profile>();
        var figs = Figures.All(root);
        var overlays = new List<KeyValuePair<Fig, Profile>>();
        foreach (var f in figs)
        {
            var raw = Trace.Run(f, 0.25);
            var pts = new List<double[]>(); foreach (var p in raw) pts.Add(new[] { p[0], p[1] });
            var hints = new List<double[]>(); foreach (var h in f.XHints) hints.Add(new[] { f.R(h[0]), f.Z(h[1]) });
            var pr = FromPoints(f.Name, pts, hints, !f.XExact, f.Source, "digitised;", 40);
            profiles.Add(pr); overlays.Add(new KeyValuePair<Fig, Profile>(f, pr));
        }
        // parametric model for machines without a reference figure
        var model = new[] {
            new Machine("ITER", 6.20, 2.00, 1.85, 0.49, 0.49, false, true, "LSN"),
            new Machine("ARC",  4.62, 1.18, 1.80, 0.65, 0.65, true, true,  "DN (V3A)"),
        };
        foreach (var m in model)
        {
            var b = TokamakGen.Boundary(m, 1600);
            var xs = new List<double[]>();
            if (m.XUpper) xs.Add(new[] { m.R0 - m.DeltaU * m.A, m.Kappa * m.A });
            if (m.XLower) xs.Add(new[] { m.R0 - m.DeltaL * m.A, -m.Kappa * m.A });
            // snap X-points to the exact model vertices
            var snapped = new List<double[]>();
            foreach (var x in xs) { double bd = 1e9; double[] best = null; foreach (var p in b) { double d = BS.Dist(p, x); if (d < bd) { bd = d; best = p; } } snapped.Add(best); }
            var pr = FromPointsExact(m.Name, b, snapped, "parametric model (R0 " + m.R0 + ", a " + m.A + ", kappa " + m.Kappa + ", delta " + m.DeltaU + "/" + m.DeltaL + ")");
            profiles.Add(pr);
        }
        // order for outputs
        var order = new[] { "WEST", "EAST", "DIII-D", "SPARC", "ITER", "ARC", "NSTX-U", "JET", "JT-60U" };
        profiles.Sort((u, v) => Array.IndexOf(order, u.Name).CompareTo(Array.IndexOf(order, v.Name)));

        var rep = new StringBuilder();
        rep.AppendLine("Machine | R0 | a | kappa | dU | dL | X-points | area m2 | volume m3 | fit rms/max mm (real) | concave | STEP faces | mesh check");
        var csvAll = new StringBuilder("machine,R_m,Z_m,R_mm_1to100,Z_mm_1to100\n");
        string[] colors = { "#1f77b4", "#d62728", "#2ca02c", "#ff7f0e", "#9467bd", "#8c564b", "#e377c2", "#bcbd22", "#7f7f7f" };
        var svg = new StringBuilder(); double ppm = 100, W = 9.2 * ppm + 80, H = 9.0 * ppm + 60, ox = 50, oy = H / 2;
        SvgFrame(svg, W, H, ox, oy, ppm);
        for (int k = 0; k < profiles.Count; k++)
        {
            var pr = profiles[k]; string tag = pr.Name.Replace("-", "");
            var dense = pr.Dense(200);
            double cr, cz, area, vol; TokamakGen.Centroid(dense, out cr, out cz, out area, out vol);
            double rmin = 1e9, rmax = -1e9, zmin = 1e9, zmax = -1e9; int iT = 0, iB = 0;
            for (int i = 0; i < dense.Count; i++) { var p = dense[i]; rmin = Math.Min(rmin, p[0]); rmax = Math.Max(rmax, p[0]); if (p[1] > zmax) { zmax = p[1]; iT = i; } if (p[1] < zmin) { zmin = p[1]; iB = i; } }
            double R0 = 0.5 * (rmax + rmin), a = 0.5 * (rmax - rmin), kap = (zmax - zmin) / (2 * a);
            double dU = (R0 - dense[iT][0]) / a, dL = (R0 - dense[iB][0]) / a;
            File.WriteAllText(Path.Combine(stepDir, tag + "_plasma_1to100.step"), Step(pr, tag + "_plasma_1to100.step"));
            var cut = TokamakGen.Cutaway(dense, cr, cz, 270, 1.5);
            cut.Write(Path.Combine(stlDir, tag + "_cutaway270_1to100.stl"), pr.Name + " LCFS 1:100 cutaway (mm)");
            TokamakGen.Plate(dense, cr, cz, 3.0).Write(Path.Combine(stlDir, tag + "_profile_plate_1to100.stl"), pr.Name + " LCFS 1:100 plate (mm)");
            double rmsMax = 0, maxMax = 0; foreach (var s in pr.Segs) { rmsMax = Math.Max(rmsMax, s.RmsMm); maxMax = Math.Max(maxMax, s.MaxMm); }
            rep.AppendLine(string.Format(Inv, "{0} | {1:0.000} | {2:0.000} | {3:0.00} | {4:0.00} | {5:0.00} | {6} | {7:0.00} | {8:0.0} | {9:0}/{10:0} | {11} | {12} | {13:0}/{14:0} mm3 | {15}",
                pr.Name, R0, a, kap, dU, dL, pr.XPts.Count, area, vol, rmsMax, maxMax, TokamakGen.ConcaveCount(dense), 2 * pr.Segs.Count,
                cut.SignedVolume(), vol * 750, pr.Note));
            var csv = new StringBuilder("R_m,Z_m,R_mm_1to100,Z_mm_1to100\n"); var path = new StringBuilder();
            for (int i = 0; i < dense.Count; i++)
            {
                var p = dense[i];
                string line = string.Format(Inv, "{0:0.00000},{1:0.00000},{2:0.0000},{3:0.0000}", p[0], p[1], p[0] * Scale, p[1] * Scale);
                csv.AppendLine(line); csvAll.AppendLine(pr.Name + "," + line);
                path.Append(string.Format(Inv, "{0}{1:0.0},{2:0.0} ", i == 0 ? "M" : "L", ox + p[0] * ppm, oy - p[1] * ppm));
            }
            File.WriteAllText(Path.Combine(csvDir, tag + "_boundary.csv"), csv.ToString());
            svg.AppendLine(string.Format(Inv, "<path d='{0}Z' fill='{1}' fill-opacity='0.18' stroke='{1}' stroke-width='2'/>", path, colors[k]));
            double lift = pr.Name == "EAST" ? 14 : pr.Name == "SPARC" ? 28 : pr.Name == "DIII-D" ? 0 : 0;
            svg.AppendLine(string.Format(Inv, "<text x='{0}' y='{1}' font-size='13' font-weight='600' text-anchor='middle' fill='{2}'>{3}</text>", ox + R0 * ppm, oy - zmax * ppm - 6 - lift, colors[k], pr.Name));
        }
        svg.AppendLine(string.Format(Inv, "<text x='{0}' y='{1}' font-size='12' fill='#666'>Equal R and Z scale. Grid: 1 m = 10 mm at 1:100.</text>", W - 330, H - 40));
        svg.AppendLine("</svg>");
        File.WriteAllText(Path.Combine(outDir, "comparison_cross_sections.svg"), svg.ToString());
        File.WriteAllText(Path.Combine(csvDir, "all_boundaries.csv"), csvAll.ToString());
        // verification overlays: fitted profile (black line) and X-points (blue) on each source figure
        string ovDir = Path.Combine(outDir, "verification"); Directory.CreateDirectory(ovDir);
        foreach (var kv in overlays)
        {
            var f = kv.Key; var pr = kv.Value;
            var raw = Trace.Run(f, 0.25);
            Trace.Overlay(f, raw, pr.XPts, pr.Dense(200), Path.Combine(ovDir, pr.Name.Replace("-", "") + "_trace_vs_figure.png"), 2);
        }
        return rep.ToString();
    }

    // X-points known exactly (model): split there and at Rmax/Rmin, fit without refining.
    static Profile FromPointsExact(string name, List<double[]> b, List<double[]> xs, string src)
    {
        var pr = FromPoints(name, b, xs, false, src, "model;", 40);
        return pr;
    }

    static void SvgFrame(StringBuilder svg, double W, double H, double ox, double oy, double ppm)
    {
        svg.AppendLine(string.Format(Inv, "<svg xmlns='http://www.w3.org/2000/svg' width='{0}' height='{1}' viewBox='0 0 {0} {1}' font-family='Segoe UI,Arial,sans-serif'>", W, H));
        svg.AppendLine("<rect width='100%' height='100%' fill='white'/>");
        for (int r = 0; r <= 9; r++)
        {
            double x = ox + r * ppm;
            svg.AppendLine(string.Format(Inv, "<line x1='{0}' y1='20' x2='{0}' y2='{1}' stroke='#eee'/><text x='{0}' y='{2}' font-size='12' text-anchor='middle' fill='#666'>{3} m</text>", x, H - 30, H - 14, r));
        }
        for (int z = -4; z <= 4; z++)
        {
            double y = oy - z * ppm;
            svg.AppendLine(string.Format(Inv, "<line x1='{0}' y1='{1}' x2='{2}' y2='{1}' stroke='#eee'/><text x='{3}' y='{4}' font-size='12' text-anchor='end' fill='#666'>{5}</text>", ox, y, W - 10, ox - 6, y + 4, z));
        }
        svg.AppendLine(string.Format(Inv, "<text x='14' y='{0}' font-size='12' fill='#666' transform='rotate(-90 14 {0})' text-anchor='middle'>Z (m)</text>", oy));
        svg.AppendLine(string.Format(Inv, "<line x1='{0}' y1='20' x2='{0}' y2='{1}' stroke='#333' stroke-width='2'/><text x='{2}' y='34' font-size='12' fill='#333'>machine axis (R = 0)</text>", ox, H - 30, ox + 6));
    }
}
