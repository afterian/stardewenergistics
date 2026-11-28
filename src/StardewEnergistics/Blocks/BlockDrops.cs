using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewEnergistics.Content;
using StardewEnergistics.Crafting;
using StardewEnergistics.Power;
using StardewEnergistics.Storage;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Blocks;

/// <summary>
/// When a block or linked machine is broken or picked up, whatever it holds pops out as items:
/// disks (with everything stored on them), Battery Packs, generator fuel, ingredients set aside for
/// autocraft jobs, and attached Links or Power Feeds. The dropped block comes back empty, so nothing
/// is lost or doubled.
/// </summary>
internal sealed class BlockDrops
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly DiskStore disks;
    private readonly PowerService power;

    /// <summary>modData keys that hold contents, cleared from the dropped item once they've popped out.</summary>
    private static readonly string[] ContentKeys =
    {
        Ids.Data.RackDisks, Ids.Data.RackCharge, Ids.Data.Fuel, Ids.Data.Jobs,
        Ids.Data.ChestLink, Ids.Data.MachineLink, Ids.Data.ShippingLink, Ids.Data.PowerFeed,
        Ids.Data.LinkSettings, Ids.Data.LinkState, Ids.Data.ForJob,
    };

    /// <summary>Dropped items to clean on the next tick, in case the game makes the debris after this event.</summary>
    private readonly List<(GameLocation Loc, string ItemId)> pendingClean = new();

    public BlockDrops(IModHelper helper, IMonitor monitor, DiskStore disks, PowerService power)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.disks = disks;
        this.power = power;
    }

    public void Register()
    {
        helper.Events.World.ObjectListChanged += OnObjectListChanged;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;
        foreach (var (tile, obj) in e.Removed)
        {
            var drops = Contents(obj);
            if (drops.Count == 0)
                continue;

            var at = tile * 64 + new Vector2(32, 32);
            foreach (var item in drops)
                Game1.createItemDebris(item, at, -1, e.Location);
            ClearContents(obj);
            CleanDebris(e.Location, obj.QualifiedItemId);
            pendingClean.Add((e.Location, obj.QualifiedItemId));
            monitor.Log($"{obj.Name} removed at {tile} in {e.Location.NameOrUniqueName}: dropped {string.Join(", ", drops.Select(d => $"{d.Stack} {d.Name}"))}.", LogLevel.Debug);
        }
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (pendingClean.Count == 0)
            return;
        foreach (var (loc, id) in pendingClean)
            CleanDebris(loc, id);
        pendingClean.Clear();
    }

    /// <summary>Everything the object holds, as items.</summary>
    private List<Item> Contents(SObject obj)
    {
        var items = new List<Item>();
        var data = obj.modData;

        foreach (string id in RackSlots.Get(obj, Ids.Data.RackDisks))
        {
            string[] parts = id.Split(':');
            if (parts[0].Length == 0)
                continue;
            var tier = parts.Length > 1 && System.Enum.TryParse<StardewEnergistics.Core.Storage.DiskTier>(parts[1], out var t) ? t : StardewEnergistics.Core.Storage.DiskTier.Copper;
            items.Add(disks.CreateItem(disks.Get(parts[0], tier)));
        }

        int batteries = RackSlots.GetInts(obj, Ids.Data.RackCharge).Count(c => c > 0);
        if (batteries > 0)
            items.Add(ItemRegistry.Create("(O)" + VanillaIds.BatteryPack, batteries));

        int coal = power.Table.CoalPower > 0 ? RackSlots.GetInt(obj, Ids.Data.Fuel) / power.Table.CoalPower : 0;
        if (coal > 0)
            items.Add(ItemRegistry.Create("(O)" + VanillaIds.Coal, coal));

        if (data.ContainsKey(Ids.Data.Jobs))
        {
            foreach (var job in CraftingService.JobsOf(obj).Jobs)
            {
                foreach (var (key, count) in job.Reserve.All())
                {
                    for (long left = count; left > 0; left -= 999)
                        items.Add(StardewItemKeys.Create(key, (int)System.Math.Min(999, left)));
                }
            }
        }

        if (data.ContainsKey(Ids.Data.ChestLink))
            items.Add(ItemRegistry.Create("(O)" + Ids.ChestLink));
        if (data.ContainsKey(Ids.Data.MachineLink))
            items.Add(ItemRegistry.Create("(O)" + Ids.MachineLink));
        if (data.ContainsKey(Ids.Data.ShippingLink))
            items.Add(ItemRegistry.Create("(O)" + Ids.ShippingLink));
        if (data.ContainsKey(Ids.Data.PowerFeed))
            items.Add(ItemRegistry.Create("(O)" + Ids.PowerFeed));

        return items;
    }

    private static void ClearContents(SObject obj)
    {
        foreach (string key in ContentKeys)
            obj.modData.Remove(key);
        if (obj.QualifiedItemId.StartsWith("(BC)" + Ids.Prefix))
            RackSlots.ShowFrame(obj, 0);
    }

    /// <summary>The game drops the broken block as debris, which may be a copy made before we cleared it.</summary>
    private static void CleanDebris(GameLocation location, string qualifiedId)
    {
        foreach (var debris in location.debris)
        {
            if (debris.item is SObject o && o.QualifiedItemId == qualifiedId && ContentKeys.Any(o.modData.ContainsKey))
                ClearContents(o);
        }
    }
}
