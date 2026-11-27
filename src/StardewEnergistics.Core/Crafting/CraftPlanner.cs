using StardewEnergistics.Core.Storage;

namespace StardewEnergistics.Core.Crafting;

/// <summary>One step in a plan: run <see cref="Recipe"/> <see cref="Times"/> times.</summary>
public sealed record CraftStep(RecipeDef Recipe, int Times);

/// <summary>Ordered steps (leaves first), items taken from stock, and anything missing.</summary>
public sealed record CraftPlan(IReadOnlyList<CraftStep> Steps, IReadOnlyList<ItemAmount> FromStock, IReadOnlyList<ItemAmount> Missing)
{
    public bool CanRun => Missing.Count == 0;
}

/// <summary>
/// Expands a recipe tree, using stock first and sub-recipes for the rest.
/// Ingredients match stock by item ID in any quality (lowest quality used first), like vanilla crafting.
/// Only plain items (no flavour or colour) count as stock. The requested item itself is always crafted,
/// never taken from stock. Leftover output from a step (e.g. a recipe that makes 5) is reused by later steps.
/// When an item has several recipes, the first one that can be fully made is used; otherwise the first one.
/// Cycles (A needs B needs A) are cut and reported as missing.
/// </summary>
public sealed class CraftPlanner
{
    /// <summary>Recipe tree depth limit, a guard against runaway data.</summary>
    public const int MaxDepth = 24;

    public CraftPlan Plan(ItemKey target, int n, RecipeBook book, IStockView stock)
    {
        if (n <= 0)
            return new CraftPlan(Array.Empty<CraftStep>(), Array.Empty<ItemAmount>(), Array.Empty<ItemAmount>());

        var state = State.From(stock);
        var path = new HashSet<string>();
        Craft(target.QualifiedId, n, book, state, path, 0);
        return state.ToPlan();
    }

    /// <summary>Make <paramref name="count"/> of an item by recipe (not from stock).</summary>
    private static void Craft(string id, long count, RecipeBook book, State state, HashSet<string> path, int depth)
    {
        var recipes = book.Making(new ItemKey(id));
        if (recipes.Count == 0 || depth >= MaxDepth || path.Contains(id))
        {
            state.AddMissing(id, count);
            return;
        }

        path.Add(id);
        State? fallback = null;
        foreach (var r in recipes)
        {
            var trial = state.Clone();
            RunRecipe(r, id, count, book, trial, path, depth);
            if (trial.MissingTotal == state.MissingTotal)
            {
                state.CopyFrom(trial);
                path.Remove(id);
                return;
            }
            fallback ??= trial;
        }
        state.CopyFrom(fallback!);
        path.Remove(id);
    }

    private static void RunRecipe(RecipeDef r, string id, long count, RecipeBook book, State state, HashSet<string> path, int depth)
    {
        int perRun = Math.Max(1, r.Out.Count);
        long times = (count + perRun - 1) / perRun;
        foreach (var input in r.In)
            Need(input.Item.QualifiedId, input.Count * times, book, state, path, depth + 1);
        state.AddStep(r, times);
        long surplus = times * perRun - count;
        if (surplus > 0)
            state.AddSurplus(id, surplus);
    }

    /// <summary>Get <paramref name="count"/> of an item: leftovers, then stock, then crafting.</summary>
    private static void Need(string id, long count, RecipeBook book, State state, HashSet<string> path, int depth)
    {
        count -= state.TakeSurplus(id, count);
        count -= state.TakeStock(id, count);
        if (count > 0)
            Craft(id, count, book, state, path, depth);
    }

    private sealed class State
    {
        // Stock left, by item ID, as (quality, count) lowest quality first.
        private Dictionary<string, List<(ItemKey Key, long Count)>> stock = new();
        private Dictionary<string, long> surplus = new();
        private Dictionary<ItemKey, long> taken = new();
        private Dictionary<string, long> missing = new();
        private List<(RecipeDef Recipe, long Times)> steps = new();
        public long MissingTotal { get; private set; }

        public static State From(IStockView view)
        {
            var s = new State();
            foreach (var (key, count) in view.All())
            {
                if (count <= 0 || key.PreserveId != null || key.Color != null)
                    continue;
                if (!s.stock.TryGetValue(key.QualifiedId, out var list))
                    s.stock[key.QualifiedId] = list = new();
                list.Add((key, count));
            }
            foreach (var list in s.stock.Values)
                list.Sort((a, b) => a.Key.Quality.CompareTo(b.Key.Quality));
            return s;
        }

        public State Clone() => new()
        {
            stock = stock.ToDictionary(kv => kv.Key, kv => kv.Value.ToList()),
            surplus = new(surplus),
            taken = new(taken),
            missing = new(missing),
            steps = steps.ToList(),
            MissingTotal = MissingTotal,
        };

        public void CopyFrom(State o)
        {
            stock = o.stock;
            surplus = o.surplus;
            taken = o.taken;
            missing = o.missing;
            steps = o.steps;
            MissingTotal = o.MissingTotal;
        }

        public long TakeStock(string id, long want)
        {
            if (want <= 0 || !stock.TryGetValue(id, out var list))
                return 0;
            long got = 0;
            for (int i = 0; i < list.Count && got < want; i++)
            {
                var (key, have) = list[i];
                long use = Math.Min(have, want - got);
                if (use <= 0)
                    continue;
                list[i] = (key, have - use);
                taken[key] = taken.GetValueOrDefault(key) + use;
                got += use;
            }
            return got;
        }

        public long TakeSurplus(string id, long want)
        {
            long have = surplus.GetValueOrDefault(id);
            long use = Math.Min(have, Math.Max(0, want));
            if (use > 0)
                surplus[id] = have - use;
            return use;
        }

        public void AddSurplus(string id, long n) => surplus[id] = surplus.GetValueOrDefault(id) + n;

        public void AddMissing(string id, long n)
        {
            missing[id] = missing.GetValueOrDefault(id) + n;
            MissingTotal += n;
        }

        public void AddStep(RecipeDef r, long times)
        {
            if (steps.Count > 0 && steps[^1].Recipe == r)
                steps[^1] = (r, steps[^1].Times + times);
            else
                steps.Add((r, times));
        }

        public CraftPlan ToPlan() => new(
            steps.Select(s => new CraftStep(s.Recipe, (int)Math.Min(int.MaxValue, s.Times))).ToList(),
            taken.Where(kv => kv.Value > 0).Select(kv => new ItemAmount(kv.Key, (int)Math.Min(int.MaxValue, kv.Value))).ToList(),
            missing.Select(kv => new ItemAmount(new ItemKey(kv.Key), (int)Math.Min(int.MaxValue, kv.Value))).ToList());
    }
}
