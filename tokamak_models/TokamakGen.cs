// Generates 1:100 plasma (LCFS) models of several tokamaks.
// Shape model: Miller-type boundary, with an optional X-point (90 deg corner) on the
// upper and/or lower half.  Units in: metres.  Units out: millimetres at 1:100 (1 m -> 10 mm).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public class Machine
{
    public string Name, Note;
    public double R0, A, Kappa, DeltaU, DeltaL;
    public bool XUpper, XLower;
    public Machine(string name, double r0, double a, double k, double du, double dl, bool xu, bool xl, string note)
    { Name = name; R0 = r0; A = a; Kappa = k; DeltaU = du; DeltaL = dl; XUpper = xu; XLower = xl; Note = note; }
}

public static class TokamakGen
{
    const double Scale = 10.0; // mm per metre at 1:100
    // Conic weight for the X-point quadrants (0.707 = elliptical quarter, 1 = parabola).
    // Fitted to the digitised SPARC V2 separatrix (Creely et al. 2020, Fig. 2): mean deviation 5 mm (real scale).
    public static double W = 0.9;        // inboard X-point quadrant
    public static double Wo = 1.05;      // outboard X-point quadrant
    public static double TiltGain = 1.25; // scales the tilt of the X-point corner bisector

    public static string Volumes(double w)
    {
        W = w;
        var ms = new[] {
            new Machine("ITER", 6.20, 2.00, 1.85, 0.49, 0.49, false, true, "target 830 m3 / 21.9 m2"),
            new Machine("ARC",  4.62, 1.18, 1.80, 0.65, 0.65, true, true, "target 189 m3"),
            new Machine("SPARC",1.85, 0.57, 1.97, 0.54, 0.54, true, true, "target ~20 m3"),
            new Machine("WEST", 2.50, 0.50, 1.35, 0.50, 0.50, false, true, "target ~15 m3"),
        };
        var sb = new StringBuilder("w=" + w + ": ");
        foreach (var x in ms)
        {
            double cr, cz, area, vol; Centroid(Boundary(x, 720), out cr, out cz, out area, out vol);
            sb.Append(string.Format(CultureInfo.InvariantCulture, "{0} A={1:0.00} V={2:0.0} ({3}); ", x.Name, area, vol, x.Note));
        }
        return sb.ToString();
    }

    // Rational quadratic Bezier (a conic arc): always convex, tangent to P0-P1 at P0 and P1-P2 at P2.
    static double[] Conic(double[] p0, double[] p1, double[] p2, double w, double t)
    {
        double b0 = (1 - t) * (1 - t), b1 = 2 * w * t * (1 - t), b2 = t * t, s = b0 + b1 + b2;
        return new[] { (b0 * p0[0] + b1 * p1[0] + b2 * p2[0]) / s, (b0 * p0[1] + b1 * p1[1] + b2 * p2[1]) / s };
    }

