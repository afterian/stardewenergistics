namespace StardewEnergistics.Core.Persistence;

/// <summary>
/// Everything the host saves through helper.Data.WriteSaveData, as plain DTOs.
/// Bump <see cref="CurrentVersion"/> and add a step to <see cref="SaveMigrator"/> on any shape change.
/// </summary>
public sealed class SaveModel
{
    public const int CurrentVersion = 1;

    public int SchemaVersion { get; set; } = CurrentVersion;

    /// <summary>Wire layer per location, keyed by location name.</summary>
    public Dictionary<string, WireLayerDto> Wires { get; set; } = new();

    /// <summary>Disk contents keyed by disk ID.</summary>
    public Dictionary<string, DiskContentsDto> Disks { get; set; } = new();

    /// <summary>Power state keyed by network ID.</summary>
    public Dictionary<string, PowerStateDto> Power { get; set; } = new();
}

public sealed class WireLayerDto
{
    public List<WireTileDto> Tiles { get; set; } = new();
}

public sealed class WireTileDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Kind { get; set; } = "Copper";
    public bool Covered { get; set; }
    public int Color { get; set; }
}

public sealed class DiskContentsDto
{
    public string Tier { get; set; } = "Copper";
    public List<StoredStackDto> Stacks { get; set; } = new();
}

public sealed class StoredStackDto
{
    public string QualifiedId { get; set; } = "";
    public int Quality { get; set; }
    public string? PreserveId { get; set; }
    public int? Color { get; set; }
    public long Count { get; set; }
}

public sealed class PowerStateDto
{
    public List<int> BatteryCharge { get; set; } = new();
    public int Fuel { get; set; }
    public int LastWarningDay { get; set; } = -1;
}
