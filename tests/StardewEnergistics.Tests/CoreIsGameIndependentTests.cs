using StardewEnergistics.Core.Art;

namespace StardewEnergistics.Tests;

/// <summary>Core must never reference the game, so its logic runs in CI and a plain shell.</summary>
public class CoreIsGameIndependentTests
{
    private static readonly string[] Forbidden =
    {
        "Stardew Valley", "StardewValley", "StardewValley.GameData", "StardewModdingAPI",
        "MonoGame.Framework", "xTile", "0Harmony", "SMAPI.Toolkit",
    };

    [Fact]
    public void CoreReferencesNoGameAssemblies()
    {
        var refs = typeof(AssetCatalog).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? "").ToList();
        var bad = refs.Where(r => Forbidden.Any(f => r.StartsWith(f, StringComparison.OrdinalIgnoreCase))).ToList();
        Assert.True(bad.Count == 0, "StardewEnergistics.Core references game assemblies: " + string.Join(", ", bad));
    }

    [Fact]
    public void CoreReferencesOnlyTheFramework()
    {
        var refs = typeof(AssetCatalog).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? "");
        var unexpected = refs.Where(r => !(r.StartsWith("System") || r == "netstandard" || r == "mscorlib" || r.StartsWith("Microsoft."))).ToList();
        Assert.True(unexpected.Count == 0, "Unexpected Core references: " + string.Join(", ", unexpected));
    }
}
