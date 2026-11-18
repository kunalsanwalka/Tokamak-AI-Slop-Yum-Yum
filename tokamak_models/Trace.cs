// Digitises a plasma boundary (LCFS / separatrix) from a published figure by radial ray casting.
// Pixel -> metres: R = Ax + Bx*x, Z = Az + Bz*y (calibrated from the figure's axis ticks/labels).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text;

public class Fig
{
    public string Name, Path, Pred, Source;
    public double Ax, Bx, Az, Bz;          // calibration
    public double Cx, Cy;                  // ray origin (px), inside the plasma
    public double YMin = -1e9, YMax = 1e9; // keep-out beyond the X-points (px rows); legs live outside
    public double XMin = -1e9, XMax = 1e9; // keep rays inside the relevant panel
    public int Thick = 0;                  // min count of line pixels in a 7x7 window (rejects dotted flux surfaces)
    public double RMinPx = 10;             // ignore hits closer than this to the origin (e.g. magnetic-axis dot)
    public double HalfWidthPx = -1;        // >0: boundary = first hit + this (when the line touches other features); else run midpoint
    public List<double[]> MaskSegs = new List<double[]>(); // x0,y0,x1,y1,halfwidth : annotation arrows to ignore
    public List<double[]> XHints = new List<double[]>();   // approximate X-point pixels
    public bool XExact = false;            // true: XHints are the known X-points (e.g. printed by the equilibrium code), not refined
    public int NthRun = 0;                 // >0: figure draws nested flux surfaces; boundary = inner edge of the Nth line crossed + HalfWidthPx
    public string BridgePred = null;       // colour (e.g. overlaid dashed contours / labels) that may interrupt a line without ending it
    public double R(double x) { return Ax + Bx * x; }
    public double Z(double y) { return Az + Bz * y; }
}

public static class Trace
{
    static bool IsPred(string p, Color c)
    {
        switch (p)
        {
            case "black": return c.R < 100 && c.G < 100 && c.B < 100;
            case "red": return c.R > 150 && c.G < 110 && c.B < 110 && c.R - c.G > 70;
            case "purered": return c.R > 200 && c.G < 70 && c.B < 70;
            case "pink": return c.R > 190 && c.G < 75 && c.B > 95 && c.B < 185; // core (230,30,130); excludes red line's anti-aliasing
            case "cyan": return c.G > 170 && c.B > 170 && c.R < 170;
            case "brown": return c.R > 140 && c.R - c.G > 50 && c.R - c.B > 60;   // dashed ripple contours / labels (~(190,80,50))
            case "viridis0": return c.R < 130 && c.G < 60 && c.B > 60 && c.B < 150 && c.B - c.G > 50; // matplotlib viridis low end (68,1,84)
        }
        return false;
    }

    static double SegDist(double px, double py, double[] s)
    {
        double dx = s[2] - s[0], dy = s[3] - s[1];
        double t = Math.Max(0, Math.Min(1, ((px - s[0]) * dx + (py - s[1]) * dy) / (dx * dx + dy * dy)));
        double qx = s[0] + t * dx - px, qy = s[1] + t * dy - py;
        return Math.Sqrt(qx * qx + qy * qy);
    }

