using System.Collections.Generic;
using System.Linq;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.Core.Storage;
using StardewEnergistics.Networks;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Objects;

namespace StardewEnergistics.Storage;

/// <summary>One Terminal row: a keyed item (summed over storage) or one specific chest item.</summary>
internal sealed record StoredRow(ItemKey? Key, Item Display, long Count, Chest? Chest);

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
    /// Everything stored, for the Terminal: keyed items are summed over disks and linked chests;
    /// other chest items (tools, preserves…) are listed one by one.
    /// </summary>
    public List<StoredRow> Rows()
    {
        var keyed = new Dictionary<ItemKey, long>();
        foreach (var (key, count) in Disks.All())
            keyed[key] = count;

        var rows = new List<StoredRow>();
        foreach (var chest in Chests)
        {
            foreach (var item in chest.Items)
            {
                if (item == null)
                    continue;
                if (StardewItemKeys.TryGetKey(item, out var key))
                    keyed[key] = keyed.TryGetValue(key, out long n) ? n + item.Stack : item.Stack;
                else
                    rows.Add(new StoredRow(null, item, item.Stack, chest));
            }
        }

        rows.InsertRange(0, keyed.Select(kv => new StoredRow(kv.Key, StardewItemKeys.Create(kv.Key, 1), kv.Value, null)));
        return rows;
    }

    /// <summary>Takes up to <paramref name="n"/> of a keyed item (disks first, then chests).</summary>
    public Item? Take(ItemKey key, int n)
    {
        int got = Disks.Extract(key, n).Extracted;
        foreach (var chest in Chests)
        {
            for (int i = 0; i < chest.Items.Count && got < n; i++)
            {
                var item = chest.Items[i];
                if (item == null || !StardewItemKeys.TryGetKey(item, out var k) || k != key)
                    continue;
                int take = System.Math.Min(n - got, item.Stack);
                got += take;
                item.Stack -= take;
                if (item.Stack <= 0)
                    chest.Items[i] = null;
            }
            chest.clearNulls();
        }
        return got > 0 ? StardewItemKeys.Create(key, got) : null;
    }

    /// <summary>Takes up to <paramref name="n"/> of one specific (unkeyed) item from a chest.</summary>
    public Item? TakeFromChest(Chest chest, Item item, int n)
    {
        int index = chest.Items.IndexOf(item);
        if (index < 0)
            return null;
        if (n >= item.Stack)
        {
            chest.Items[index] = null;
            chest.clearNulls();
            return item;
        }
        var part = item.getOne();
        part.Stack = n;
        item.Stack -= n;
        return part;
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
