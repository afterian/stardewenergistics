namespace StardewEnergistics.Core.Art;

/// <summary>
/// All 36 art files, copied from docs/art-asset-list.md. Code depends only on these names,
/// frame sizes and frame counts. Change this file and the art list together.
/// </summary>
public static class AssetCatalog
{
    /// <summary>Prefix of every game asset name the mod registers.</summary>
    public const string AssetRoot = "Mods/afterian.StardewEnergistics/Sprites";

    public static readonly IReadOnlyList<AssetSpec> All = new[]
    {
        // Inventory items, 16x16, one frame
        Item(1, "copper-processor.png", 1),
        Item(2, "gold-processor.png", 3),
        Item(3, "iridium-processor.png", 5),
        Item(4, "copper-wire-spool.png", 1),
        Item(5, "copper-disk.png", 2),
        Item(6, "iron-disk.png", 3),
        Item(7, "gold-disk.png", 3),
        Item(8, "iridium-disk.png", 5),
        Item(9, "prismatic-disk.png", 6),
        Item(10, "wireless-terminal.png", 5),

        // Placeable machines and racks, 16x32
        Placeable(11, "processor-press.png", 3, 1),
        Placeable(12, "computer-core.png", 4, 2),
        Placeable(13, "storage-rack.png", 5, 2),
        Placeable(14, "power-rack.png", 5, 2),
        Placeable(15, "crafting-rack.png", 3, 4),
        Placeable(16, "advanced-crafting-rack.png", 3, 6),
        Placeable(17, "network-terminal.png", 2, 2),
        Placeable(18, "coal-generator.png", 3, 2),
        Placeable(19, "network-port.png", 2, 5),
        Placeable(20, "island-uplink.png", 3, 6),

        // Wire tiles, 16x16, 16 frames (frame = N1 E2 S4 W8 mask)
        new AssetSpec(21, "wire-tiles.png", AssetKind.WireTiles, 16, 16, 16, 1),
        new AssetSpec(22, "wire-tiles-glow.png", AssetKind.WireTiles, 16, 16, 16, 1),
        new AssetSpec(35, "heavy-cable-tiles.png", AssetKind.WireTiles, 16, 16, 16, 1),

        // Attachment overlays, 16x16
        new AssetSpec(23, "chest-link.png", AssetKind.Attachment, 16, 16, 1, 2),
        new AssetSpec(24, "machine-link.png", AssetKind.Attachment, 16, 16, 4, 3),
        new AssetSpec(25, "shipping-link.png", AssetKind.Attachment, 16, 16, 2, 3),
        new AssetSpec(26, "power-feed.png", AssetKind.Attachment, 16, 16, 2, 5),

        // Tools
        new AssetSpec(36, "network-wrench.png", AssetKind.Tool, 16, 16, 1, 1),

        // UI
        new AssetSpec(27, "terminal-frame.png", AssetKind.Ui, 48, 48, 1, 2),
        new AssetSpec(28, "tab-icons.png", AssetKind.Ui, 16, 16, 3, 2),
        new AssetSpec(29, "settings-icons.png", AssetKind.Ui, 9, 9, 7, 3),
        new AssetSpec(30, "power-meter.png", AssetKind.Ui, 48, 8, 2, 2),
        new AssetSpec(31, "storage-meter.png", AssetKind.Ui, 48, 8, 2, 2),
        new AssetSpec(32, "network-view-markers.png", AssetKind.Ui, 9, 9, 5, 2),

        // Promo (not loaded in game)
        new AssetSpec(33, "mod-icon.png", AssetKind.Promo, 64, 64, 1, 6),
        new AssetSpec(34, "nexus-banner.png", AssetKind.Promo, 480, 120, 1, 6),
    };

    private static readonly Dictionary<string, AssetSpec> ByFile =
        All.ToDictionary(a => a.FileName, StringComparer.Ordinal);

    public static AssetSpec? Find(string fileName) => ByFile.TryGetValue(fileName, out var spec) ? spec : null;

    public static AssetSpec Get(string fileName) =>
        Find(fileName) ?? throw new KeyNotFoundException($"'{fileName}' is not in the art catalogue.");

    private static AssetSpec Item(int n, string file, int milestone) => new(n, file, AssetKind.InventoryItem, 16, 16, 1, milestone);

    private static AssetSpec Placeable(int n, string file, int frames, int milestone) => new(n, file, AssetKind.Placeable, 16, 32, frames, milestone);
}
