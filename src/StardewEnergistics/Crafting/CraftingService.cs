using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewEnergistics.Core.Crafting;
using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.Core.Machines;
using StardewEnergistics.Core.Persistence;
using StardewEnergistics.Core.Storage;
using StardewEnergistics.Machines;
using StardewEnergistics.Networks;
using StardewEnergistics.Power;
using StardewEnergistics.Storage;
using StardewModdingAPI;
using StardewValley;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Crafting;

/// <summary>
/// Autocrafting (host). Jobs live in the Computer Core's modData. Every 10 game minutes each Crafting Rack
/// works on one job: craft steps run at once from storage; Furnace steps load free Furnaces that have a
/// Machine Link (and the Furnaces touching them), and their bars go back to storage when done.
/// When a job starts, its ingredients from stock are set aside on the job (<see cref="JobReserve"/>), out of
/// network storage, and the parts it makes stay there too; only the finished item goes to storage.
/// Leftovers and a cancelled job's items go back. Jobs saved before this (not reserved) use storage as they go.
/// </summary>
internal sealed class CraftingService
{
    /// <summary>Recipe runs a craft step may do per tick.</summary>
    public const int MaxRunsPerTick = 100;

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly NetworkManager networks;
    private readonly DiskStore disks;
    private readonly PowerService power;
    private readonly CraftPlanner planner = new();

