// Named plaques for the other mirror machines (2XIIB, GAMMA 10, GDT, TMX-U), in the same form as the WHAM plaque.
// Input: Elliot Claveau's 1:100 flux-surface solids in mirror_models/step_raw/ (mm, mirror axis along Z).
// Each solid is halved on its x = 0 plane, a plane of mirror symmetry for all four (the quadrupole end cells of
// 2XIIB, TMX-U and GAMMA 10 keep the x = 0 and y = 0 reflection symmetries), so the cut face is a true half that
// lies flat on a 2 mm base plate. The name is raised 0.8 mm off the plate, centred under the middle of the machine.
// Before halving, anything that would be under 0.8 mm tall or wide on the plaque is thickened (MirrorThin.cs): stretched
// copies of the real cross-sections are lofted over those stretches and merged in, so the shape is kept elsewhere.
// Outputs (mirror_models/): stl/<tag>_plasma_1to100.stl (full solid), step_named/ + stl_named/<tag>_plaque_named_1to100,
// verification/<tag>_named_topview.png and _isoview.png.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

public static class MirrorSw
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    // same plaque constants as WhamSw
    public const double PlateMm = 2.0, RaiseMm = 0.8, CapMm = 5.0;
    public const double EndMarginMm = 3.3, SideMarginMm = 2.3, TextGapMm = 2.1, TextFootMm = 1.7;

    class M { public string Tag, Text, File; public M(string t, string x, string f) { Tag = t; Text = x; File = f; } }

    static readonly M[] Machines =
    {
        new M("2XIIB", "2XIIB", "2XIIB_flux_surface_1to100.step"),
        new M("GAMMA10", "GAMMA 10", "GAMMA_10_flux_surface_1to100.step"),
        new M("GDT", "GDT", "GDT_flux_surface_1to100.step"),
        new M("TMX-U", "TMX-U", "TMX-U_flux_surface_1to100.step"),
    };

    static double[] Box(PartDoc part, out int n)
    {
        var bodies = (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, false);
        n = bodies == null ? 0 : bodies.Length;
        var bb = new[] { 1e9, 1e9, 1e9, -1e9, -1e9, -1e9 };
        if (bodies != null) foreach (Body2 b in bodies)
        {
            var x = (double[])b.GetBodyBox();
            for (int i = 0; i < 3; i++) { bb[i] = Math.Min(bb[i], x[i] * 1000); bb[i + 3] = Math.Max(bb[i + 3], x[i + 3] * 1000); }
        }
        return bb;
    }

    static double Vol(ModelDoc2 doc) { return ((MassProperty)doc.Extension.CreateMassProperty()).Volume * 1e9; }

    static int Faults(PartDoc part)
    {
        int f = 0; var bodies = (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, false);
        if (bodies != null) foreach (Body2 b in bodies) { var e = b.Check3; if (e != null) f += e.Count; }
        return f;
    }

    // smallest x (mm) over the faces a feature made (the plate's faces are planar, so their boxes are tight)
    static double FeatureMinX(Feature f)
    {
        double x = 1e9; var faces = (object[])f.GetFaces();
        if (faces != null) foreach (Face2 fc in faces) x = Math.Min(x, ((double[])fc.GetBox())[0] * 1000);
        return x;
    }

    // largest |y| (mm) of any face whose box overlaps the axial band |z| <= zHalf: clearance for the name
    static double YUnder(PartDoc part, double zHalf)
    {
        double y = 0; var bodies = (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, false);
        foreach (Body2 b in bodies)
            foreach (Face2 f in (object[])b.GetFaces())
            {
                var x = (double[])f.GetBox();
                if (x[2] * 1000 > zHalf || x[5] * 1000 < -zHalf) continue;
                y = Math.Max(y, Math.Max(Math.Abs(x[1]), Math.Abs(x[4])) * 1000);
            }
        return y;
    }

    // closed spline through the planned section points (mm, model x/y) on a new plane at model z
    // All section planes are offset the same way (flipped, i.e. towards -Z) from one base plane above the solid, so every
    // profile has the same orientation; mixing flipped and unflipped planes makes the loft twist where they meet.
    static Feature SectionSketch(ModelDoc2 doc, MathUtility mu, Feature basePlane, double zBase, double zmm, double[] p, ref int badPlanes)
    {
        doc.ClearSelection2(true);
        basePlane.Select2(false, 0);
        int c = (int)swRefPlaneReferenceConstraints_e.swRefPlaneReferenceConstraint_Distance | (int)swRefPlaneReferenceConstraints_e.swRefPlaneReferenceConstraint_OptionFlip;
        var plane = (Feature)doc.FeatureManager.InsertRefPlane(c, (zBase - zmm) / 1000, 0, 0, 0, 0);
        doc.ClearSelection2(true); plane.Select2(false, 0);
        var sm = doc.SketchManager;
        sm.InsertSketch(true); sm.AddToDB = true;
        var xf = (MathTransform)((Sketch)sm.ActiveSketch).ModelToSketchTransform;
        int n = p.Length / 2; var pts = new double[3 * (n + 1)];
        for (int k = 0; k <= n; k++)
        {
            int j = k % n;
            var q = (double[])((MathPoint)((MathPoint)mu.CreatePoint(new[] { p[2 * j] / 1000, p[2 * j + 1] / 1000, zmm / 1000 })).MultiplyTransform(xf)).ArrayData;
            if (Math.Abs(q[2]) > 1e-7) badPlanes++;
            pts[3 * k] = q[0]; pts[3 * k + 1] = q[1]; pts[3 * k + 2] = 0;
        }
        sm.CreateSpline2(pts, false);
        sm.AddToDB = false; sm.InsertSketch(true);
        return (Feature)doc.FeatureByPositionReverse(0);
    }

    // loft the stretched sections over every thin range and merge them into the solid
    static void Thicken(SldWorks sw, ModelDoc2 doc, string stl, StringBuilder log)
    {
        var mu = (MathUtility)sw.GetMathUtility();
        var mesh = MirrorThin.Load(stl);
        var ranges = MirrorThin.Ranges(mesh, log);
        double v0 = Vol(doc); int ok = 0, nSec = 0, bad = 0;
        doc.FeatureManager.EnableFeatureTree = false;
        double zBase = mesh.ZMax + 5;
        doc.ClearSelection2(true);
        doc.Extension.SelectByID2("Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
        var basePlane = (Feature)doc.FeatureManager.InsertRefPlane((int)swRefPlaneReferenceConstraints_e.swRefPlaneReferenceConstraint_Distance, zBase / 1000, 0, 0, 0, 0);
        foreach (var rg in ranges)
        {
            var plan = MirrorThin.Plan(mesh, rg); var sks = new List<Feature>();
            foreach (var sec in plan) sks.Add(SectionSketch(doc, mu, basePlane, zBase, sec.Key, sec.Value, ref bad));
            nSec += sks.Count;
            // one short loft between each pair of neighbouring sections: a single loft through dozens of sections
            // interpolates along the axis and can pinch or overshoot where the section shape turns quickly (2XIIB centre)
            int segOk = 0;
            for (int i = 0; i + 1 < sks.Count; i++)
            {
                if (plan[i + 1].Key - plan[i].Key < 2 * MirrorThin.SeamGap) { segOk++; continue; }   // the gap at the z = 0 seam
                doc.ClearSelection2(true);
                sks[i].Select2(true, 1); sks[i + 1].Select2(true, 1);
                var loft = doc.FeatureManager.InsertProtrusionBlend2(false, true, false, 1.0, 0, 0, 1, 1, false, false, false, 0, 0, 0, true, true, true, 0);
                if (loft != null) segOk++;
                else log.AppendLine(string.Format(Inv, "  loft FAILED between z {0:0.00} and {1:0.00}", plan[i].Key, plan[i + 1].Key));
            }
            if (segOk == sks.Count - 1) ok++;
        }
        doc.FeatureManager.EnableFeatureTree = true;
        int n; Box((PartDoc)doc, out n);
        log.AppendLine(string.Format(Inv, "  thickened: {0}/{1} lofts ok ({2} sections, {3} off-plane points) | bodies={4} | volume +{5:0.0} mm3", ok, ranges.Count, nSec, bad, n, Vol(doc) - v0));
    }

    static Feature RightPlaneSketch(ModelDoc2 doc, Action<SketchManager> draw)
    {
        doc.ClearSelection2(true);
        doc.Extension.SelectByID2("Right Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
        doc.SketchManager.InsertSketch(true); doc.SketchManager.AddToDB = true;
        draw(doc.SketchManager);
        doc.SketchManager.AddToDB = false; doc.SketchManager.InsertSketch(true);
        return (Feature)doc.FeatureByPositionReverse(0);
    }

    public static string Run(string root, bool visible, string only)
    {
        string mm = Path.Combine(root, "mirror_models");
        foreach (var d in new[] { "stl", "step_named", "stl_named", "verification" }) Directory.CreateDirectory(Path.Combine(mm, d));
        var log = new StringBuilder();

        var sw = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
        sw.Visible = visible;
        sw.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swStepAP, 214);
        sw.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swSTLBinaryFormat, true);
        sw.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swSTLDontTranslateToPositive, true);
        sw.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swExportStlUnits, (int)swLengthUnit_e.swMM);
        sw.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swSTLQuality, (int)swSTLQuality_e.swSTLQuality_Custom);
        sw.SetUserPreferenceDoubleValue((int)swUserPreferenceDoubleValue_e.swSTLDeviation, 0.000005);
        sw.SetUserPreferenceDoubleValue((int)swUserPreferenceDoubleValue_e.swSTLAngleTolerance, 0.0349);
        int err = 0, warn = 0, n;

        foreach (var m in Machines)
        {
            if (!string.IsNullOrEmpty(only) && m.Tag != only && m.Text != only) continue;
            string src = Path.Combine(mm, "step_raw", m.File);
            var doc = (ModelDoc2)sw.LoadFile4(src, "r", sw.GetImportFileData(src), ref err);
            if (doc == null) { log.AppendLine(m.Tag + ": load FAILED, error " + err); continue; }
            var part = (PartDoc)doc;
            var bb = Box(part, out n); double vFull = Vol(doc);
            log.AppendLine(string.Format(Inv, "{0}: imported bodies={1} | box x {2:0.00}..{3:0.00} y {4:0.00}..{5:0.00} z {6:0.00}..{7:0.00} mm | volume {8:0.0} mm3",
                m.Tag, n, bb[0], bb[3], bb[1], bb[4], bb[2], bb[5], vFull));
            string fullStl = Path.Combine(mm, "stl", m.Tag + "_plasma_1to100.stl");
            doc.Extension.SaveAs(fullStl, 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref err, ref warn);

            // 0. thicken anything thinner than 0.8 mm on the plaque, then re-slice to check
            Thicken(sw, doc, fullStl, log);
            string chk = Path.Combine(Path.GetTempPath(), m.Tag + "_thickened_check.stl");
            doc.ClearSelection2(true);   // with a body selected, STL export writes only that body
            doc.Extension.SaveAs(chk, 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref err, ref warn);
            var thick = MirrorThin.Load(chk);
            log.AppendLine("  after: " + MirrorThin.Report(thick));
            string dbg = System.Environment.GetEnvironmentVariable("MIRROR_DEBUG_DIR");
            if (!string.IsNullOrEmpty(dbg))
            {
                File.Copy(chk, Path.Combine(dbg, m.Tag + "_thickened_check.stl"), true);
                doc.Extension.SaveAs(Path.Combine(dbg, m.Tag + "_thickened.SLDPRT"), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent | (int)swSaveAsOptions_e.swSaveAsOptions_Copy, null, ref err, ref warn);
            }
            File.Delete(chk);
            bb = Box(part, out n);
            double vTrue = vFull; vFull = Vol(doc);   // the half is checked against the thickened volume

            // sizes from the tessellated thickened solid (SolidWorks boxes are loose on lofted B-spline faces)
            double halfLen = Math.Max(-thick.ZMin, thick.ZMax), yMax = MirrorThin.MaxAbsY(thick, -1e9, 1e9), big = Math.Max(halfLen, yMax) + 20;
            double wText = 0; foreach (var w in Emboss.GlyphAdvances(m.Text, CapMm)) wText += w / 1.04;
            double yUnder = MirrorThin.MaxAbsY(thick, -(0.5 * wText + 1.0), 0.5 * wText + 1.0);

            // 1. halve: cut away x < 0
            var cutSk = RightPlaneSketch(doc, sm => sm.CreateCornerRectangle(-big / 1000, -big / 1000, 0, big / 1000, big / 1000, 0));
            Feature cut = null;
            for (int attempt = 0; attempt < 2 && cut == null; attempt++)
            {
                doc.ClearSelection2(true); cutSk.Select2(false, 0);
                cut = doc.FeatureManager.FeatureCut4(true, false, attempt == 0, (int)swEndConditions_e.swEndCondBlind, 0, big / 1000, 0,
                    false, false, false, false, 0, 0, false, false, false, false, false, false, true, false, false, false,
                    (int)swStartConditions_e.swStartSketchPlane, 0, false, false);
                bb = Box(part, out n);
                // body boxes are loose on lofted B-spline faces (they include control points), so check the volume is
                // half and that the kept side is +x rather than requiring x >= 0 exactly
                bool ok = cut != null && n == 1 && Math.Abs(Vol(doc) / vFull - 0.5) < 0.01 && bb[3] > -bb[0];
                if (!ok && cut != null) { doc.EditUndo2(1); cut = null; }
            }
            double vHalf = Vol(doc);
            log.AppendLine(string.Format(Inv, "  half: cut {0} | bodies={1} | box x {2:0.00}..{3:0.00} | volume {4:0.0} mm3 ({5:0.000} of thickened full {6:0.0}, true full {7:0.0})",
                cut == null ? "FAILED" : "ok", n, bb[0], bb[3], vHalf, vHalf / vFull, vFull, vTrue));

            // 2. base plate on the cut plane, extruded 2 mm away from the relief (-x)
            double textTop = -(yUnder + TextGapMm), textBase = textTop - CapMm;
            double yHi = yMax + SideMarginMm, yLo = Math.Min(-(yMax + SideMarginMm), textBase - TextFootMm), zP = halfLen + EndMarginMm;
            var plateSk = RightPlaneSketch(doc, sm => sm.CreateCornerRectangle(-zP / 1000, yLo / 1000, 0, zP / 1000, yHi / 1000, 0));
            Feature plate = null;
            for (int attempt = 0; attempt < 2 && plate == null; attempt++)
            {
                doc.ClearSelection2(true); plateSk.Select2(false, 0);
                plate = doc.FeatureManager.FeatureExtrusion3(true, false, attempt == 0, (int)swEndConditions_e.swEndCondBlind, 0, PlateMm / 1000, 0,
                    false, false, false, false, 0, 0, false, false, false, false, true, true, true, (int)swStartConditions_e.swStartSketchPlane, 0, false);
                bb = Box(part, out n);
                bool ok = plate != null && n == 1 && Math.Abs(FeatureMinX(plate) + PlateMm) < 1e-3;
                if (!ok && plate != null) { doc.EditUndo2(1); plate = null; }
            }
            double vPlate = Vol(doc);
            log.AppendLine(string.Format(Inv, "  base: {0} | bodies={1} | plate y {2:0.00}..{3:0.00} z +-{4:0.00} mm (box y {5:0.00}..{6:0.00} z {7:0.00}..{8:0.00}) | volume {9:0.0} mm3",
                plate == null ? "FAILED" : "ok", n, yLo, yHi, zP, bb[1], bb[4], bb[2], bb[5], vPlate));

            // 3. raised name, centred along the axis below the middle of the machine
            var textSk = RightPlaneSketch(doc, sm =>
            {
                var st = (SketchText)doc.InsertSketchText(-0.5 * wText / 1000, textBase / 1000, 0, m.Text, (int)swTextJustification_e.swTextJustificationLeft, 0, 0, 100, 100);
                if (st != null)
                {
                    var tf = (TextFormat)st.GetTextFormat();
                    tf.TypeFaceName = "Arial Black"; tf.Bold = false; tf.CharHeight = CapMm / 1000;
                    st.SetTextFormat(false, tf);
                }
            });
            Feature txt = null; double vText = 0; var gain = new double[2];
            for (int attempt = 0; attempt < 3; attempt++)
            {
                bool dir = attempt == 2 ? gain[0] >= gain[1] : attempt == 0;
                doc.ClearSelection2(true); textSk.Select2(false, 0);
                txt = doc.FeatureManager.FeatureExtrusion3(true, false, dir, (int)swEndConditions_e.swEndCondBlind, 0, RaiseMm / 1000, 0,
                    false, false, false, false, 0, 0, false, false, false, false, true, true, true, (int)swStartConditions_e.swStartSketchPlane, 0, false);
                bb = Box(part, out n); vText = Vol(doc);
                if (attempt < 2) { gain[attempt] = txt != null && n == 1 ? vText - vPlate : -1; if (txt != null) doc.EditUndo2(1); txt = null; }
                else if (txt == null || n != 1 || vText - vPlate < 5) txt = null;
            }
            bb = Box(part, out n);
            log.AppendLine(string.Format(Inv, "  name '{0}': {1} | text width {2:0.0} mm, base y {3:0.00}, top y {4:0.00} (relief |y| under text <= {5:0.00}) | bodies={6} faults={7}",
                m.Text, txt == null ? "FAILED" : "ok", wText, textBase, textTop, yUnder, n, Faults(part)));
            log.AppendLine(string.Format(Inv, "  plaque: box x {0:0.00}..{1:0.00} y {2:0.00}..{3:0.00} z {4:0.00}..{5:0.00} mm | volume {6:0.0} mm3 (+{7:0.0} letters)",
                bb[0], bb[3], bb[1], bb[4], bb[2], bb[5], vText, vText - vPlate));

            doc.ClearSelection2(true);
            doc.Extension.SaveAs(Path.Combine(mm, "step_named", m.Tag + "_plaque_named_1to100.step"), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref err, ref warn);
            doc.Extension.SaveAs(Path.Combine(mm, "stl_named", m.Tag + "_plaque_named_1to100.stl"), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref err, ref warn);
            // hide the loft section planes and sketches in the renders
            doc.Extension.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swDisplayPlanes, (int)swUserPreferenceOption_e.swDetailingNoOptionSpecified, false);
            doc.Extension.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swDisplaySketches, (int)swUserPreferenceOption_e.swDetailingNoOptionSpecified, false);
            doc.ShowNamedView2("*Right", -1); doc.ViewZoomtofit2();
            doc.Extension.SaveAs(Path.Combine(mm, "verification", m.Tag + "_named_topview.png"), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref err, ref warn);
            doc.ShowNamedView2("*Isometric", -1); doc.ViewZoomtofit2();
            doc.Extension.SaveAs(Path.Combine(mm, "verification", m.Tag + "_named_isoview.png"), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref err, ref warn);
            sw.CloseDoc(doc.GetTitle());
        }
        sw.ExitApp();
        return log.ToString();
    }
}
