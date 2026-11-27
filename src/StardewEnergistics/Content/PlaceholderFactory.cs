using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewEnergistics.Core.Art;
using StardewValley;

namespace StardewEnergistics.Content;

/// <summary>
/// Generates a stand-in texture of exactly the catalogue size until real art lands in artwork/exports/.
/// Boxes are tier-coloured with a dark outline and one dot per frame index, so states look different.
/// Wire tiles draw real connections, so auto-tiling can be tested with placeholders.
/// </summary>
internal static class PlaceholderFactory
{
    private static readonly Color Outline = new(40, 30, 30);
    private static readonly Color Teal = new(80, 220, 200);

    public static Texture2D Create(AssetSpec spec)
    {
        int w = spec.SheetWidth, h = spec.SheetHeight;
        var px = new Color[w * h];

        for (int f = 0; f < spec.Frames; f++)
        {
            var frame = new Canvas(px, w, f * spec.FrameWidth, 0, spec.FrameWidth, spec.FrameHeight);
            if (spec.Kind == AssetKind.WireTiles)
                DrawWire(frame, spec.Name, mask: f);
            else
                DrawBox(frame, spec, f);
        }

        var tex = new Texture2D(Game1.graphics.GraphicsDevice, w, h);
        tex.SetData(px);
        return tex;
    }

    private static void DrawBox(Canvas c, AssetSpec spec, int frame)
    {
        var fill = TierColor(spec.Name);
        if (spec.Kind is AssetKind.Placeable && frame > 0)
            fill = Color.Lerp(fill, Color.White, 0.15f * frame);

        // Inventory-sized things get a small margin; overlays sit in the top-right corner.
        int x0 = 1, y0 = 1, x1 = c.W - 2, y1 = c.H - 2;
        if (spec.Kind == AssetKind.Attachment)
        {
            x0 = c.W - 8;
            y1 = 7;
        }
        else if (spec.Kind == AssetKind.Placeable)
        {
            y0 = 4; // leave headroom like vanilla 16x32 machines
        }

        c.Rect(x0, y0, x1, y1, fill, Outline);

        // One teal dot per frame index, along the top edge.
        for (int i = 0; i < frame && x0 + 2 + i * 2 < x1; i++)
            c.Set(x0 + 2 + i * 2, y0 + 2, Teal);
    }

    private static void DrawWire(Canvas c, string name, int mask)
    {
        bool heavy = name.Contains("heavy");
        bool glow = name.Contains("glow");
        var color = heavy ? new Color(200, 200, 205) : glow ? Teal : new Color(196, 110, 50);
        int half = heavy ? 3 : 1;
        int cx = c.W / 2, cy = c.H / 2;

        c.Rect(cx - half - 1, cy - half - 1, cx + half, cy + half, color, Outline);
        if ((mask & 1) != 0) c.Rect(cx - half - 1, 0, cx + half, cy, color, Outline);            // N
        if ((mask & 2) != 0) c.Rect(cx, cy - half - 1, c.W - 1, cy + half, color, Outline);      // E
        if ((mask & 4) != 0) c.Rect(cx - half - 1, cy, cx + half, c.H - 1, color, Outline);      // S
        if ((mask & 8) != 0) c.Rect(0, cy - half - 1, cx, cy + half, color, Outline);            // W
        c.FillRect(cx - half, cy - half, cx + half - 1, cy + half - 1, color);                    // clean centre join
    }

    private static Color TierColor(string name)
    {
        if (name.Contains("prismatic")) return new Color(230, 120, 220);
        if (name.Contains("iridium") || name.Contains("advanced") || name.Contains("uplink")) return new Color(140, 80, 190);
        if (name.Contains("gold")) return new Color(235, 190, 60);
        if (name.Contains("iron") || name.Contains("coal")) return new Color(130, 130, 140);
        if (name.Contains("copper") || name.Contains("press") || name.Contains("wrench")) return new Color(196, 110, 50);
        if (name.Contains("rack") || name.Contains("core") || name.Contains("terminal") || name.Contains("port")) return new Color(90, 100, 110);
        return new Color(60, 160, 150);
    }

    /// <summary>A rectangular window into a pixel array.</summary>
    private readonly struct Canvas
    {
        private readonly Color[] px;
        private readonly int stride, ox, oy;

        public Canvas(Color[] px, int stride, int ox, int oy, int w, int h)
        {
            this.px = px;
            this.stride = stride;
            this.ox = ox;
            this.oy = oy;
            W = w;
            H = h;
        }

        public int W { get; }

        public int H { get; }

        public void Set(int x, int y, Color c)
        {
            if (x >= 0 && y >= 0 && x < W && y < H)
                px[(oy + y) * stride + ox + x] = c;
        }

        public void FillRect(int x0, int y0, int x1, int y1, Color c)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    Set(x, y, c);
        }

        /// <summary>Filled rectangle with a 1px outline.</summary>
        public void Rect(int x0, int y0, int x1, int y1, Color fill, Color outline)
        {
            FillRect(x0, y0, x1, y1, outline);
            FillRect(x0 + 1, y0 + 1, x1 - 1, y1 - 1, fill);
        }
    }
}
