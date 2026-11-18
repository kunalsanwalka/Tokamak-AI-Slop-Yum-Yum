// Finds the parts of a mirror flux-surface solid that would be too thin on the half-body plaque, and plans the
// "thickening" sections that MirrorSw lofts and unions onto the solid.
// The plaque keeps the x >= 0 half, so at axial position z the relief is a(z) = max x tall and 2 b(z) = 2 max |y| wide.
// Minimum printable: height >= MinMm and width >= MinMm (0.8 mm = two 0.4 mm nozzle lines).
// Each cross-section is sliced from the solid's STL (mm, axis along Z). Where it is thin, the real section is stretched
// along x by sx = max(Shrink, TargetMm / a) and along y by sy = max(Shrink, TargetMm / (2 b)); elsewhere it is shrunk by
// Shrink = 0.85, so it lies inside the solid and the union leaves the true shape untouched there. The cross-sections
// are star-shaped about the axis and symmetric about x = 0 and y = 0, so scaling about the axis keeps them centred.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public static class MirrorThin
{
    // sections are planned to TargetMm so the loft, which sags slightly between its 1 mm-spaced sections, stays >= MinMm
    // Shrink is well below 1 so the lofted sections stay clearly inside the real surface where they are not thickening:
    // at 0.97 the two surfaces nearly coincide on small sections and SolidWorks fails to merge them
    public const double MinMm = 0.8, TargetMm = 0.85, Shrink = 0.85, Step = 0.25, Spacing = 1.0, Margin = 1.0, EndInset = 0.02;
    public const int NPts = 40;
    public const double SeamGap = 0.001;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public class Mesh { public float[] T; public double ZMin, ZMax; public int[][] Bins; public double BinW; }

    public static Mesh Load(string stl)
    {
        var b = File.ReadAllBytes(stl);
        int n = BitConverter.ToInt32(b, 80);
        var t = new float[n * 9];
        for (int i = 0; i < n; i++) for (int k = 0; k < 9; k++) t[i * 9 + k] = BitConverter.ToSingle(b, 84 + i * 50 + 12 + 4 * k);
        var m = new Mesh { T = t, ZMin = 1e9, ZMax = -1e9 };
        for (int i = 0; i < n; i++) for (int k = 0; k < 3; k++) { m.ZMin = Math.Min(m.ZMin, t[i * 9 + 3 * k + 2]); m.ZMax = Math.Max(m.ZMax, t[i * 9 + 3 * k + 2]); }
        m.BinW = 0.5; int nb = (int)((m.ZMax - m.ZMin) / m.BinW) + 1;
        var bins = new List<int>[nb]; for (int i = 0; i < nb; i++) bins[i] = new List<int>();
        for (int i = 0; i < n; i++)
        {
            double lo = Math.Min(t[i * 9 + 2], Math.Min(t[i * 9 + 5], t[i * 9 + 8])), hi = Math.Max(t[i * 9 + 2], Math.Max(t[i * 9 + 5], t[i * 9 + 8]));
            for (int j = (int)((lo - m.ZMin) / m.BinW); j <= Math.Min(nb - 1, (int)((hi - m.ZMin) / m.BinW)); j++) bins[j].Add(i);
        }
        m.Bins = new int[nb][]; for (int i = 0; i < nb; i++) m.Bins[i] = bins[i].ToArray();
        return m;
    }

    // segments (x1,y1,x2,y2) of the section at z
    public static List<double[]> Section(Mesh m, double z)
    {
        var o = new List<double[]>(); var t = m.T;
        int j = Math.Max(0, Math.Min(m.Bins.Length - 1, (int)((z - m.ZMin) / m.BinW)));
        foreach (int i in m.Bins[j])
        {
            var p = new List<double>();
            for (int k = 0; k < 3; k++)
            {
                int a = i * 9 + 3 * k, c = i * 9 + 3 * ((k + 1) % 3);
                double za = t[a + 2] - z, zc = t[c + 2] - z;
                if ((za < 0) == (zc < 0)) continue;
                double f = za / (za - zc);
                p.Add(t[a] + f * (t[c] - t[a])); p.Add(t[a + 1] + f * (t[c + 1] - t[a + 1]));
            }
            if (p.Count == 4) o.Add(p.ToArray());
        }
        return o;
    }

    public static void Extent(List<double[]> s, out double a, out double b)
    {
        a = 0; b = 0;
        foreach (var q in s) { a = Math.Max(a, Math.Max(Math.Abs(q[0]), Math.Abs(q[2]))); b = Math.Max(b, Math.Max(Math.Abs(q[1]), Math.Abs(q[3]))); }
    }

    // farthest crossing of the ray from the axis at angle th
    public static double Radius(List<double[]> s, double th)
    {
        double dx = Math.Cos(th), dy = Math.Sin(th), r = 0;
        foreach (var q in s)
        {
            double ex = q[2] - q[0], ey = q[3] - q[1], den = dx * ey - dy * ex;
            if (Math.Abs(den) < 1e-15) continue;
            double tt = (q[0] * ey - q[1] * ex) / den, u = (q[0] * dy - q[1] * dx) / den;
            if (u >= -1e-9 && u <= 1 + 1e-9 && tt > r) r = tt;
        }
        return r;
    }

    public static double Sx(double a) { return Math.Max(Shrink, TargetMm / a); }
    public static double Sy(double b) { return Math.Max(Shrink, TargetMm / (2 * b)); }
    static bool Thin(double a, double b) { return Sx(a) > Shrink + 1e-9 || Sy(b) > Shrink + 1e-9; }

    // z ranges (mm) that need thickening, already merged and padded
    public static List<double[]> Ranges(Mesh m, StringBuilder log)
    {
        double z0 = m.ZMin + EndInset, z1 = m.ZMax - EndInset;
        var r = new List<double[]>(); double start = double.NaN, last = double.NaN;
        double minA = 1e9, minW = 1e9;
        for (double z = z0; z <= z1 + 1e-9; z += Step)
        {
            var s = Section(m, z); if (s.Count == 0) continue;
            double a, b; Extent(s, out a, out b); minA = Math.Min(minA, a); minW = Math.Min(minW, 2 * b);
            if (Thin(a, b)) { if (double.IsNaN(start) || z - last > 3) { if (!double.IsNaN(start)) r.Add(new[] { start, last }); start = z; } last = z; }
        }
        if (!double.IsNaN(start)) r.Add(new[] { start, last });
        foreach (var q in r) { q[0] = Math.Max(z0, q[0] - Margin); q[1] = Math.Min(z1, q[1] + Margin); }
        if (log != null)
        {
            log.Append(string.Format(Inv, "  thinnest before: height {0:0.00} mm, width {1:0.00} mm; thickening ranges (z mm):", minA, minW));
            foreach (var q in r) log.Append(string.Format(Inv, " [{0:0.0}, {1:0.0}]", q[0], q[1]));
            log.AppendLine();
        }
        return r;
    }

    // planned sections for one range: z and NPts (x,y) points in mm (closed: not repeated)
    public static List<KeyValuePair<double, double[]>> Plan(Mesh m, double[] range)
    {
        var o = new List<KeyValuePair<double, double[]>>();
        int n = Math.Max(2, (int)Math.Ceiling((range[1] - range[0]) / Spacing) + 1);
        var zs = new List<double>();
        for (int i = 0; i < n; i++) zs.Add(range[0] + (range[1] - range[0]) * i / (n - 1));
        // the solids are built as two halves that meet at z = 0, and SolidWorks cannot merge a loft that crosses that
        // seam. A loft ending exactly on it from below merges; from above it only merges if it starts just off it, so
        // sections go at z = 0 and z = SeamGap, and MirrorSw skips the 1 um piece between them (far below print resolution)
        if (range[0] < 0 && range[1] > 0)
        {
            zs.RemoveAll(z => Math.Abs(z) < 0.3 * Spacing); zs.Add(0); zs.Add(SeamGap); zs.Sort();
        }
        foreach (double z in zs)
        {
            var s = Section(m, z); double a, b; Extent(s, out a, out b);
            double sx = Sx(a), sy = Sy(b); var p = new double[2 * NPts];
            for (int k = 0; k < NPts; k++)
            {
                // even spacing around the section: rays towards an ellipse of the section's own a, b at equal parameter steps
                double u = 2 * Math.PI * k / NPts, th = Math.Atan2(b * Math.Sin(u), a * Math.Cos(u));
                double rr = Radius(s, th);
                p[2 * k] = sx * rr * Math.Cos(th); p[2 * k + 1] = sy * rr * Math.Sin(th);
            }
            o.Add(new KeyValuePair<double, double[]>(z, p));
        }
        return o;
    }

    // largest |y| (mm) of the mesh over triangles touching z0 <= z <= z1
    public static double MaxAbsY(Mesh m, double z0, double z1)
    {
        double y = 0; var t = m.T;
        for (int i = 0; i < t.Length / 9; i++)
        {
            double lo = Math.Min(t[i * 9 + 2], Math.Min(t[i * 9 + 5], t[i * 9 + 8])), hi = Math.Max(t[i * 9 + 2], Math.Max(t[i * 9 + 5], t[i * 9 + 8]));
            if (hi < z0 || lo > z1) continue;
            for (int k = 0; k < 3; k++) y = Math.Max(y, Math.Abs(t[i * 9 + 3 * k + 1]));
        }
        return y;
    }

    // thinnest height/width of the relief in [z0, z1] (for the check after thickening)
    public static string Report(Mesh m)
    {
        double minA = 1e9, minW = 1e9, za = 0, zw = 0;
        for (double z = m.ZMin + EndInset; z <= m.ZMax - EndInset; z += Step)
        {
            var s = Section(m, z); if (s.Count == 0) continue;
            double a, b; Extent(s, out a, out b);
            if (a < minA) { minA = a; za = z; }
            if (2 * b < minW) { minW = 2 * b; zw = z; }
        }
        return string.Format(Inv, "thinnest height {0:0.00} mm at z {1:0.0}, width {2:0.00} mm at z {3:0.0}", minA, za, minW, zw);
    }
}
