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

    /// <summary>Maru's letters wait for hearts, mine depth and earlier tiers. False sends them as soon as possible.</summary>
    public bool TierGates { get; set; } = true;

    /// <summary>Power from one Battery Pack.</summary>
    public int BatteryPower { get; set; } = 100;

    /// <summary>Power from one Coal (user, 2026-11-28: 5, so a starter network burns about 1–2 Coal a day).</summary>
    public int CoalPower { get; set; } = 5;

    /// <summary>Power from one Hardwood.</summary>
    public int HardwoodPower { get; set; } = 1;

    /// <summary>Every building's network joins the farmhouse's without Network Ports (open question; default off: own building only until T5).</summary>
    public bool TerminalReachAllBeforePorts { get; set; } = false;

    /// <summary>Config format version, for one-time updates of old config.json files (kept on the Decks).</summary>
    public int ConfigVersion { get; set; }

    /// <summary>The current <see cref="ConfigVersion"/>.</summary>
    internal const int CurrentVersion = 1;

    /// <summary>
    /// Brings an older config.json up to date. Returns true if anything changed (then write it back).
    /// Version 1: the Coal default went from 1 to 5 power; an old file still holding 1 gets 5.
    /// </summary>
    internal bool Upgrade()
    {
        if (ConfigVersion >= CurrentVersion)
            return false;
        if (ConfigVersion < 1 && CoalPower == 1)
            CoalPower = 5;
        ConfigVersion = CurrentVersion;
        return true;
    }

    internal PowerTable ToPowerTable() => PowerTable.Default with { BatteryPower = BatteryPower, CoalPower = CoalPower, HardwoodPower = HardwoodPower };
}
