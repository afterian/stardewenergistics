using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewEnergistics.Content;
using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.Networks;
using StardewEnergistics.Power;
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
    private readonly NetworkManager networks;
    private readonly PowerService power;
    private readonly ModConfig config;
    private readonly ConditionalWeakTable<LocationNetworks, Dictionary<TilePos, WireSignal>> routes = new();

    private readonly record struct WireSignal(Network Network, int Distance);

    public WireRenderer(WireLayerStore store, TextureRegistry textures, NetworkManager networks, PowerService power, ModConfig config, IMonitor monitor)
    {
        this.store = store;
        this.textures = textures;
        this.monitor = monitor;
        this.networks = networks;
        this.power = power;
        this.config = config;
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

        var copper = textures.Get("wire-tiles.png");
        var copperInspect = NetworkView ? textures.Get("wire-tiles-glow.png") : copper;
        var copperSignal = textures.GetOptional("copper-data-signal.png");
        var heavySignal = textures.GetOptional("heavy-data-signal.png");
        var loc = networks.For(location);
        var wireRoutes = routes.GetValue(loc, _ => TraceRoutes(loc, layer));
        var powered = new Dictionary<Network, bool>();
        int tick = (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 125) & 7;
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
            bool isHeavy = tile.Kind == WireKind.Heavy;
            bool isPowered = false;
            int distance = 0;
            if (wireRoutes.TryGetValue(pos, out var route))
            {
                if (!powered.TryGetValue(route.Network, out isPowered))
                    powered[route.Network] = isPowered = power.IsPowered(loc, route.Network);
                distance = route.Distance;
            }
            var tex = isHeavy ? heavy : (isPowered ? copperInspect : copper);
            var color = isHeavy ? CableColor(tile.Color) : Color.White;
            if (hidden)
                color *= 0.55f;

            var at = Game1.GlobalToLocal(Game1.viewport, new Vector2(pos.X * 64, pos.Y * 64));
            b.Draw(
                tex,
                at,
                new Rectangle(frame * 16, 0, 16, 16),
                color,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                1.5E-08f);
            var signal = isHeavy ? heavySignal : copperSignal;
            if (isPowered && signal != null)
            {
                int phase = config.AnimateDataCables ? (tick - distance) & 7 : 7;
                b.Draw(signal, at, new Rectangle((phase * 16 + frame) * 16, 0, 16, 16),
                    hidden ? Color.White * .55f : Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 2E-08f);
            }
        }
    }

    private static Dictionary<TilePos, WireSignal> TraceRoutes(LocationNetworks location, WireLayer layer)
    {
        var result = new Dictionary<TilePos, WireSignal>();
        foreach (var network in location.Graph.Networks)
        {
            if (network.Core is not { } core || !layer.Has(core.Pos))
                continue;
            var queue = new Queue<TilePos>();
            if (!result.TryAdd(core.Pos, new WireSignal(network, 0)))
                continue;
            queue.Enqueue(core.Pos);
            while (queue.TryDequeue(out var tile))
            {
                int nextDistance = result[tile].Distance + 1;
                foreach (var next in tile.Neighbours())
                    if (layer.Has(next) && result.TryAdd(next, new WireSignal(network, nextDistance)))
                        queue.Enqueue(next);
            }
        }
        return result;
    }

    private static bool HasFlooring(GameLocation location, int x, int y) =>
        location.terrainFeatures.TryGetValue(new Vector2(x, y), out var feature) && feature is Flooring;

    /// <summary>0 = Maru plum; 1..19 = Stardew's chest colours.</summary>
    public static Color CableColor(int color) => color <= 0 ? new Color(157, 119, 180) : DiscreteColorPicker.getColorFromSelection(color);
}
