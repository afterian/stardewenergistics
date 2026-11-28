using System.Text.Json;
using StardewEnergistics.Core.Crafting;
using StardewEnergistics.Core.Storage;

namespace StardewEnergistics.Core.Persistence;

/// <summary>Stores a network's autocraft jobs as JSON (kept in the Computer Core's modData).</summary>
public static class JobQueueCodec
{
    private sealed class Dto
    {
        public int V { get; set; } = 1;
        public List<JobDto> Jobs { get; set; } = new();
    }

    private sealed class JobDto
    {
        public int Id { get; set; }
        public string Target { get; set; } = "";
        public int Count { get; set; }
        public string Status { get; set; } = nameof(JobStatus.Queued);
        public int Step { get; set; }
        public int Done { get; set; }
        public List<StepDto> Steps { get; set; } = new();
        public bool Reserved { get; set; }
        /// <summary>Items set aside, "count qualifiedId quality".</summary>
        public List<string> Reserve { get; set; } = new();
    }

    private sealed class StepDto
    {
        public string Id { get; set; } = "";
        public string Kind { get; set; } = nameof(RecipeKind.Craft);
        public List<string> In { get; set; } = new();
        public string Out { get; set; } = "";
        public int Times { get; set; }
    }

    // Item amounts are written as "count qualifiedId" so the JSON stays short.
    private static string Amount(ItemAmount a) => $"{a.Count} {a.Item.QualifiedId}";

    private static ItemAmount Amount(string s)
    {
        int space = s.IndexOf(' ');
        return space > 0 && int.TryParse(s[..space], out int n)
            ? new ItemAmount(new ItemKey(s[(space + 1)..]), n)
            : throw new FormatException(s);
    }

    public static string Encode(JobQueue queue) => JsonSerializer.Serialize(new Dto
    {
        Jobs = queue.Jobs.Where(j => !j.Finished).Select(j => new JobDto
        {
            Id = j.Id,
            Target = j.Target.Item.QualifiedId,
            Count = j.Target.Count,
            Status = j.Status.ToString(),
            Step = j.StepIndex,
            Done = j.RunsDone,
            Reserved = j.Reserved,
            Reserve = j.Reserve.All().Select(r => $"{r.Count} {r.Key.QualifiedId} {r.Key.Quality}").ToList(),
            Steps = j.Steps.Select(s => new StepDto
            {
                Id = s.Recipe.Id,
                Kind = s.Recipe.Kind.ToString(),
                In = s.Recipe.In.Select(Amount).ToList(),
                Out = Amount(s.Recipe.Out),
                Times = s.Times,
            }).ToList(),
        }).ToList(),
    });

    /// <summary>Reads jobs back. Missing or damaged data gives an empty queue; a damaged job is dropped.</summary>
    public static JobQueue Decode(string? json)
    {
        var queue = new JobQueue();
        if (string.IsNullOrWhiteSpace(json))
            return queue;
        Dto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<Dto>(json);
        }
        catch (JsonException)
        {
            return queue;
        }
        foreach (var j in dto?.Jobs ?? new())
        {
            try
            {
                var steps = j.Steps.Select(s => new CraftStep(
                    new RecipeDef(s.Id, s.In.Select(Amount).ToList(), Amount(s.Out), Enum.Parse<RecipeKind>(s.Kind)),
                    s.Times)).ToList();
                var status = Enum.TryParse<JobStatus>(j.Status, out var st) ? st : JobStatus.Queued;
                var job = queue.Restore(j.Id, new ItemAmount(new ItemKey(j.Target), j.Count), steps, status,
                    Math.Clamp(j.Step, 0, steps.Count), Math.Max(0, j.Done));
                job.Reserved = j.Reserved;
                foreach (string r in j.Reserve ?? new())
                {
                    string[] parts = r.Split(' ');
                    if (parts.Length == 3 && long.TryParse(parts[0], out long n) && int.TryParse(parts[2], out int q))
                        job.Reserve.Add(new ItemKey(parts[1], q), n);
                }
            }
            catch (Exception e) when (e is FormatException or ArgumentException)
            {
                // Skip a damaged job.
            }
        }
        return queue;
    }
}
