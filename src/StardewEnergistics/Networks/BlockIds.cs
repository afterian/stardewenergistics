using System.Collections.Generic;
using StardewEnergistics.Core.Graph;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Networks;

/// <summary>Maps placed game objects to Core block kinds.</summary>
internal static class BlockIds
{
    private static readonly Dictionary<string, BlockKind> ByQualifiedId = new()
    {
        ["(BC)" + Ids.ComputerCore] = BlockKind.Core,
        ["(BC)" + Ids.StorageRack] = BlockKind.StorageRack,
        ["(BC)" + Ids.PowerRack] = BlockKind.PowerRack,
        ["(BC)" + Ids.NetworkTerminal] = BlockKind.Terminal,
        ["(BC)" + Ids.CoalGenerator] = BlockKind.CoalGenerator,
        ["(BC)" + Ids.CraftingRack] = BlockKind.CraftingRack,
        ["(BC)" + Ids.AdvancedCraftingRack] = BlockKind.AdvCraftingRack,
        ["(BC)" + Ids.NetworkPort] = BlockKind.Port,
        ["(BC)" + Ids.IslandUplink] = BlockKind.Uplink,
    };

    /// <summary>The block kind of a placed object, or null if it isn't part of a network.</summary>
    public static BlockKind? KindOf(SObject obj)
    {
        if (ByQualifiedId.TryGetValue(obj.QualifiedItemId, out var kind))
            return kind;
        if (obj is Chest && obj.modData.ContainsKey(Ids.Data.ChestLink))
            return BlockKind.ChestLink;
        return null;
    }

    public static bool IsBlock(SObject obj, BlockKind kind) => KindOf(obj) == kind;
}
