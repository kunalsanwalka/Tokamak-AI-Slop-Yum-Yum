// Adds the device name as raised, flat-topped lettering on the top of each plasma solid (SolidWorks API),
// then exports STEP + STL and a top-view PNG.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

public static class Emboss
{
    public const double RaiseMm = 0.8;   // letter tops this far above the highest surface point under them
    public const double SinkMm = 0.6;    // extra depth into the body below the lowest surface point under them

    static List<double[]> LoadProfile(string csv)
    {
        var p = new List<double[]>(); bool first = true;
        foreach (var line in File.ReadAllLines(csv))
        {
            if (first) { first = false; continue; }
            var s = line.Split(','); if (s.Length < 4) continue;
            p.Add(new[] { double.Parse(s[2], CultureInfo.InvariantCulture), double.Parse(s[3], CultureInfo.InvariantCulture) });
        }
        return p;
    }

    static double UpperZ(List<double[]> p, double r)
    {
        double z = double.NaN;
        for (int i = 0; i < p.Count; i++)
        {
            var a = p[i]; var b = p[(i + 1) % p.Count];
            if ((a[0] - r) * (b[0] - r) <= 0 && a[0] != b[0])
            {
                double zz = a[1] + (r - a[0]) / (b[0] - a[0]) * (b[1] - a[1]);
                if (double.IsNaN(z) || zz > z) z = zz;
            }
        }
        return z;
    }

    // Surface height range (max, drop) under a text band centred at rc for a profile CSV.
    public static double[] DropAt(string csv, double rc, double h)
    {
        var p = LoadProfile(csv); double zmx = -1e9, zmn = 1e9;
        for (double r = rc - 0.5 * h; r <= rc + 0.6 * h; r += 0.05) { double z = UpperZ(p, r); zmx = Math.Max(zmx, z); zmn = Math.Min(zmn, z); }
        return new[] { zmx, zmx - zmn };
    }

    // Choose the text band (radius) where the top surface is flattest. Returns rc, zTopUnderBand, drop (mm).
    public static double[] Band(List<double[]> p, double h)
    {
        double rmin = 1e9, rmax = -1e9; foreach (var q in p) { rmin = Math.Min(rmin, q[0]); rmax = Math.Max(rmax, q[0]); }
        double[] best = null;
        for (double rc = rmin + 0.5 * h; rc <= rmax - 0.6 * h; rc += 0.1)
        {
            double zmx = -1e9, zmn = 1e9;
            for (double r = rc - 0.5 * h; r <= rc + 0.6 * h; r += 0.05) { double z = UpperZ(p, r); zmx = Math.Max(zmx, z); zmn = Math.Min(zmn, z); }
            if (best == null || zmx - zmn < best[2]) best = new[] { rc, zmx, zmx - zmn };
        }
        return best;
    }

    // Advance width of each character (mm) for Arial Black with cap height capMm (SolidWorks CharHeight = cap height).
    public static double[] GlyphAdvances(string s, double capMm)
    {
        var res = new double[s.Length];
        using (var bmp = new System.Drawing.Bitmap(10, 10))
        using (var g = System.Drawing.Graphics.FromImage(bmp))
        using (var font = new System.Drawing.Font("Arial Black", 1000f, System.Drawing.GraphicsUnit.Pixel))
        {
            var fmt = System.Drawing.StringFormat.GenericTypographic;
            fmt.FormatFlags |= System.Drawing.StringFormatFlags.MeasureTrailingSpaces;
            var ff = font.FontFamily;
            double em = ff.GetEmHeight(System.Drawing.FontStyle.Regular);
            // cap height from the outline of "H"
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddString("H", ff, (int)System.Drawing.FontStyle.Regular, 1000f, new System.Drawing.PointF(0, 0), fmt);
            double cap = path.GetBounds().Height;                       // px at em = 1000 px
            for (int i = 0; i < s.Length; i++)
            {
                double w = g.MeasureString(s[i].ToString(), font, 100000, fmt).Width; // px at em = 1000 px
                res[i] = w / cap * capMm * 1.04;                        // 4 % extra tracking for clean gaps
            }
        }
        return res;
    }

    public static string Run(string root, bool visible) { return Run(root, visible, null); }

