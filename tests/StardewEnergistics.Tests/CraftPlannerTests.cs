using StardewEnergistics.Core.Crafting;
using StardewEnergistics.Core.Storage;
using Xunit;

namespace StardewEnergistics.Tests;

public class CraftPlannerTests
{
    private static ItemKey K(string id, int q = 0) => new("(O)" + id, q);
    private static ItemAmount A(string id, int n) => new(K(id), n);

    private sealed class Stock : IStockView
    {
        private readonly Dictionary<ItemKey, long> items = new();
        public Stock Add(string id, long n, int q = 0) { items[K(id, q)] = n; return this; }
        public long Count(ItemKey k) => items.GetValueOrDefault(k);
        public IEnumerable<(ItemKey Key, long Count)> All() => items.Select(kv => (kv.Key, kv.Value));
    }

    // Vanilla: Quality Sprinkler = Iron Bar + Gold Bar + Refined Quartz; bars from 5 ore + 1 coal; Refined Quartz from Quartz + Coal.
    private static readonly RecipeDef IronBar = new("furnace:335", new[] { A("380", 5), A("382", 1) }, A("335", 1), RecipeKind.Furnace);
    private static readonly RecipeDef GoldBar = new("furnace:336", new[] { A("384", 5), A("382", 1) }, A("336", 1), RecipeKind.Furnace);
    private static readonly RecipeDef Quartz = new("furnace:338", new[] { A("80", 1), A("382", 1) }, A("338", 1), RecipeKind.Furnace);
    private static readonly RecipeDef QualitySprinkler = new("Quality Sprinkler", new[] { A("335", 1), A("336", 1), A("338", 1) }, A("621", 1), RecipeKind.Craft);
    private static RecipeBook Book(params RecipeDef[] extra) => new(new[] { IronBar, GoldBar, Quartz, QualitySprinkler }.Concat(extra));

    [Fact]
    public void QualitySprinklerFromOre()
    {
        var stock = new Stock().Add("380", 10).Add("384", 10).Add("80", 5).Add("382", 10);
        var plan = new CraftPlanner().Plan(K("621"), 2, Book(), stock);

        Assert.True(plan.CanRun);
        Assert.Equal(new[] { "furnace:335", "furnace:336", "furnace:338", "Quality Sprinkler" }, plan.Steps.Select(s => s.Recipe.Id));
        Assert.All(plan.Steps, s => Assert.Equal(2, s.Times));
        Assert.Contains(A("380", 10), plan.FromStock);
        Assert.Contains(A("382", 6), plan.FromStock);
    }

    [Fact]
    public void UsesStockBeforeCrafting()
    {
        var stock = new Stock().Add("335", 1).Add("336", 1).Add("338", 1);
        var plan = new CraftPlanner().Plan(K("621"), 1, Book(), stock);
        Assert.True(plan.CanRun);
        Assert.Single(plan.Steps);
    }

    [Fact]
    public void ReportsMissingLeaves()
    {
        var stock = new Stock().Add("380", 5).Add("384", 2).Add("80", 1).Add("382", 3);
        var plan = new CraftPlanner().Plan(K("621"), 1, Book(), stock);
        Assert.False(plan.CanRun);
        Assert.Equal(new[] { A("384", 3) }, plan.Missing);
    }

    [Fact]
    public void LowerQualityFirst()
    {
        var stock = new Stock().Add("335", 1, q: 2).Add("335", 1, q: 0).Add("336", 1).Add("338", 1);
        var plan = new CraftPlanner().Plan(K("621"), 1, Book(), stock);
        Assert.Contains(new ItemAmount(K("335", 0), 1), plan.FromStock);
        Assert.DoesNotContain(plan.FromStock, a => a.Item == K("335", 2));
    }

    [Fact]
    public void ReusesLeftoverOutput()
    {
        // Wire makes 5 per craft; 2 widgets need 6 wire => 2 crafts, and the first craft's leftover is reused.
        var wire = new RecipeDef("wire", new[] { A("334", 1) }, A("w", 5), RecipeKind.Craft);
        var widget = new RecipeDef("widget", new[] { A("w", 3) }, A("x", 1), RecipeKind.Craft);
        var plan = new CraftPlanner().Plan(K("x"), 2, new RecipeBook(new[] { wire, widget }), new Stock().Add("334", 9));
        Assert.True(plan.CanRun);
        Assert.Equal(2, plan.Steps.Where(s => s.Recipe == wire).Sum(s => s.Times));
        Assert.Contains(A("334", 2), plan.FromStock);
    }

    [Fact]
    public void CyclesAreMissingNotInfinite()
    {
        var a = new RecipeDef("a", new[] { A("b", 1) }, A("a", 1), RecipeKind.Craft);
        var b = new RecipeDef("b", new[] { A("a", 1) }, A("b", 1), RecipeKind.Craft);
        var plan = new CraftPlanner().Plan(K("a"), 1, new RecipeBook(new[] { a, b }), new Stock());
        Assert.False(plan.CanRun);
        Assert.Equal(new[] { A("a", 1) }, plan.Missing);
    }

