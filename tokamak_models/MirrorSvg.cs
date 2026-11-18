// mirror_models/comparison_mirrors.svg: the five mirror machines at true scale (same scale on both axes), stacked on a
// common axial scale. Each outline is what the plaque shows from above: the plasma's width across the plate at each
// axial position. For the axisymmetric machines (WHAM, GDT) that is the radial profile; for 2XIIB, GAMMA 10 and TMX-U,
// whose quadrupole sections are flattened ellipses, it is the extent in y (the plaques keep the x >= 0 half and are
// viewed along x). Outlines are of the true flux surfaces, not the 0.8 mm-thickened plaque reliefs (the thickening
// changes widths by at most ~4 cm real). The axial direction runs as on the plaques: -Z to the right.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public static class MirrorSvg
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    const double Ppm = 50;          // px per metre
    const double StepMm = 0.1;      // 1:100 mm between slices (= 1 cm real)

    class Row { public string Name, Color, Note; public List<double[]> Outline; public double HalfLen, HalfW; } // outline in metres (axial, across)

    // top-down outline from a full-solid STL (mm, axis along Z): +y edge out, -y edge back
    static List<double[]> FromStl(string stl)
    {
        var m = MirrorThin.Load(stl);
        var top = new List<double[]>();
        double z0 = m.ZMin + 0.005, z1 = m.ZMax - 0.005;
        int n = (int)Math.Ceiling((z1 - z0) / StepMm);
        for (int i = 0; i <= n; i++)
        {
            double z = z0 + (z1 - z0) * i / n; var s = MirrorThin.Section(m, z); if (s.Count == 0) continue;
            double a, b; MirrorThin.Extent(s, out a, out b);
            top.Add(new[] { -z / 10, b / 10 });   // 1:100 mm -> real m; plaque orientation: -Z to the right
        }
        var o = new List<double[]>(top);
        for (int i = top.Count - 1; i >= 0; i--) o.Add(new[] { top[i][0], -top[i][1] });
        return o;
    }

    static List<double[]> FromCsv(string csv)
    {
        var o = new List<double[]>(); bool first = true;
        foreach (var l in File.ReadAllLines(csv))
        {
            if (first) { first = false; continue; }
            var s = l.Split(','); o.Add(new[] { double.Parse(s[0], Inv), double.Parse(s[1], Inv) });
        }
        return o;
    }

    public static string Run(string root)
    {
        string mm = Path.Combine(root, "mirror_models");
        var rows = new List<Row>
        {
            new Row { Name = "GAMMA 10", Color = "#d62728", Note = "tandem mirror; quadrupole anchors (outline as on the plaque, top view)", Outline = FromStl(Path.Combine(mm, "stl", "GAMMA10_plasma_1to100.stl")) },
            new Row { Name = "TMX-U", Color = "#9467bd", Note = "tandem mirror; quadrupole end cells (outline as on the plaque, top view)", Outline = FromStl(Path.Combine(mm, "stl", "TMX-U_plasma_1to100.stl")) },
            new Row { Name = "GDT", Color = "#2ca02c", Note = "axisymmetric gas-dynamic trap", Outline = FromStl(Path.Combine(mm, "stl", "GDT_plasma_1to100.stl")) },
            new Row { Name = "2XIIB", Color = "#ff7f0e", Note = "minimum-B quadrupole mirror (outline as on the plaque, top view)", Outline = FromStl(Path.Combine(mm, "stl", "2XIIB_plasma_1to100.stl")) },
            new Row { Name = "WHAM", Color = "#17becf", Note = "axisymmetric HTS mirror", Outline = FromCsv(Path.Combine(root, "tokamak_models", "csv", "WHAM_boundary.csv")) },
        };
        double maxHalfLen = 0;
        foreach (var r in rows)
            foreach (var p in r.Outline) { r.HalfLen = Math.Max(r.HalfLen, Math.Abs(p[0])); r.HalfW = Math.Max(r.HalfW, Math.Abs(p[1])); maxHalfLen = Math.Max(maxHalfLen, r.HalfLen); }

        int grid = (int)Math.Ceiling(maxHalfLen);
        double left = 60, cx = left + grid * Ppm, width = cx + grid * Ppm + 40;
        double y = 40, gap = 46;
        var body = new StringBuilder(); var centres = new List<double>();
        foreach (var r in rows)
        {
            double half = Math.Max(r.HalfW * Ppm, 12);
            double cy = y + 18 + half; centres.Add(cy);
            var path = new StringBuilder();
            for (int i = 0; i < r.Outline.Count; i++)
                path.Append(string.Format(Inv, "{0}{1:0.0},{2:0.0} ", i == 0 ? "M" : "L", cx + r.Outline[i][0] * Ppm, cy - r.Outline[i][1] * Ppm));
            body.AppendLine(string.Format(Inv, "<line x1='{0:0.0}' y1='{2:0.0}' x2='{1:0.0}' y2='{2:0.0}' stroke='{3}' stroke-dasharray='4 3' stroke-width='1'/>", cx - r.HalfLen * Ppm - 10, cx + r.HalfLen * Ppm + 10, cy, r.Color));
            body.AppendLine(string.Format("<path d='{0}Z' fill='{1}' fill-opacity='0.25' stroke='{1}' stroke-width='1.5' stroke-linejoin='round'/>", path, r.Color));
            body.AppendLine(string.Format(Inv, "<text x='{0:0.0}' y='{1:0.0}' font-size='14' font-weight='600' fill='{2}'>{3}</text><text x='{4:0.0}' y='{1:0.0}' font-size='11' fill='#666'>{5}; {6:0.0} m long, {7:0.00} m max half-width</text>",
                cx - r.HalfLen * Ppm, y + 10, r.Color, r.Name, cx - r.HalfLen * Ppm + 10.5 * r.Name.Length + 14, r.Note, 2 * r.HalfLen, r.HalfW));
            y = cy + half + gap;
        }
        double h = y + 10;
        var sb = new StringBuilder();
        sb.AppendLine(string.Format(Inv, "<svg xmlns='http://www.w3.org/2000/svg' width='{0:0}' height='{1:0}' viewBox='0 0 {0:0} {1:0}' font-family='Segoe UI,Arial,sans-serif'>", width, h + 30));
        sb.AppendLine("<rect width='100%' height='100%' fill='white'/>");
        for (int k = -grid; k <= grid; k++)
        {
            double x = cx + k * Ppm;
            sb.AppendLine(string.Format(Inv, "<line x1='{0:0}' y1='20' x2='{0:0}' y2='{1:0}' stroke='{2}'/>", x, h - 10, k == 0 ? "#ccc" : "#eee"));
            if (k % 2 == 0) sb.AppendLine(string.Format(Inv, "<text x='{0:0}' y='{1:0}' font-size='12' text-anchor='middle' fill='#666'>{2} m</text>", x, h + 6, Math.Abs(k)));
        }
        sb.AppendLine(string.Format(Inv, "<text x='{0:0}' y='{1:0}' font-size='12' text-anchor='middle' fill='#666'>distance along the axis from each machine's centre (m); same scale across and along (1 m = {2:0} px); dashed = machine axis; drawn as on the plaques</text>", cx, h + 24, Ppm));
        sb.Append(body);
        sb.AppendLine("</svg>");
        string outPath = Path.Combine(mm, "comparison_mirrors.svg");
        File.WriteAllText(outPath, sb.ToString());
        var log = new StringBuilder("wrote " + outPath + "\n");
        foreach (var r in rows) log.AppendLine(string.Format(Inv, "  {0}: {1} outline points, length {2:0.00} m, max half-width {3:0.000} m", r.Name, r.Outline.Count, 2 * r.HalfLen, r.HalfW));
        return log.ToString();
    }
}
