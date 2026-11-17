// WHAM (Wisconsin HTS Axisymmetric Mirror) plasma at 1:100, built in SolidWorks from the traced profile.
//  1. step/WHAM_plasma_1to100.step + stl/WHAM_plasma_1to100.stl: the full solid of revolution, mirror axis along X.
//  2. step_named/WHAM_plaque_named_1to100.step + stl_named/...: a printable plaque. The 1:100 plasma is only
//     ~1.6 mm across at the centre and ~0.4 mm at the mirror throats, too thin to print or letter as a free
//     body, so the plaque carries the same 1:100 shape as a half-body relief on a 2 mm base, with "WHAM" raised beside it.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

public static class WhamSw
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    public const double PlateMm = 2.0, RaiseMm = 0.8, CapMm = 5.0;
    public const double PlateX = 24.0, PlateYLo = -11.0, PlateYHi = 4.5, TextBaseY = -9.3;

    static double[] Pt(double[] mm, double sx) { return new[] { sx * mm[0] / 1000, mm[1] / 1000, 0 }; }

    // Closed half profile (r >= 0) on the Front plane: x = position along the mirror axis, y = radius.
    static void ProfileSketch(ModelDoc2 doc, WhamTrace.Shape s)
    {
        var sm = doc.SketchManager;
        doc.ClearSelection2(true);
        doc.Extension.SelectByID2("Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
        sm.InsertSketch(true);
        sm.AddToDB = true;
        // core: one spline from the left throat to the right throat
        var core = new List<double>();
        for (int i = s.Core.Count - 1; i >= 1; i--) core.AddRange(Pt(WhamTrace.Mm(s.Core[i]), -1));
        for (int i = 0; i < s.Core.Count; i++) core.AddRange(Pt(WhamTrace.Mm(s.Core[i]), +1));
        sm.CreateSpline2(core.ToArray(), false);
        foreach (int sx in new[] { -1, 1 })
        {
            var u1 = WhamTrace.Mm(new[] { s.U1, s.Rw }); var u2 = WhamTrace.Mm(new[] { s.U2, s.Rw });
            var a = Pt(u1, sx); var b = Pt(u2, sx);
            sm.CreateLine(a[0], a[1], 0, b[0], b[1], 0);                                   // throat (bore wall)
            var fan = new List<double>();
            foreach (var q in s.Fan) fan.AddRange(Pt(WhamTrace.Mm(q), sx));
            fan.AddRange(Pt(WhamTrace.Mm(s.Rim), sx));
            sm.CreateSpline2(fan.ToArray(), false);                                       // expander fan
            var rim = Pt(WhamTrace.Mm(s.Rim), sx); var knee = Pt(WhamTrace.Mm(s.Knee), sx); var apex = Pt(WhamTrace.Mm(s.Apex), sx);
            sm.CreateLine(rim[0], rim[1], 0, knee[0], knee[1], 0);                         // end wall, conical part
            sm.CreateLine(knee[0], knee[1], 0, apex[0], apex[1], 0);                       // end wall, flat centre
        }
        double ua = WhamTrace.Mm(s.Apex)[0] / 1000;
        sm.CreateLine(-ua, 0, 0, ua, 0, 0);                                                 // along the axis
        sm.CreateCenterLine(-ua - 0.003, 0, 0, -ua - 0.001, 0, 0);                         // revolve axis (collinear)
        sm.AddToDB = false;
        sm.InsertSketch(true);
    }

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

    static Feature Revolve(ModelDoc2 doc, double angle)
    {
        var sk = (Feature)doc.FeatureByPositionReverse(0);
        doc.ClearSelection2(true); sk.Select2(false, 0);
        return doc.FeatureManager.FeatureRevolve2(true, true, false, false, false, false, 0, 0, angle, 0,
            false, false, 0, 0, 0, 0, 0, true, true, true);
    }

    public static string Run(string root, bool visible)
    {
        string md = Path.Combine(root, "tokamak_models");
        string png = Path.Combine(root, "reference_figures", "WHAM_fig.png");
        var log = new StringBuilder();
        WhamTrace.Prof prof; var s = WhamTrace.Fit(png, out prof);
        var closed = WhamTrace.ClosedMm(s);
        double vAnalytic = WhamTrace.VolumeMm3(closed);

        // CSV + verification overlay
        Directory.CreateDirectory(Path.Combine(md, "csv")); Directory.CreateDirectory(Path.Combine(md, "verification"));
        var csv = new StringBuilder("Z_m,R_m,Z_mm_1to100,R_mm_1to100\n");
        foreach (var q in closed) csv.AppendLine(string.Format(Inv, "{0:0.00000},{1:0.00000},{2:0.0000},{3:0.0000}", q[0] / 10, q[1] / 10, q[0], q[1]));
        File.WriteAllText(Path.Combine(md, "csv", "WHAM_boundary.csv"), csv.ToString());
        WhamTrace.Overlay(png, prof, WhamTrace.Outline(s, 1), Path.Combine(md, "verification", "WHAM_trace_vs_figure.png"));
        string svgPath = Path.Combine(md, "comparison_cross_sections.svg");
        if (File.Exists(svgPath)) WhamTrace.AddToSvg(svgPath, closed);
        var c0 = WhamTrace.Mm(s.Core[0]); var th = WhamTrace.Mm(new[] { s.U1, s.Rw }); var rim = WhamTrace.Mm(s.Rim); var ap = WhamTrace.Mm(s.Apex);
        log.AppendLine(string.Format(Inv, "WHAM profile (1:100 mm): centre r={0:0.000}, throat r={1:0.000} for |z| {2:0.00}..{3:0.00}, rim r={4:0.000} at |z|={5:0.00}, end |z|={6:0.00}; left/right end-wall offset {7:0} px = {8:0.0} cm real; analytic volume {9:0.0} mm3",
            c0[1], th[1], th[0], WhamTrace.Mm(new[] { s.U2, 0.0 })[0], rim[1], rim[0], ap[0], s.EndAsym, s.EndAsym / WhamTrace.PxPerM_Z * 100, vAnalytic));

        var sw = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
        sw.Visible = visible;
        sw.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swStepAP, 214);
        sw.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swSTLBinaryFormat, true);
        sw.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swSTLDontTranslateToPositive, true);
        sw.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swExportStlUnits, (int)swLengthUnit_e.swMM);
        sw.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swSTLQuality, (int)swSTLQuality_e.swSTLQuality_Custom);
        sw.SetUserPreferenceDoubleValue((int)swUserPreferenceDoubleValue_e.swSTLDeviation, 0.000005);
        sw.SetUserPreferenceDoubleValue((int)swUserPreferenceDoubleValue_e.swSTLAngleTolerance, 0.0349);
        string tmpl = sw.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplatePart);
        int err = 0, warn = 0, n;

        // 1. full solid of revolution
        {
            var doc = (ModelDoc2)sw.NewDocument(tmpl, 0, 0, 0); var part = (PartDoc)doc;
            ProfileSketch(doc, s);
            var f = Revolve(doc, 2 * Math.PI);
            var bb = Box(part, out n); double v = Vol(doc);
            log.AppendLine(string.Format(Inv, "full solid: revolve {0} | bodies={1} | box x {2:0.00}..{3:0.00} y {4:0.00}..{5:0.00} z {6:0.00}..{7:0.00} mm | volume {8:0.0} mm3 (analytic {9:0.0})",
                f == null ? "FAILED" : "ok", n, bb[0], bb[3], bb[1], bb[4], bb[2], bb[5], v, vAnalytic));
            doc.Extension.SaveAs(Path.Combine(md, "step", "WHAM_plasma_1to100.step"), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref err, ref warn);
            doc.Extension.SaveAs(Path.Combine(md, "stl", "WHAM_plasma_1to100.stl"), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref err, ref warn);
            sw.CloseDoc(doc.GetTitle());
        }

        // 2. plaque: half-body relief + base plate + raised name
        {
            var doc = (ModelDoc2)sw.NewDocument(tmpl, 0, 0, 0); var part = (PartDoc)doc;
            ProfileSketch(doc, s);
            var f = Revolve(doc, Math.PI);
            var bb = Box(part, out n); double vHalf = Vol(doc);
            int side = (bb[5] > -bb[2]) ? 1 : -1;     // which side of the Front plane the relief is on
            log.AppendLine(string.Format(Inv, "plaque relief: revolve 180 {0} | relief on {1}Z side | volume {2:0.0} mm3", f == null ? "FAILED" : "ok", side > 0 ? "+" : "-", vHalf));

            // base plate below the relief
            doc.ClearSelection2(true);
            doc.Extension.SelectByID2("Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
            doc.SketchManager.InsertSketch(true); doc.SketchManager.AddToDB = true;
            doc.SketchManager.CreateCornerRectangle(-PlateX / 1000, PlateYLo / 1000, 0, PlateX / 1000, PlateYHi / 1000, 0);
            doc.SketchManager.AddToDB = false; doc.SketchManager.InsertSketch(true);
            var plateSk = (Feature)doc.FeatureByPositionReverse(0);
            Feature plate = null;
            for (int attempt = 0; attempt < 2 && plate == null; attempt++)
            {
                doc.ClearSelection2(true); plateSk.Select2(false, 0);
                plate = doc.FeatureManager.FeatureExtrusion3(true, false, attempt == 0, (int)swEndConditions_e.swEndCondBlind, 0, PlateMm / 1000, 0,
                    false, false, false, false, 0, 0, false, false, false, false, true, true, true, (int)swStartConditions_e.swStartSketchPlane, 0, false);
                bb = Box(part, out n);
                bool ok = plate != null && n == 1 && (side > 0 ? Math.Abs(bb[2] + PlateMm) < 1e-3 : Math.Abs(bb[5] - PlateMm) < 1e-3);
                if (!ok && plate != null) { doc.EditUndo2(1); plate = null; }
            }
            double vPlate = Vol(doc);
            log.AppendLine(string.Format(Inv, "plaque base: {0} | bodies={1} | volume {2:0.0} mm3", plate == null ? "FAILED" : "ok", n, vPlate));

            // raised name on the plate, beside the relief
            doc.ClearSelection2(true);
            doc.Extension.SelectByID2("Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
            doc.SketchManager.InsertSketch(true); doc.SketchManager.AddToDB = true;
            // InsertSketchText anchors the text at its left end, so centre it using the font's advance widths
            double wText = 0; foreach (var w in Emboss.GlyphAdvances("WHAM", CapMm)) wText += w / 1.04;
            var st = (SketchText)doc.InsertSketchText(-0.5 * wText / 1000, TextBaseY / 1000, 0, "WHAM", (int)swTextJustification_e.swTextJustificationLeft, 0, 0, 100, 100);
            if (st != null)
            {
                var tf = (TextFormat)st.GetTextFormat();
                tf.TypeFaceName = "Arial Black"; tf.Bold = false; tf.CharHeight = CapMm / 1000;
                st.SetTextFormat(false, tf);
            }
            doc.SketchManager.AddToDB = false; doc.SketchManager.InsertSketch(true);
            var textSk = (Feature)doc.FeatureByPositionReverse(0);
            // extrude both ways in turn; the right one adds material (the wrong one goes into the plate and adds ~nothing)
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
            log.AppendLine(string.Format(Inv, "plaque name: {0} | bodies={1} | box x {2:0.00}..{3:0.00} y {4:0.00}..{5:0.00} z {6:0.00}..{7:0.00} mm | volume {8:0.0} mm3 (+{9:0.0} letters)",
                txt == null ? "FAILED" : "ok", n, bb[0], bb[3], bb[1], bb[4], bb[2], bb[5], vText, vText - vPlate));

            doc.ClearSelection2(true);
            doc.Extension.SaveAs(Path.Combine(md, "step_named", "WHAM_plaque_named_1to100.step"), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref err, ref warn);
            doc.Extension.SaveAs(Path.Combine(md, "stl_named", "WHAM_plaque_named_1to100.stl"), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref err, ref warn);
            doc.ShowNamedView2(side > 0 ? "*Front" : "*Back", -1); doc.ViewZoomtofit2();
            doc.Extension.SaveAs(Path.Combine(md, "verification", "WHAM_named_topview.png"), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref err, ref warn);
            doc.ShowNamedView2("*Isometric", -1); doc.ViewZoomtofit2();
            doc.Extension.SaveAs(Path.Combine(md, "verification", "WHAM_named_isoview.png"), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, ref err, ref warn);
            sw.CloseDoc(doc.GetTitle());
        }
        sw.ExitApp();
        return log.ToString();
    }
}
