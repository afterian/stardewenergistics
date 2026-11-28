using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Content;

/// <summary>
/// Output method for the Processor Press. Data/Machines can only take extra ingredients for the whole
/// machine, but each processor needs different ones, so this method checks and takes them from the player.
/// Referenced from Data/Machines as "StardewEnergistics.Content.ProcessorPressOutput, StardewEnergistics: GetOutput".
/// </summary>
public static class ProcessorPressOutput
{
    internal static ITranslationHelper? Translation;

    /// <summary>Extra items (qualified ID, count) needed besides the bar that triggers the rule.</summary>
    internal static readonly IReadOnlyDictionary<string, (string Id, int Count)[]> ExtrasByBar = new Dictionary<string, (string, int)[]>
    {
        [VanillaIds.Obj(VanillaIds.CopperBar)] = new[] { (VanillaIds.Obj(VanillaIds.Quartz), 1) },
        [VanillaIds.Obj(VanillaIds.GoldBar)] = new[] { (VanillaIds.Obj(VanillaIds.RefinedQuartz), 1), (VanillaIds.Obj(Ids.CopperProcessor), 1) },
        [VanillaIds.Obj(VanillaIds.IridiumBar)] = new[] { (VanillaIds.Obj(VanillaIds.Diamond), 1), (VanillaIds.Obj(Ids.GoldProcessor), 1) },
    };

    /// <summary>Matches the game's MachineOutputDelegate (Stardew 1.6.9+).</summary>
    public static Item? GetOutput(SObject machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
    {
        overrideMinutesUntilReady = null;
        if (player == null || inputItem == null || !ExtrasByBar.TryGetValue(inputItem.QualifiedItemId, out var extras))
            return null;

        // A Machine Link loads extras from network items through autoLoadFrom, like a hopper.
        var source = SObject.autoLoadFrom ?? player.Items;
        var missing = extras.Where(x => source.CountId(x.Id) < x.Count).ToList();
        if (missing.Count > 0)
        {
            if (!probe)
            {
                string names = string.Join(", ", missing.Select(x => $"{x.Count} {ItemRegistry.GetData(x.Id)?.DisplayName ?? x.Id}"));
                string text = Translation?.Get("press.needs", new { items = names }).ToString() ?? $"Also needs: {names}.";
                Game1.showRedMessage(text);
            }
            return null;
        }

        if (!probe)
        {
            foreach (var (id, count) in extras)
                source.ReduceId(id, count);
            if (outputData.ItemId == "(O)" + Ids.CopperProcessor)
                player.stats.Increment(Ids.StatCopperProcessors);
        }

        return ItemRegistry.Create(outputData.ItemId);
    }
}
