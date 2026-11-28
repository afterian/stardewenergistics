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

    /// <summary>Power from one Coal (user, 2026-11-28: 5, so a starter network burns about 1–2 Coal a day).</summary>
    public int CoalPower { get; set; } = 5;

    /// <summary>Power from one Hardwood.</summary>
    public int HardwoodPower { get; set; } = 1;

    /// <summary>Every building's network joins the farmhouse's without Network Ports (default off: own building only until T5, user 2026-11-28).</summary>
    public bool TerminalReachAllBeforePorts { get; set; } = false;

    /// <summary>Opens the Tinkerer's Journal (the quest guide). The journal item opens it too.</summary>
    public KeybindList GuideKey { get; set; } = KeybindList.Parse("F8");
    /// <summary>Config format version, for one-time updates of old config.json files (kept on the Decks).</summary>
    public int ConfigVersion { get; set; }

    /// <summary>The current <see cref="ConfigVersion"/>.</summary>
    internal const int CurrentVersion = 2;

    /// <summary>
    /// Brings an older config.json up to date. Returns true if anything changed (then write it back).
    /// Version 1: the Coal default went from 1 to 5 power; an old file still holding 1 gets 5.
    /// Version 2: test mode became off by default (user, 2026-11-28); old files, saved with it on, turn it off once.
    /// Switching it back on later sticks, since the file is then at version 2.
    /// </summary>
    internal bool Upgrade()
    {
        if (ConfigVersion >= CurrentVersion)
            return false;
        if (ConfigVersion < 1 && CoalPower == 1)
            CoalPower = 5;
        if (ConfigVersion < 2)
            TestMode = false;
        ConfigVersion = CurrentVersion;
        return true;
    }

    /// <summary>Test mode: the cheat menu (catalog, starter kit, unlock recipes). Off by default (user, 2026-11-28); switch on in the mod settings.</summary>
    public bool TestMode { get; set; } = false;

    /// <summary>Opens the test menu when test mode is on. Default F7, or click both sticks (L3 + R3) on a controller.</summary>
    public KeybindList TestMenuKey { get; set; } = KeybindList.Parse("F7, LeftStick + RightStick");

    internal PowerTable ToPowerTable() => PowerTable.Default with { BatteryPower = BatteryPower, CoalPower = CoalPower, HardwoodPower = HardwoodPower };
}
