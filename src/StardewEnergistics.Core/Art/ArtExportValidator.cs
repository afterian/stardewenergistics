namespace StardewEnergistics.Core.Art;

public enum ArtCheckStatus
{
    Ok,
    /// <summary>No export yet; the game uses a generated placeholder. Not an error.</summary>
    Placeholder,
    Error,
}

public sealed record ArtCheck(string FileName, ArtCheckStatus Status, string Message);

/// <summary>Checks artwork/exports/ against <see cref="AssetCatalog"/>: names, sheet size, colour type.</summary>
public static class ArtExportValidator
{
    public static IReadOnlyList<ArtCheck> Check(string exportsDir)
    {
        var results = new List<ArtCheck>();
        var present = Directory.Exists(exportsDir)
            ? Directory.GetFiles(exportsDir, "*.png").Select(Path.GetFileName).OfType<string>().ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        foreach (var spec in AssetCatalog.All)
        {
            if (!present.Contains(spec.FileName))
            {
                results.Add(new ArtCheck(spec.FileName, ArtCheckStatus.Placeholder, "not exported yet; placeholder in use"));
                continue;
            }

            results.Add(CheckFile(spec, Path.Combine(exportsDir, spec.FileName)));
        }

        foreach (var name in present.Where(n => AssetCatalog.Find(n) is null).OrderBy(n => n, StringComparer.Ordinal))
        {
            string hint = AssetCatalog.All.FirstOrDefault(a => string.Equals(a.FileName, name, StringComparison.OrdinalIgnoreCase)) is { } close
                ? $" (did you mean '{close.FileName}'? names are lower-case)"
                : "";
            results.Add(new ArtCheck(name, ArtCheckStatus.Error, "not in docs/art-asset-list.md" + hint));
        }

        return results;
    }

    public static ArtCheck CheckFile(AssetSpec spec, string path)
    {
        PngHeader h;
        try
        {
            h = PngHeader.Read(path);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException)
        {
            return new ArtCheck(spec.FileName, ArtCheckStatus.Error, ex.Message);
        }

        if (h.Width != spec.SheetWidth || h.Height != spec.SheetHeight)
        {
            return new ArtCheck(spec.FileName, ArtCheckStatus.Error,
                $"is {h.Width}x{h.Height}, expected {spec.SheetWidth}x{spec.SheetHeight} " +
                $"({spec.Frames} frame(s) of {spec.FrameWidth}x{spec.FrameHeight}, left to right, drawn at 1x)");
        }

        if (!h.SupportsTransparency)
            return new ArtCheck(spec.FileName, ArtCheckStatus.Error, $"colour type {h.ColorType} has no transparency; save as RGBA PNG");

        return new ArtCheck(spec.FileName, ArtCheckStatus.Ok, "ok");
    }
}
