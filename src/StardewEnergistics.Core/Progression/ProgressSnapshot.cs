namespace StardewEnergistics.Core.Progression;

/// <summary>The facts tier gates look at, read from the game by the mod.</summary>
public sealed record ProgressSnapshot(
    int MaruHearts,
    int DeepestMineLevel,
    int DeepestSkullCavernLevel,
    int CopperProcessorsCrafted,
    bool HasIridiumBar,
    bool ReachedGingerIsland,
    bool QiWalnutRoomOpen,
    bool HasPrismaticShard,
    IReadOnlySet<TierId> TiersBuilt);
