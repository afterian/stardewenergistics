using StardewEnergistics.Core.Art;
using Xunit.Abstractions;

namespace StardewEnergistics.Tests;

/// <summary>
/// One-command check for the art lane: `dotnet test` fails if a PNG in artwork/exports/
/// has the wrong size, colour type or name. Missing files just mean "placeholder in use".
/// </summary>
public class ArtExportTests
{
    private readonly ITestOutputHelper output;

    public ArtExportTests(ITestOutputHelper output) => this.output = output;

    [Fact]
    public void ExportsMatchTheArtList()
    {
        var checks = ArtExportValidator.Check(RepoPaths.ArtExports);
        foreach (var c in checks)
            output.WriteLine($"{c.Status,-11} {c.FileName}: {c.Message}");

        var errors = checks.Where(c => c.Status == ArtCheckStatus.Error).Select(c => $"{c.FileName}: {c.Message}").ToList();
        Assert.True(errors.Count == 0, "Art export problems:\n" + string.Join("\n", errors));
    }

    [Fact]
    public void CorrectSheetPasses()
    {
        using var dir = new TempDir();
        TestPng.Write(Path.Combine(dir.Path, "computer-core.png"), 64, 32);
        var check = ArtExportValidator.Check(dir.Path).Single(c => c.FileName == "computer-core.png");
        Assert.Equal(ArtCheckStatus.Ok, check.Status);
    }

    [Fact]
    public void WrongFrameCountFails()
    {
        using var dir = new TempDir();
        TestPng.Write(Path.Combine(dir.Path, "computer-core.png"), 48, 32);
        var check = ArtExportValidator.Check(dir.Path).Single(c => c.FileName == "computer-core.png");
        Assert.Equal(ArtCheckStatus.Error, check.Status);
        Assert.Contains("expected 64x32", check.Message);
    }

    [Fact]
    public void UpscaledArtFails()
    {
        using var dir = new TempDir();
        TestPng.Write(Path.Combine(dir.Path, "copper-disk.png"), 64, 64);
        Assert.Equal(ArtCheckStatus.Error, ArtExportValidator.Check(dir.Path).Single(c => c.FileName == "copper-disk.png").Status);
    }

    [Fact]
    public void RgbWithoutAlphaFails()
    {
        using var dir = new TempDir();
        TestPng.Write(Path.Combine(dir.Path, "copper-disk.png"), 16, 16, colorType: 2);
        Assert.Equal(ArtCheckStatus.Error, ArtExportValidator.Check(dir.Path).Single(c => c.FileName == "copper-disk.png").Status);
    }

    [Fact]
    public void UnknownOrMiscasedNameFails()
    {
        using var dir = new TempDir();
        TestPng.Write(Path.Combine(dir.Path, "Copper-Disk.png"), 16, 16);
        var check = ArtExportValidator.Check(dir.Path).Single(c => c.FileName == "Copper-Disk.png");
        Assert.Equal(ArtCheckStatus.Error, check.Status);
        Assert.Contains("copper-disk.png", check.Message);
    }

    [Fact]
    public void MissingFilesArePlaceholdersNotErrors()
    {
        using var dir = new TempDir();
        var checks = ArtExportValidator.Check(dir.Path);
        Assert.Equal(38, checks.Count);
        Assert.All(checks, c => Assert.Equal(ArtCheckStatus.Placeholder, c.Status));
    }

    [Fact]
    public void NotAPngFails()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "copper-disk.png"), "this is not a png at all, just some text");
        Assert.Equal(ArtCheckStatus.Error, ArtExportValidator.Check(dir.Path).Single(c => c.FileName == "copper-disk.png").Status);
    }
}
