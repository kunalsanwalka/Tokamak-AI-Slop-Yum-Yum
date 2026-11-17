// Small image helpers used to calibrate figure axes and digitise plasma boundaries.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text;

public static class ImgTool
{
    public static bool Dark(Color c, int t) { return c.R < t && c.G < t && c.B < t; }

    // Long dark horizontal / vertical runs (axis lines, frames).
    public static string Lines(string path, int minLen, int thr)
    {
        var b = new Bitmap(path); var sb = new StringBuilder();
        for (int y = 0; y < b.Height; y++)
        {
            int run = 0;
            for (int x = 0; x <= b.Width; x++)
            {
                bool d = x < b.Width && Dark(b.GetPixel(x, y), thr);
                if (d) run++; else { if (run >= minLen) sb.AppendLine("H y=" + y + " x=" + (x - run) + ".." + (x - 1)); run = 0; }
            }
        }
        for (int x = 0; x < b.Width; x++)
        {
            int run = 0;
            for (int y = 0; y <= b.Height; y++)
            {
                bool d = y < b.Height && Dark(b.GetPixel(x, y), thr);
                if (d) run++; else { if (run >= minLen) sb.AppendLine("V x=" + x + " y=" + (y - run) + ".." + (y - 1)); run = 0; }
            }
        }
        b.Dispose(); return sb.ToString();
    }

    // Tick marks on a horizontal axis line at row y: columns where the pixels at rows y+off1..y+off2 are all dark.
    public static string TicksH(string path, int y, int off1, int off2, int x0, int x1, int thr)
    {
        var b = new Bitmap(path); var cols = new List<int>();
        for (int x = x0; x <= x1; x++)
        {
            bool all = true;
            for (int yy = Math.Min(y + off1, y + off2); yy <= Math.Max(y + off1, y + off2); yy++) if (!Dark(b.GetPixel(x, yy), thr)) { all = false; break; }
            if (all) cols.Add(x);
        }
        b.Dispose(); return string.Join(",", cols);
    }

    public static string TicksV(string path, int x, int off1, int off2, int y0, int y1, int thr)
    {
        var b = new Bitmap(path); var rows = new List<int>();
        for (int y = y0; y <= y1; y++)
        {
            bool all = true;
            for (int xx = Math.Min(x + off1, x + off2); xx <= Math.Max(x + off1, x + off2); xx++) if (!Dark(b.GetPixel(xx, y), thr)) { all = false; break; }
            if (all) rows.Add(y);
        }
        b.Dispose(); return string.Join(",", rows);
    }

    // Crop and enlarge a region (nearest neighbour) with a light pixel grid every 'grid' source pixels.
    public static void Crop(string path, int x0, int y0, int x1, int y1, int scale, int grid, string outPath)
    {
        var b = new Bitmap(path);
        var o = new Bitmap((x1 - x0) * scale, (y1 - y0) * scale);
        using (var g = Graphics.FromImage(o))
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(b, new Rectangle(0, 0, o.Width, o.Height), new Rectangle(x0, y0, x1 - x0, y1 - y0), GraphicsUnit.Pixel);
            if (grid > 0)
            {
                var pen = new Pen(Color.FromArgb(90, 0, 160, 255));
                var f = new Font("Arial", 9);
                for (int x = ((x0 + grid - 1) / grid) * grid; x < x1; x += grid) { g.DrawLine(pen, (x - x0) * scale, 0, (x - x0) * scale, o.Height); g.DrawString(x.ToString(), f, Brushes.Blue, (x - x0) * scale + 1, 1); }
                for (int y = ((y0 + grid - 1) / grid) * grid; y < y1; y += grid) { g.DrawLine(pen, 0, (y - y0) * scale, o.Width, (y - y0) * scale); g.DrawString(y.ToString(), f, Brushes.Blue, 1, (y - y0) * scale + 1); }
            }
        }
        o.Save(outPath, ImageFormat.Png); o.Dispose(); b.Dispose();
    }

    public static string Px(string path, int x, int y)
    {
        var b = new Bitmap(path); var c = b.GetPixel(x, y); b.Dispose();
        return c.R + "," + c.G + "," + c.B;
    }
}
