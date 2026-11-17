// Digitises the WHAM plasma (pink) from reference_figures/WHAM_fig.png
// (Endrizzi et al., J. Plasma Phys. 2023, Fig. 2: overhead section of WHAM).
// The figure is a plan view: horizontal = machine axis Z, vertical = X (radius).
// Calibration from the tick marks: 428.4 px/m along Z (Z = 0 at x = 1031.5),
// 428.9 px/m along X (equal aspect within 0.1%).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;

public static class WhamTrace
{
    public const double PxPerM_Z = 428.4, PxPerM_X = 428.9, X0px = 1031.5;
    public const int AxisGuess = 386;

    static int Pink(Color c) { return Math.Min(c.R - c.G, c.B - c.G); }
    static bool Dark(Color c) { return c.R < 130 && c.G < 130 && c.B < 130; }
    static bool Whitish(Color c) { int mn = Math.Min(c.R, Math.Min(c.G, c.B)), mx = Math.Max(c.R, Math.Max(c.G, c.B)); return mn > 195 && mx - mn < 45 && c.G <= c.R + 8; }

    public class Col { public int X; public double Top = double.NaN, Bot = double.NaN; public int Core; }

    // Walk from (x, y0) in direction dy until the pinkness drops below thr. Returns the sub-pixel
    // edge, or NaN when the walk meets a line or a non-plasma colour (the edge is hidden).
    // In the mirror throats the pink fills the bore right up to the vessel wall; there the wall is the edge.
    public static bool ThroatZone(int x) { return (x >= 540 && x <= 672) || (x >= 1390 && x <= 1525); }
    static int Lum(Color c) { return (c.R + c.G + c.B) / 3; }

    static double Walk(Bitmap b, int x, int y0, int dy, double thr, int maxSteps)
    {
        int prevP = Pink(b.GetPixel(x, y0));
        for (int k = 1; k < maxSteps; k++)
        {
            int y = y0 + dy * k; if (y < 0 || y >= b.Height) return double.NaN;
            var c = b.GetPixel(x, y);
            if (ThroatZone(x) && prevP >= thr && Lum(c) < 190) return y - 0.5 * dy;   // pink meets the wall
            if (Dark(c)) return double.NaN;
            int p = Pink(c);
            if (p < thr)
            {
                // the pixel just outside must look like background (white / pale pink), otherwise hidden
                for (int j = 0; j <= 3; j++)
                {
                    int yy = y + dy * j; if (yy < 0 || yy >= b.Height) break;
                    var cc = b.GetPixel(x, yy);
                    bool gradient = Math.Abs(cc.R - cc.B) < 30 && cc.R > 200 && cc.G <= cc.R;   // magenta fading to white
                    if (Dark(cc) || !(Whitish(cc) || gradient)) return double.NaN;
                }
                double f = (prevP - thr) / Math.Max(1.0, (double)(prevP - p));
                return (y - dy) + dy * f;
            }
            prevP = p;
        }
        return double.NaN;
    }

    static double WalkX(Bitmap b, int x0, int y, int dx, double thr, int maxSteps)
    {
        int prevP = Pink(b.GetPixel(x0, y));
        for (int k = 1; k < maxSteps; k++)
        {
            int x = x0 + dx * k; if (x < 0 || x >= b.Width) return double.NaN;
            var c = b.GetPixel(x, y);
            int p = Pink(c);
            if (p < thr || Dark(c))
            {
                // thin contour lines (the blue expansion-ratio arc) cross the fan: step over them if pink resumes
                bool resumes = false;
                for (int j = 1; j <= 12; j++)
                {
                    int xx = x + dx * j; if (xx < 0 || xx >= b.Width) break;
                    var cc = b.GetPixel(xx, y);
                    if (!Dark(cc) && Pink(cc) >= thr && Math.Abs(cc.R - cc.B) < 30) { resumes = true; x = xx; k += j; break; }
                }
                if (resumes) { prevP = Pink(b.GetPixel(x, y)); continue; }
                double f = (prevP - thr) / Math.Max(1.0, (double)(prevP - p));
                return (x - dx) + dx * Math.Max(0, Math.Min(1, f));
            }
            prevP = p;
        }
        return double.NaN;
    }

