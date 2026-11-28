using StardewEnergistics.Core.Storage;

namespace StardewEnergistics.Core.Crafting;

/// <summary>
/// Items set aside for one autocraft job: its ingredients from stock (taken out when the job starts) and
/// the parts it has made so far. They're out of network storage, so Terminals, Machine Links and other
/// jobs can't use them. Whatever is left when the job ends or is cancelled goes back to storage.
/// </summary>
public sealed class JobReserve
{
    private readonly Dictionary<ItemKey, long> items = new();

    public bool IsEmpty => items.Count == 0;

    public IEnumerable<(ItemKey Key, long Count)> All() => items.Select(kv => (kv.Key, kv.Value));

    /// <summary>How many of an item (any quality) are set aside.</summary>
    public long CountOf(string qualifiedId) => items.Where(kv => kv.Key.QualifiedId == qualifiedId).Sum(kv => kv.Value);

    public void Add(ItemKey key, long n)
    {
        if (n > 0)
            items[key] = items.GetValueOrDefault(key) + n;
    }

    /// <summary>Takes up to <paramref name="n"/> of an item, lowest quality first. Returns what was taken.</summary>
    public List<(ItemKey Key, int Count)> Take(string qualifiedId, int n)
    {
        var taken = new List<(ItemKey, int)>();
        foreach (var key in items.Keys.Where(k => k.QualifiedId == qualifiedId).OrderBy(k => k.Quality).ToList())
        {
            if (n <= 0)
                break;
            int got = (int)Math.Min(n, items[key]);
            items[key] -= got;
            if (items[key] <= 0)
                items.Remove(key);
            n -= got;
            taken.Add((key, got));
        }
        return taken;
    }

    /// <summary>Empties the reserve, returning everything that was in it.</summary>
    public List<(ItemKey Key, long Count)> Drain()
    {
        var all = All().ToList();
        items.Clear();
        return all;
    }
}