    // One half of the boundary in "upper" coordinates (Z >= 0), ordered outer midplane -> inner midplane,
    // both endpoints included.
    static List<double[]> Half(Machine m, double d, bool xp, int h)
    {
        var pts = new List<double[]>();
        double R0 = m.R0, a = m.A, k = m.Kappa;
        if (!xp)
        {
            // Miller form: smooth top with horizontal tangent at (R0 - d a, k a)
            for (int i = 0; i <= h; i++)
            {
                double th = Math.PI * i / h;
                pts.Add(new[] { R0 + a * Math.Cos(th + Math.Asin(d) * Math.Sin(th)), k * a * Math.Sin(th) });
            }
            return pts;
        }
        // X-point at (R0 - d a, k a).  The two separatrix branches meet at 90 deg; the bisector of that
        // corner points from the X-point towards the geometric centre (R0, 0), tilted atan(d/k) from vertical.
        // => outboard flank leaves the X-point at (45 deg - tilt) below horizontal,
        //    inboard flank at (45 deg + tilt) below horizontal.
        double tilt = TiltGain * Math.Atan2(d, k);
        double ao = Math.PI / 4 - tilt, ai = Math.PI / 4 + tilt;
        // keep each control point at least 30% of the half-height above the midplane, so a flank
        // never degenerates into a straight line (matters for low-elongation, high-triangularity shapes)
        ao = Math.Min(ao, Math.Atan(0.7 * k / (1 + d)));
        ai = Math.Min(ai, Math.Atan(0.7 * k / Math.Max(1e-6, 1 - d)));
        var X = new[] { R0 - d * a, k * a };
        var Po = new[] { R0 + a, 0.0 };
        var Pi = new[] { R0 - a, 0.0 };
        // control points: intersection of the vertical midplane tangent with the X-point flank line
        var Co = new[] { R0 + a, k * a - (1 + d) * a * Math.Tan(ao) };
        var Ci = new[] { R0 - a, k * a - (1 - d) * a * Math.Tan(ai) };
        int ho = h / 2, hi = h - ho;
        for (int i = 0; i < ho; i++) pts.Add(Conic(Po, Co, X, Wo, (double)i / ho));
        for (int i = 0; i <= hi; i++) pts.Add(Conic(X, Ci, Pi, W, (double)i / hi));
        return pts;
    }

    // Boundary, counter-clockwise in (R,Z), metres, starting at the outer midplane.
    public static List<double[]> Boundary(Machine m, int n)
    {
        var up = Half(m, m.DeltaU, m.XUpper, n / 2);
        var lo = Half(m, m.DeltaL, m.XLower, n / 2);
        var pts = new List<double[]>();
        for (int i = 0; i < up.Count - 1; i++) pts.Add(up[i]);                 // outer -> top -> inner
        for (int i = lo.Count - 1; i > 0; i--) pts.Add(new[] { lo[i][0], -lo[i][1] }); // inner -> bottom -> outer
        return pts;
    }

    // Number of vertices where the boundary turns the wrong way (should be 0 for a convex shape).
    public static int ConcaveCount(List<double[]> p)
    {
        int n = p.Count, c = 0;
        for (int i = 0; i < n; i++)
        {
            var a = p[(i + n - 1) % n]; var b = p[i]; var q = p[(i + 1) % n];
            double cr = (b[0] - a[0]) * (q[1] - b[1]) - (b[1] - a[1]) * (q[0] - b[0]);
            double l = Math.Sqrt(((b[0] - a[0]) * (b[0] - a[0]) + (b[1] - a[1]) * (b[1] - a[1])) * ((q[0] - b[0]) * (q[0] - b[0]) + (q[1] - b[1]) * (q[1] - b[1])));
            if (cr < -1e-9 * l) c++;
        }
        return c;
    }

    // Inboard and outboard R of the boundary at height z (z >= 0 assumed on the upper half).
    public static string Probe(Machine m, double z)
    {
        var b = Boundary(m, 2000); double rin = double.NaN, rout = double.NaN;
        for (int i = 0; i < b.Count; i++)
        {
            var a = b[i]; var c = b[(i + 1) % b.Count];
            if ((a[1] - z) * (c[1] - z) <= 0 && a[1] != c[1])
            {
                double r = a[0] + (z - a[1]) / (c[1] - a[1]) * (c[0] - a[0]);
                if (a[0] > m.R0 - m.DeltaU * m.A) rout = r; else rin = r;
            }
        }
        return string.Format(CultureInfo.InvariantCulture, "Z={0:0.00}: R_in={1:0.000} R_out={2:0.000}", z, rin, rout);
    }

    public static void Centroid(List<double[]> p, out double cr, out double cz, out double area, out double vol)
    {
        double A2 = 0, cx = 0, cy = 0; int n = p.Count;
        for (int i = 0; i < n; i++)
        {
            var a = p[i]; var b = p[(i + 1) % n];
            double cross = a[0] * b[1] - b[0] * a[1];
            A2 += cross; cx += (a[0] + b[0]) * cross; cy += (a[1] + b[1]) * cross;
        }
        area = A2 / 2; cr = cx / (3 * A2); cz = cy / (3 * A2);
        vol = 2 * Math.PI * cr * area; // Pappus
    }

