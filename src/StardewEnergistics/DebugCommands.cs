using System.Linq;
using StardewEnergistics.Content;
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

    public DebugCommands(IModHelper helper, IMonitor monitor, WireLayerStore wires)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.wires = wires;
    }

    public void Register()
    {
        helper.ConsoleCommands.Add("se_unlock", "Teach the current player Stardew Energistics recipes.\nUsage: se_unlock all", Unlock);
        helper.ConsoleCommands.Add("se_give", "Give the current player a Stardew Energistics item.\nUsage: se_give <name> [count]\nNames: " + string.Join(", ", ItemNames()), Give);
        helper.ConsoleCommands.Add("se_wires", "Show wire tile counts per location.", ShowWires);
    }

    private static string[] ItemNames() => typeof(Ids).GetFields()
        .Where(f => f.IsLiteral && f.Name != nameof(Ids.ModId) && f.Name != nameof(Ids.Prefix) && f.Name != nameof(Ids.ModDataPrefix))
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
        int added = 0;
        foreach (string id in ContentInjector.M1DebugRecipes)
        {
            if (!Game1.player.craftingRecipes.ContainsKey(id))
            {
                Game1.player.craftingRecipes.Add(id, 0);
                added++;
            }
        }
        monitor.Log($"Taught {added} new recipes ({string.Join(", ", ContentInjector.M1DebugRecipes)}).", LogLevel.Info);
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

        var item = ItemRegistry.Create(qualified, count);
        Game1.player.addItemByMenuIfNecessary(item);
        monitor.Log($"Gave {count} x {item.DisplayName}.", LogLevel.Info);
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
}
