using StardewEnergistics.Core.Storage;

namespace StardewEnergistics.Core.Machines;

/// <summary>
/// Chooses the next item to feed a machine from storage, per a Link's settings:
/// inputs in priority order, never dipping into keep-back amounts, and only items the machine accepts.
/// Within one input, lower qualities are used first so the best stays in storage.
/// </summary>
public static class InputSelector
{
    /// <returns>The item to feed, or null when nothing allowed is in stock (or the Link isn't set up).</returns>
    public static ItemKey? Choose(LinkSettings s, IStockView stock, Func<ItemKey, bool> accepts, int needed = 1)
    {
        if (!s.Configured)
            return null;

        foreach (var rule in s.Inputs)
        {
            var candidates = rule.AnyQuality
                ? stock.All().Where(a => a.Key.QualifiedId == rule.Item.QualifiedId && a.Key.PreserveId == rule.Item.PreserveId).Select(a => a.Key).OrderBy(k => k.Quality)
                : new[] { rule.Item }.AsEnumerable();

            foreach (var key in candidates)
            {
                if (Usable(s, stock, key) >= needed && accepts(key))
                    return key;
            }
        }
        return null;
    }

    /// <summary>How many of an item may be used: stock minus keep-back (shared across qualities for any-quality rules).</summary>
    public static long Usable(LinkSettings s, IStockView stock, ItemKey key)
    {
        long have = stock.Count(key);
        long usable = have;
        foreach (var keep in s.KeepBack)
        {
            if (keep.AnyQuality)
            {
                if (keep.Item.QualifiedId != key.QualifiedId || keep.Item.PreserveId != key.PreserveId)
                    continue;
                // Reserve comes out of the whole family; use lower qualities' stock to cover it first.
                long others = stock.All().Where(a => a.Key.QualifiedId == key.QualifiedId && a.Key.PreserveId == key.PreserveId && a.Key != key).Sum(a => a.Count);
                long total = have + others;
                usable = Math.Min(usable, Math.Max(0, total - keep.Amount));
            }
            else if (keep.Item == key)
            {
                usable = Math.Min(usable, Math.Max(0, have - keep.Amount));
            }
        }
        return usable;
    }
}
