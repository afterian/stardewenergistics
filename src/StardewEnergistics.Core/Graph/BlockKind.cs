namespace StardewEnergistics.Core.Graph;

/// <summary>Every kind of network block the graph knows about.</summary>
public enum BlockKind
{
    Core,
    StorageRack,
    PowerRack,
    CraftingRack,
    AdvCraftingRack,
    Terminal,
    ChestLink,
    MachineLink,
    ShippingLink,
    CoalGenerator,
    PowerFeed,
    Port,
    Uplink,
}

public static class BlockKindExt
{
    /// <summary>Racks join a network by touching the Core or another rack, not through wire.</summary>
    public static bool IsRack(this BlockKind k) =>
        k is BlockKind.StorageRack or BlockKind.PowerRack or BlockKind.CraftingRack or BlockKind.AdvCraftingRack;
}
