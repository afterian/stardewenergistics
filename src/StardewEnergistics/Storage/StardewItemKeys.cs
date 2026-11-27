using System.Linq;
using StardewEnergistics.Core.Storage;
using StardewValley;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Storage;

/// <summary>
/// Converts between game items and Core <see cref="ItemKey"/>s. For now only plain stackable objects
/// (crops, ores, bars, forage…, with quality) can go on disks. Flavoured preserves, coloured items, tools,
/// rings and weapons need the whole-item store (spike S4) and stay in linked chests until then.
/// </summary>
internal static class StardewItemKeys
{
    public static bool TryGetKey(Item? item, out ItemKey key)
    {
        key = default;
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

    public static Item Create(ItemKey key, int count) => ItemRegistry.Create(key.QualifiedId, count, key.Quality);
}
