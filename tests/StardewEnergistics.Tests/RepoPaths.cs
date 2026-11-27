namespace StardewEnergistics.Tests;

/// <summary>Finds the repository root (the folder with StardewEnergistics.sln) from the test output folder.</summary>
internal static class RepoPaths
{
    public static string Root { get; } = FindRoot();

    public static string ArtExports => Path.Combine(Root, "artwork", "exports");

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "StardewEnergistics.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Could not find StardewEnergistics.sln above " + AppContext.BaseDirectory);
    }
}
