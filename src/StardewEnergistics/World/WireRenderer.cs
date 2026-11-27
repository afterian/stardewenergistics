using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewEnergistics.Content;
using StardewEnergistics.Core.Wires;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;

namespace StardewEnergistics.World;

/// <summary>
/// Draws the wire layer on the floor: after the location's floor decorations (paths/flooring) and
/// before objects and players, so wire sits under everything. Tiles that are covered (wrench) or under
/// flooring are hidden, except in Network View, which also uses the glow sprites.
/// </summary>
internal sealed class WireRenderer
{
    private static WireRenderer? instance;

    private readonly WireLayerStore store;
    private readonly TextureRegistry textures;
    private readonly IMonitor monitor;
    private bool failedOnce;

    public WireRenderer(WireLayerStore store, TextureRegistry textures, IMonitor monitor)
    {
        this.store = store;
        this.textures = textures;
        this.monitor = monitor;
    }

    private readonly PerScreen<bool> networkView = new();

    /// <summary>Set by the Network View key or by holding the wrench (per split-screen player).</summary>
    public bool NetworkView
    {
        get => networkView.Value;
        set => networkView.Value = value;
    }

    public void Patch(Harmony harmony)
    {
        instance = this;
        harmony.Patch(
            original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.drawFloorDecorations)),
            postfix: new HarmonyMethod(typeof(WireRenderer), nameof(AfterDrawFloorDecorations)));
    }

    private static void AfterDrawFloorDecorations(GameLocation __instance, SpriteBatch b)
    {
        var self = instance;
        if (self == null)
            return;
        try
        {
            self.Draw(__instance, b);
        }
        catch (Exception ex)
        {
            if (!self.failedOnce)
            {
                self.failedOnce = true;
                self.monitor.Log($"Wire drawing failed: {ex}", LogLevel.Error);
            }
        }
    }

    private void Draw(GameLocation location, SpriteBatch b)
    {
        var layer = store.Find(location);
        if (layer == null || layer.Count == 0)
            return;

        var copper = textures.Get(NetworkView ? "wire-tiles-glow.png" : "wire-tiles.png");
        var heavy = textures.Get("heavy-cable-tiles.png");

        var vp = Game1.viewport;
        int minX = vp.X / 64 - 1, minY = vp.Y / 64 - 1;
        int maxX = (vp.X + vp.Width) / 64 + 1, maxY = (vp.Y + vp.Height) / 64 + 1;

        foreach (var (pos, tile) in layer.Tiles)
        {
            if (pos.X < minX || pos.X > maxX || pos.Y < minY || pos.Y > maxY)
                continue;

            bool hidden = tile.Covered || HasFlooring(location, pos.X, pos.Y);
            if (hidden && !NetworkView)
                continue;

            int frame = layer.MaskAt(pos);
            var tex = tile.Kind == WireKind.Heavy ? heavy : copper;
            var color = tile.Kind == WireKind.Heavy ? CableColor(tile.Color) : Color.White;
            if (hidden)
                color *= 0.55f;

            b.Draw(
                tex,
                Game1.GlobalToLocal(Game1.viewport, new Vector2(pos.X * 64, pos.Y * 64)),
                new Rectangle(frame * 16, 0, 16, 16),
                color,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                1.5E-08f);
        }
    }

    private static bool HasFlooring(GameLocation location, int x, int y) =>
        location.terrainFeatures.TryGetValue(new Vector2(x, y), out var feature) && feature is Flooring;

    /// <summary>0 = unpainted (light grey art as-is); 1..19 = Stardew's chest colours.</summary>
    public static Color CableColor(int color) => color <= 0 ? Color.White : DiscreteColorPicker.getColorFromSelection(color);
}
