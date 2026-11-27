using System.Collections.Generic;
using System.Linq;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.Core.Power;
using StardewEnergistics.Networks;
using StardewEnergistics.Storage;
using StardewModdingAPI;
using StardewValley;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Power;

/// <summary>
/// Runs each network's daily power draw at the start of the day (host). Battery charge lives on each
/// Power Rack and fuel on each Coal Generator (modData), so they survive pick-up.
/// </summary>
internal sealed class PowerService
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly NetworkManager networks;
    private readonly PowerLedger ledger = new();

    public PowerService(IModHelper helper, IMonitor monitor, NetworkManager networks)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.networks = networks;
        helper.Events.World.ObjectListChanged += OnObjectListChanged;
    }

    public PowerTable Table { get; set; } = PowerTable.Default;

    public int DailyUse(Network n) => ledger.DailyUse(n, Table);

    public PowerState StateOf(LocationNetworks loc, Network n)
    {
        var charge = loc.ObjectsOf(n, BlockKind.PowerRack).SelectMany(r => RackSlots.GetInts(r, Ids.Data.RackCharge)).ToList();
        int fuel = loc.ObjectsOf(n, BlockKind.CoalGenerator).Sum(g => RackSlots.GetInt(g, Ids.Data.Fuel));
        int lastWarning = n.Core != null && loc.ObjectOf(n.Core).modData.TryGetValue(Ids.Data.LastWarningDay, out string? raw) && int.TryParse(raw, out int d) ? d : -1;
        return new PowerState(charge, fuel, lastWarning);
    }

    /// <summary>True if the network ran today (or, before its first morning, has enough power stored for a day).</summary>
    public bool IsPowered(LocationNetworks loc, Network n)
    {
        if (n.Core == null)
            return false;
        var core = loc.ObjectOf(n.Core);
        if (core.modData.TryGetValue(Ids.Data.Powered, out string? flag))
            return flag == "1";
        return StateOf(loc, n).Available >= DailyUse(n);
    }

    public void OnDayStarted()
    {
        if (!Context.IsMainPlayer)
            return;

        int today = (int)Game1.stats.DaysPlayed;
        foreach (var loc in networks.AllWithCores())
        {
            foreach (var n in loc.Graph.Networks.Where(n => n.Working))
            {
                int use = DailyUse(n);
                var day = ledger.RunDay(StateOf(loc, n), use);
                WriteBack(loc, n, day.After);

                var core = loc.ObjectOf(n.Core!);
                core.modData[Ids.Data.Powered] = day.Powered ? "1" : "0";

                var (state, warn) = ledger.CheckWarning(day.After, use, today);
                if (state.LastWarningDay == -1)
                    core.modData.Remove(Ids.Data.LastWarningDay);
                else
                    core.modData[Ids.Data.LastWarningDay] = state.LastWarningDay.ToString();

                int days = ledger.DaysRemaining(day.After, use);
                RackSlots.ShowFrame(core, !day.Powered ? 0 : days <= PowerLedger.WarningDays ? 3 : 1);

                string where = loc.Location.DisplayName;
                if (!day.Powered)
                    Game1.addHUDMessage(new HUDMessage(helper.Translation.Get("power.out", new { location = where })) { noIcon = true });
                else if (warn)
                    Game1.addHUDMessage(new HUDMessage(helper.Translation.Get("power.low", new { location = where, days })) { noIcon = true });

                monitor.Log($"{where}: power use {use}, powered={day.Powered}, {day.After.Available} left.", LogLevel.Trace);
            }
            ShowTerminals(loc);
        }
    }

    /// <summary>Shows each Network Terminal's "on" frame when its network is powered, "off" otherwise (host).</summary>
    public void ShowTerminals(LocationNetworks loc)
    {
        if (!Context.IsMainPlayer)
            return;
        foreach (var n in loc.Graph.Networks)
        {
            bool on = IsPowered(loc, n);
            foreach (var terminal in loc.ObjectsOf(n, BlockKind.Terminal))
                RackSlots.ShowFrame(terminal, on ? 1 : 0);
        }
    }

    /// <summary>A placed Terminal lights up at once; a picked-up one goes back to its "off" icon.</summary>
    private void OnObjectListChanged(object? sender, StardewModdingAPI.Events.ObjectListChangedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;
        foreach (var removed in e.Removed)
        {
            if (BlockIds.KindOf(removed.Value) == BlockKind.Terminal)
                RackSlots.ShowFrame(removed.Value, 0);
        }
        if (e.Added.Any(a => BlockIds.KindOf(a.Value) == BlockKind.Terminal))
            ShowTerminals(networks.For(e.Location));
    }

    /// <summary>Writes battery charge and fuel back onto the racks and generators, removing spent batteries.</summary>
    private static void WriteBack(LocationNetworks loc, Network n, PowerState s)
    {
        int i = 0;
        foreach (var rack in loc.ObjectsOf(n, BlockKind.PowerRack))
        {
            int slots = RackSlots.GetInts(rack, Ids.Data.RackCharge).Count;
            var charge = s.BatteryCharge.Skip(i).Take(slots).Where(c => c > 0).ToList();
            i += slots;
            RackSlots.SetInts(rack, Ids.Data.RackCharge, charge);
            RackSlots.ShowFrame(rack, charge.Count);
        }

        int fuelLeft = s.Fuel;
        foreach (var gen in loc.ObjectsOf(n, BlockKind.CoalGenerator).ToList())
        {
            int had = RackSlots.GetInt(gen, Ids.Data.Fuel);
            int keep = System.Math.Min(had, fuelLeft);
            fuelLeft -= keep;
            gen.modData[Ids.Data.Fuel] = keep.ToString();
            RackSlots.ShowFrame(gen, keep > 0 ? 1 : 0);
        }
    }

    public string Describe(LocationNetworks loc, Network n)
    {
        var s = StateOf(loc, n);
        int use = DailyUse(n);
        string powered = helper.Translation.Get(IsPowered(loc, n) ? "network.powered" : "network.unpowered");
        return $"{powered} {s.Available} power, {use}/day ({ledger.DaysRemaining(s, use)} days).";
    }
}
