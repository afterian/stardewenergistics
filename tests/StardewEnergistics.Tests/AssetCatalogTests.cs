using System.Text.RegularExpressions;
using StardewEnergistics.Core.Art;

namespace StardewEnergistics.Tests;

public class AssetCatalogTests
{
    [Fact]
    public void HasAll38AssetsNumberedOneTo38()
    {
        Assert.Equal(38, AssetCatalog.All.Count);
        Assert.Equal(Enumerable.Range(1, 38), AssetCatalog.All.Select(a => a.Number).OrderBy(n => n));
    }

    [Fact]
    public void FileNamesAreUniqueLowerCaseWithDashes()
    {
        Assert.Equal(AssetCatalog.All.Count, AssetCatalog.All.Select(a => a.FileName).Distinct().Count());
        foreach (var a in AssetCatalog.All)
            Assert.Matches(new Regex("^[a-z0-9]+(-[a-z0-9]+)*\\.png$"), a.FileName);
    }

    [Fact]
    public void EveryCatalogFileIsNamedInTheArtList()
    {
        string artList = File.ReadAllText(Path.Combine(RepoPaths.Root, "docs", "art-asset-list.md"));
        foreach (var a in AssetCatalog.All)
            Assert.Contains($"`{a.FileName}`", artList);
    }

    [Theory]
    [InlineData("computer-core.png", 16, 32, 4)]
    [InlineData("storage-rack.png", 16, 32, 5)]
    [InlineData("wire-tiles.png", 16, 16, 16)]
    [InlineData("heavy-cable-tiles.png", 16, 16, 16)]
    [InlineData("settings-icons.png", 9, 9, 7)]
    [InlineData("machine-link.png", 16, 16, 4)]
    [InlineData("nexus-banner.png", 480, 120, 1)]
    public void KeyEntriesMatchTheArtList(string file, int w, int h, int frames)
    {
        var a = AssetCatalog.Get(file);
        Assert.Equal((w, h, frames), (a.FrameWidth, a.FrameHeight, a.Frames));
    }

    [Fact]
    public void GameAssetNamesUseTheModPrefix()
    {
        Assert.Equal("Mods/afterian.StardewEnergistics/Sprites/computer-core", AssetCatalog.Get("computer-core.png").GameAssetName);
    }
}
