using StardewEnergistics.Core.Power;
using StardewModdingAPI.Utilities;

namespace StardewEnergistics;

/// <summary>Player settings, saved in the mod folder's config.json (kept across Deck updates).</summary>
public sealed class ModConfig
{
    /// <summary>Toggles Network View: shows all wire, covered or not.</summary>
    public KeybindList NetworkViewKey { get; set; } = KeybindList.Parse("F6");

    /// <summary>Animate the data-window lights on powered cables. False shows a steady status.</summary>
    public bool AnimateDataCables { get; set; } = true;

    /// <summary>Ticks (60 per second) the wrench must be held to cover or uncover a whole run.</summary>
    public int WrenchHoldTicks { get; set; } = 30;

    /// <summary>
    /// False (default): a chest, machine or block connects to wire on its own tile or on any of the 4 tiles
    /// beside it. True: only wire on its own tile counts. Takes effect when a save is loaded.
    /// </summary>
    public bool WireUnderBlockOnly { get; set; } = false;

    /// <summary>Maru's letters wait for hearts, mine depth and earlier tiers. False sends them as soon as possible.</summary>
    public bool TierGates { get; set; } = true;

    /// <summary>Power from one Battery Pack.</summary>
    public int BatteryPower { get; set; } = 100;

    /// <summary>Power from one Coal (open question: 1 or 10; confirm in the first power playtest).</summary>
    public int CoalPower { get; set; } = 1;

    /// <summary>Power from one Hardwood.</summary>
    public int HardwoodPower { get; set; } = 1;

    /// <summary>Every building's network joins the farmhouse's without Network Ports (open question; default off: own building only until T5).</summary>
    public bool TerminalReachAllBeforePorts { get; set; } = false;

    /// <summary>Test mode: the cheat menu (catalog, starter kit, unlock recipes). On while playtesting; switch off before release.</summary>
    public bool TestMode { get; set; } = true;

    /// <summary>Opens the test menu when test mode is on. Default F7, or click both sticks (L3 + R3) on a controller.</summary>
    public KeybindList TestMenuKey { get; set; } = KeybindList.Parse("F7, LeftStick + RightStick");

    internal PowerTable ToPowerTable() => PowerTable.Default with { BatteryPower = BatteryPower, CoalPower = CoalPower, HardwoodPower = HardwoodPower };
}
