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
/// One Terminal row: a keyed item (summed over storage), one whole item on a disk (<paramref name="DiskId"/>),
/// or one specific chest item (<paramref name="Chest"/>).
/// </summary>
internal sealed record StoredRow(ItemKey? Key, Item Display, long Count, Chest? Chest, string? DiskId = null);

/// <summary>
/// One network's storage: disks in its Storage Racks (rack order), then chests with a Chest Link.
/// Items go to disks first. Plain items are kept as counts; anything with its own state (enchantments,
/// flavour, colour, mod data…) is kept whole in the disk's global inventory (see <see cref="DiskStore.WholeItems"/>).
/// Linked chests take what the disks can't.
/// </summary>
internal sealed class NetworkStorage : IStockView
{
    private NetworkStorage(StorageIndex disks, IReadOnlyList<Chest> chests)
    {
        Disks = disks;
        Chests = chests;
    }

    public StorageIndex Disks { get; }

    public IReadOnlyList<Chest> Chests { get; }

    /// <summary>Storage of a network and every network a Network Port joins it to.</summary>
    public static NetworkStorage For(LocationNetworks loc, Network network, DiskStore store)
    {
        var contents = new List<DiskContents>();
        var chests = new List<Chest>();
        foreach (var (mloc, mn) in loc.Manager.Members(loc, network))
            Collect(mloc, mn, store, contents, chests);
        return new NetworkStorage(new StorageIndex(contents), chests);
    }

    /// <summary>Storage of one network only, ignoring Port links.</summary>
    public static NetworkStorage ForOne(LocationNetworks loc, Network network, DiskStore store)
    {
        var contents = new List<DiskContents>();
        var chests = new List<Chest>();
        Collect(loc, network, store, contents, chests);
        return new NetworkStorage(new StorageIndex(contents), chests);
    }

    private static void Collect(LocationNetworks loc, Network network, DiskStore store, List<DiskContents> contents, List<Chest> chests)
    {
        foreach (var rack in loc.ObjectsOf(network, BlockKind.StorageRack))
        {
            foreach (string id in RackSlots.Get(rack, Ids.Data.RackDisks))
            {
                string[] parts = id.Split(':');
                var tier = parts.Length > 1 && System.Enum.TryParse<DiskTier>(parts[1], out var t) ? t : DiskTier.Copper;
                contents.Add(store.Get(parts[0], tier));
            }
        }

        chests.AddRange(loc.ObjectsOf(network, BlockKind.ChestLink).OfType<Chest>());
    }

    public int KindsStored => Disks.All().Select(a => a.Key.QualifiedId)
        .Concat(Disks.Disks.SelectMany(d => DiskStore.WholeItems(d.Id.Value)).Where(i => i != null).Select(i => i!.QualifiedItemId))
        .Distinct().Count();

    /// <summary>Total count of an item across disks and linked chests.</summary>
    public long Count(ItemKey key) =>
        Disks.Count(key) + Chests.Sum(c => c.Items.Where(i => StardewItemKeys.TryGetKey(i, out var k) && k == key).Sum(i => (long)i.Stack));

    /// <summary>Keyed items summed over disks and linked chests (for machine input choice and crafting).</summary>
    public IEnumerable<(ItemKey Key, long Count)> All()
    {
        var totals = new Dictionary<ItemKey, long>();
        foreach (var (key, count) in Disks.All())
            totals[key] = count;
        foreach (var chest in Chests)
        {
            foreach (var item in chest.Items)
            {
                if (StardewItemKeys.TryGetKey(item, out var key))
                    totals[key] = totals.TryGetValue(key, out long n) ? n + item.Stack : item.Stack;
            }
        }
        return totals.Select(kv => (kv.Key, kv.Value));
    }

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
        else if (DiskStore.IsDisk(item))
        {
            // A disk can't go inside a disk; it can still go in a linked chest.
        }
        else if (PutWhole(item) is not { } rest)
        {
            return null;
        }
        else
        {
            item = rest;
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
    /// Stores an item with its own state on disks, whole: first on a disk already holding that kind, then on
    /// the first with room. A stack can be split across disks. Returns what didn't fit (null if all of it did).
    /// </summary>
    private Item? PutWhole(Item item)
    {
        string id = item.QualifiedItemId;
        foreach (var disk in Disks.Disks.OrderByDescending(d => d.HasKind(id)))
        {
            long fits = disk.AddWhole(id, item.Stack);
            if (fits <= 0)
                continue;
            Item part = item;
            if (fits < item.Stack)
            {
                part = item.getOne();
                part.Stack = (int)fits;
                item.Stack -= (int)fits;
            }
            AddToInventory(DiskStore.WholeItems(disk.Id.Value), part);
            if (ReferenceEquals(part, item))
                return null;
        }
        return item;
    }

    /// <summary>Adds an item to a disk's inventory, topping up a matching stack first.</summary>
    private static void AddToInventory(Inventory inventory, Item item)
    {
        foreach (var have in inventory)
        {
            if (have != null && have.canStackWith(item))
            {
                int left = have.addToStack(item);
                if (left <= 0)
                    return;
                item.Stack = left;
            }
        }
        inventory.Add(item);
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

        var whole = Disks.Disks.SelectMany(d => DiskStore.WholeItems(d.Id.Value)
            .Where(i => i != null)
            .Select(i => new StoredRow(null, i!, i!.Stack, null, d.Id.Value)));
        rows.InsertRange(0, keyed.Select(kv => new StoredRow(kv.Key, StardewItemKeys.Create(kv.Key, 1), kv.Value, null)).Concat(whole));
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

    /// <summary>Takes up to <paramref name="n"/> of one whole item off a disk.</summary>
    public Item? TakeWhole(string diskId, Item item, int n)
    {
        var disk = Disks.Disks.FirstOrDefault(d => d.Id.Value == diskId);
        var inventory = DiskStore.WholeItems(diskId);
        int index = inventory.IndexOf(item);
        if (disk == null || index < 0 || n <= 0)
            return null;
        Item taken;
        if (n >= item.Stack)
        {
            inventory.RemoveAt(index);
            taken = item;
        }
        else
        {
            taken = item.getOne();
            taken.Stack = n;
            item.Stack -= n;
        }
        disk.TakeWhole(taken.QualifiedItemId, taken.Stack);
        return taken;
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