    public class Stl
    {
        public List<float[]> Tris = new List<float[]>();
        public void Add(double[] a, double[] b, double[] c)
        {
            Tris.Add(new float[] { (float)a[0], (float)a[1], (float)a[2], (float)b[0], (float)b[1], (float)b[2], (float)c[0], (float)c[1], (float)c[2] });
        }
        public double SignedVolume()
        {
            double v = 0;
            foreach (var t in Tris)
                v += (t[0] * (t[4] * t[8] - t[5] * t[7]) - t[1] * (t[3] * t[8] - t[5] * t[6]) + t[2] * (t[3] * t[7] - t[4] * t[6])) / 6.0;
            return v;
        }
        public void Write(string path, string header)
        {
            using (var w = new BinaryWriter(File.Create(path)))
            {
                var h = new byte[80]; var hb = Encoding.ASCII.GetBytes(header);
                Array.Copy(hb, h, Math.Min(80, hb.Length)); w.Write(h);
                w.Write((uint)Tris.Count);
                foreach (var t in Tris)
                {
                    double ux = t[3] - t[0], uy = t[4] - t[1], uz = t[5] - t[2];
                    double vx = t[6] - t[0], vy = t[7] - t[1], vz = t[8] - t[2];
                    double nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
                    double l = Math.Sqrt(nx * nx + ny * ny + nz * nz); if (l == 0) l = 1;
                    w.Write((float)(nx / l)); w.Write((float)(ny / l)); w.Write((float)(nz / l));
                    for (int k = 0; k < 9; k++) w.Write(t[k]);
                    w.Write((ushort)0);
                }
            }
        }
    }

    // 270-degree cutaway torus, sitting with its lowest point at z = 0.
    public static Stl Cutaway(List<double[]> b, double cr, double cz, double sweepDeg, double stepDeg)
    {
        var s = new Stl(); int n = b.Count;
        double zmin = double.MaxValue; foreach (var p in b) zmin = Math.Min(zmin, p[1]);
        int nt = (int)Math.Round(sweepDeg / stepDeg);
        Func<double, double, double, double[]> P = (R, Z, phi) =>
            new[] { R * Scale * Math.Cos(phi), R * Scale * Math.Sin(phi), (Z - zmin) * Scale };
        for (int j = 0; j < nt; j++)
        {
            double p0 = sweepDeg * Math.PI / 180 * j / nt, p1 = sweepDeg * Math.PI / 180 * (j + 1) / nt;
            for (int i = 0; i < n; i++)
            {
                var a = b[i]; var c = b[(i + 1) % n];
                var A = P(a[0], a[1], p0); var B = P(c[0], c[1], p0);
                var C = P(c[0], c[1], p1); var D = P(a[0], a[1], p1);
                s.Add(A, D, B); s.Add(B, D, C);
            }
        }
        double pe = sweepDeg * Math.PI / 180;
        var c0 = P(cr, cz, 0); var c1 = P(cr, cz, pe);
        for (int i = 0; i < n; i++)
        {
            var a = b[i]; var c = b[(i + 1) % n];
            s.Add(c0, P(a[0], a[1], 0), P(c[0], c[1], 0));
            s.Add(c1, P(c[0], c[1], pe), P(a[0], a[1], pe));
        }
        return s;
    }

