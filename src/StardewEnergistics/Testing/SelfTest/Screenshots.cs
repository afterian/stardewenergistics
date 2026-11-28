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
/// next frame, after the game has drawn everything. Folder: SE_SELFTEST_SHOTS, else selftest-shots in the mod folder.
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

    private void OnRendered(object? sender, RenderedEventArgs e)
    {
        if (pending is not { } shot)
            return;
        pending = null;
        try
        {
            var device = Game1.graphics.GraphicsDevice;
            int w = device.PresentationParameters.BackBufferWidth, h = device.PresentationParameters.BackBufferHeight;
            var data = new Color[w * h];
            device.GetBackBufferData(data);
            for (int i = 0; i < data.Length; i++)
                data[i].A = 255;
            using var tex = new Texture2D(device, w, h);
            tex.SetData(data);
            Directory.CreateDirectory(Folder);
            using var stream = File.Create(Path.Combine(Folder, shot.File));
            tex.SaveAsPng(stream, w, h);
            Taken.Add(shot);
            monitor.Log($"[self-test] screenshot {shot.File}: {shot.Caption}", LogLevel.Debug);
        }
        catch (Exception ex)
        {
            monitor.Log($"[self-test] screenshot failed: {ex.Message}", LogLevel.Warn);
        }
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
