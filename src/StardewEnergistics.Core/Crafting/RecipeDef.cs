using StardewEnergistics.Core.Storage;

namespace StardewEnergistics.Core.Crafting;

public enum RecipeKind
{
    Craft,
    Furnace,
    Cooking,
}

public readonly record struct ItemAmount(ItemKey Item, int Count);

/// <summary>One recipe the planner can use: a crafting recipe, a Furnace rule, or a cooking recipe.</summary>
public sealed record RecipeDef(string Id, IReadOnlyList<ItemAmount> In, ItemAmount Out, RecipeKind Kind);
