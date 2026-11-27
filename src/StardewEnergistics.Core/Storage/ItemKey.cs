namespace StardewEnergistics.Core.Storage;

/// <summary>
/// Identifies a stackable item kind for storage. Quality and flavour make separate
/// terminal rows but do not use extra disk kinds (kinds count <see cref="QualifiedId"/> only).
/// </summary>
/// <param name="QualifiedId">Game qualified item ID, e.g. "(O)24".</param>
/// <param name="Quality">0 normal, 1 silver, 2 gold, 4 iridium.</param>
/// <param name="PreserveId">Flavour source for preserves (e.g. the fruit in a Jelly), or null.</param>
/// <param name="Color">Packed colour for coloured objects, or null.</param>
public readonly record struct ItemKey(string QualifiedId, int Quality = 0, string? PreserveId = null, int? Color = null)
{
    public override string ToString() =>
        QualifiedId + (Quality != 0 ? $" q{Quality}" : "") + (PreserveId != null ? $" [{PreserveId}]" : "") + (Color != null ? $" #{Color:X}" : "");
}
