using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StardewEnergistics.Content;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Objects;

namespace StardewEnergistics.Patches;

/// <summary>
/// Lets crafting recipes that call for Qi Gems spend them from the player's Qi Gem balance.
/// <para>
/// The game never keeps Qi Gems in the backpack: picking them up adds them to <see cref="Farmer.QiGems"/>
/// (the Walnut Room currency). A recipe with Qi Gems as an ingredient could therefore never be crafted.
/// This patch hands the crafting checks one extra, virtual stack of Qi Gems the size of the player's balance,
/// so the crafting menu shows and counts them like any other ingredient, and when the ingredients are used up
/// it takes what was spent from that stack out of the balance.
/// </para>
/// </summary>
internal static class QiGemCostPatch
{
    private const string QiGemId = "(O)" + VanillaIds.QiGem;
    private const string MarkerKey = Ids.Prefix + "QiGemBalance";

    public static void Apply(Harmony harmony)
    {
        // The prefixes name the game's parameters (ref) so they can hand the method a new list.
        harmony.Patch(AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.doesFarmerHaveIngredientsInInventory)),
            prefix: new HarmonyMethod(typeof(QiGemCostPatch), nameof(BeforeHaveIngredients)));
        harmony.Patch(AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.drawRecipeDescription)),
            prefix: new HarmonyMethod(typeof(QiGemCostPatch), nameof(BeforeDrawDescription)));
        harmony.Patch(AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.getCraftableCount), new[] { typeof(IList<Item>) }),
            prefix: new HarmonyMethod(typeof(QiGemCostPatch), nameof(BeforeCountItems)));
        harmony.Patch(AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.getCraftableCount), new[] { typeof(IList<Chest>) }),
            prefix: new HarmonyMethod(typeof(QiGemCostPatch), nameof(BeforeCountChests)));
        harmony.Patch(AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.consumeIngredients)),
            prefix: new HarmonyMethod(typeof(QiGemCostPatch), nameof(BeforeConsume)),
            postfix: new HarmonyMethod(typeof(QiGemCostPatch), nameof(AfterConsume)));
    }

    private static void BeforeHaveIngredients(CraftingRecipe __instance, ref IList<Item>? extraToCheck) => extraToCheck = WithBalance(__instance, extraToCheck);
    private static void BeforeDrawDescription(CraftingRecipe __instance, ref IList<Item>? additional_crafting_items) => additional_crafting_items = WithBalance(__instance, additional_crafting_items);
    private static void BeforeCountItems(CraftingRecipe __instance, ref IList<Item>? additional_materials) => additional_materials = WithBalance(__instance, additional_materials);

    /// <summary>Whether a recipe uses Qi Gems and the player has a balance to spend.</summary>
    private static bool Applies(CraftingRecipe recipe) =>
        Game1.player?.QiGems > 0 && recipe.recipeList.Keys.Any(id => (ItemRegistry.QualifyItemId(id) ?? id) == QiGemId);

    /// <summary>A stack of Qi Gems standing for the player's balance (marked so it's never added twice).</summary>
    private static Item Balance()
    {
        var item = ItemRegistry.Create(QiGemId, Game1.player.QiGems);
        item.Stack = Game1.player.QiGems; // above the usual stack limit if need be
        item.modData[MarkerKey] = "1";
        return item;
    }

    private static bool IsBalance(Item? item) => item?.modData.ContainsKey(MarkerKey) == true;

    /// <summary>The list of extra items with the balance added, or the same list when it doesn't apply.</summary>
    private static IList<Item>? WithBalance(CraftingRecipe recipe, IList<Item>? items)
    {
        if (!Applies(recipe) || items?.Any(IsBalance) == true)
            return items;
        var list = items != null ? new List<Item>(items) : new List<Item>();
        list.Add(Balance());
        return list;
    }

    private static void BeforeCountChests(CraftingRecipe __instance, ref IList<Chest>? additional_material_chests)
    {
        if (!Applies(__instance) || additional_material_chests?.Any(c => c.Items.Any(IsBalance)) == true)
            return;
        var list = additional_material_chests != null ? new List<Chest>(additional_material_chests) : new List<Chest>();
        var chest = new Chest();
        chest.Items.Add(Balance());
        list.Add(chest);
        additional_material_chests = list;
    }

    /// <summary>Adds the balance as one more container, after the backpack and the page's chests.</summary>
    private static void BeforeConsume(CraftingRecipe __instance, ref List<IInventory>? additionalMaterials, out Inventory? __state)
    {
        __state = null;
        if (!Applies(__instance))
            return;
        additionalMaterials = additionalMaterials != null ? new List<IInventory>(additionalMaterials) : new List<IInventory>();
        __state = new Inventory();
        __state.Add(Balance());
        additionalMaterials.Add(__state);
    }

    /// <summary>Takes the Qi Gems the recipe used from the balance.</summary>
    private static void AfterConsume(Inventory? __state)
    {
        if (__state == null)
            return;
        Game1.player.QiGems = __state.Where(i => i != null).Sum(i => i.Stack);
    }
}
