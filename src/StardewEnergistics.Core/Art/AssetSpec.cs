namespace StardewEnergistics.Core.Art;

public enum AssetKind
{
    InventoryItem,
    Placeable,
    WireTiles,
    Attachment,
    Tool,
    Ui,
    Promo,
}

/// <summary>One art file from docs/art-asset-list.md: a single left-to-right strip of equal frames.</summary>
/// <param name="Number">The "#" column in the art list.</param>
/// <param name="FileName">File name in artwork/exports/, e.g. "computer-core.png".</param>
/// <param name="Milestone">First milestone whose code draws this asset (0 = promo only).</param>
public sealed record AssetSpec(int Number, string FileName, AssetKind Kind, int FrameWidth, int FrameHeight, int Frames, int Milestone)
{
    /// <summary>Name without ".png"; also the last part of the game asset name.</summary>
    public string Name => Path.GetFileNameWithoutExtension(FileName);

    public int SheetWidth => FrameWidth * Frames;

    public int SheetHeight => FrameHeight;

    /// <summary>Game asset name the mod registers, e.g. "Mods/afterian.StardewEnergistics/Sprites/computer-core".</summary>
    public string GameAssetName => $"{AssetCatalog.AssetRoot}/{Name}";
}