    public static List<Col> Columns(Bitmap b, int x0, int x1)
    {
        var cols = new List<Col>();
        for (int x = x0; x <= x1; x++)
        {
            var col = new Col { X = x };
            int core = -999;
            for (int y = AxisGuess - 4; y <= AxisGuess + 4; y++) { var c = b.GetPixel(x, y); if (!Dark(c)) core = Math.Max(core, Pink(c)); }
            col.Core = core;
            if (core >= 20)
            {
                double thr = Math.Max(12, 0.5 * core);
                col.Top = Walk(b, x, AxisGuess, -1, thr, 160);
                col.Bot = Walk(b, x, AxisGuess, +1, thr, 160);
            }
            cols.Add(col);
        }
        return cols;
    }

    // Outer end of each expander fan: for every row, the last pink pixel walking outward along Z.
    public static List<double[]> EndCap(Bitmap b, int xStart, int dx, int y0, int y1)
    {
        var pts = new List<double[]>();
        for (int y = y0; y <= y1; y++)
        {
            var c = b.GetPixel(xStart, y); int p = Pink(c);
            if (p < 20) continue;
            double e = WalkX(b, xStart, y, dx, Math.Max(12, 0.5 * p), 260);
            if (!double.IsNaN(e)) pts.Add(new[] { e, (double)y });
        }
        return pts;
    }

    static bool RedWall(Color c) { return c.R > 140 && c.R - c.G > 60 && c.R - c.B > 45; }

    // Inner face of the red hatched end wall: first red pixel met walking outward along Z, per row.
    public static List<double[]> EndWall(Bitmap b, int xStart, int dx, int y0, int y1)
    {
        var pts = new List<double[]>();
        for (int y = y0; y <= y1; y++)
            for (int k = 0; k < 140; k++)
            {
                int x = xStart + dx * k;
                if (RedWall(b.GetPixel(x, y))) { pts.Add(new[] { x - 0.5 * dx, (double)y }); break; }
            }
        return pts;
    }

    static double Median(List<double> v) { if (v.Count == 0) return double.NaN; var a = v.ToArray(); Array.Sort(a); int n = a.Length; return n % 2 == 1 ? a[n / 2] : 0.5 * (a[n / 2 - 1] + a[n / 2]); }

    // Folded radius profile: |Z| in px from the centre (x = X0px) -> radius in px from the axis row.
    // Both halves (Z < 0 and Z > 0) and both edges (top and bottom) are pooled, then median filtered.
    public class Prof { public double[] U; public double[] R; public int[] N; public double AxisY; public List<double[]> Cap; public double CapRim; }