    [Fact]
    public void PicksRecipeThatCanBeMade()
    {
        var viaCopper = new RecipeDef("viaCopper", new[] { A("334", 1) }, A("x", 1), RecipeKind.Craft);
        var viaIron = new RecipeDef("viaIron", new[] { A("335", 1) }, A("x", 1), RecipeKind.Craft);
        var plan = new CraftPlanner().Plan(K("x"), 1, new RecipeBook(new[] { viaCopper, viaIron }), new Stock().Add("335", 1));
        Assert.True(plan.CanRun);
        Assert.Equal("viaIron", plan.Steps.Single().Recipe.Id);
    }

    [Fact]
    public void IgnoresFlavouredStock()
    {
        var jelly = new RecipeDef("j", new[] { A("344", 1) }, A("x", 1), RecipeKind.Craft);
        var stock = new FlavourStock();
        var plan = new CraftPlanner().Plan(K("x"), 1, new RecipeBook(new[] { jelly }), stock);
        Assert.False(plan.CanRun);
    }

    private sealed class FlavourStock : IStockView
    {
        public long Count(ItemKey k) => 0;
        public IEnumerable<(ItemKey Key, long Count)> All() { yield return (new ItemKey("(O)344", 0, "(O)613"), 5); }
    }

    [Fact]
    public void JobQueueRunsOneJobPerRackInOrder()
    {
        var stock = new Stock().Add("380", 10).Add("384", 10).Add("80", 5).Add("382", 10);
        var planner = new CraftPlanner();
        var q = new JobQueue();
        var j1 = q.Add(A("621", 1), planner.Plan(K("621"), 1, Book(), stock));
        var j2 = q.Add(A("621", 1), planner.Plan(K("621"), 1, Book(), stock));

        Assert.Equal(new[] { j1 }, q.Active(1));
        Assert.Equal(JobStatus.Queued, j2.Status);
        Assert.Equal(2, q.Active(2).Count);

        q.Report(j1, 2);
        Assert.Equal(2, j1.StepIndex);
        Assert.Equal(0.5, j1.Progress);
        q.Report(j1, 2);
        Assert.Equal(JobStatus.Done, j1.Status);

        Assert.True(q.Cancel(j2.Id));
        Assert.False(q.Cancel(j2.Id));
        Assert.Equal(2, q.RemoveFinished());
        Assert.Empty(q.Jobs);
    }

    [Fact]
    public void JobQueueRejectsPlansWithMissingItems()
    {
        var plan = new CraftPlanner().Plan(K("621"), 1, Book(), new Stock());
        Assert.Throws<ArgumentException>(() => new JobQueue().Add(A("621", 1), plan));
    }
}

public class JobQueueCodecTests
{
    [Fact]
    public void RoundTripsUnfinishedJobs()
    {
        var ore = new ItemAmount(new ItemKey("(O)380"), 5);
        var coal = new ItemAmount(new ItemKey("(O)382"), 1);
        var bar = new ItemAmount(new ItemKey("(O)335"), 1);
        var smelt = new RecipeDef("furnace:335", new[] { ore, coal }, bar, RecipeKind.Furnace);
        var plan = new CraftPlan(new[] { new CraftStep(smelt, 3) }, Array.Empty<ItemAmount>(), Array.Empty<ItemAmount>());

        var q = new JobQueue();
        var a = q.Add(new ItemAmount(bar.Item, 3), plan);
        var b = q.Add(new ItemAmount(bar.Item, 3), plan);
        q.Active(1);
        q.Report(a, 2);
        q.Cancel(b.Id);

        var back = Core.Persistence.JobQueueCodec.Decode(Core.Persistence.JobQueueCodec.Encode(q));
        var job = Assert.Single(back.Jobs);
        Assert.Equal(a.Id, job.Id);
        Assert.Equal(JobStatus.Running, job.Status);
        Assert.Equal(2, job.RunsDone);
        Assert.Equal("furnace:335", job.Steps[0].Recipe.Id);
        Assert.Equal(bar, job.Steps[0].Recipe.Out);
        Assert.Equal(new[] { ore, coal }, job.Steps[0].Recipe.In);
        Assert.Equal(RecipeKind.Furnace, job.Steps[0].Recipe.Kind);

        // New jobs get fresh IDs after a reload.
        var c = back.Add(new ItemAmount(bar.Item, 1), plan);
        Assert.True(c.Id > a.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{not json")]
    [InlineData("{\"Jobs\":[{\"Id\":1,\"Steps\":[{\"Id\":\"x\",\"Out\":\"bad\"}]}]}")]
    public void BadDataGivesEmptyQueue(string? json) => Assert.Empty(Core.Persistence.JobQueueCodec.Decode(json).Jobs);
}
