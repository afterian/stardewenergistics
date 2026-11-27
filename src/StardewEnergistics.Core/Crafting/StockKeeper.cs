using StardewEnergistics.Core.Storage;

namespace StardewEnergistics.Core.Crafting;

/// <summary>"Always keep <see cref="Target"/> of this item" (Advanced Crafting Rack).</summary>
public sealed record StockRule(ItemKey Item, int Target);

/// <summary>Works out which keep-in-stock jobs to start.</summary>
public static class StockKeeper
{
    /// <summary>
    /// For each rule, how many to make now: the target minus what's stored (any quality) and minus what
    /// unfinished jobs for the same item will still make. Rules already met are left out.
    /// </summary>
    public static IReadOnlyList<ItemAmount> Due(IEnumerable<StockRule> rules, IStockView stock, JobQueue queue)
    {
        var stored = new Dictionary<string, long>();
        foreach (var (key, count) in stock.All())
            if (key.PreserveId == null && key.Color == null)
                stored[key.QualifiedId] = stored.GetValueOrDefault(key.QualifiedId) + count;

        var coming = queue.Jobs.Where(j => !j.Finished)
            .GroupBy(j => j.Target.Item.QualifiedId)
            .ToDictionary(g => g.Key, g => g.Sum(j => (long)j.Target.Count));

        var due = new List<ItemAmount>();
        foreach (var rule in rules.GroupBy(r => r.Item.QualifiedId).Select(g => g.Last()))
        {
            long have = stored.GetValueOrDefault(rule.Item.QualifiedId) + coming.GetValueOrDefault(rule.Item.QualifiedId);
            long need = rule.Target - have;
            if (need > 0)
                due.Add(new ItemAmount(new ItemKey(rule.Item.QualifiedId), (int)Math.Min(int.MaxValue, need)));
        }
        return due;
    }
}

/// <summary>Keep-in-stock rules as a short string: "count qualifiedId" entries separated by ';'.</summary>
public static class StockRuleCodec
{
    public static string Encode(IEnumerable<StockRule> rules) =>
        string.Join(";", rules.Where(r => r.Target > 0).Select(r => $"{r.Target} {r.Item.QualifiedId}"));

    public static List<StockRule> Decode(string? raw)
    {
        var list = new List<StockRule>();
        foreach (string part in (raw ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int space = part.IndexOf(' ');
            if (space > 0 && int.TryParse(part[..space], out int n) && n > 0 && space < part.Length - 1)
                list.Add(new StockRule(new ItemKey(part[(space + 1)..]), n));
        }
        return list;
    }

    /// <summary>Sets (or with 0, removes) the rule for one item.</summary>
    public static List<StockRule> With(IEnumerable<StockRule> rules, ItemKey item, int target)
    {
        var list = rules.Where(r => r.Item.QualifiedId != item.QualifiedId).ToList();
        if (target > 0)
            list.Add(new StockRule(new ItemKey(item.QualifiedId), target));
        return list;
    }
}
