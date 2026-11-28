using System.Collections.Generic;
using System.Linq;
using StardewValley;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Storage;

/// <summary>
/// Finds disk contents that no disk item or rack refers to any more (for example a disk lost when a rack
/// was broken before blocks dropped their contents) and makes new disk items for them.
/// </summary>
internal static class DiskRecovery
{
    public static List<Item> Recover(DiskStore store)
    {
        var used = new HashSet<string>();
        void Note(Item item)
        {
            if (item.modData.TryGetValue(Ids.Data.DiskId, out string? id) && !string.IsNullOrEmpty(id))
                used.Add(id);
            if (item is SObject obj)
            {
                foreach (string slot in RackSlots.Get(obj, Ids.Data.RackDisks))
                    used.Add(slot.Split(':')[0]);
            }
        }

        Utility.ForEachItem(item =>
        {
            Note(item);
            return true;
        });
        foreach (var farmer in Game1.getAllFarmers())
        {
            foreach (var item in farmer.Items.Where(i => i != null))
                Note(item);
        }
        Utility.ForEachLocation(loc =>
        {
            foreach (var obj in loc.objects.Values)
                Note(obj);
            foreach (var debris in loc.debris)
            {
                if (debris.item != null)
                    Note(debris.item);
            }
            return true;
        });

        return store.All
            .Where(d => d.TotalItems > 0 && !used.Contains(d.Id.Value))
            .Select(store.CreateItem)
            .ToList();
    }
}
