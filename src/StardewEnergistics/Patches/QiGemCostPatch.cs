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
        var prefix = new HarmonyMethod(typeof(QiGemCostPatch), nameof(AddBalanceToItems));
        harmony.Patch(AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.doesFarmerHaveIngredientsInInventory)), prefix: prefix);
        harmony.Patch(AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.drawRecipeDescription)), prefix: prefix);
        harmony.Patch(AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.getCraftableCount), new[] { typeof(IList<Item>) }), prefix: prefix);
        harmony.Patch(AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.getCraftableCount), new[] { typeof(IList<Chest>) }),
            prefix: new HarmonyMethod(typeof(QiGemCostPatch), nameof(AddBalanceToChests)));
        harmony.Patch(AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.consumeIngredients)),
            prefix: new HarmonyMethod(typeof(QiGemCostPatch), nameof(BeforeConsume)),
            postfix: new HarmonyMethod(typeof(QiGemCostPatch), nameof(AfterConsume)));
    }

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

    /// <summary>Adds the balance to the method's list of extra items (the last argument that is one).</summary>
    private static void AddBalanceToItems(CraftingRecipe __instance, object?[] __args)
    {
        if (!Applies(__instance))
            return;
        int i = __args.Length - 1;
        if (__args[i] is IList<Item> items && items.Any(IsBalance))
            return;
        var list = __args[i] is IEnumerable<Item> given ? new List<Item>(given) : new List<Item>();
        list.Add(Balance());
        __args[i] = list;
    }

    private static void AddBalanceToChests(CraftingRecipe __instance, object?[] __args)
    {
        if (!Applies(__instance))
            return;
        if (__args[0] is IList<Chest> chests && chests.Any(c => c.Items.Any(IsBalance)))
            return;
        var list = __args[0] is IEnumerable<Chest> given ? new List<Chest>(given) : new List<Chest>();
        var chest = new Chest();
        chest.Items.Add(Balance());
        list.Add(chest);
        __args[0] = list;
    }

    /// <summary>Adds the balance as one more container, after the backpack and the page's chests.</summary>
    private static void BeforeConsume(CraftingRecipe __instance, object?[] __args, out Inventory? __state)
    {
        __state = null;
        if (!Applies(__instance))
            return;
        var list = __args[0] is IEnumerable<IInventory> given ? new List<IInventory>(given) : new List<IInventory>();
        __state = new Inventory();
        __state.Add(Balance());
        list.Add(__state);
        __args[0] = list;
    }

    /// <summary>Takes the Qi Gems the recipe used from the balance.</summary>
    private static void AfterConsume(Inventory? __state)
    {
        if (__state == null)
            return;
        Game1.player.QiGems = __state.Where(i => i != null).Sum(i => i.Stack);
    }
}
