using StardewEnergistics.Core.Storage;

namespace StardewEnergistics.Core.Machines;

/// <summary>Feed this item, in list order (first = highest priority).</summary>
public sealed record InputRule(ItemKey Item, bool AnyQuality = true);

/// <summary>Never use the last <paramref name="Amount"/> of this item.</summary>
public sealed record KeepBackRule(ItemKey Item, long Amount, bool AnyQuality = true);

public enum OutputMode
{
    /// <summary>Collect finished output into network storage (default).</summary>
    ToStorage,
    /// <summary>Leave output in the machine for the player.</summary>
    LeaveInMachine,
}

/// <summary>When a Link may run. Hours use Stardew time (600 = 6am, 2600 = 2am).</summary>
public sealed record Schedule(bool Always = true, int FromTime = 600, int ToTime = 2600)
{
    public static readonly Schedule AlwaysOn = new();
}

/// <summary>A Machine Link's or Shipping Link's player-set rules. A new Link is not <see cref="Configured"/> and does nothing.</summary>
public sealed record LinkSettings(
    IReadOnlyList<InputRule> Inputs,
    IReadOnlyList<KeepBackRule> KeepBack,
    OutputMode Output,
    Schedule Schedule,
    bool Configured)
{
    public static readonly LinkSettings New = new(Array.Empty<InputRule>(), Array.Empty<KeepBackRule>(), OutputMode.ToStorage, Schedule.AlwaysOn, false);
}
