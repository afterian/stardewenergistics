using System.Collections.Generic;
using System.Linq;
using StardewEnergistics.Core.Storage;
using StardewEnergistics.Storage;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.Inventories;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Machines;

/// <summary>
/// Drives a vanilla (or modded) machine from Data/Machines with no player present: checks what it accepts,
/// loads an input plus any extra consumed items (e.g. Coal for a Furnace) from network storage, and collects output.
/// Extras are offered through <see cref="SObject.autoLoadFrom"/>, the same hook the game's hopper uses.
/// </summary>
internal static class MachineDriver
{
    public static MachineData? DataOf(SObject machine) => machine.GetMachineData();

    public static bool IsMachine(SObject? obj) => obj != null && obj.GetMachineData() != null && obj is not StardewValley.Objects.Chest;

    public static bool IsIdle(SObject machine) => machine.heldObject.Value == null && !machine.readyForHarvest.Value;

    public static bool IsReady(SObject machine) => machine.readyForHarvest.Value && machine.heldObject.Value != null;

    /// <summary>Whether the machine would take this item now (probe; nothing is used).</summary>
    public static bool Accepts(SObject machine, ItemKey key, long available, NetworkStorage storage)
    {
        var item = StardewItemKeys.Create(key, (int)System.Math.Min(999, System.Math.Max(1, available)));
        var extras = ExtrasInventory(machine, storage, take: false);
        return WithAutoLoad(extras, () => machine.performObjectDropInAction(item, true, Game1.MasterPlayer));
    }

    /// <summary>Loads up to <paramref name="usable"/> of an item; returns true if the machine started.</summary>
    public static bool Load(SObject machine, ItemKey key, long usable, NetworkStorage storage)
    {
        var item = storage.Take(key, (int)System.Math.Min(999, usable));
        if (item == null)
            return false;

        var extras = ExtrasInventory(machine, storage, take: true);
        bool loaded = WithAutoLoad(extras, () => machine.performObjectDropInAction(item, false, Game1.MasterPlayer));

        // Put back whatever wasn't used.
        if (item.Stack > 0)
            storage.Put(item);
        foreach (var extra in extras.Where(e => e != null && e.Stack > 0).ToList())
            storage.Put(extra!);
        return loaded;
    }

    /// <summary>Takes finished output out of the machine. Returns it, or null.</summary>
    public static Item? Collect(SObject machine)
    {
        if (!IsReady(machine))
            return null;
        var output = machine.heldObject.Value;
        machine.heldObject.Value = null;
        machine.readyForHarvest.Value = false;
        machine.showNextIndex.Value = false;
        machine.ResetParentSheetIndex();
        return output;
    }

    /// <summary>The machine's extra consumed items, copied (probe) or taken (load) from storage.</summary>
    private static Inventory ExtrasInventory(SObject machine, NetworkStorage storage, bool take)
    {
        var inv = new Inventory();
        var data = DataOf(machine);
        if (data?.AdditionalConsumedItems == null)
            return inv;

        foreach (var extra in data.AdditionalConsumedItems)
        {
            string id = ItemRegistry.QualifyItemId(extra.ItemId) ?? extra.ItemId;
            var key = new ItemKey(id);
            long have = storage.Count(key);
            int n = (int)System.Math.Min(have, extra.RequiredCount);
            if (n <= 0)
                continue;
            var item = take ? storage.Take(key, n) : StardewItemKeys.Create(key, n);
            if (item != null)
                inv.Add(item);
        }
        return inv;
    }

    private static bool WithAutoLoad(IInventory extras, System.Func<bool> action)
    {
        var old = SObject.autoLoadFrom;
        SObject.autoLoadFrom = extras;
        try
        {
            return action();
        }
        finally
        {
            SObject.autoLoadFrom = old;
        }
    }

    /// <summary>Qualified machine ID on a tile, if it's a machine.</summary>
    public static string? MachineIdAt(GameLocation location, int x, int y) =>
        location.objects.TryGetValue(new Microsoft.Xna.Framework.Vector2(x, y), out var obj) && IsMachine(obj) ? obj.QualifiedItemId : null;
}
