using System.Collections.Generic;
using System.Linq;
using StardewEnergistics.Content;
using StardewEnergistics.Core.Crafting;
using StardewEnergistics.Core.Storage;
using StardewEnergistics.Storage;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;

namespace StardewEnergistics.Crafting;

/// <summary>
/// Builds the planner's <see cref="RecipeBook"/> from the crafting recipes any player on the farm knows,
/// plus the Furnace's smelting rules from Data/Machines.
/// Left out: recipes with category ingredients (e.g. "any egg"), big craftable or non-stackable ingredients,
/// and anything involving storage disks (their contents live outside the item).
/// </summary>
internal static class RecipeSource
{
    public const string FurnaceId = "(BC)" + VanillaIds.FurnaceBig;

    public static RecipeBook Build(IMonitor? monitor = null)
    {
        var known = new HashSet<string>();
        foreach (var farmer in Game1.getAllFarmers())
            foreach (string name in farmer.craftingRecipes.Keys)
                known.Add(name);

        var recipes = new List<RecipeDef>();
        foreach (string name in known.OrderBy(n => n))
        {
            if (!CraftingRecipe.craftingRecipes.ContainsKey(name))
                continue;
            try
            {
                if (FromCrafting(new CraftingRecipe(name, false)) is { } def)
                    recipes.Add(def);
            }
            catch (System.Exception e)
            {
                monitor?.Log($"Skipped recipe {name} for autocrafting: {e.Message}", LogLevel.Trace);
            }
        }
        recipes.AddRange(FurnaceRules());
        return new RecipeBook(recipes);
    }

    private static string? Qualify(string id) => ItemRegistry.IsQualifiedItemId(id) ? id : ItemRegistry.QualifyItemId(id);

    private static RecipeDef? FromCrafting(CraftingRecipe r)
    {
        if (r.itemToProduce == null || r.itemToProduce.Count == 0)
            return null;
        string raw = r.itemToProduce[0];
        string? output = ItemRegistry.IsQualifiedItemId(raw) ? raw : (r.bigCraftable ? "(BC)" : "(O)") + raw;
        if (output == null || DiskTierOf(output) || !ItemRegistry.Exists(output))
            return null;

        var inputs = new List<ItemAmount>();
        foreach (var (id, count) in r.recipeList)
        {
            if (id.StartsWith("-"))
                return null; // category ingredient
            string? q = Qualify(id);
            if (q == null || DiskTierOf(q) || !IsStackable(q))
                return null;
            inputs.Add(new ItemAmount(new ItemKey(q), count));
        }
        if (inputs.Count == 0)
            return null;
        return new RecipeDef(r.name, inputs, new ItemAmount(new ItemKey(output), System.Math.Max(1, r.numberProducedPerCraft)), RecipeKind.Craft);
    }

    /// <summary>Simple Furnace rules: one required item and count in, one fixed item out, plus its extra (Coal).</summary>
    public static IEnumerable<RecipeDef> FurnaceRules()
    {
        if (!DataLoader.Machines(Game1.content).TryGetValue(FurnaceId, out MachineData? data) || data.OutputRules == null)
            yield break;

        var extras = (data.AdditionalConsumedItems ?? new())
            .Select(e => Qualify(e.ItemId) is { } q ? new ItemAmount(new ItemKey(q), e.RequiredCount) : (ItemAmount?)null)
            .Where(e => e != null).Select(e => e!.Value).ToList();

        foreach (var rule in data.OutputRules)
        {
            var output = rule.OutputItem?.FirstOrDefault();
            if (output?.ItemId == null || output.OutputMethod != null || Qualify(output.ItemId) is not { } outId || !IsStackable(outId))
                continue;
            int outCount = System.Math.Max(1, output.MinStack);
            foreach (var trigger in rule.Triggers ?? new())
            {
                if (!trigger.Trigger.HasFlag(MachineOutputTrigger.ItemPlacedInMachine) || trigger.RequiredItemId == null
                    || (trigger.RequiredTags?.Count ?? 0) > 0 || !string.IsNullOrEmpty(trigger.Condition))
                    continue;
                if (Qualify(trigger.RequiredItemId) is not { } inId)
                    continue;
                var inputs = new List<ItemAmount> { new(new ItemKey(inId), System.Math.Max(1, trigger.RequiredCount)) };
                inputs.AddRange(extras);
                yield return new RecipeDef($"furnace:{inId}", inputs, new ItemAmount(new ItemKey(outId), outCount), RecipeKind.Furnace);
            }
        }
    }

    /// <summary>The ingredient that goes in the machine (the extras come from storage on their own).</summary>
    public static ItemAmount MainInput(RecipeDef r) => r.In[0];

    private static bool DiskTierOf(string qualifiedId) => DiskStore.IsDisk(ItemRegistry.Create(qualifiedId, allowNull: true));

    private static readonly Dictionary<string, bool> stackable = new();

    /// <summary>Whether the item can be counted in network storage (plain stackable object).</summary>
    public static bool IsStackable(string qualifiedId)
    {
        if (!stackable.TryGetValue(qualifiedId, out bool ok))
        {
            var item = ItemRegistry.Create(qualifiedId, allowNull: true);
            stackable[qualifiedId] = ok = item != null && StardewItemKeys.TryGetKey(item, out _);
        }
        return ok;
    }
}
