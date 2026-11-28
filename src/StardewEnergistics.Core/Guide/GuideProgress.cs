using StardewEnergistics.Core.Progression;

namespace StardewEnergistics.Core.Guide;

/// <summary>What the journal looks at for one player. <see cref="Got"/> holds mod item keys crafted or held.</summary>
public sealed record GuideFacts(
    ProgressSnapshot Progress,
    bool JournalOpened,
    IReadOnlySet<TierId> LettersRead,
    IReadOnlySet<string> Got,
    IReadOnlySet<WorldFlag> World);

/// <summary>
/// Ticks goals off. A goal stays done once done (picking an item back up or a network losing power
/// never un-ticks it), so the journal only moves forward.
/// </summary>
public static class GuideProgress
{
    /// <summary>All goals done now: everything done before, plus any goal whose condition is met.</summary>
    public static HashSet<string> Update(GuideFacts facts, IEnumerable<string> doneBefore)
    {
        var done = new HashSet<string>(doneBefore.Where(id => GuideBook.Find(id) != null));
        foreach (var q in GuideBook.AllQuests.Where(q => q.Goal.Kind != GoalKind.AllDone))
            if (IsMet(q.Goal, facts))
                done.Add(q.Id);
        foreach (var q in GuideBook.AllQuests.Where(q => q.Goal.Kind == GoalKind.AllDone))
            if (GuideBook.AllQuests.All(o => o.Goal.Kind == GoalKind.AllDone || done.Contains(o.Id)))
                done.Add(q.Id);
        return done;
    }

    public static bool IsMet(GuideGoal g, GuideFacts f) => g.Kind switch
    {
        GoalKind.OpenJournal => f.JournalOpened,
        GoalKind.MaruHearts => f.Progress.MaruHearts >= g.Amount,
        GoalKind.CopperProcessors => f.Progress.CopperProcessorsCrafted >= g.Amount,
        GoalKind.ReadLetter => g.Tier is { } t && f.LettersRead.Contains(t),
        GoalKind.Get => g.Item != null && f.Got.Contains(g.Item),
        GoalKind.World => g.Flag is { } w && f.World.Contains(w),
        _ => false,
    };

    /// <summary>A counter to show beside a goal ("3/10"), or null for yes/no goals.</summary>
    public static (int Have, int Need)? Counter(GuideGoal g, GuideFacts f) => g.Kind switch
    {
        GoalKind.MaruHearts => (Math.Min(f.Progress.MaruHearts, g.Amount), g.Amount),
        GoalKind.CopperProcessors => (Math.Min(f.Progress.CopperProcessorsCrafted, g.Amount), g.Amount),
        _ => null,
    };

    public static bool ChapterDone(GuideChapter c, IReadOnlySet<string> done) => c.Quests.All(q => done.Contains(q.Id));

    /// <summary>The first goal not done yet, in journal order (null when the journal is finished).</summary>
    public static GuideQuest? Next(IReadOnlySet<string> done) => GuideBook.AllQuests.FirstOrDefault(q => !done.Contains(q.Id));

    /// <summary>Chapters finished in <paramref name="now"/> that weren't in <paramref name="before"/>.</summary>
    public static IEnumerable<GuideChapter> NewlyFinishedChapters(IReadOnlySet<string> before, IReadOnlySet<string> now) =>
        GuideBook.Chapters.Where(c => ChapterDone(c, now) && !ChapterDone(c, before));

    /// <summary>Saved form: quest IDs, comma-separated.</summary>
    public static string Encode(IEnumerable<string> done) => string.Join(",", done.OrderBy(s => s, StringComparer.Ordinal));

    public static HashSet<string> Decode(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? new HashSet<string>() : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
}
