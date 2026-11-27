using StardewEnergistics.Core.Storage;

namespace StardewEnergistics.Core.Crafting;

/// <summary>One step in a plan: run <see cref="Recipe"/> <see cref="Times"/> times.</summary>
public sealed record CraftStep(RecipeDef Recipe, int Times);

/// <summary>Ordered steps (leaves first), items taken from stock, and anything missing.</summary>
public sealed record CraftPlan(IReadOnlyList<CraftStep> Steps, IReadOnlyList<ItemAmount> FromStock, IReadOnlyList<ItemAmount> Missing)
{
    public bool CanRun => Missing.Count == 0;
}

/// <summary>Expands a recipe tree, using stock first and sub-recipes for the rest.</summary>
public sealed class CraftPlanner
{
    public CraftPlan Plan(ItemKey target, int n, RecipeBook book, IStockView stock) => throw new NotImplementedException();
}
