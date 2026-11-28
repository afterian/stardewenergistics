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
/// Saves what's on screen (world, HUD and menus) as PNGs for the showcase run. A request is taken over the
/// next two frames (see <see cref="OnRendered"/>). Folder: SE_SELFTEST_SHOTS, else selftest-shots in the mod folder.
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
        helper.Events.Display.Rendering += OnRenderingNextFrame;
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
    /// The game draws the world into one render target and the HUD and menus into another (uiScreen), and
    /// only puts them together on the back buffer after mods' Rendered event. So the picture is taken in two
    /// halves: the world at Rendered (from the back buffer or the game's screen target), and the UI layer at
    /// the start of the next frame, before anything is drawn over it. Then the UI is laid over the world.
    /// </summary>
    private void OnRendered(object? sender, RenderedEventArgs e)
    {
        if (pending is not { } shot || world != null)
            return;
        try
        {
            var device = Game1.graphics.GraphicsDevice;
            int w = device.PresentationParameters.BackBufferWidth, h = device.PresentationParameters.BackBufferHeight;
            var screen = Game1.game1.screen;
            if (screen != null && !screen.IsDisposed && Game1.game1.ShouldDrawOnBuffer())
                world = (Grab(screen), screen.Width, screen.Height, false);
            else
            {
                var data = new Color[w * h];
                device.GetBackBufferData(data);
                world = (data, w, h, true);
            }
        }
        catch (Exception ex)
        {
            pending = null;
            world = null;
            monitor.Log($"[self-test] screenshot failed (world): {ex}", LogLevel.Warn);
        }
    }

    /// <summary>The world half, taken at Rendered: pixels, size, and whether it came from the back buffer.</summary>
    private (Color[] Data, int W, int H, bool BackBuffer)? world;

    private void OnRenderingNextFrame(object? sender, RenderingEventArgs e)
    {
        if (pending is not { } shot || world is not { } w)
            return;
        pending = null;
        world = null;
        try
        {
            var device = Game1.graphics.GraphicsDevice;
            int outW = device.PresentationParameters.BackBufferWidth, outH = device.PresentationParameters.BackBufferHeight;
            var result = Scale(w.Data, w.W, w.H, outW, outH);
            var ui = Game1.game1.uiScreen;
            bool withUi = false;
            if (ui != null && !ui.IsDisposed)
            {
                var layer = Scale(Grab(ui), ui.Width, ui.Height, outW, outH);
                for (int i = 0; i < result.Length; i++)
                {
                    // Premultiplied alpha, as the game draws it: ui + world * (1 - ui.a).
                    var u = layer[i];
                    int keep = 255 - u.A;
                    var b = result[i];
                    result[i] = new Color(
                        Math.Min(255, u.R + b.R * keep / 255),
                        Math.Min(255, u.G + b.G * keep / 255),
                        Math.Min(255, u.B + b.B * keep / 255),
                        255);
                }
                withUi = true;
            }
            for (int i = 0; i < result.Length; i++)
                result[i].A = 255;
            using var tex = new Texture2D(device, outW, outH);
            tex.SetData(result);
            Directory.CreateDirectory(Folder);
            using var stream = File.Create(Path.Combine(Folder, shot.File));
            tex.SaveAsPng(stream, outW, outH);
            Taken.Add(shot);
            monitor.Log($"[self-test] screenshot {shot.File} ({outW}x{outH}, world {(w.BackBuffer ? "back buffer" : "screen target")}, {(withUi ? "with" : "without")} UI layer): {shot.Caption}", LogLevel.Debug);
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
