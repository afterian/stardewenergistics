using StardewEnergistics.Core.Storage;

namespace StardewEnergistics.Core.Persistence;

/// <summary>Converts <see cref="DiskContents"/> to and from its save DTO.</summary>
public static class DiskContentsMapper
{
    public static DiskContentsDto ToDto(DiskContents disk) => new()
    {
        Tier = disk.Tier.ToString(),
        Stacks = disk.Stacks
            .OrderBy(s => s.Key.QualifiedId, StringComparer.Ordinal).ThenBy(s => s.Key.Quality)
            .Select(s => new StoredStackDto
            {
                QualifiedId = s.Key.QualifiedId,
                Quality = s.Key.Quality,
                PreserveId = s.Key.PreserveId,
                Color = s.Key.Color,
                Count = s.Count,
            })
            .ToList(),
    };

    /// <param name="specFor">Disk sizes (default <see cref="DiskSpec.For"/>; config may change them).</param>
    public static DiskContents FromDto(DiskId id, DiskContentsDto dto, Func<DiskTier, DiskSpec>? specFor = null)
    {
        specFor ??= DiskSpec.For;
        var tier = Enum.TryParse<DiskTier>(dto.Tier, ignoreCase: true, out var t) ? t : DiskTier.Copper;
        var disk = new DiskContents(id, tier, specFor(tier));
        foreach (var s in dto.Stacks ?? new List<StoredStackDto>())
        {
            if (!string.IsNullOrEmpty(s.QualifiedId))
                disk.Restore(new ItemKey(s.QualifiedId, s.Quality, s.PreserveId, s.Color), s.Count);
        }
        return disk;
    }
}
