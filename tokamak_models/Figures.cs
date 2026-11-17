// Calibration and tracing settings for each reference figure (paths relative to the project folder).
using System.Collections.Generic;

public static class Figures
{
    public static List<Fig> All(string root)
    {
        var list = new List<Fig>();
        string d = root + "\\reference_figures\\";

        // EAST: Chen et al. 2019 Chin. Phys. B 28 015201, Fig. 1. Ticks: R 1.5/2/2.5/3 at x=275.5/367/459/551, Z 2..-2 at y=48.5..784.
        var east = new Fig { Name = "EAST", Path = d + "EAST_fig.png", Pred = "black", Source = "Chen et al. 2019, Chin. Phys. B 28 015201, Fig. 1",
            Bx = 1 / 183.67, Bz = -1 / 184.0 };
        east.Ax = 1.5 - 275.5 * east.Bx; east.Az = -416.5 * east.Bz;
        east.Cx = 339.8; east.Cy = 416.5; east.YMax = 568;
        east.MaskSegs.Add(new double[] { 352, 476, 480, 530, 6 });   // "magnetic surface" arrow inside the plasma
        east.MaskSegs.Add(new double[] { 354, 474, 398, 494, 13 });  // its arrowhead (tip ~(357,475))
        east.MaskSegs.Add(new double[] { 404, 345, 470, 300, 7 });   // "plasma boundary" arrow touching the LCFS
        east.XHints.Add(new double[] { 294.5, 565 });
        list.Add(east);

        // DIII-D: Jackson et al. 2009 Nucl. Fusion 49 115027, Fig. 1(b), 0.32 s (pink dashed). Ticks: R 1.5/2/2.5 at x=1001/1134.5/1268,
        // Z 0.5/0/-0.5/-1 at y=284.5/421/549.5/693.5 (unevenly spaced in the source; least-squares fit used).
        var d3d = new Fig { Name = "DIII-D", Path = d + "DIIID_fig.png", Pred = "pink", Source = "Jackson et al. 2009, Nucl. Fusion 49 115027, Fig. 1(b), t = 0.32 s",
            Bx = 1 / 267.0, Bz = -1 / 271.1 };
        d3d.Ax = 1.5 - 1001 * d3d.Bx; d3d.Az = -419.35 * d3d.Bz;
        d3d.Cx = 1067.75; d3d.Cy = 432.9; d3d.YMax = 722; d3d.XMin = 870; d3d.XMax = 1300;
        d3d.MaskSegs.Add(new double[] { 1215, 210, 1300, 210, 10 });  // legend swatch for 0.32 s
        d3d.XHints.Add(new double[] { 967, 719 });
        list.Add(d3d);

        // NSTX-U: Pajares et al. 2025 Fus. Eng. Des. 220 115302, panel (i) t = 325 ms. R ticks 0/1/2 at x=17/176/335.5.
        // No Z axis in the panel: equal aspect assumed, Z = 0 at the vertical centre of the machine drawing (y = 380).
        var nstx = new Fig { Name = "NSTX-U", Path = d + "NSTXU_fig.png", Pred = "cyan", Thick = 18, Source = "Pajares et al. 2025, Fus. Eng. Des. 220 115302, t = 325 ms",
            Bx = 1 / 159.25, Bz = -1 / 159.25 };
        nstx.Ax = -17 * nstx.Bx; nstx.Az = -380 * nstx.Bz;
        nstx.Cx = 160.3; nstx.Cy = 372; nstx.YMax = 534; nstx.HalfWidthPx = 2.5; // cyan line ~5 px wide
        nstx.XHints.Add(new double[] { 115, 531 });
        list.Add(nstx);

        // WEST: WEST machine capabilities 2024, page 1 figure. Label centres: R 1.5..3.5 at x=241..894, Z 1..-1 at y=25..679.
        var west = new Fig { Name = "WEST", Path = d + "WEST_fig.png", Pred = "red", Source = "WEST machine capabilities for 2024 (CEA), p. 1",
            Bx = 1 / 326.5, Bz = -1 / 326.75 };
        west.Ax = 1.5 - 241 * west.Bx; west.Az = -351.5 * west.Bz;
        west.Cx = 583.8; west.Cy = 351.5; west.YMax = 555; west.RMinPx = 8;
        west.XHints.Add(new double[] { 484, 551 });
        list.Add(west);

        // SPARC: Creely et al. 2020 J. Plasma Phys. 86 865860502, Fig. 2. Frame R 0..4 at x=324.5..906, Z 3..-3 at y=23..895.5.
        var sparc = new Fig { Name = "SPARC", Path = d + "SPARC_Creely2020_fig2.png", Pred = "purered", Source = "Creely et al. 2020, J. Plasma Phys. 86, 865860502, Fig. 2",
            Bx = 1 / 145.375, Bz = -1 / 145.375 };
        sparc.Ax = -324.5 * sparc.Bx; sparc.Az = 459.25 / 145.375;
        sparc.Cx = 600.7; sparc.Cy = 459.25; sparc.YMin = 292; sparc.YMax = 626;
        sparc.XHints.Add(new double[] { 548, 296 }); sparc.XHints.Add(new double[] { 548, 622 });
        list.Add(sparc);
        return list;
    }
}
