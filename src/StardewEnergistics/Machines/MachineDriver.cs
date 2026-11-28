using System.Collections.Generic;
using System.Linq;
using StardewEnergistics.Content;
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

    /// <summary>True if the machine has finished output to collect (an incubating egg never counts).</summary>
    public static bool IsReady(SObject machine) =>
        machine.readyForHarvest.Value && machine.heldObject.Value != null && DataOf(machine)?.IsIncubator != true;

    /// <summary>Whether the machine would take this item now (probe; nothing is used).</summary>
    public static bool Accepts(SObject machine, ItemKey key, long available, NetworkStorage storage)
    {
        var item = StardewItemKeys.Create(key, (int)System.Math.Min(999, System.Math.Max(1, available)));
        var extras = ExtrasInventory(machine, key, storage, take: false);
        return WithAutoLoad(extras, () => machine.performObjectDropInAction(item, true, Game1.MasterPlayer));
    }

    /// <summary>Loads up to <paramref name="usable"/> of an item; returns true if the machine started.</summary>
    public static bool Load(SObject machine, ItemKey key, long usable, NetworkStorage storage)
    {
        var item = storage.Take(key, (int)System.Math.Min(999, usable));
        if (item == null)
            return false;

        var extras = ExtrasInventory(machine, key, storage, take: true);
        bool loaded = WithAutoLoad(extras, () => machine.performObjectDropInAction(item, false, Game1.MasterPlayer));

        // Put back whatever wasn't used.
        if (item.Stack > 0)
            storage.Put(item);
        foreach (var extra in extras.Where(e => e != null && e.Stack > 0).ToList())
            storage.Put(extra!);
        return loaded;
    }

    /// <summary>Takes finished output out of the machine and restarts it like a player would. Returns it, or null.</summary>
    public static Item? Collect(SObject machine)
    {
        if (!IsReady(machine))
            return null;
        Recalculate(machine);
        var output = machine.heldObject.Value;
        if (output == null)
            return null;
        Finish(machine, output);
        return output;
    }

    /// <summary>
    /// Collects finished output into storage. Anything that doesn't fit stays in the machine (it isn't
    /// reset), so a full network never loses output. Returns true if everything was stored.
    /// </summary>
    public static bool CollectInto(SObject machine, NetworkStorage storage)
    {
        if (!IsReady(machine))
            return false;
        Recalculate(machine);
        var output = machine.heldObject.Value;
        if (output == null)
            return false;

        var copy = output.getOne();
        copy.Stack = output.Stack;
        var left = storage.Put(copy);
        if (left != null && left.Stack > 0)
        {
            output.Stack = left.Stack; // the stored part is gone; the rest waits in the machine
            return false;
        }
        Finish(machine, output);
        return true;
    }

    /// <summary>Some outputs (e.g. Bee House honey) are worked out again when collected.</summary>
    private static void Recalculate(SObject machine)
    {
        var data = DataOf(machine);
        if (data == null || machine.lastOutputRuleId.Value == null)
            return;
        var rule = data.OutputRules?.FirstOrDefault(r => r.Id == machine.lastOutputRuleId.Value);
        if (rule?.RecalculateOnCollect != true)
            return;
        var previous = machine.heldObject.Value;
        machine.heldObject.Value = null;
        machine.OutputMachine(data, rule, machine.lastInputItem.Value, null, machine.Location, false);
        machine.heldObject.Value ??= previous;
    }

    /// <summary>
    /// Resets the machine after its output was taken, the way the game does when a player collects it:
    /// stats, then the machine's "output collected" rule (Crystalarium, Solar Panel, Mushroom Box… start again), then tappers.
    /// </summary>
    private static void Finish(SObject machine, Item output)
    {
        var data = DataOf(machine);
        MachineDataUtility.UpdateStats(data?.StatsToIncrementWhenHarvested, output, System.Math.Max(1, output.Stack));

        machine.heldObject.Value = null;
        machine.readyForHarvest.Value = false;
        machine.showNextIndex.Value = false;
        machine.ResetParentSheetIndex();

        if (data != null && MachineDataUtility.TryGetMachineOutputRule(machine, data, MachineOutputTrigger.OutputCollected, output.getOne(), null, machine.Location, out MachineOutputRule rule, out _, out _, out _))
            machine.OutputMachine(data, rule, machine.lastInputItem.Value, null, machine.Location, false);

        if (machine.IsTapper() && machine.Location.terrainFeatures.TryGetValue(machine.TileLocation, out var feature) && feature is StardewValley.TerrainFeatures.Tree tree)
            tree.UpdateTapperProduct(machine, output as SObject);
    }

    /// <summary>
    /// The machine's extra consumed items, copied (probe) or taken (load) from storage: the machine's own
    /// extras from Data/Machines (e.g. Coal for a Furnace), plus the Processor Press's per-processor parts.
    /// </summary>
    private static Inventory ExtrasInventory(SObject machine, ItemKey input, NetworkStorage storage, bool take)
    {
        var inv = new Inventory();
        var wanted = new List<(string Id, int Count)>();
        var own = DataOf(machine)?.AdditionalConsumedItems;
        if (own != null)
        {
            foreach (var extra in own)
                wanted.Add((ItemRegistry.QualifyItemId(extra.ItemId) ?? extra.ItemId, extra.RequiredCount));
        }
        if (machine.QualifiedItemId == "(BC)" + Ids.ProcessorPress && ProcessorPressOutput.ExtrasByBar.TryGetValue(input.QualifiedId, out var parts))
            wanted.AddRange(parts);

        foreach (var (id, count) in wanted)
        {
            var key = new ItemKey(id);
            long have = storage.Count(key);
            int n = (int)System.Math.Min(have, count);
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
