namespace StardewEnergistics.Core.Storage;

/// <summary>
/// What one disk holds. Kinds count base item IDs only, so every quality and flavour
/// of Parsnip together uses one kind. Contents travel with the disk (and survive a tier upgrade).
/// </summary>
public sealed class DiskContents
{
    private readonly Dictionary<ItemKey, long> counts = new();
    private readonly Dictionary<string, int> keysPerBaseId = new(StringComparer.Ordinal);

    /// <summary>
    /// Whole items (ones with their own state: enchanted tools, rings, preserves…) by base item ID. The items
    /// themselves are kept by the game side; this only counts them toward the disk's kinds and items.
    /// </summary>
    private readonly Dictionary<string, long> wholeCounts = new(StringComparer.Ordinal);

    public DiskContents(DiskId id, DiskTier tier, DiskSpec spec)
    {
        Id = id;
        Tier = tier;
        Spec = spec;
    }

    public DiskId Id { get; }

    public DiskTier Tier { get; private set; }

    public DiskSpec Spec { get; private set; }

    public long TotalItems { get; private set; }

    public int KindsUsed => keysPerBaseId.Count + wholeCounts.Keys.Count(k => !keysPerBaseId.ContainsKey(k));

    /// <summary>How many whole items (with their own state) this disk holds.</summary>
    public long WholeItems => wholeCounts.Values.Sum();

    public long FreeItems => Math.Max(0, Spec.MaxItems - TotalItems);

    public IEnumerable<(ItemKey Key, long Count)> Stacks => counts.Select(kv => (kv.Key, kv.Value));

    public long Count(ItemKey k) => counts.TryGetValue(k, out long n) ? n : 0;

    public bool HasKind(string qualifiedId) => keysPerBaseId.ContainsKey(qualifiedId) || wholeCounts.ContainsKey(qualifiedId);

    /// <summary>How many of this item would fit right now.</summary>
    public long RoomFor(ItemKey k) => RoomFor(k.QualifiedId);

    /// <summary>How many items with this base ID would fit right now.</summary>
    public long RoomFor(string qualifiedId)
    {
        if (!HasKind(qualifiedId) && !Spec.UnlimitedKinds && KindsUsed >= Spec.MaxKinds)
            return 0;
        return FreeItems;
    }

    /// <summary>Counts up to <paramref name="n"/> whole items toward this disk; returns how many fit.</summary>
    public long AddWhole(string qualifiedId, long n)
    {
        if (n <= 0)
            return 0;
        long take = Math.Min(n, RoomFor(qualifiedId));
        if (take <= 0)
            return 0;
        wholeCounts[qualifiedId] = wholeCounts.GetValueOrDefault(qualifiedId) + take;
        TotalItems += take;
        return take;
    }

    /// <summary>Stops counting up to <paramref name="n"/> whole items; returns how many were counted.</summary>
    public long TakeWhole(string qualifiedId, long n)
    {
        if (n <= 0 || !wholeCounts.TryGetValue(qualifiedId, out long have))
            return 0;
        long take = Math.Min(n, have);
        if (take == have)
            wholeCounts.Remove(qualifiedId);
        else
            wholeCounts[qualifiedId] = have - take;
        TotalItems -= take;
        return take;
    }

    /// <summary>Recounts whole items from what the game side actually holds (on load), without limit checks.</summary>
    public void SetWhole(IEnumerable<(string QualifiedId, long Count)> items)
    {
        TotalItems -= WholeItems;
        wholeCounts.Clear();
        foreach (var (id, n) in items)
        {
            if (n <= 0)
                continue;
            wholeCounts[id] = wholeCounts.GetValueOrDefault(id) + n;
            TotalItems += n;
        }
    }

    /// <summary>Stores up to <paramref name="n"/>; returns how many were stored.</summary>
    public long Add(ItemKey k, long n)
    {
        if (n <= 0)
            return 0;
        long take = Math.Min(n, RoomFor(k));
        if (take <= 0)
            return 0;

        if (counts.TryGetValue(k, out long have))
        {
            counts[k] = have + take;
        }
        else
        {
            counts[k] = take;
            keysPerBaseId[k.QualifiedId] = keysPerBaseId.TryGetValue(k.QualifiedId, out int c) ? c + 1 : 1;
        }
        TotalItems += take;
        return take;
    }

    /// <summary>Takes up to <paramref name="n"/>; returns how many were taken.</summary>
    public long Take(ItemKey k, long n)
    {
        if (n <= 0 || !counts.TryGetValue(k, out long have))
            return 0;
        long take = Math.Min(n, have);
        if (take == have)
        {
            counts.Remove(k);
            int left = keysPerBaseId[k.QualifiedId] - 1;
            if (left == 0)
                keysPerBaseId.Remove(k.QualifiedId);
            else
                keysPerBaseId[k.QualifiedId] = left;
        }
        else
        {
            counts[k] = have - take;
        }
        TotalItems -= take;
        return take;
    }

    /// <summary>Crafting a disk into the next tier keeps everything on it.</summary>
    public void Upgrade(DiskTier tier, DiskSpec spec)
    {
        Tier = tier;
        Spec = spec;
    }

    /// <summary>Puts a saved stack back without limit checks (a smaller config must not delete items).</summary>
    public void Restore(ItemKey k, long n)
    {
        if (n <= 0)
            return;
        if (counts.TryGetValue(k, out long have))
        {
            counts[k] = have + n;
        }
        else
        {
            counts[k] = n;
            keysPerBaseId[k.QualifiedId] = keysPerBaseId.TryGetValue(k.QualifiedId, out int c) ? c + 1 : 1;
        }
        TotalItems += n;
    }
}
