using System;
using System.Linq;
using StardewEnergistics.Content;
using StardewEnergistics.Core.Progression;
using StardewEnergistics.Networks;
using StardewEnergistics.Power;
using StardewEnergistics.Storage;
using StardewEnergistics.World;
using StardewModdingAPI;
using StardewValley;

namespace StardewEnergistics;

/// <summary>SMAPI console commands (prefixed se_) for testing a milestone without replaying a season.</summary>
internal sealed class DebugCommands
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly WireLayerStore wires;
    private readonly NetworkManager networks;
    private readonly DiskStore disks;
    private readonly PowerService power;

    public DebugCommands(IModHelper helper, IMonitor monitor, WireLayerStore wires, NetworkManager networks, DiskStore disks, PowerService power)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.wires = wires;
        this.networks = networks;
        this.disks = disks;
        this.power = power;
    }

    public void Register()
    {
        helper.ConsoleCommands.Add("se_unlock", "Teach the current player Stardew Energistics recipes.\nUsage: se_unlock all | se_unlock <1-6>", Unlock);
        helper.ConsoleCommands.Add("se_give", "Give the current player a Stardew Energistics item.\nUsage: se_give <name> [count]\nNames: " + string.Join(", ", ItemNames()), Give);
        helper.ConsoleCommands.Add("se_wires", "Show wire tile counts per location.", ShowWires);
        helper.ConsoleCommands.Add("se_graph", "Print the networks in the current location.", ShowGraph);
        helper.ConsoleCommands.Add("se_power", "Run the morning power pass now (as if a new day started).", (_, _) => { power.OnDayStarted(); ShowGraph("", Array.Empty<string>()); });
    }

    private static string[] ItemNames() => typeof(Ids).GetFields()
        .Where(f => f.IsLiteral && f.Name != nameof(Ids.ModId) && f.Name != nameof(Ids.Prefix) && f.Name != nameof(Ids.ModDataPrefix) && !f.Name.StartsWith("Stat"))
        .Select(f => f.Name)
        .ToArray();

    private bool Ready()
    {
        if (Context.IsWorldReady)
            return true;
        monitor.Log("Load a save first.", LogLevel.Warn);
        return false;
    }

    private void Unlock(string command, string[] args)
    {
        if (!Ready())
            return;
        var ids = args.Length > 0 && int.TryParse(args[0], out int tier) && tier is >= 1 and <= 6
            ? ContentInjector.RecipesFor((TierId)tier).ToList()
            : ContentInjector.AllRecipes.ToList();
        int added = 0;
        foreach (string id in ids)
        {
            if (!Game1.player.craftingRecipes.ContainsKey(id))
            {
                Game1.player.craftingRecipes.Add(id, 0);
                added++;
            }
        }
        monitor.Log($"Taught {added} new recipes ({string.Join(", ", ids.Select(i => i.Replace(Ids.Prefix, "")))}).", LogLevel.Info);
    }

    private void Give(string command, string[] args)
    {
        if (!Ready())
            return;
        if (args.Length == 0)
        {
            monitor.Log("Usage: se_give <name> [count]. Names: " + string.Join(", ", ItemNames()), LogLevel.Info);
            return;
        }

        string id = Ids.Prefix + args[0];
        int count = args.Length > 1 && int.TryParse(args[1], out int n) ? n : 1;
        string qualified = ItemRegistry.GetData("(O)" + id) != null ? "(O)" + id : "(BC)" + id;
        if (ItemRegistry.GetData(qualified) == null)
        {
            monitor.Log($"No item '{args[0]}' yet (it may arrive in a later milestone).", LogLevel.Warn);
            return;
        }

        // Disks don't stack, so give them one at a time.
        int each = DiskStore.IsDisk(ItemRegistry.Create(qualified)) ? 1 : count;
        for (int given = 0; given < count; given += each)
            Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create(qualified, each));
        monitor.Log($"Gave {count} x {ItemRegistry.GetData(qualified)!.DisplayName}.", LogLevel.Info);
    }

    private void ShowWires(string command, string[] args)
    {
        if (!Ready())
            return;
        var rows = wires.All.Where(kv => kv.Value.Count > 0).ToList();
        if (rows.Count == 0)
            monitor.Log("No wire anywhere yet.", LogLevel.Info);
        foreach (var (location, layer) in rows)
            monitor.Log($"{location}: {layer.Count} tiles, {layer.Tiles.Count(t => t.Value.Covered)} hidden.", LogLevel.Info);
    }

    private void ShowGraph(string command, string[] args)
    {
        if (!Ready())
            return;
        var loc = networks.For(Game1.currentLocation);
        monitor.Log($"{Game1.currentLocation.NameOrUniqueName}: {loc.Graph.Networks.Count} network(s).", LogLevel.Info);
        foreach (var n in loc.Graph.Networks)
        {
            var kinds = string.Join(", ", n.Blocks.GroupBy(b => b.Kind).Select(g => $"{g.Count()} {g.Key}"));
            string state = n.Working ? power.Describe(loc, n) : "not working";
            var storage = NetworkStorage.For(loc, n, disks);
            monitor.Log($"  {n.Id}: {kinds}. {state} Disks: {storage.Disks.Disks.Count}, {storage.KindsStored} kinds, {storage.Disks.TotalItems} items; {storage.Chests.Count} linked chests.", LogLevel.Info);
        }
        foreach (var issue in loc.Graph.Issues)
            monitor.Log($"  Problem: {issue.Kind} at {issue.Pos}", LogLevel.Info);
    }
}