    public static Prof Fold(string png)
    {
        var b = new Bitmap(png);
        var cols = Columns(b, 100, 1950);
        // axis row: median centre of columns with both edges
        var ctr = new List<double>();
        foreach (var c in cols) if (!double.IsNaN(c.Top) && !double.IsNaN(c.Bot)) ctr.Add(0.5 * (c.Top + c.Bot));
        double ay = Median(ctr);
        int umax = 900;
        var bins = new List<double>[umax + 1];
        for (int i = 0; i <= umax; i++) bins[i] = new List<double>();
        foreach (var c in cols)
        {
            int u = (int)Math.Round(Math.Abs(c.X - X0px)); if (u > umax) continue;
            if (!double.IsNaN(c.Top)) bins[u].Add(ay - c.Top);
            if (!double.IsNaN(c.Bot)) bins[u].Add(c.Bot - ay);
        }
        // pool +-2 px in u, take the median, reject points > 2.5 px from a +-15 px running median
        var R = new double[umax + 1]; var N = new int[umax + 1];
        for (int u = 0; u <= umax; u++)
        {
            var v = new List<double>();
            for (int k = Math.Max(0, u - 2); k <= Math.Min(umax, u + 2); k++) v.AddRange(bins[k]);
            R[u] = Median(v); N[u] = v.Count;
        }
        var R2 = new double[umax + 1];
        for (int u = 0; u <= umax; u++)
        {
            var v = new List<double>();
            for (int k = Math.Max(0, u - 15); k <= Math.Min(umax, u + 15); k++) if (!double.IsNaN(R[k])) v.Add(R[k]);
            double m = Median(v);
            R2[u] = (!double.IsNaN(R[u]) && Math.Abs(R[u] - m) <= 2.5) ? R[u] : double.NaN;
        }
        var U = new double[umax + 1]; for (int u = 0; u <= umax; u++) U[u] = u;
        // end caps, folded: (|Z| px, radius px)
        var cap = new List<double[]>();
        foreach (var p in EndWall(b, 230, -1, 270, 500)) cap.Add(new[] { Math.Abs(p[0] - X0px), Math.Abs(p[1] - ay) });
        foreach (var p in EndWall(b, 1833, +1, 270, 500)) cap.Add(new[] { Math.Abs(p[0] - X0px), Math.Abs(p[1] - ay) });
        b.Dispose();
        return new Prof { U = U, R = R2, N = N, AxisY = ay, Cap = cap };
    }

