namespace StardewEnergistics.Core.Storage;

/// <summary>
/// Item counts across a network's disks, in rack order.
/// Inserts go first to disks that already hold that kind (so kinds aren't wasted), then to
/// the first disk with room. Extracts empty the last disks first, so early disks stay full.
/// Chest Links are added on the game side, after disks (Proposed priority).
/// </summary>
public sealed class StorageIndex : IStockView
{
    private readonly List<DiskContents> disks;

    public StorageIndex(IEnumerable<DiskContents> disksInRackOrder) => disks = disksInRackOrder.ToList();

    public IReadOnlyList<DiskContents> Disks => disks;

    public long TotalItems => disks.Sum(d => d.TotalItems);

    public long Capacity => disks.Sum(d => d.Spec.MaxItems);

    public InsertResult Insert(ItemKey k, int n)
    {
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n));
        long left = n;
        foreach (var d in disks.Where(d => d.HasKind(k.QualifiedId)))
        {
            if (left == 0)
                break;
            left -= d.Add(k, left);
        }
        foreach (var d in disks)
        {
            if (left == 0)
                break;
            left -= d.Add(k, left);
        }
        return new InsertResult(n - (int)left, (int)left);
    }

    /// <summary>How many of this item would fit right now.</summary>
    public long RoomFor(ItemKey k) => disks.Sum(d => d.RoomFor(k));

    public ExtractResult Extract(ItemKey k, int n)
    {
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n));
        long left = n;
        for (int i = disks.Count - 1; i >= 0 && left > 0; i--)
            left -= disks[i].Take(k, left);
        return new ExtractResult(n - (int)left, (int)left);
    }

    public long Count(ItemKey k) => disks.Sum(d => d.Count(k));

    public IEnumerable<(ItemKey Key, long Count)> All()
    {
        var totals = new Dictionary<ItemKey, long>();
        foreach (var d in disks)
        {
            foreach (var (key, count) in d.Stacks)
                totals[key] = totals.TryGetValue(key, out long t) ? t + count : count;
        }
        return totals.Select(kv => (kv.Key, kv.Value));
    }

    /// <summary>Kinds used on a disk; counts base QualifiedId only.</summary>
    public int KindsUsed(DiskId d) =>
        disks.FirstOrDefault(x => x.Id == d)?.KindsUsed ?? throw new KeyNotFoundException($"Disk {d} is not in this network.");
}