    // only: a single machine name (e.g. "NSTX-U") to re-letter just that one; null/empty = all
    public static string Run(string root, bool visible, string only)
    {
        var names = new[] { "WEST", "EAST", "DIII-D", "SPARC", "ITER", "ARC", "NSTX-U" };
        var heights = new Dictionary<string, double> { { "WEST", 4 }, { "EAST", 4 }, { "DIII-D", 4 }, { "SPARC", 4 }, { "ITER", 8 }, { "ARC", 6 }, { "NSTX-U", 2.8 } };
        // NSTX-U's top ring is tiny (crown at R ~ 7.6 mm); at the flattest band the name would wrap ~180 deg round the
        // hole and SolidWorks mis-sizes letters rotated beyond ~60 deg. Put it just outboard of the crown (R = 10 mm),
        // where the end letters turn ~42 deg and the surface under the text drops ~2.1 mm.
        var rcOverride = new Dictionary<string, double> { { "NSTX-U", 10.0 } };
        if (!string.IsNullOrEmpty(only)) names = Array.FindAll(names, x => x.Replace("-", "").Equals(only.Replace("-", ""), StringComparison.OrdinalIgnoreCase));
        string md = Path.Combine(root, "tokamak_models");
        string stepIn = Path.Combine(md, "step"), outStep = Path.Combine(md, "step_named"), outStl = Path.Combine(md, "stl_named"), outImg = Path.Combine(md, "verification");
        Directory.CreateDirectory(outStep); Directory.CreateDirectory(outStl); Directory.CreateDirectory(outImg);
        var log = new StringBuilder();
        var sw = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
        sw.Visible = visible;
        sw.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swStepAP, 214);
        sw.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swSTLBinaryFormat, true);
        sw.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swSTLDontTranslateToPositive, true);
        sw.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swExportStlUnits, (int)swLengthUnit_e.swMM);
        sw.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swSTLQuality, (int)swSTLQuality_e.swSTLQuality_Custom);
        sw.SetUserPreferenceDoubleValue((int)swUserPreferenceDoubleValue_e.swSTLDeviation, 0.00001);   // 0.01 mm chordal
        sw.SetUserPreferenceDoubleValue((int)swUserPreferenceDoubleValue_e.swSTLAngleTolerance, 0.0349); // ~2 deg

        foreach (var name in names)
        {
            string tag = name.Replace("-", "");
            double h = heights[name];
            var prof = LoadProfile(Path.Combine(md, "csv", tag + "_boundary.csv"));
            var band = Band(prof, h);
            if (rcOverride.ContainsKey(name))
            {
                var d = DropAt(Path.Combine(md, "csv", tag + "_boundary.csv"), rcOverride[name], h);
                band = new[] { rcOverride[name], d[0], d[1] };
            }
            double rc = band[0], zTop = band[1] + RaiseMm, depth = band[2] + RaiseMm + SinkMm;
            int err = 0, warn = 0;
            string f = Path.Combine(stepIn, tag + "_plasma_1to100.step");
            var doc = (ModelDoc2)sw.LoadFile4(f, "r", sw.GetImportFileData(f), ref err);
            if (doc == null) { log.AppendLine(name + ": load failed " + err); continue; }
            var part = (PartDoc)doc;
            double vol0 = ((MassProperty)doc.Extension.CreateMassProperty()).Volume * 1e9;
            double zmax0 = -1e9;
            foreach (Body2 b in (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, false)) zmax0 = Math.Max(zmax0, ((double[])b.GetBodyBox())[5] * 1000);

            // reference plane parallel to the Front Plane (XY, normal +Z) at the letter-top height
            doc.ClearSelection2(true);
            doc.Extension.SelectByID2("Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
            var plane = (RefPlane)doc.FeatureManager.InsertRefPlane((int)swRefPlaneReferenceConstraints_e.swRefPlaneReferenceConstraint_Distance, zTop / 1000, 0, 0, 0, 0);
            var planeFeat = (Feature)plane;
            // make sure the plane sits at +zTop (not -zTop)
            var mt = (MathTransform)plane.Transform; var arr = (double[])mt.ArrayData;
            double planeZ = arr[11] * 1000;
            if (Math.Abs(planeZ - zTop) > 1e-3)
            {
                doc.EditUndo2(1); doc.ClearSelection2(true);
                doc.Extension.SelectByID2("Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
                plane = (RefPlane)doc.FeatureManager.InsertRefPlane((int)swRefPlaneReferenceConstraints_e.swRefPlaneReferenceConstraint_Distance
                    | (int)swRefPlaneReferenceConstraints_e.swRefPlaneReferenceConstraint_OptionFlip, zTop / 1000, 0, 0, 0, 0);
                planeFeat = (Feature)plane; arr = (double[])((MathTransform)plane.Transform).ArrayData; planeZ = arr[11] * 1000;
            }

            // sketch: construction arc over +Y, text along it
            doc.ClearSelection2(true);
            planeFeat.Select2(false, 0);
            doc.SketchManager.InsertSketch(true);
            doc.SketchManager.AddToDB = true;
            // Letters are placed one by one along a circle of radius rb (baseline), centred on +Y, reading
            // left-to-right when viewed from above, tops pointing outward. Each letter is rotated (Escapement)
            // so its baseline is tangent to the circle. Advance widths come from the Arial Black font metrics.
            double rbMm = rc - 0.5 * h;
            var adv = GlyphAdvances(name, h);                          // mm, per character
            double total = 0; foreach (var w in adv) total += w;
            double phi = Math.PI / 2 + (total / rbMm) / 2;            // angle of the left end of the text
            int placed = 0;
            for (int i = 0; i < name.Length; i++)
            {
                double w = adv[i], phc = phi - (w / 2) / rbMm;         // letter centre angle
                double tx = Math.Sin(phc), ty = -Math.Cos(phc);         // reading direction (clockwise tangent)
                double px = rbMm * Math.Cos(phc) - (w / 2) * tx, py = rbMm * Math.Sin(phc) - (w / 2) * ty;
                doc.ClearSelection2(true);
                var st = (SketchText)doc.InsertSketchText(px / 1000, py / 1000, 0, name[i].ToString(), (int)swTextJustification_e.swTextJustificationLeft, 0, 0, 100, 100);
                if (st != null)
                {
                    var tf = (TextFormat)st.GetTextFormat();
                    tf.TypeFaceName = "Arial Black"; tf.Bold = false; tf.CharHeight = h / 1000;
                    tf.Escapement = phc - Math.PI / 2;
                    st.SetTextFormat(false, tf);
                    placed++;
                }
                phi -= w / rbMm;
            }
            doc.SketchManager.AddToDB = false;
            doc.SketchManager.InsertSketch(true);   // exit sketch
            var sketchFeat = (Feature)doc.FeatureByPositionReverse(0);

            // extrude down into the body (try reversed direction first, check result)
            Feature ext = null; int nb = 0; double zmaxBody = 0, vol1 = 0;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                doc.ClearSelection2(true);
                sketchFeat.Select2(false, 0);
                ext = doc.FeatureManager.FeatureExtrusion3(true, false, attempt == 0, (int)swEndConditions_e.swEndCondBlind, 0,
                    depth / 1000, 0, false, false, false, false, 0, 0, false, false, false, false, true, true, true,
                    (int)swStartConditions_e.swStartSketchPlane, 0, false);
                var bodies = (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, false);
                nb = bodies == null ? 0 : bodies.Length;
                zmaxBody = -1e9;
                if (bodies != null) foreach (Body2 b in bodies) { var bx = (double[])b.GetBodyBox(); zmaxBody = Math.Max(zmaxBody, bx[5] * 1000); }
                vol1 = ((MassProperty)doc.Extension.CreateMassProperty()).Volume * 1e9;
                // success = one merged body whose top is the letter tops (or the original crown, if that is higher)
                if (ext != null && nb == 1 && Math.Abs(zmaxBody - Math.Max(zTop, zmax0)) < 0.01) break;
                if (ext != null) { doc.EditUndo2(1); ext = null; }
            }
            string status = ext == null ? "EXTRUDE FAILED" : "ok";
            log.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0}: {11} letters, cap H={1}mm, band centre R={2:0.00}mm, plane Z={3:0.00}mm, depth {4:0.00}mm | bodies={5} zmax={6:0.00} | volume {7:0} -> {8:0} mm3 (+{9:0}) | {10}",
                name, h, rc, planeZ, depth, nb, zmaxBody, vol0, vol1, vol1 - vol0, status, placed));

            // hide helper plane/sketch, export
            doc.ClearSelection2(true); planeFeat.Select2(false, 0); doc.BlankRefGeom();
            doc.ClearSelection2(true);
            doc.Extension.SaveAs(Path.Combine(outStep, tag + "_plasma_named_1to100.step"), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref err, ref warn);
            doc.Extension.SaveAs(Path.Combine(outStl, tag + "_plasma_named_1to100.stl"), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref err, ref warn);
            doc.ShowNamedView2("*Front", -1); doc.ViewZoomtofit2();
            doc.Extension.SaveAs(Path.Combine(outImg, tag + "_named_topview.png"), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref err, ref warn);
            sw.CloseDoc(doc.GetTitle());
        }
        sw.ExitApp();
        return log.ToString();
    }
}
