using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewEnergistics.Content;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.Machines;
using StardewEnergistics.Networks;
using StardewEnergistics.Power;
using StardewEnergistics.Storage;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace StardewEnergistics.World;

/// <summary>
/// Draws attachment overlays (Chest, Machine, Shipping Link and Power Feed) on the objects they are
/// attached to, and the Network View status markers above network blocks and linked machines.
/// </summary>
internal sealed class OverlayRenderer
{
    private static OverlayRenderer? instance;

    private readonly TextureRegistry textures;
    private readonly NetworkManager networks;
    private readonly PowerService power;
    private readonly DiskStore disks;
    private readonly WireRenderer wires;
    private readonly IMonitor monitor;
    private bool failedOnce;

    /// <summary>Storage-full check per rack, refreshed about once a second.</summary>
    private readonly Dictionary<SObject, (int Tick, bool Full)> fullCache = new();

    // network-view-markers.png frame order.
    private const int Connected = 0, NoPower = 1, NotSetUp = 2, Full = 3, MissingInput = 4;

    public OverlayRenderer(TextureRegistry textures, NetworkManager networks, PowerService power, DiskStore disks, WireRenderer wires, IMonitor monitor)
    {
        this.textures = textures;
        this.networks = networks;
        this.power = power;
        this.disks = disks;
        this.wires = wires;
        this.monitor = monitor;
    }

    public void Patch(Harmony harmony)
    {
        instance = this;
        var args = new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) };
        var postfix = new HarmonyMethod(typeof(OverlayRenderer), nameof(AfterDraw));
        harmony.Patch(AccessTools.Method(typeof(SObject), nameof(SObject.draw), args), postfix: postfix);
        harmony.Patch(AccessTools.Method(typeof(Chest), nameof(Chest.draw), args), postfix: postfix);
    }

    private static void AfterDraw(SObject __instance, SpriteBatch spriteBatch, int x, int y, float alpha, MethodBase __originalMethod)
    {
        // Chest.draw may call the base method; draw once, from the most specific patch.
        if (__instance is Chest && __originalMethod.DeclaringType != typeof(Chest))
            return;

        var self = instance;
        if (self == null)
            return;
        try
        {
            self.Draw(__instance, spriteBatch, x, y, alpha);
        }
        catch (Exception ex)
        {
            if (!self.failedOnce)
            {
                self.failedOnce = true;
                self.monitor.Log($"Overlay drawing failed: {ex}", LogLevel.Error);
            }
        }
    }

    private void Draw(SObject obj, SpriteBatch b, int x, int y, float alpha)
    {
        // Big craftables are two tiles tall; overlays and markers sit on the upper tile.
        int top = obj.bigCraftable.Value ? y - 1 : y;
        float depth = Math.Max(0f, (y + 1) * 64 / 10000f + x / 50000f) + 0.0002f;

        var (file, frame) = AttachmentOf(obj);
        if (file != null)
        {
            var pos = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, top * 64));
            b.Draw(textures.Get(file), pos, new Rectangle(frame * 16, 0, 16, 16), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, depth);
        }

        if (wires.NetworkView && MarkerOf(obj, x, y) is { } marker)
        {
            var pos = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 14, top * 64 - 40));
            b.Draw(textures.Get("network-view-markers.png"), pos, new Rectangle(marker * 9, 0, 9, 9), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.99f);
        }
    }

    private static (string? File, int Frame) AttachmentOf(SObject obj)
    {
        var data = obj.modData;
        if (data.ContainsKey(Ids.Data.ChestLink))
            return ("chest-link.png", 0);
        if (data.ContainsKey(Ids.Data.ShippingLink))
            return ("shipping-link.png", obj is Chest bin && bin.Items.Any(i => i != null) ? 1 : 0);
        if (data.ContainsKey(Ids.Data.MachineLink))
            return ("machine-link.png", Math.Clamp(LinkState(obj), 0, 3));
        if (data.ContainsKey(Ids.Data.PowerFeed))
            return ("power-feed.png", obj.heldObject.Value != null ? 1 : 0);
        return (null, 0);
    }

    private static int LinkState(SObject obj) =>
        obj.modData.TryGetValue(Ids.Data.LinkState, out string? raw) && int.TryParse(raw, out int s) ? s : MachineLinkService.NotSetUp;

    private int? MarkerOf(SObject obj, int x, int y)
    {
        var kind = BlockIds.KindOf(obj);
        bool linked = obj.modData.ContainsKey(Ids.Data.MachineLink) || obj.modData.ContainsKey(Ids.Data.ShippingLink);
        if (kind is not (BlockKind.Core or BlockKind.Terminal or BlockKind.StorageRack) && !linked)
            return null;

        var location = Game1.currentLocation;
        if (location == null)
            return null;
        var loc = networks.For(location);
        var network = loc.NetworkAt(new Vector2(x, y));
        bool powered = network != null && power.IsPowered(loc, network);

        if (linked)
        {
            int state = LinkState(obj);
            if (state == MachineLinkService.NotSetUp)
                return NotSetUp;
            if (!powered)
                return NoPower;
            return state == MachineLinkService.MissingInput ? MissingInput : Connected;
        }

        if (!powered)
            return NoPower;
        if (kind == BlockKind.StorageRack && network != null && IsFull(loc, network, obj))
            return Full;
        return kind == BlockKind.Core ? Connected : null;
    }

    /// <summary>True when no disk on the network has room left (host only; farmhands don't hold disk contents).</summary>
    private bool IsFull(LocationNetworks loc, Network network, SObject rack)
    {
        if (!Context.IsMainPlayer)
            return false;
        int tick = Game1.ticks;
        if (fullCache.TryGetValue(rack, out var cached) && tick - cached.Tick < 60)
            return cached.Full;

        var contents = NetworkStorage.ForOne(loc, network, disks).Disks.Disks;
        bool full = contents.Count > 0 && contents.All(d => d.FreeItems <= 0 || (!d.Spec.UnlimitedKinds && d.KindsUsed >= d.Spec.MaxKinds));
        if (fullCache.Count > 500)
            fullCache.Clear();
        fullCache[rack] = (tick, full);
        return full;
    }
}
