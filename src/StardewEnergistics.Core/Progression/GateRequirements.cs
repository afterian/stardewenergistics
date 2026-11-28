namespace StardewEnergistics.Core.Progression;

/// <summary>One condition for Maru's next letter, with how far the player has got. i18n: guide.req.&lt;Key&gt;.</summary>
public sealed record GateRequirement(string Key, int Have, int Need)
{
    public bool Met => Have >= Need;
}

/// <summary>
/// The same tier gates as <see cref="GateEvaluator"/>, spelled out one by one so the journal can show
/// "Mines: 64/80". A tier unlocks when its requirements and every earlier tier's are met.
/// </summary>
public static class GateRequirements
{
    public static IReadOnlyList<GateRequirement> For(TierId tier, ProgressSnapshot s)
    {
        int B(bool v) => v ? 1 : 0;
        GateRequirement Built(TierId t) => new($"built{(int)t}", B(s.TiersBuilt.Contains(t)), 1);

        return tier switch
        {
            TierId.T1Components => new[] { new GateRequirement("maru", s.MaruHearts, 2) },
            TierId.T2SimpleStorage => new[]
            {
                new GateRequirement("processors", s.CopperProcessorsCrafted, 10),
                new GateRequirement("mines", s.DeepestMineLevel, 20),
            },
            TierId.T3Automation => new[]
            {
                Built(TierId.T2SimpleStorage),
                new GateRequirement("mines", s.DeepestMineLevel, 80),
                new GateRequirement("maru", s.MaruHearts, 4),
            },
            TierId.T4Autocrafting => new[]
            {
                Built(TierId.T3Automation),
                new GateRequirement("mines", s.DeepestMineLevel, 120),
                new GateRequirement("maru", s.MaruHearts, 6),
            },
            TierId.T5LinkedBuildings => new[]
            {
                Built(TierId.T4Autocrafting),
                new GateRequirement("skull", s.DeepestSkullCavernLevel, 25),
                new GateRequirement("iridiumBar", B(s.HasIridiumBar), 1),
                new GateRequirement("maru", s.MaruHearts, 8),
            },
            TierId.T6WholeFarm => new[]
            {
                Built(TierId.T5LinkedBuildings),
                new GateRequirement("island", B(s.ReachedGingerIsland), 1),
                new GateRequirement("walnuts", B(s.QiWalnutRoomOpen), 1),
                new GateRequirement("shard", B(s.HasPrismaticShard), 1),
            },
            _ => Array.Empty<GateRequirement>(),
        };
    }

    /// <summary>True when this tier and every tier before it have all their requirements met.</summary>
    public static bool AllMet(TierId tier, ProgressSnapshot s) =>
        Enum.GetValues<TierId>().Where(t => t <= tier).All(t => For(t, s).All(r => r.Met));
}
