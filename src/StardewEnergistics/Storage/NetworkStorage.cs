using System.Collections.Generic;
using System.Linq;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.Core.Storage;
using StardewEnergistics.Networks;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Objects;

namespace StardewEnergistics.Storage;

/// <summary>
/// One network's storage: disks in its Storage Racks (rack order), then chests with a Chest Link.
/// Items go to disks first when they can; anything else goes to linked chests.
/// </summary>
internal sealed class NetworkStorage
{
    private NetworkStorage(StorageIndex disks, IReadOnlyList<Chest> chests)
    {
        Disks = disks;
        Chests = chests;
    }

    public StorageIndex Disks { get; }

    public IReadOnlyList<Chest> Chests { get; }

    public static NetworkStorage For(LocationNetworks loc, Network network, DiskStore store)
    {
        var contents = new List<DiskContents>();
        foreach (var rack in loc.ObjectsOf(network, BlockKind.StorageRack))
        {
            foreach (string id in RackSlots.Get(rack, Ids.Data.RackDisks))
            {
                string[] parts = id.Split(':');
                var tier = parts.Length > 1 && System.Enum.TryParse<DiskTier>(parts[1], out var t) ? t : DiskTier.Copper;
                contents.Add(store.Get(parts[0], tier));
            }
        }

        var chests = loc.ObjectsOf(network, BlockKind.ChestLink).OfType<Chest>().ToList();
        return new NetworkStorage(new StorageIndex(contents), chests);
    }

    public int KindsStored => Disks.All().Select(a => a.Key.QualifiedId).Distinct().Count();

    /// <summary>Total count of an item across disks and linked chests.</summary>
    public long Count(ItemKey key) =>
        Disks.Count(key) + Chests.Sum(c => c.Items.Where(i => StardewItemKeys.TryGetKey(i, out var k) && k == key).Sum(i => (long)i.Stack));

    /// <summary>Stores an item stack. Returns what didn't fit (null if everything was stored).</summary>
    public Item? Put(Item item)
    {
        if (StardewItemKeys.TryGetKey(item, out var key))
        {
            var r = Disks.Insert(key, item.Stack);
            if (r.Remaining == 0)
                return null;
            item.Stack = r.Remaining;
        }

        foreach (var chest in Chests)
        {
            var left = chest.addItem(item);
            if (left == null)
                return null;
            item = left;
        }
        return item;
    }

    /// <summary>
    /// Moves up to 999 of every disk item into a temporary inventory, for menus that need real items
    /// (the Storage tab and the crafting page). Call <see cref="ReturnFromTemp"/> when the menu closes.
    /// </summary>
    public Inventory ExtractToTemp(int maxStacks = int.MaxValue)
    {
        var temp = new Inventory();
        foreach (var (key, count) in Disks.All().OrderByDescending(a => a.Count).ToList())
        {
            long left = count;
            while (left > 0 && temp.Count < maxStacks)
            {
                int n = (int)System.Math.Min(999, left);
                var got = Disks.Extract(key, n).Extracted;
                if (got <= 0)
                    break;
                temp.Add(StardewItemKeys.Create(key, got));
                left -= got;
            }
        }
        return temp;
    }

    /// <summary>Puts everything left in a temporary inventory back. Returns what didn't fit.</summary>
    public List<Item> ReturnFromTemp(IEnumerable<Item?> temp)
    {
        var leftovers = new List<Item>();
        foreach (var item in temp)
        {
            if (item == null || item.Stack <= 0)
                continue;
            var left = Put(item);
            if (left != null)
                leftovers.Add(left);
        }
        return leftovers;
    }
}
