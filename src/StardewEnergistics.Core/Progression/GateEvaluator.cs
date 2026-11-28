namespace StardewEnergistics.Core.Progression;

/// <summary>
/// Decides which tiers are unlocked from hearts, mine depth, items crafted and tiers already built.
/// Each tier also needs the previous tier unlocked; from T3 on it needs the previous tier built.
/// A tier already built counts as unlocked whatever its requirements say now.
/// </summary>
public sealed class GateEvaluator
{
    /// <param name="gatesEnabled">Config switch; false unlocks everything.</param>
    public GateEvaluator(bool gatesEnabled = true) => GatesEnabled = gatesEnabled;

    public bool GatesEnabled { get; }

    public IReadOnlySet<TierId> Unlocked(ProgressSnapshot s)
    {
        var result = new HashSet<TierId>();
        if (!GatesEnabled)
        {
            foreach (var t in Enum.GetValues<TierId>())
                result.Add(t);
            return result;
        }

        bool Built(TierId t) => s.TiersBuilt.Contains(t);

        // A tier the farm has built stays passed: spending the Iridium Bar or losing a heart after Maru's
        // letter must not lock the tiers after it.
        if (Built(TierId.T1Components) || s.MaruHearts >= 2)
            result.Add(TierId.T1Components);
        else
            return result;

        if (Built(TierId.T2SimpleStorage) || s.CopperProcessorsCrafted >= 10 && s.DeepestMineLevel >= 20)
            result.Add(TierId.T2SimpleStorage);
        else
            return result;

        if (Built(TierId.T3Automation) || Built(TierId.T2SimpleStorage) && s.DeepestMineLevel >= 80 && s.MaruHearts >= 4)
            result.Add(TierId.T3Automation);
        else
            return result;

        if (Built(TierId.T4Autocrafting) || Built(TierId.T3Automation) && s.DeepestMineLevel >= 120 && s.MaruHearts >= 6)
            result.Add(TierId.T4Autocrafting);
        else
            return result;

        if (Built(TierId.T5LinkedBuildings) || Built(TierId.T4Autocrafting) && s.DeepestSkullCavernLevel >= 25 && s.HasIridiumBar && s.MaruHearts >= 8)
            result.Add(TierId.T5LinkedBuildings);
        else
            return result;

        if (Built(TierId.T6WholeFarm) || Built(TierId.T5LinkedBuildings) && s.ReachedGingerIsland && s.QiWalnutRoomOpen && s.HasPrismaticShard)
            result.Add(TierId.T6WholeFarm);

        return result;
    }
}
