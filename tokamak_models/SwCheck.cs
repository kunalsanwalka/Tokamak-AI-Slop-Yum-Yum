// Imports STEP files into SolidWorks and reports bodies, faces, geometry-check faults and volume.
using System;
using System.IO;
using System.Text;
using SolidWorks.Interop.sldworks;

public static class SwCheck
{
    public static string Run(string stepDir)
    {
        var sb = new StringBuilder();
        var sw = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
        sw.Visible = false;
        foreach (var f in Directory.GetFiles(stepDir, "*.step"))
        {
            int err = 0;
            object imp = sw.GetImportFileData(f);
            var doc = (ModelDoc2)sw.LoadFile4(f, "r", imp, ref err);
            if (doc == null) { sb.AppendLine(Path.GetFileName(f) + ": FAILED to load, error " + err); continue; }
            var part = doc as PartDoc;
            object[] solids = part != null ? (object[])part.GetBodies2(0, false) : null;
            object[] sheets = part != null ? (object[])part.GetBodies2(1, false) : null;
            int faces = 0, faults = 0;
            if (solids != null)
                foreach (Body2 b in solids)
                {
                    faces += b.GetFaceCount();
                    var fe = b.Check3;
                    if (fe != null) faults += fe.Count;
                }
            double vol = -1;
            var mp = (MassProperty)doc.Extension.CreateMassProperty();
            if (mp != null) vol = mp.Volume * 1e9; // m^3 -> mm^3
            sb.AppendLine(string.Format("{0}: solid bodies={1} sheet bodies={2} faces={3} check faults={4} volume={5:0} mm3",
                Path.GetFileName(f), solids == null ? 0 : solids.Length, sheets == null ? 0 : sheets.Length, faces, faults, vol));
            sw.CloseDoc(doc.GetTitle());
        }
        sw.ExitApp();
        return sb.ToString();
    }
}