    // Returns boundary points (R,Z in metres), CCW, one per ray that found the line.
    public static List<double[]> Run(Fig f, double stepDeg)
    {
        var bmp = new Bitmap(f.Path);
        int W = bmp.Width, H = bmp.Height;
        var px = new Color[W, H];
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) px[x, y] = bmp.GetPixel(x, y);
        bmp.Dispose();
        Func<int, int, bool> ok = (x, y) =>
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return false;
            if (y < f.YMin || y > f.YMax || x < f.XMin || x > f.XMax) return false;
            foreach (var s in f.MaskSegs) if (SegDist(x, y, s) <= s[4]) return false;
            return IsPred(f.Pred, px[x, y]);
        };
        Func<int, int, bool> thick = (x, y) =>
        {
            if (f.Thick <= 0) return true;
            int n = 0;
            for (int j = -3; j <= 3; j++) for (int i = -3; i <= 3; i++)
                { int xx = x + i, yy = y + j; if (xx >= 0 && yy >= 0 && xx < W && yy < H && IsPred(f.Pred, px[xx, yy])) n++; }
            return n >= f.Thick;
        };
        var raw = new List<double[]>(); // angle(rad, in R-Z space), rpx, xpx, ypx
        var nthStarts = new List<KeyValuePair<double, List<double>>>();
        int nr = (int)Math.Round(360 / stepDeg);
        for (int k = 0; k < nr; k++)
        {
            double th = 2 * Math.PI * k / nr;
            // direction in pixel space: +R -> sign(Bx) x, +Z -> sign(Bz) y
            double dx = Math.Cos(th) * Math.Sign(f.Bx), dy = Math.Sin(th) * Math.Sign(f.Bz);
            double hit = -1, end = -1;
            if (f.NthRun > 0)
            {
                // list every completed line crossing outward (run starts); a run ends after >0.75 px of non-line, non-bridge pixels
                var starts = new List<double>(); double st = -1, last = -1; bool inRun = false;
                for (double r = f.RMinPx; r < 2000; r += 0.25)
                {
                    int x = (int)Math.Round(f.Cx + r * dx), y = (int)Math.Round(f.Cy + r * dy);
                    if (x < 0 || y < 0 || x >= W || y >= H || y < f.YMin || y > f.YMax || x < f.XMin || x > f.XMax) break;
                    bool line = IsPred(f.Pred, px[x, y]);
                    bool bridge = f.BridgePred != null && IsPred(f.BridgePred, px[x, y]);
                    if (line) { if (!inRun) { inRun = true; st = r; } last = r; }
                    else if (inRun && !bridge && r - last > 0.75) { inRun = false; starts.Add(st); }
                }
                nthStarts.Add(new KeyValuePair<double, List<double>>(th, starts));
                continue;
            }
            for (double r = f.RMinPx; r < 2000; r += 0.25)
            {
                int x = (int)Math.Round(f.Cx + r * dx), y = (int)Math.Round(f.Cy + r * dy);
                if (x < 0 || y < 0 || x >= W || y >= H) break;
                if (hit < 0) { if (ok(x, y) && thick(x, y)) hit = r; }
                else
                {
                    // continue through the line; tolerate up to 4 px of non-line (e.g. black dots drawn on it)
                    if (ok(x, y)) end = r;
                    else if (r - Math.Max(end, hit) > 4) break;
                }
            }
            if (hit < 0) continue;
            if (end < hit) end = hit;
            double rm = f.HalfWidthPx > 0 ? hit + f.HalfWidthPx : 0.5 * (hit + end);
            double xp = f.Cx + rm * dx, yp = f.Cy + rm * dy;
            raw.Add(new[] { th, rm, xp, yp });
        }
        if (f.NthRun > 0)
        {
            // first guess: the Nth line on each ray. A line hidden under an overlaid contour/label shifts the count by
            // one over a stretch of rays, so take the running median of the guesses (+-5 deg) as the reference and pick,
            // on every ray, the line closest to it (accepted within 2.5 px).
            int n = nthStarts.Count; var guess = new double[n];
            for (int i = 0; i < n; i++) { var s = nthStarts[i].Value; guess[i] = s.Count >= f.NthRun ? s[f.NthRun - 1] : double.NaN; }
            int hw = (int)Math.Round(5 / stepDeg);
            for (int i = 0; i < n; i++)
            {
                var v = new List<double>();
                for (int j = -hw; j <= hw; j++) { double g = guess[(i + j + n) % n]; if (!double.IsNaN(g)) v.Add(g); }
                if (v.Count < hw) continue;
                v.Sort(); double refR = v[v.Count / 2];
                double best = double.NaN, bd = 1e9;
                foreach (var s in nthStarts[i].Value) if (Math.Abs(s - refR) < bd) { bd = Math.Abs(s - refR); best = s; }
                if (double.IsNaN(best) || bd > 2.5) continue;
                double th = nthStarts[i].Key, ddx = Math.Cos(th) * Math.Sign(f.Bx), ddy = Math.Sin(th) * Math.Sign(f.Bz);
                double rm = best + f.HalfWidthPx;
                raw.Add(new[] { th, rm, f.Cx + rm * ddx, f.Cy + rm * ddy });
            }
        }
        // outlier rejection: (1) vs median of +-3 neighbours, (2) vs median of +-15 neighbours with a looser bound
        var pass1 = Filter(raw, 3, 0.04);
        var pass2 = Filter(pass1, 15, 0.08);
        // nested-surface figures: a hidden line shifts the count by one line (~5-30 px); reject anything > 3 px off
        if (f.NthRun > 0) pass2 = Filter(pass2, 8, 0.012);
        var outPts = new List<double[]>();
        foreach (var p in pass2) outPts.Add(new[] { f.R(p[2]), f.Z(p[3]), p[2], p[3] });
        return outPts;
    }

    static List<double[]> Filter(List<double[]> raw, int half, double frac)
    {
        var o = new List<double[]>();
        for (int i = 0; i < raw.Count; i++)
        {
            var rs = new List<double>();
            for (int j = -half; j <= half; j++) rs.Add(raw[(i + j + raw.Count) % raw.Count][1]);
            rs.Sort(); double med = rs[half];
            if (Math.Abs(raw[i][1] - med) <= Math.Max(3, frac * med)) o.Add(raw[i]);
        }
        return o;
    }

    // Overlay the traced points (and X-points) on the figure for visual verification.
    public static void Overlay(Fig f, List<double[]> pts, List<double[]> xpts, List<double[]> fitted, string outPath, int scale)
    {
        var b = new Bitmap(f.Path);
        var o = new Bitmap(b.Width * scale, b.Height * scale);
        using (var g = Graphics.FromImage(o))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.DrawImage(b, 0, 0, o.Width, o.Height);
            var br = new SolidBrush(Color.FromArgb(255, 0, 200, 0));
            foreach (var p in pts) g.FillRectangle(br, (float)(p[2] * scale - 1), (float)(p[3] * scale - 1), 2, 2);
            if (fitted != null)
            {
                var pen = new Pen(Color.FromArgb(220, 0, 0, 0), 1);
                for (int i = 0; i < fitted.Count; i++)
                {
                    var a = fitted[i]; var c = fitted[(i + 1) % fitted.Count];
                    g.DrawLine(pen, (float)((a[0] - f.Ax) / f.Bx * scale), (float)((a[1] - f.Az) / f.Bz * scale), (float)((c[0] - f.Ax) / f.Bx * scale), (float)((c[1] - f.Az) / f.Bz * scale));
                }
            }
            if (xpts != null)
                foreach (var x in xpts)
                {
                    float xx = (float)((x[0] - f.Ax) / f.Bx * scale), yy = (float)((x[1] - f.Az) / f.Bz * scale);
                    g.DrawEllipse(new Pen(Color.Blue, 2), xx - 6, yy - 6, 12, 12);
                }
        }
        o.Save(outPath, ImageFormat.Png); o.Dispose(); b.Dispose();
    }
}