    public static void Overlay(string png, Prof p, List<double[]> fitUR, string outPng)
    {
        var b = new Bitmap(png);
        using (var g = Graphics.FromImage(b))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var green = new SolidBrush(Color.FromArgb(220, 0, 170, 0));
            for (int u = 0; u < p.U.Length; u += 3) if (!double.IsNaN(p.R[u]))
                foreach (int s in new[] { -1, 1 }) foreach (int t in new[] { -1, 1 })
                    g.FillEllipse(green, (float)(X0px + s * u - 1.5), (float)(p.AxisY + t * p.R[u] - 1.5), 3, 3);
            var orange = new SolidBrush(Color.FromArgb(220, 255, 140, 0));
            foreach (var c in p.Cap) foreach (int s in new[] { -1, 1 }) foreach (int t in new[] { -1, 1 })
                g.FillEllipse(orange, (float)(X0px + s * c[0] - 1.5), (float)(p.AxisY + t * c[1] - 1.5), 3, 3);
            if (fitUR != null)
            {
                var pen = new Pen(Color.Black, 1.2f);
                foreach (int s in new[] { -1, 1 }) foreach (int t in new[] { -1, 1 })
                {
                    var pts = new PointF[fitUR.Count];
                    for (int i = 0; i < fitUR.Count; i++) pts[i] = new PointF((float)(X0px + s * fitUR[i][0]), (float)(p.AxisY + t * fitUR[i][1]));
                    g.DrawLines(pen, pts);
                }
            }
        }
        b.Save(outPng, ImageFormat.Png); b.Dispose();
    }

    // Local quadratic regression of the folded radii at u (window +-hw px). NaN if too few points.
    static double LocalQuad(Prof p, double u, int hw, int uLo, int uHi)
    {
        double s0 = 0, s1 = 0, s2 = 0, s3 = 0, s4 = 0, t0 = 0, t1 = 0, t2 = 0; int n = 0;
        for (int k = Math.Max(uLo, (int)u - hw); k <= Math.Min(uHi, (int)u + hw); k++)
        {
            if (double.IsNaN(p.R[k])) continue;
            double x = k - u, y = p.R[k]; n++;
            s0 += 1; s1 += x; s2 += x * x; s3 += x * x * x; s4 += x * x * x * x; t0 += y; t1 += x * y; t2 += x * x * y;
        }
        if (n < 6) return double.NaN;
        // solve [s0 s1 s2; s1 s2 s3; s2 s3 s4] c = [t0 t1 t2], return c0
        double[,] A = { { s0, s1, s2, t0 }, { s1, s2, s3, t1 }, { s2, s3, s4, t2 } };
        for (int i = 0; i < 3; i++) for (int j = i + 1; j < 3; j++) { double f = A[j, i] / A[i, i]; for (int k = i; k < 4; k++) A[j, k] -= f * A[i, k]; }
        double c2 = A[2, 3] / A[2, 2], c1 = (A[1, 3] - A[1, 2] * c2) / A[1, 1];
        return (A[0, 3] - A[0, 1] * c1 - A[0, 2] * c2) / A[0, 0];
    }

    // Half profile (u >= 0, r >= 0) in pixels, as named pieces. Pieces join with sharp corners.
    public class Shape
    {
        public List<double[]> Core = new List<double[]>();   // spline, u = 0 .. U1 (ends on the throat wall)
        public double U1, U2, Rw;                             // throat: straight, r = Rw from U1 to U2
        public List<double[]> Fan = new List<double[]>();    // spline, U2 .. rim
        public double[] Rim, Knee, Apex;                      // end wall: rim -> knee (cone), knee -> apex (flat disc)
        public double EndAsym;                                // left/right end-wall offset (px)
    }

    public static Shape Fit(string png, out Prof prof)
    {
        var p = Fold(png); prof = p;
        var s = new Shape();
        // throat: wall-limited radius
        var tw = new List<double>(); for (int u = 372; u <= 462; u++) if (!double.IsNaN(p.R[u])) tw.Add(p.R[u]);
        s.Rw = Median(tw); s.U1 = 368; s.U2 = 468;
        // core: smoothed, symmetric about u = 0 (mirror the data for the fit near the centre)
        for (int u = 0; u <= 360; u += 10)
        {
            double r = LocalQuad(p, Math.Max(u, 20), 20, 0, 362);
            s.Core.Add(new[] { (double)u, r });
        }
        s.Core[0][1] = s.Core[1][1] = 0.5 * (s.Core[0][1] + s.Core[1][1]) + 0.0;   // flat top at the midplane
        s.Core.Add(new[] { s.U1, s.Rw });
        // end wall (left/right averaged): flat disc u = uFlat for r <= rKnee, then a cone out to the rim
        var b = new Bitmap(png);
        var L = EndWall(b, 230, -1, 270, 500); var Rr = EndWall(b, 1833, +1, 270, 500); b.Dispose();
        var uL = new Dictionary<int, double>(); foreach (var q in L) uL[(int)Math.Round(Math.Abs(q[1] - p.AxisY))] = X0px - q[0];
        var uR = new Dictionary<int, double>(); foreach (var q in Rr) uR[(int)Math.Round(Math.Abs(q[1] - p.AxisY))] = q[0] - X0px;
        var flat = new List<double>(); var asym = new List<double>();
        // cone: least squares u = a + c r over r in 42..96 (both sides, both edges pooled)
        double n = 0, sr = 0, su = 0, srr = 0, sru = 0;
        foreach (var d in new[] { uL, uR }) foreach (var kv in d)
        {
            if (kv.Key <= 30) flat.Add(kv.Value);
            if (kv.Key >= 42 && kv.Key <= 96) { n++; sr += kv.Key; su += kv.Value; srr += kv.Key * kv.Key; sru += kv.Key * kv.Value; }
        }
        foreach (var kv in uL) if (kv.Key <= 30 && uR.ContainsKey(kv.Key)) asym.Add(kv.Value - uR[kv.Key]);
        double c = (n * sru - sr * su) / (n * srr - sr * sr), a = (su - c * sr) / n;
        double uFlat = Median(flat); s.EndAsym = Median(asym);
        double rKnee = (uFlat - a) / c;
        s.Knee = new[] { uFlat, rKnee }; s.Apex = new[] { uFlat, 0.0 };
        // fan: smoothed data from U2 to ~835, then extended along its end slope to the cone
        s.Fan.Add(new[] { s.U2, s.Rw });
        for (int u = 476; u <= 836; u += 10) s.Fan.Add(new[] { (double)u, LocalQuad(p, u, u < 500 ? 8 : 18, 470, 845) });
        for (int i = 1; i < s.Core.Count - 1; i++) s.Core[i][1] = Math.Min(s.Core[i][1], s.Core[i - 1][1]);   // flux tube narrows monotonically towards the throat
        for (int i = 1; i < s.Fan.Count; i++) s.Fan[i][1] = Math.Max(s.Fan[i][1], s.Fan[i - 1][1]);           // and widens monotonically in the expander
        var f1 = s.Fan[s.Fan.Count - 1]; var f0 = s.Fan[s.Fan.Count - 6];
        double slope = (f1[1] - f0[1]) / (f1[0] - f0[0]);
        // intersect r = f1r + slope (u - f1u) with u = a + c r
        double rr = (f1[1] + slope * (a - f1[0])) / (1 - slope * c);
        s.Rim = new[] { a + c * rr, rr };
        return s;
    }

    // px -> mm at 1:100 (1 m real = 10 mm)
    public static double[] Mm(double[] q) { return new[] { q[0] / PxPerM_Z * 10, q[1] / PxPerM_X * 10 }; }

    // Closed outline in mm, (z along the mirror axis, r) from the left apex over the top to the right apex
    // and back underneath. Spline pieces are densified with Catmull-Rom so the CSV/SVG follow the CAD curve.
    public static List<double[]> ClosedMm(Shape s)
    {
        var half = new List<double[]>();   // u >= 0, from the midplane to the apex
        Action<List<double[]>> dens = pts =>
        {
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var p0 = pts[Math.Max(0, i - 1)]; var p1 = pts[i]; var p2 = pts[i + 1]; var p3 = pts[Math.Min(pts.Count - 1, i + 2)];
                for (int k = 0; k < 4; k++)
                {
                    double t = k / 4.0, t2 = t * t, t3 = t2 * t;
                    half.Add(new[] {
                        0.5 * (2 * p1[0] + (-p0[0] + p2[0]) * t + (2 * p0[0] - 5 * p1[0] + 4 * p2[0] - p3[0]) * t2 + (-p0[0] + 3 * p1[0] - 3 * p2[0] + p3[0]) * t3),
                        0.5 * (2 * p1[1] + (-p0[1] + p2[1]) * t + (2 * p0[1] - 5 * p1[1] + 4 * p2[1] - p3[1]) * t2 + (-p0[1] + 3 * p1[1] - 3 * p2[1] + p3[1]) * t3) });
                }
            }
        };
        dens(s.Core);
        var fan = new List<double[]>(s.Fan); fan.Add(s.Rim); dens(fan);
        half.Add(s.Rim); half.Add(s.Knee); half.Add(s.Apex);
        var mm = new List<double[]>(); foreach (var q in half) mm.Add(Mm(q));
        var o = new List<double[]>();
        for (int i = mm.Count - 1; i >= 1; i--) o.Add(new[] { -mm[i][0], mm[i][1] });    // left apex .. midplane (top)
        for (int i = 0; i < mm.Count; i++) o.Add(new[] { mm[i][0], mm[i][1] });          // midplane .. right apex (top)
        for (int i = mm.Count - 2; i >= 0; i--) o.Add(new[] { mm[i][0], -mm[i][1] });    // right .. midplane (bottom)
        for (int i = 1; i < mm.Count - 1; i++) o.Add(new[] { -mm[i][0], -mm[i][1] });    // midplane .. left (bottom)
        return o;
    }

    // Volume (mm^3) of the solid of revolution of the half profile, pi * integral r^2 dz.
    public static double VolumeMm3(List<double[]> closed)
    {
        double v = 0;
        for (int i = 0; i < closed.Count - 1; i++)
        {
            var a = closed[i]; var b = closed[i + 1];
            if (a[1] < 0 || b[1] < 0) continue;      // top half only
            v += Math.PI * (b[0] - a[0]) * (a[1] * a[1] + a[1] * b[1] + b[1] * b[1]) / 3;
        }
        return Math.Abs(v);
    }

    // Adds WHAM to comparison_cross_sections.svg (written by Build.Run) at the same 100 px/m scale, lying with its
    // mirror axis horizontal in the free lower-left area, centred at R = 2.3 m, Z = -3.0 m. Idempotent.
    public static void AddToSvg(string svgPath, List<double[]> closedMm)
    {
        string t = File.ReadAllText(svgPath);
        int a = t.IndexOf("<g id='WHAM'>"); if (a >= 0) { int e = t.IndexOf("</g>", a) + 4; t = t.Remove(a, e - a); }
        double ppm = 100, ox = 50, oy = (9.0 * ppm + 60) / 2, cx = ox + 2.3 * ppm, cy = oy + 3.0 * ppm;
        var sb = new StringBuilder("<g id='WHAM'>");
        var path = new StringBuilder();
        for (int i = 0; i < closedMm.Count; i++)
            path.Append(string.Format(CultureInfo.InvariantCulture, "{0}{1:0.0},{2:0.0} ", i == 0 ? "M" : "L", cx + closedMm[i][0] / 10 * ppm, cy - closedMm[i][1] / 10 * ppm));
        sb.Append(string.Format(CultureInfo.InvariantCulture, "<line x1='{0:0.0}' y1='{2:0.0}' x2='{1:0.0}' y2='{2:0.0}' stroke='#17becf' stroke-dasharray='4 3'/>", cx - 2.2 * ppm, cx + 2.2 * ppm, cy));
        sb.Append(string.Format("<path d='{0}Z' fill='#17becf' fill-opacity='0.25' stroke='#17becf' stroke-width='2'/>", path));
        sb.Append(string.Format(CultureInfo.InvariantCulture, "<text x='{0:0.0}' y='{1:0.0}' font-size='13' font-weight='600' text-anchor='middle' fill='#17becf'>WHAM</text>", cx, cy - 32));
        sb.Append(string.Format(CultureInfo.InvariantCulture, "<text x='{0:0.0}' y='{1:0.0}' font-size='11' text-anchor='middle' fill='#666'>mirror, same scale, drawn lying down (dashed = its axis)</text>", cx, cy + 40));
        sb.Append("</g>\n");
        t = t.Replace("</svg>", sb + "</svg>");
        File.WriteAllText(svgPath, t);
    }

    public static List<double[]> Outline(Shape s, double step)
    {
        // dense half outline (u, r) px from the midplane to the apex, for CSV/SVG/overlay
        var o = new List<double[]>();
        foreach (var q in s.Core) o.Add(q);
        o.Add(new[] { s.U2, s.Rw });
        for (int i = 1; i < s.Fan.Count; i++) o.Add(s.Fan[i]);
        o.Add(s.Rim); o.Add(s.Knee); o.Add(s.Apex);
        return o;
    }

    public static string Dump(string png, string outCsv)
    {
        var b = new Bitmap(png);
        var cols = Columns(b, 100, 1950);
        var sb = new StringBuilder("x,core,top,bot\n");
        foreach (var c in cols) sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:F2},{3:F2}", c.X, c.Core, c.Top, c.Bot));
        File.WriteAllText(outCsv, sb.ToString());
        var L = EndCap(b, 300, -1, 250, 520); var R = EndCap(b, 1760, +1, 250, 520);
        var s2 = new StringBuilder("side,x,y\n");
        foreach (var p in L) s2.AppendLine(string.Format(CultureInfo.InvariantCulture, "L,{0:F2},{1}", p[0], p[1]));
        foreach (var p in R) s2.AppendLine(string.Format(CultureInfo.InvariantCulture, "R,{0:F2},{1}", p[0], p[1]));
        File.WriteAllText(outCsv.Replace(".csv", "_ends.csv"), s2.ToString());
        b.Dispose();
        int nt = 0, nb = 0; foreach (var c in cols) { if (!double.IsNaN(c.Top)) nt++; if (!double.IsNaN(c.Bot)) nb++; }
        return "cols=" + cols.Count + " top ok=" + nt + " bot ok=" + nb + " endL=" + L.Count + " endR=" + R.Count;
    }
}