    // Flat cross-section plate (extruded profile), lying on the bed. x = R, y = Z (mm).
    public static Stl Plate(List<double[]> b, double cr, double cz, double thick)
    {
        var s = new Stl(); int n = b.Count;
        Func<double[], double, double[]> P = (p, z) => new[] { p[0] * Scale, p[1] * Scale, z };
        var cb = new[] { cr * Scale, cz * Scale, 0.0 }; var ct = new[] { cr * Scale, cz * Scale, thick };
        for (int i = 0; i < n; i++)
        {
            var a = b[i]; var c = b[(i + 1) % n];
            s.Add(ct, P(a, thick), P(c, thick));
            s.Add(cb, P(c, 0), P(a, 0));
            var A = P(a, 0); var B = P(c, 0); var C = P(c, thick); var D = P(a, thick);
            s.Add(A, B, C); s.Add(A, C, D);
        }
        return s;
    }

    public static string Run(string outDir)
    {
        var inv = CultureInfo.InvariantCulture;
        var machines = new[] {
            new Machine("WEST",   2.50, 0.50, 1.35, 0.50, 0.50, false, true,  "LSN"),
            new Machine("EAST",   1.85, 0.45, 1.80, 0.60, 0.60, false, true,  "LSN"),
            new Machine("DIII-D", 1.67, 0.60, 1.81, 0.35, 0.69, false, true,  "LSN, ITER-similar shape"),
            new Machine("SPARC",  1.85, 0.57, 1.97, 0.54, 0.54, true,  true,  "DN"),
            new Machine("ITER",   6.20, 2.00, 1.85, 0.49, 0.49, false, true,  "LSN"),
            new Machine("ARC",    4.62, 1.18, 1.80, 0.65, 0.65, true,  true,  "DN (V3A)"),
            new Machine("NSTX-U", 0.93, 0.57, 2.50, 0.60, 0.60, true,  true,  "DN"),
        };
        string[] colors = { "#1f77b4", "#d62728", "#2ca02c", "#ff7f0e", "#9467bd", "#8c564b", "#e377c2" };
        Directory.CreateDirectory(outDir);
        var report = new StringBuilder();
        report.AppendLine("Machine | R0 | a | kappa | dU | dL | config | area m2 | volume m3 | print WxDxH mm (cutaway) | triangles | mesh vol check");
        var csvAll = new StringBuilder("machine,R_m,Z_m,R_mm_1to100,Z_mm_1to100\n");
        var svg = new StringBuilder();
        double ppm = 100; // px per metre
        double W = 9.2 * ppm + 80, H = 9.0 * ppm + 60, ox = 50, oy = H / 2;
        svg.AppendLine(string.Format(inv, "<svg xmlns='http://www.w3.org/2000/svg' width='{0}' height='{1}' viewBox='0 0 {0} {1}' font-family='Segoe UI,Arial,sans-serif'>", W, H));
        svg.AppendLine("<rect width='100%' height='100%' fill='white'/>");
        for (int r = 0; r <= 9; r++)
        {
            double x = ox + r * ppm;
            svg.AppendLine(string.Format(inv, "<line x1='{0}' y1='20' x2='{0}' y2='{1}' stroke='#eee'/><text x='{0}' y='{2}' font-size='12' text-anchor='middle' fill='#666'>{3} m</text>", x, H - 30, H - 14, r));
        }
        for (int z = -4; z <= 4; z++)
        {
            double y = oy - z * ppm;
            svg.AppendLine(string.Format(inv, "<line x1='{0}' y1='{1}' x2='{2}' y2='{1}' stroke='#eee'/><text x='{3}' y='{4}' font-size='12' text-anchor='end' fill='#666'>{5}</text>", ox, y, W - 10, ox - 6, y + 4, z));
        }
        svg.AppendLine(string.Format(inv, "<text x='{0}' y='{1}' font-size='12' fill='#666' transform='rotate(-90 {0} {1})' text-anchor='middle'>Z (m)</text>", 14, oy));
        svg.AppendLine(string.Format(inv, "<line x1='{0}' y1='{1}' x2='{2}' y2='{1}' stroke='#ccc' stroke-dasharray='4 4'/>", ox, oy, W - 10));
        svg.AppendLine(string.Format(inv, "<line x1='{0}' y1='20' x2='{0}' y2='{1}' stroke='#333' stroke-width='2'/><text x='{2}' y='34' font-size='12' fill='#333'>machine axis (R = 0)</text>", ox, H - 30, ox + 6));

        for (int k = 0; k < machines.Length; k++)
        {
            var m = machines[k];
            var b = Boundary(m, 480);
            double cr, cz, area, vol; Centroid(b, out cr, out cz, out area, out vol);
            double zmin = double.MaxValue, zmax = double.MinValue, rmax = 0;
            foreach (var p in b) { zmin = Math.Min(zmin, p[1]); zmax = Math.Max(zmax, p[1]); rmax = Math.Max(rmax, p[0]); }
            string tag = m.Name.Replace("-", "");
            var cut = Cutaway(b, cr, cz, 270, 1.5);
            cut.Write(Path.Combine(outDir, tag + "_cutaway270_1to100.stl"), m.Name + " LCFS 1:100 cutaway (mm)");
            var plate = Plate(b, cr, cz, 3.0);
            plate.Write(Path.Combine(outDir, tag + "_profile_plate_1to100.stl"), m.Name + " LCFS 1:100 profile plate (mm)");
            double meshVol = cut.SignedVolume();                // mm^3 (positive => outward normals)
            double analytic = vol * 0.75 * Math.Pow(Scale, 3);  // 1 m^3 real = 1000 mm^3 at 1:100; 270/360 of torus
            report.AppendLine(string.Format(inv, "{0} | {1:0.00} | {2:0.00} | {3:0.00} | {4:0.00} | {5:0.00} | {6} | {7:0.00} | {8:0.0} | {9:0}x{9:0}x{10:0.0} | {11} | {12:0.0}/{13:0.0} mm3 | concave vertices: {14}",
                m.Name, m.R0, m.A, m.Kappa, m.DeltaU, m.DeltaL, m.Note, area, vol, 2 * rmax * Scale, (zmax - zmin) * Scale, cut.Tris.Count, meshVol, analytic, ConcaveCount(b)));
            var csv = new StringBuilder("R_m,Z_m,R_mm_1to100,Z_mm_1to100\n");
            var path = new StringBuilder();
            for (int i = 0; i < b.Count; i++)
            {
                var p = b[i];
                string line = string.Format(inv, "{0:0.0000},{1:0.0000},{2:0.000},{3:0.000}", p[0], p[1], p[0] * Scale, p[1] * Scale);
                csv.AppendLine(line); csvAll.AppendLine(m.Name + "," + line);
                path.Append(string.Format(inv, "{0}{1:0.0},{2:0.0} ", i == 0 ? "M" : "L", ox + p[0] * ppm, oy - p[1] * ppm));
            }
            File.WriteAllText(Path.Combine(outDir, tag + "_boundary.csv"), csv.ToString());
            svg.AppendLine(string.Format(inv, "<path d='{0}Z' fill='{1}' fill-opacity='0.18' stroke='{1}' stroke-width='2'/>", path, colors[k]));
            svg.AppendLine(string.Format(inv, "<text x='{0}' y='{1}' font-size='13' font-weight='600' text-anchor='middle' fill='{2}'>{3}</text>",
                ox + m.R0 * ppm, oy - zmax * ppm - 6 - (m.Name == "EAST" ? 14 : 0) - (m.Name == "SPARC" ? 28 : 0), colors[k], m.Name));
        }
        svg.AppendLine(string.Format(inv, "<text x='{0}' y='{1}' font-size='12' fill='#666'>Equal R and Z scale. Grid: 1 m = 10 mm at 1:100.</text>", W - 330, H - 40));
        svg.AppendLine("</svg>");
        File.WriteAllText(Path.Combine(outDir, "comparison_cross_sections.svg"), svg.ToString());
        File.WriteAllText(Path.Combine(outDir, "all_boundaries.csv"), csvAll.ToString());
        return report.ToString();
    }
}
