using StardewEnergistics.Core.Storage;

namespace StardewEnergistics.Core.Crafting;

/// <summary>The recipes a player knows, indexed by output item.</summary>
public sealed class RecipeBook
{
    private readonly Dictionary<string, List<RecipeDef>> byOutput = new();

    public RecipeBook(IEnumerable<RecipeDef> recipes)
    {
        foreach (var r in recipes)
        {
            if (!byOutput.TryGetValue(r.Out.Item.QualifiedId, out var list))
                byOutput[r.Out.Item.QualifiedId] = list = new List<RecipeDef>();
            list.Add(r);
        }
    }

    /// <summary>Recipes that make this item, in the order they were added.</summary>
    public IReadOnlyList<RecipeDef> Making(ItemKey item) =>
        byOutput.TryGetValue(item.QualifiedId, out var list) ? list : Array.Empty<RecipeDef>();
}
