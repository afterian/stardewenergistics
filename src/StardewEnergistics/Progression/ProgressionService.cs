using System.Collections.Generic;
using System.Linq;
using StardewEnergistics.Content;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.Core.Progression;
using StardewEnergistics.Networks;
using StardewEnergistics.Power;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace StardewEnergistics.Progression;

/// <summary>Each morning: records tiers built, checks tier gates, and has Maru mail new blueprints.</summary>
internal sealed class ProgressionService
{
    private readonly IMonitor monitor;
    private readonly NetworkManager networks;
    private readonly PowerService power;

    public ProgressionService(IMonitor monitor, NetworkManager networks, PowerService power)
    {
        this.monitor = monitor;
        this.networks = networks;
        this.power = power;
    }

    public bool GatesEnabled { get; set; } = true;

    public static HashSet<TierId> TiersBuilt()
    {
        var farm = Game1.getFarm();
        return farm.modData.TryGetValue(Ids.Data.TiersBuilt, out string? raw)
            ? raw.Split(',').Where(s => int.TryParse(s, out _)).Select(s => (TierId)int.Parse(s)).ToHashSet()
            : new HashSet<TierId>();
    }

    private static void MarkBuilt(TierId tier)
    {
        var built = TiersBuilt();
        if (built.Add(tier))
            Game1.getFarm().modData[Ids.Data.TiersBuilt] = string.Join(",", built.Select(t => (int)t).OrderBy(n => n));
    }

    public ProgressSnapshot Snapshot(Farmer player) => new(
        MaruHearts: player.getFriendshipHeartLevelForNPC("Maru"),
        DeepestMineLevel: player.deepestMineLevel,
        DeepestSkullCavernLevel: System.Math.Max(0, MineShaft.lowestLevelReached - 120),
        CopperProcessorsCrafted: (int)player.stats.Get(Ids.StatCopperProcessors),
        HasIridiumBar: player.Items.ContainsId("(O)337"),
        ReachedGingerIsland: player.hasOrWillReceiveMail("Visited_Island"),
        QiWalnutRoomOpen: false, // T6 gate details land in M6
        HasPrismaticShard: player.Items.ContainsId("(O)74"),
        TiersBuilt: TiersBuilt());

    public void OnDayStarted()
    {
        if (Context.IsMainPlayer)
            RecordBuiltTiers();

        var player = Game1.player;
        var unlocked = new GateEvaluator(GatesEnabled).Unlocked(Snapshot(player));
        foreach (var tier in ContentInjector.TiersWithMail.Where(unlocked.Contains))
        {
            string mail = ContentInjector.MailId(tier);
            if (!player.hasOrWillReceiveMail(mail))
            {
                player.mailbox.Add(mail);
                monitor.Log($"Maru sent the {tier} blueprints.", LogLevel.Info);
            }
        }
    }

    private void RecordBuiltTiers()
    {
        if (Game1.getAllFarmers().Any(f => f.craftingRecipes.ContainsKey(Ids.ProcessorPress) && f.stats.Get(Ids.StatCopperProcessors) > 0))
            MarkBuilt(TierId.T1Components);

        foreach (var loc in networks.AllWithCores())
        {
            foreach (var n in loc.Graph.Networks.Where(n => n.Working))
            {
                if (n.OfKind(BlockKind.StorageRack).Any() && n.OfKind(BlockKind.Terminal).Any() && power.IsPowered(loc, n))
                    MarkBuilt(TierId.T2SimpleStorage);
                if (power.IsPowered(loc, n) && loc.ObjectsOf(n, BlockKind.MachineLink).Any(o => Machines.MachineLinkService.SettingsOf(o).Configured))
                    MarkBuilt(TierId.T3Automation);
            }
        }
    }
}