    public CraftingService(IModHelper helper, IMonitor monitor, NetworkManager networks, DiskStore disks, PowerService power)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.networks = networks;
        this.disks = disks;
        this.power = power;
    }

    private string T(string key, object? tokens = null) => helper.Translation.Get(key, tokens);

    public static JobQueue JobsOf(SObject core) =>
        JobQueueCodec.Decode(core.modData.TryGetValue(Ids.Data.Jobs, out string? json) ? json : null);

    private static void SaveJobs(SObject core, JobQueue queue)
    {
        queue.RemoveFinished();
        if (queue.Jobs.Count == 0)
            core.modData.Remove(Ids.Data.Jobs);
        else
            core.modData[Ids.Data.Jobs] = JobQueueCodec.Encode(queue);
    }

    public static string NameOf(string qualifiedId) => ItemRegistry.GetDataOrErrorItem(qualifiedId).DisplayName;

    // ---- Requests (from the Terminal) ----

    private readonly RecipeBook?[] books = new RecipeBook?[2];
    private readonly int[] bookTicks = { -1, -1 };

    /// <summary>Known recipes (with cooking for an Advanced Crafting Rack), rebuilt at most once per in-game time change.</summary>
    public RecipeBook Book(bool cooking)
    {
        int i = cooking ? 1 : 0;
        int tick = Game1.timeOfDay + (int)Game1.stats.DaysPlayed * 10000;
        if (books[i] == null || bookTicks[i] != tick)
        {
            books[i] = RecipeSource.Build(monitor, cooking);
            bookTicks[i] = tick;
        }
        return books[i]!;
    }

    /// <summary>True if the network has an Advanced Crafting Rack (cooking, keep-in-stock, all Furnaces).</summary>
    public static bool IsAdvanced(Network n) => n.OfKind(BlockKind.AdvCraftingRack).Any();

    private static bool HasRack(Network n) => n.OfKind(BlockKind.CraftingRack).Any() || IsAdvanced(n);

    public CraftPlan Plan(LocationNetworks loc, Network n, ItemKey target, int count) =>
        planner.Plan(target, count, Book(IsAdvanced(n)), NetworkStorage.For(loc, n, disks));

    /// <summary>Starts a job. Returns null on success, or the reason it can't start.</summary>
    public string? Start(LocationNetworks loc, Network n, ItemKey target, int count)
    {
        if (!Context.IsMainPlayer && !Context.IsSplitScreen)
            return T("auto.hostOnly");
        if (n.Core == null || !HasRack(n))
            return T("auto.noRack");
        if (!power.IsPowered(loc, n))
            return T("auto.noPower");

        var plan = Plan(loc, n, target, count);
        if (!plan.CanRun)
            return T("auto.missing", new { items = Describe(plan.Missing) });

        if (plan.Steps.Take(plan.Steps.Count - 1).Any(st => !RecipeSource.IsStackable(st.Recipe.Out.Item.QualifiedId)))
            return T("auto.missing", new { items = NameOf(target.QualifiedId) });

        var core = loc.ObjectOf(n.Core);
        var queue = JobsOf(core);
        var job = queue.Add(new ItemAmount(target, count), plan);
        SetAside(job, plan, NetworkStorage.For(loc, n, disks));
        SaveJobs(core, queue);
        monitor.Log($"Autocraft job: {count} {target} in {plan.Steps.Count} steps.", LogLevel.Debug);
        return null;
    }

    public bool Cancel(LocationNetworks loc, Network n, int jobId)
    {
        if (n.Core == null)
            return false;
        var core = loc.ObjectOf(n.Core);
        var queue = JobsOf(core);
        bool ok = queue.Cancel(jobId);
        ReturnEnded(queue, NetworkStorage.For(loc, n, disks), loc.Location, core.TileLocation);
        SaveJobs(core, queue);
        return ok;
    }

    // ---- Keep in stock (Advanced Crafting Rack) ----

    private static SObject? AdvancedRack(LocationNetworks loc, Network n) => loc.ObjectsOf(n, BlockKind.AdvCraftingRack).FirstOrDefault();

    public static List<StockRule> StockRulesOf(LocationNetworks loc, Network n) =>
        AdvancedRack(loc, n) is { } rack && rack.modData.TryGetValue(Ids.Data.StockRules, out string? raw) ? StockRuleCodec.Decode(raw) : new List<StockRule>();

    /// <summary>Sets how many of an item to keep (0 removes the rule). Returns null, or why it can't.</summary>
    public string? SetStockRule(LocationNetworks loc, Network n, ItemKey item, int target)
    {
        if (AdvancedRack(loc, n) is not { } rack)
            return T("auto.keep.needsRack");
        var rules = StockRuleCodec.With(StockRulesOf(loc, n), item, target);
        if (rules.Count == 0)
            rack.modData.Remove(Ids.Data.StockRules);
        else
            rack.modData[Ids.Data.StockRules] = StockRuleCodec.Encode(rules);
        return null;
    }

    /// <summary>Overnight: start jobs for every keep-in-stock rule that's short and can be made.</summary>
    public void OnDayEnding()
    {
        if (!Context.IsMainPlayer)
            return;
        int started = 0;
        foreach (var loc in networks.AllWithCores())
        {
            foreach (var n in loc.Graph.Networks.Where(n => n.Working && n.Core != null && IsAdvanced(n)))
            {
                var rules = StockRulesOf(loc, n);
                if (rules.Count == 0 || !power.IsPowered(loc, n))
                    continue;
                var core = loc.ObjectOf(n.Core!);
                var queue = JobsOf(core);
                var storage = NetworkStorage.For(loc, n, disks);
                foreach (var due in StockKeeper.Due(rules, storage, queue))
                {
                    var plan = planner.Plan(due.Item, due.Count, Book(true), storage);
                    if (!plan.CanRun || plan.Steps.Take(plan.Steps.Count - 1).Any(st => !RecipeSource.IsStackable(st.Recipe.Out.Item.QualifiedId)))
                        continue;
                    SetAside(queue.Add(due, plan), plan, storage);
                    started++;
                }
                SaveJobs(core, queue);
            }
        }
        if (started > 0)
            monitor.Log($"Keep-in-stock started {started} job(s).", LogLevel.Debug);
        pendingStockMessage = started;
    }

    private int pendingStockMessage;

    /// <summary>Shows the overnight keep-in-stock summary in the morning.</summary>
    public void OnDayStarted()
    {
        if (pendingStockMessage > 0)
            Game1.addHUDMessage(new HUDMessage(T("auto.keep.started", new { count = pendingStockMessage })) { noIcon = true });
        pendingStockMessage = 0;
    }

    public static string Describe(IEnumerable<ItemAmount> items) =>
        string.Join(", ", items.Select(a => $"{a.Count} {NameOf(a.Item.QualifiedId)}"));

    // ---- Ticks ----

    public void OnTimeChanged()
    {
        if (!Context.IsMainPlayer)
            return;
        foreach (var loc in networks.AllWithCores())
        {
            foreach (var n in loc.Graph.Networks.Where(n => n.Working && n.Core != null))
            {
                var racks = loc.ObjectsOf(n, BlockKind.CraftingRack).Concat(loc.ObjectsOf(n, BlockKind.AdvCraftingRack)).ToList();
                if (racks.Count == 0)
                    continue;
                var core = loc.ObjectOf(n.Core!);
                if (!core.modData.ContainsKey(Ids.Data.Jobs) || !power.IsPowered(loc, n))
                {
                    foreach (var r in racks)
                        RackSlots.ShowFrame(r, 0);
                    continue;
                }
                try
                {
                    Run(loc, n, core, racks);
                }
                catch (System.Exception e)
                {
                    monitor.Log($"Autocraft failed in {loc.Location.NameOrUniqueName}: {e}", LogLevel.Error);
                }
            }
        }
    }

    private void Run(LocationNetworks loc, Network n, SObject core, List<SObject> racks)
    {
        var queue = JobsOf(core);
        var storage = NetworkStorage.For(loc, n, disks);
        var furnaces = LinkedFurnaces(loc, n, allBuildings: IsAdvanced(n));

        // Bring finished bars back and credit their jobs.
        foreach (var f in furnaces)
        {
            if (!f.modData.TryGetValue(Ids.Data.ForJob, out string? tag))
                continue;
            if (f.heldObject.Value == null)
            {
                f.modData.Remove(Ids.Data.ForJob); // emptied by hand
                continue;
            }
            if (!MachineDriver.IsReady(f))
                continue;
            var output = MachineDriver.Collect(f);
            f.modData.Remove(Ids.Data.ForJob);
            if (output == null)
                continue;
            var job = int.TryParse(tag, out int id) ? queue.Jobs.FirstOrDefault(j => j.Id == id) : null;
            bool forStep = job?.Current is { } step && step.Recipe.Kind == RecipeKind.Furnace && step.Recipe.Out.Item.QualifiedId == output.QualifiedItemId;
            bool part = forStep && job!.Reserved && !job.IsLastStep && !job.Finished;
            if (forStep)
                queue.Report(job!, System.Math.Max(1, output.Stack / System.Math.Max(1, job!.Current!.Recipe.Out.Count)));
            if (part && StardewItemKeys.TryGetKey(output, out var partKey))
                job!.Reserve.Add(partKey, output.Stack); // a part for a later step stays with its job
            else
                PutOrDrop(storage, output, loc.Location, core.TileLocation);
        }

        var active = queue.Active(racks.Count);
        foreach (var job in active)
        {
            if (job.Current is not { } step)
                continue;
            if (step.Recipe.Kind == RecipeKind.Furnace)
                LoadFurnaces(job, step, furnaces, storage);
            else
                queue.Report(job, RunCrafts(job, step.Recipe, System.Math.Min(job.RunsLeft, MaxRunsPerTick), storage, loc.Location, core.TileLocation));

            if (job.Status == JobStatus.Done)
            {
                Game1.addHUDMessage(new HUDMessage(T("auto.done", new { n = job.Target.Count, item = NameOf(job.Target.Item.QualifiedId) })) { noIcon = true });
                Game1.playSound("newArtifact");
            }
        }

        for (int i = 0; i < racks.Count; i++)
            RackSlots.ShowFrame(racks[i], i < active.Count(j => !j.Finished) ? 1 : 0);
        ReturnEnded(queue, storage, loc.Location, core.TileLocation);
        SaveJobs(core, queue);
    }

    /// <summary>Furnaces with a Machine Link, and every Furnace touching them, in this network and any joined by Ports.</summary>
    private static List<SObject> LinkedFurnaces(LocationNetworks home, Network homeNet, bool allBuildings)
    {
        var list = new List<SObject>();
        foreach (var (loc, n) in home.Manager.Members(home, homeNet))
        {
            // Another building with its own Crafting Rack uses its own Furnaces.
            if (!allBuildings && n != homeNet && n.Working && HasRack(n))
                continue;
            var seen = new HashSet<Vector2>();
            foreach (var link in loc.ObjectsOf(n, BlockKind.MachineLink).Where(o => o.QualifiedItemId == RecipeSource.FurnaceId))
            {
                var start = new TilePos((int)link.TileLocation.X, (int)link.TileLocation.Y);
                foreach (var p in MachineGroupFinder.Group(start, t => MachineDriver.MachineIdAt(loc.Location, t.X, t.Y)))
                {
                    var v = new Vector2(p.X, p.Y);
                    if (seen.Add(v) && loc.Location.objects.TryGetValue(v, out var f))
                        list.Add(f);
                }
            }
        }
        return list;
    }

    private static void LoadFurnaces(CraftJob job, CraftStep step, List<SObject> furnaces, NetworkStorage storage)
    {
        string tag = job.Id.ToString();
        int inFlight = furnaces.Count(f => f.modData.TryGetValue(Ids.Data.ForJob, out string? t) && t == tag);
        int need = job.RunsLeft - inFlight;
        var main = RecipeSource.MainInput(step.Recipe);
        foreach (var f in furnaces)
        {
            if (need <= 0)
                break;
            if (!MachineDriver.IsIdle(f) || f.modData.ContainsKey(Ids.Data.ForJob))
                continue;

            // A reserved job hands this run's ore and Coal to storage just long enough for the Furnace to take them.
            var lent = job.Reserved ? Lend(job, step.Recipe.In, storage) : new List<(ItemKey Key, int Count)>();
            var key = storage.All()
                .Where(k => k.Key.QualifiedId == main.Item.QualifiedId && k.Key.PreserveId == null && k.Key.Color == null && k.Count >= main.Count)
                .OrderBy(k => k.Key.Quality).Select(k => (ItemKey?)k.Key).FirstOrDefault();
            bool loaded = key != null && MachineDriver.Load(f, key.Value, main.Count, storage);
            if (!loaded)
            {
                TakeBack(job, lent, storage);
                break;
            }
            f.modData[Ids.Data.ForJob] = tag;
            need--;
        }
    }

    /// <summary>Runs a crafting recipe up to <paramref name="max"/> times. Returns runs done.</summary>
    private static int RunCrafts(CraftJob job, RecipeDef recipe, int max, NetworkStorage storage, GameLocation location, Vector2 dropAt)
    {
        bool part = job.Reserved && !job.IsLastStep;
        int done = 0;
        for (; done < max; done++)
        {
            if (!TakeAll(job, recipe.In, storage))
                break;
            if (part)
            {
                job.Reserve.Add(new ItemKey(recipe.Out.Item.QualifiedId), recipe.Out.Count); // stays with the job
                continue;
            }
            var item = ItemRegistry.Create(recipe.Out.Item.QualifiedId, recipe.Out.Count);
            PutOrDrop(storage, item, location, dropAt);
        }
        return done;
    }

    /// <summary>Takes every ingredient (the job's reserve first, then storage, lowest quality first), or nothing.</summary>
    private static bool TakeAll(CraftJob job, IReadOnlyList<ItemAmount> inputs, NetworkStorage storage)
    {
        var fromReserve = new List<(ItemKey Key, int Count)>();
        var fromStorage = new List<Item>();
        foreach (var input in inputs)
        {
            var got = job.Reserve.Take(input.Item.QualifiedId, input.Count);
            fromReserve.AddRange(got);
            int need = input.Count - got.Sum(g => g.Count);
            foreach (var key in LowestFirst(storage, input.Item.QualifiedId))
            {
                if (need <= 0)
                    break;
                var item = storage.Take(key, need);
                if (item != null)
                {
                    need -= item.Stack;
                    fromStorage.Add(item);
                }
            }
            if (need > 0)
            {
                foreach (var (k, c) in fromReserve)
                    job.Reserve.Add(k, c);
                foreach (var t in fromStorage)
                    storage.Put(t);
                return false;
            }
        }
        return true;
    }

    private static List<ItemKey> LowestFirst(NetworkStorage storage, string qualifiedId) => storage.All()
        .Where(k => k.Key.QualifiedId == qualifiedId && k.Key.PreserveId == null && k.Key.Color == null)
        .OrderBy(k => k.Key.Quality).Select(k => k.Key).ToList();

    /// <summary>Moves one run's ingredients from the job's reserve into storage. Returns what was moved.</summary>
    private static List<(ItemKey Key, int Count)> Lend(CraftJob job, IReadOnlyList<ItemAmount> inputs, NetworkStorage storage)
    {
        var lent = new List<(ItemKey, int)>();
        foreach (var input in inputs)
        {
            foreach (var (key, count) in job.Reserve.Take(input.Item.QualifiedId, input.Count))
            {
                var left = storage.Put(StardewItemKeys.Create(key, count));
                int stored = count - (left?.Stack ?? 0);
                if (left != null)
                    job.Reserve.Add(key, left.Stack);
                if (stored > 0)
                    lent.Add((key, stored));
            }
        }
        return lent;
    }

    /// <summary>Takes lent items back out of storage into the job's reserve (the Furnace didn't start).</summary>
    private static void TakeBack(CraftJob job, List<(ItemKey Key, int Count)> lent, NetworkStorage storage)
    {
        foreach (var (key, count) in lent)
        {
            var item = storage.Take(key, count);
            if (item != null)
                job.Reserve.Add(key, item.Stack);
        }
    }

    /// <summary>Takes a new job's ingredients from stock out of storage and holds them on the job.</summary>
    private static void SetAside(CraftJob job, CraftPlan plan, NetworkStorage storage)
    {
        foreach (var need in plan.FromStock)
        {
            int left = need.Count;
            foreach (var key in LowestFirst(storage, need.Item.QualifiedId))
            {
                if (left <= 0)
                    break;
                var item = storage.Take(key, left);
                if (item != null)
                {
                    left -= item.Stack;
                    job.Reserve.Add(key, item.Stack);
                }
            }
        }
        job.Reserved = true;
    }

    /// <summary>Returns what finished or cancelled jobs still hold to storage (dropping what doesn't fit).</summary>
    private static void ReturnEnded(JobQueue queue, NetworkStorage storage, GameLocation location, Vector2 tile)
    {
        foreach (var job in queue.EndedWithReserve().ToList())
        {
            foreach (var (key, count) in job.Reserve.Drain())
            {
                long left = count;
                while (left > 0)
                {
                    int n = (int)System.Math.Min(999, left);
                    PutOrDrop(storage, StardewItemKeys.Create(key, n), location, tile);
                    left -= n;
                }
            }
        }
    }

    private static void PutOrDrop(NetworkStorage storage, Item item, GameLocation location, Vector2 tile)
    {
        var left = storage.Put(item);
        if (left != null)
            Game1.createItemDebris(left, tile * 64 + new Vector2(32, 96), -1, location);
    }
}
