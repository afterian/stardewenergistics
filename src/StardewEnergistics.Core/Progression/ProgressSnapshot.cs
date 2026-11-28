namespace StardewEnergistics.Core.Progression;

/// <summary>
/// The facts tier gates look at, read from the game by the mod. <see cref="LettersSent"/>: tiers whose letter Maru has already sent.
/// <see cref="TalkedToMaru"/>: the player has talked to Maru (or given her a gift) at least once.
/// </summary>
public sealed record ProgressSnapshot(
    int MaruHearts,
    int DeepestMineLevel,
    int DeepestSkullCavernLevel,
    int CopperProcessorsCrafted,
    bool HasIridiumBar,
    bool ReachedGingerIsland,
    bool QiWalnutRoomOpen,
    bool HasPrismaticShard,
    IReadOnlySet<TierId> TiersBuilt,
    IReadOnlySet<TierId>? LettersSent = null,
    bool TalkedToMaru = false)
{
    /// <summary>True once Maru has sent this tier's letter: its gate stays passed whatever happens later.</summary>
    public bool Sent(TierId tier) => LettersSent?.Contains(tier) == true;
}
