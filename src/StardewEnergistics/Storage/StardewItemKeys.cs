using System.Linq;
using StardewEnergistics.Core.Storage;
using StardewValley;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Storage;

/// <summary>
/// Converts between game items and Core <see cref="ItemKey"/>s (the fast path: items stored as counts).
/// Items this refuses still go on disks, whole (see NetworkStorage.PutWhole). Plain stackable objects (crops, ores,
/// bars, forage…, with quality) and plain tools (no enchantments, attachments or mod data)
/// can go on disks. Flavoured preserves, coloured items, rings, weapons and enchanted tools need the whole-item
/// store (spike S4) and stay in linked chests until then.
/// </summary>
internal static class StardewItemKeys
{
    public static bool TryGetKey(Item? item, out ItemKey key)
    {
        key = default;
        if (item is Tool tool)
            return TryGetToolKey(tool, out key);
        if (item is not SObject obj || item.GetType() != typeof(SObject))
            return false; // subclasses (chests, furniture, coloured objects…) keep extra state
        if (obj.bigCraftable.Value || obj.modData.Keys.Any() || DiskStore.IsDisk(obj))
            return false;
        if (obj.preserve.Value != null || !string.IsNullOrEmpty(obj.preservedParentSheetIndex.Value))
            return false;
        if (obj.questItem.Value || obj.IsRecipe)
            return false;

        key = new ItemKey(obj.QualifiedItemId, obj.Quality);
        return true;
    }

    /// <summary>
    /// A tool fresh from the shop or blacksmith: its ID says everything about it (upgrade level
    /// included), so it can be stored as a count and recreated. Watering cans come back full.
    /// </summary>
    private static bool TryGetToolKey(Tool tool, out ItemKey key)
    {
        key = default;
        if (tool.modData.Keys.Any() || tool.enchantments.Count > 0 || tool.attachments.Any(a => a != null))
            return false;
        if (tool is StardewValley.Tools.MeleeWeapon)
            return false; // weapons can carry a changed look or stats; keep them whole for now
        key = new ItemKey(tool.QualifiedItemId, 0);
        return true;
    }

    public static Item Create(ItemKey key, int count) => ItemRegistry.Create(key.QualifiedId, count, key.Quality);
}
