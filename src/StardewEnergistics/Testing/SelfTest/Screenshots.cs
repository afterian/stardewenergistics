using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewEnergistics.Testing.SelfTest;

/// <summary>
/// Saves what's on screen (world, HUD and menus) as PNGs for the showcase run. A request is taken on the
/// next frame (see <see cref="OnRendered"/>). Folder: SE_SELFTEST_SHOTS, else selftest-shots in the mod folder.
/// </summary>
internal sealed class Screenshots
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private (string File, string Caption)? pending;

    public Screenshots(IModHelper helper, IMonitor monitor)
    {
        this.helper = helper;
        this.monitor = monitor;
        Folder = Environment.GetEnvironmentVariable("SE_SELFTEST_SHOTS") is { Length: > 0 } p ? p : Path.Combine(helper.DirectoryPath, "selftest-shots");
        helper.Events.Display.Rendered += OnRendered;
    }

    public string Folder { get; }

    /// <summary>Shots taken so far, in order: file name and caption.</summary>
    public List<(string File, string Caption)> Taken { get; } = new();

    public bool Busy => pending != null;

    public void Request(string caption)
    {
        string file = $"{Taken.Count + 1:00}-{Slug(caption)}.png";
        pending = (file, caption);
    }

    private static string Slug(string s)
    {
        var chars = new List<char>();
        foreach (char ch in s.ToLowerInvariant())
            chars.Add(char.IsLetterOrDigit(ch) ? ch : '-');
        string slug = new string(chars.ToArray()).Trim('-');
        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");
        return slug.Length > 40 ? slug[..40] : slug;
    }

    /// <summary>
    /// The game draws the world into the back buffer (or its screen target) and the HUD and menus into a
    /// separate UI target (uiScreen), and only lays the UI over the world after mods' Rendered event. So at
    /// Rendered both layers are finished but not yet combined: read both and combine them here.
    /// </summary>
    private void OnRendered(object? sender, RenderedEventArgs e)
    {
        if (pending is not { } shot)
            return;
        pending = null;
        try
        {
            var device = Game1.graphics.GraphicsDevice;
            int outW = device.PresentationParameters.BackBufferWidth, outH = device.PresentationParameters.BackBufferHeight;
            Color[] result;
            string worldFrom;
            var screen = Game1.game1.screen;
            if (screen != null && !screen.IsDisposed && Game1.game1.ShouldDrawOnBuffer())
            {
                result = Scale(Grab(screen), screen.Width, screen.Height, outW, outH);
                worldFrom = $"screen target {screen.Width}x{screen.Height}";
            }
            else
            {
                result = new Color[outW * outH];
                device.GetBackBufferData(result);
                worldFrom = "back buffer";
            }

            var ui = Game1.game1.uiScreen;
            string uiFrom = "no UI target";
            if (ui != null && !ui.IsDisposed)
            {
                var raw = Grab(ui);
                int opaque = 0;
                foreach (var px in raw)
                    if (px.A > 0)
                        opaque++;
                uiFrom = $"UI target {ui.Width}x{ui.Height}, {opaque * 100L / Math.Max(1, raw.Length)}% drawn";
                var layer = Scale(raw, ui.Width, ui.Height, outW, outH);
                for (int i = 0; i < result.Length; i++)
                {
                    // Premultiplied alpha, as the game draws it: ui + world * (1 - ui.a).
                    var u = layer[i];
                    if (u.A == 0)
                        continue;
                    int keep = 255 - u.A;
                    var w = result[i];
                    result[i] = new Color(Math.Min(255, u.R + w.R * keep / 255), Math.Min(255, u.G + w.G * keep / 255), Math.Min(255, u.B + w.B * keep / 255), 255);
                }
            }
            for (int i = 0; i < result.Length; i++)
                result[i].A = 255;
            using var tex = new Texture2D(device, outW, outH);
            tex.SetData(result);
            Directory.CreateDirectory(Folder);
            using var stream = File.Create(Path.Combine(Folder, shot.File));
            tex.SaveAsPng(stream, outW, outH);
            Taken.Add(shot);
            monitor.Log($"[self-test] screenshot {shot.File} ({outW}x{outH}; world from {worldFrom}; {uiFrom}; menu {Game1.activeClickableMenu?.GetType().Name ?? "none"}): {shot.Caption}", LogLevel.Debug);
        }
        catch (Exception ex)
        {
            monitor.Log($"[self-test] screenshot failed: {ex}", LogLevel.Warn);
        }
    }

    private static Color[] Grab(RenderTarget2D target)
    {
        var data = new Color[target.Width * target.Height];
        target.GetData(data);
        return data;
    }

    /// <summary>Nearest-neighbour stretch to the window size (the game scales both layers to fill it).</summary>
    private static Color[] Scale(Color[] src, int w, int h, int outW, int outH)
    {
        if (w == outW && h == outH)
            return src;
        var dst = new Color[outW * outH];
        for (int y = 0; y < outH; y++)
        {
            int sy = Math.Min(h - 1, y * h / outH);
            for (int x = 0; x < outW; x++)
                dst[y * outW + x] = src[sy * w + Math.Min(w - 1, x * w / outW)];
        }
        return dst;
    }

    /// <summary>Writes the captions next to the pictures (index.json), in order.</summary>
    public void WriteIndex()
    {
        if (Taken.Count == 0)
            return;
        Directory.CreateDirectory(Folder);
        var list = new List<object>();
        foreach (var (file, caption) in Taken)
            list.Add(new { file, caption });
        File.WriteAllText(Path.Combine(Folder, "index.json"), System.Text.Json.JsonSerializer.Serialize(list, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }
}
