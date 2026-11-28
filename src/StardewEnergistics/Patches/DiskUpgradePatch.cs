using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StardewEnergistics.Storage;
using StardewValley;
using StardewValley.Inventories;

namespace StardewEnergistics.Patches;

/// <summary>
/// Crafting a disk from the tier below keeps everything stored: the new disk takes over the old disk's ID.
/// <para>
/// The crafting page also calls <see cref="CraftingRecipe.createItem"/> just to show a hover tooltip, so the
/// hand-over can't happen there. Instead <c>createItem</c> only remembers the disk it made, and the hand-over
/// happens when the game really uses up the ingredients (<see cref="CraftingRecipe.consumeIngredients"/>),
/// which it does right after creating the crafted item. The game takes ingredients from the player's
/// inventory first, then the page's containers, so the old disk is the first matching one in that order.
/// </para>
/// </summary>
internal static class DiskUpgradePatch
{
    private static DiskStore? disks;

    /// <summary>The disk the last <c>createItem</c> call made, and its recipe (per split-screen player).</summary>
    private static readonly StardewModdingAPI.Utilities.PerScreen<(CraftingRecipe Recipe, Item Result)?> lastCreated = new();

    public static void Apply(Harmony harmony, DiskStore store)
    {
        disks = store;
        harmony.Patch(
            original: AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.createItem)),
            postfix: new HarmonyMethod(typeof(DiskUpgradePatch), nameof(AfterCreateItem)));
        harmony.Patch(
            original: AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.consumeIngredients)),
            prefix: new HarmonyMethod(typeof(DiskUpgradePatch), nameof(BeforeConsumeIngredients)));
    }

    private static void AfterCreateItem(CraftingRecipe __instance, Item __result)
    {
        if (DiskStore.IsDisk(__result) && OldDiskId(__instance) != null)
            lastCreated.Value = (__instance, __result);
    }

    /// <remarks>Reads the containers from <c>__args</c> so the patch doesn't depend on the game's parameter name.</remarks>
    private static void BeforeConsumeIngredients(CraftingRecipe __instance, object[] __args)
    {
        var additionalMaterials = __args.Length > 0 ? __args[0] as IEnumerable<IInventory> : null;
        var pending = lastCreated.Value;
        lastCreated.Value = null;
        if (disks == null || pending is not { } p || p.Recipe != __instance || OldDiskId(__instance) is not { } oldDiskId)
            return;

        var old = FindDisk(Game1.player.Items, oldDiskId)
            ?? (additionalMaterials ?? Enumerable.Empty<IInventory>()).Select(c => FindDisk(c, oldDiskId)).FirstOrDefault(i => i != null);
        if (old == null || !old.modData.TryGetValue(Ids.Data.DiskId, out string? id) || string.IsNullOrEmpty(id))
            return; // the old disk was never used: nothing to carry over

        p.Result.modData[Ids.Data.DiskId] = id;
        disks.Upgrade(id, p.Result);
        old.modData.Remove(Ids.Data.DiskId); // the old item is about to be consumed
    }

    /// <summary>The qualified ID of the recipe's disk ingredient, if it has one.</summary>
    private static string? OldDiskId(CraftingRecipe recipe) => recipe.recipeList.Keys
        .Select(id => ItemRegistry.QualifyItemId(id) ?? id)
        .FirstOrDefault(id => DiskStore.IsDisk(ItemRegistry.Create(id, allowNull: true)));

    private static Item? FindDisk(IEnumerable<Item?> items, string qualifiedId) =>
        items.FirstOrDefault(i => i != null && i.QualifiedItemId == qualifiedId);
}
