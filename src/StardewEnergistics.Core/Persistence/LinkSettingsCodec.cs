using System.Text.Json;
using StardewEnergistics.Core.Machines;
using StardewEnergistics.Core.Storage;

namespace StardewEnergistics.Core.Persistence;

/// <summary>Stores a Link's settings as a short JSON string (kept in the machine's modData).</summary>
public static class LinkSettingsCodec
{
    private sealed class Dto
    {
        public int V { get; set; } = 1;
        public bool Configured { get; set; }
        public List<RuleDto> Inputs { get; set; } = new();
        public List<RuleDto> Keep { get; set; } = new();
        public string Output { get; set; } = nameof(OutputMode.ToStorage);
        public bool Always { get; set; } = true;
        public int From { get; set; } = 600;
        public int To { get; set; } = 2600;
    }

    private sealed class RuleDto
    {
        public string Id { get; set; } = "";
        public int Q { get; set; }
        public string? P { get; set; }
        public bool Any { get; set; } = true;
        public long N { get; set; }
    }

    public static string Encode(LinkSettings s)
    {
        var dto = new Dto
        {
            Configured = s.Configured,
            Inputs = s.Inputs.Select(i => new RuleDto { Id = i.Item.QualifiedId, Q = i.Item.Quality, P = i.Item.PreserveId, Any = i.AnyQuality }).ToList(),
            Keep = s.KeepBack.Select(k => new RuleDto { Id = k.Item.QualifiedId, Q = k.Item.Quality, P = k.Item.PreserveId, Any = k.AnyQuality, N = k.Amount }).ToList(),
            Output = s.Output.ToString(),
            Always = s.Schedule.Always,
            From = s.Schedule.FromTime,
            To = s.Schedule.ToTime,
        };
        return JsonSerializer.Serialize(dto);
    }

    /// <summary>Reads settings; anything unreadable gives a new, unconfigured Link.</summary>
    public static LinkSettings Decode(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return LinkSettings.New;
        Dto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<Dto>(json);
        }
        catch (JsonException)
        {
            return LinkSettings.New;
        }
        if (dto == null)
            return LinkSettings.New;

        return new LinkSettings(
            dto.Inputs.Where(r => r.Id.Length > 0).Select(r => new InputRule(new ItemKey(r.Id, r.Q, r.P), r.Any)).ToList(),
            dto.Keep.Where(r => r.Id.Length > 0).Select(r => new KeepBackRule(new ItemKey(r.Id, r.Q, r.P), Math.Max(0, r.N), r.Any)).ToList(),
            Enum.TryParse<OutputMode>(dto.Output, out var o) ? o : OutputMode.ToStorage,
            new Schedule(dto.Always, dto.From, dto.To),
            dto.Configured);
    }
}
