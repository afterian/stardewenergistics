using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StardewEnergistics.Storage;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;

namespace StardewEnergistics.Patches;

/// <summary>
/// Crafting a disk from the tier below keeps everything stored: the new disk takes over the old disk's ID.
/// The game creates the crafted item before it consumes ingredients, taking from the player's inventory first,
/// so the disk that will be used up is the first matching one there (then in the crafting page's containers).
/// </summary>
internal static class DiskUpgradePatch
{
    private static DiskStore? disks;

    public static void Apply(Harmony harmony, DiskStore store)
    {
        disks = store;
        harmony.Patch(
            original: AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.createItem)),
            postfix: new HarmonyMethod(typeof(DiskUpgradePatch), nameof(Postfix)));
    }

    private static void Postfix(CraftingRecipe __instance, Item __result)
    {
        if (disks == null || !DiskStore.IsDisk(__result))
            return;

        // Which ingredient is a disk?
        string? oldDiskId = __instance.recipeList.Keys
            .Select(id => ItemRegistry.QualifyItemId(id) ?? id)
            .FirstOrDefault(id => DiskStore.IsDisk(ItemRegistry.Create(id)));
        if (oldDiskId == null)
            return;

        var old = FindDisk(Game1.player.Items, oldDiskId) ?? Containers().Select(c => FindDisk(c, oldDiskId)).FirstOrDefault(i => i != null);
        if (old == null || !old.modData.TryGetValue(Ids.Data.DiskId, out string? id))
            return;

        __result.modData[Ids.Data.DiskId] = id;
        disks.Upgrade(id, __result);
        old.modData.Remove(Ids.Data.DiskId); // the old item is about to be consumed
    }

    private static Item? FindDisk(IEnumerable<Item?> items, string qualifiedId) =>
        items.FirstOrDefault(i => i != null && i.QualifiedItemId == qualifiedId);

    private static IEnumerable<IInventory> Containers()
    {
        var page = Game1.activeClickableMenu as CraftingPage ?? (Game1.activeClickableMenu as GameMenu)?.GetCurrentPage() as CraftingPage;
        return page?._materialContainers ?? (IEnumerable<IInventory>)new List<IInventory>();
    }
}
