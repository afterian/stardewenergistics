using System;
using System.Collections.Generic;
using System.Linq;
using StardewEnergistics.Core.Persistence;
using StardewEnergistics.Core.Storage;
using StardewModdingAPI;
using StardewValley;

namespace StardewEnergistics.Storage;

/// <summary>
/// Contents of every disk, keyed by the disk ID stored on the disk item. Saved by the host with the farm,
/// so a disk keeps its items wherever the item goes (rack, chest, inventory). Saved by <see cref="SaveCoordinator"/>.
/// </summary>
internal sealed class DiskStore
{
    private static readonly Dictionary<string, DiskTier> TierByItem = new()
    {
        ["(O)" + Ids.CopperDisk] = DiskTier.Copper,
        ["(O)" + Ids.IronDisk] = DiskTier.Iron,
        ["(O)" + Ids.GoldDisk] = DiskTier.Gold,
        ["(O)" + Ids.IridiumDisk] = DiskTier.Iridium,
        ["(O)" + Ids.PrismaticDisk] = DiskTier.Prismatic,
    };

    private readonly IMonitor monitor;
    private readonly Dictionary<string, DiskContents> disks = new();

    /// <summary>Disks whose whole-item counts were read from the game this session.</summary>
    private readonly HashSet<string> wholeSynced = new();

    /// <summary>Prefix of the game's global inventories that hold each disk's whole items.</summary>
    public const string WholePrefix = Ids.Prefix + "Disk_";

    /// <summary>
    /// The real items (with all their state: enchantments, flavour, colour, mod data…) a disk holds, in one of
    /// the game's global inventories. The game saves it with the farm and syncs it in multiplayer, like a Junimo Chest.
    /// </summary>
    public static StardewValley.Inventories.Inventory WholeItems(string diskId) =>
        Game1.player.team.GetOrCreateGlobalInventory(WholePrefix + diskId);

    public DiskStore(IMonitor monitor) => this.monitor = monitor;

    public static bool IsDisk(Item? item) => item != null && TierByItem.ContainsKey(item.QualifiedItemId);

    public static string ItemIdFor(DiskTier tier) => tier switch
    {
        DiskTier.Copper => Ids.CopperDisk,
        DiskTier.Iron => Ids.IronDisk,
        DiskTier.Gold => Ids.GoldDisk,
        DiskTier.Iridium => Ids.IridiumDisk,
        _ => Ids.PrismaticDisk,
    };

    /// <summary>The contents of a disk item, giving it an ID (and empty contents) if it has none.</summary>
    public DiskContents ForItem(Item disk)
    {
        var tier = TierByItem[disk.QualifiedItemId];
        if (!disk.modData.TryGetValue(Ids.Data.DiskId, out string? id) || string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString("N");
            disk.modData[Ids.Data.DiskId] = id;
        }
        return Get(id, tier);
    }

    /// <summary>Contents by ID; creates an empty disk if the ID is unknown.</summary>
    public DiskContents Get(string id, DiskTier tier)
    {
        if (!disks.TryGetValue(id, out var contents))
            disks[id] = contents = new DiskContents(new DiskId(id), tier, DiskSpec.For(tier));
        if (!wholeSynced.Contains(id) && StardewModdingAPI.Context.IsWorldReady)
        {
            wholeSynced.Add(id);
            contents.SetWhole(WholeItems(id)
                .Where(i => i != null)
                .GroupBy(i => i!.QualifiedItemId)
                .Select(g => (g.Key, (long)g.Sum(i => i!.Stack))));
        }
        return contents;
    }

    /// <summary>Moves a disk's contents to a higher tier (keeping every item).</summary>
    public void Upgrade(string id, Item newDisk)
    {
        var tier = TierByItem[newDisk.QualifiedItemId];
        if (disks.TryGetValue(id, out var contents))
            contents.Upgrade(tier, DiskSpec.For(tier));
    }

    /// <summary>A disk item for stored contents (used when taking a disk out of a rack).</summary>
    public Item CreateItem(DiskContents contents)
    {
        var item = ItemRegistry.Create("(O)" + ItemIdFor(contents.Tier));
        item.modData[Ids.Data.DiskId] = contents.Id.Value;
        return item;
    }

    public void Load(SaveModel? model)
    {
        disks.Clear();
        wholeSynced.Clear();
        if (model == null)
            return;
        foreach (var (id, dto) in model.Disks)
            disks[id] = DiskContentsMapper.FromDto(new DiskId(id), dto);
        monitor.Log($"Loaded {disks.Count} disks.", LogLevel.Debug);
    }

    /// <summary>Writes disks into the shared save model (called by the save coordinator).</summary>
    public void WriteTo(SaveModel model)
    {
        model.Disks.Clear();
        foreach (var (id, disk) in disks)
        {
            if (disk.TotalItems > 0)
                model.Disks[id] = DiskContentsMapper.ToDto(disk);
        }
    }

    /// <summary>Every disk's contents known to this save.</summary>
    public IEnumerable<DiskContents> All => disks.Values;

    public void Clear()
    {
        disks.Clear();
        wholeSynced.Clear();
    }

    /// <summary>The tier this save last knew a disk at (Copper if unknown).</summary>
    public DiskTier TierOf(string id) => disks.TryGetValue(id, out var d) ? d.Tier : DiskTier.Copper;
}
