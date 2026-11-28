using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.Core.Machines;
using StardewEnergistics.Core.Persistence;
using StardewEnergistics.Core.Storage;
using StardewEnergistics.Networks;
using StardewEnergistics.Power;
using StardewEnergistics.Storage;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Mods;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Machines;

/// <summary>
/// Runs Machine Links (host): every 10 game minutes, and once overnight for machines that finished.
/// Each Link feeds and empties its whole group of identical touching machines, per its settings.
/// Unpowered networks and unconfigured Links do nothing.
/// </summary>
internal sealed class MachineLinkService
{
    public const int NotSetUp = 0, Idle = 1, Working = 2, MissingInput = 3;

    private readonly IMonitor monitor;
    private readonly NetworkManager networks;
    private readonly DiskStore disks;
    private readonly PowerService power;

    public MachineLinkService(IMonitor monitor, NetworkManager networks, DiskStore disks, PowerService power)
    {
        this.monitor = monitor;
        this.networks = networks;
        this.disks = disks;
        this.power = power;
    }

    public static LinkSettings SettingsOf(SObject obj) => SettingsOf(obj.modData);

    /// <summary>A Link's settings from the modData of whatever it's on (an object, or the main Shipping Bin building).</summary>
    public static LinkSettings SettingsOf(ModDataDictionary data) =>
        LinkSettingsCodec.Decode(data.TryGetValue(Ids.Data.LinkSettings, out string? json) ? json : null);

    public static void SetSettings(ModDataDictionary data, LinkSettings s) => data[Ids.Data.LinkSettings] = LinkSettingsCodec.Encode(s);

    /// <summary>Machines handled and time taken by the last pass (for se_perf).</summary>
    public int LastMachines { get; private set; }
    public double LastMs { get; private set; }

    public void OnTimeChanged() => RunAll(overnight: false);

    public void OnDayEnding() => RunAll(overnight: true);

    private void RunAll(bool overnight)
    {
        if (!Context.IsMainPlayer)
            return;

        var watch = Stopwatch.StartNew();
        int machines = 0;
        foreach (var loc in networks.AllWithCores())
        {
            foreach (var n in loc.Graph.Networks.Where(n => loc.IsActive(n)))
            {
                var links = loc.ObjectsOf(n, BlockKind.MachineLink).ToList();
                if (links.Count == 0)
                    continue;
                bool powered = power.IsPowered(loc, n);
                NetworkStorage? storage = null;
                foreach (var link in links)
                {
                    var settings = SettingsOf(link);
                    if (!settings.Configured)
                    {
                        link.modData[Ids.Data.LinkState] = NotSetUp.ToString();
                        continue;
                    }
                    if (!powered || (!overnight && !settings.Schedule.IsActive(Game1.timeOfDay)))
                        continue;

                    storage ??= NetworkStorage.For(loc, n, disks);
                    machines += RunLink(loc.Location, link, settings, storage);
                }
            }
        }

        LastMachines = machines;
        LastMs = watch.Elapsed.TotalMilliseconds;
        if (machines > 0)
            monitor.Log($"Machine Links: {machines} machines in {watch.Elapsed.TotalMilliseconds:0.00} ms{(overnight ? " (overnight)" : "")}.", LogLevel.Trace);
    }

    /// <summary>Feeds and empties one Link's group. Returns how many machines it looked at.</summary>
    private static int RunLink(GameLocation location, SObject link, LinkSettings settings, NetworkStorage storage)
    {
        var start = new TilePos((int)link.TileLocation.X, (int)link.TileLocation.Y);
        var group = MachineGroupFinder.Group(start, p => MachineDriver.MachineIdAt(location, p.X, p.Y));

        bool anyWorking = false, anyMissing = false;
        foreach (var pos in group)
        {
            if (!location.objects.TryGetValue(new Microsoft.Xna.Framework.Vector2(pos.X, pos.Y), out var machine))
                continue;

            // Busy with an autocraft job: the Crafting Rack handles it.
            if (machine.modData.ContainsKey(Ids.Data.ForJob))
            {
                anyWorking = true;
                continue;
            }

            // Storage full: whatever doesn't fit stays in the machine.
            if (settings.Output == OutputMode.ToStorage)
                MachineDriver.CollectInto(machine, storage);

            if (MachineDriver.IsIdle(machine))
            {
                var key = InputSelector.Choose(settings, storage, k => MachineDriver.Accepts(machine, k, InputSelector.Usable(settings, storage, k), storage));
                if (key is { } k && MachineDriver.Load(machine, k, InputSelector.Usable(settings, storage, k), storage))
                    anyWorking = true;
                else
                    anyMissing = true;
            }
            else
            {
                anyWorking = true;
            }
        }

        link.modData[Ids.Data.LinkState] = (anyWorking ? Working : anyMissing ? MissingInput : Idle).ToString();
        return group.Count;
    }
}
