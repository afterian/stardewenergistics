using StardewEnergistics.Core.Storage;

namespace StardewEnergistics.Core.Crafting;

public enum JobStatus
{
    Queued,
    Running,
    Done,
    Cancelled,
}

/// <summary>One autocraft request: its plan and how far it has got.</summary>
public sealed class CraftJob
{
    public CraftJob(int id, ItemAmount target, IReadOnlyList<CraftStep> steps)
    {
        Id = id;
        Target = target;
        Steps = steps;
    }

    public int Id { get; }
    public ItemAmount Target { get; }
    public IReadOnlyList<CraftStep> Steps { get; }
    public JobStatus Status { get; internal set; }

    /// <summary>Index of the step being worked on.</summary>
    public int StepIndex { get; internal set; }

    /// <summary>Runs of the current step already done.</summary>
    public int RunsDone { get; internal set; }

    public bool Finished => Status is JobStatus.Done or JobStatus.Cancelled;
    public CraftStep? Current => StepIndex < Steps.Count ? Steps[StepIndex] : null;
    public int RunsLeft => Current is { } s ? s.Times - RunsDone : 0;

    /// <summary>0..1 by recipe runs done.</summary>
    public double Progress
    {
        get
        {
            int total = Steps.Sum(s => s.Times);
            if (total == 0)
                return Status == JobStatus.Done ? 1 : 0;
            int done = Steps.Take(StepIndex).Sum(s => s.Times) + RunsDone;
            return Math.Clamp(done / (double)total, 0, 1);
        }
    }
}

/// <summary>
/// Autocraft jobs in request order. Each Crafting Rack works on one job at a time, so the first
/// <c>racks</c> unfinished jobs are active. The game side runs steps and reports progress here.
/// </summary>
public sealed class JobQueue
{
    private readonly List<CraftJob> jobs = new();
    private int nextId = 1;

    public IReadOnlyList<CraftJob> Jobs => jobs;

    public CraftJob Add(ItemAmount target, CraftPlan plan)
    {
        if (!plan.CanRun)
            throw new ArgumentException("Plan has missing items.", nameof(plan));
        var job = new CraftJob(nextId++, target, plan.Steps.Where(s => s.Times > 0).ToList());
        if (job.Steps.Count == 0)
            job.Status = JobStatus.Done;
        jobs.Add(job);
        return job;
    }

    /// <summary>Jobs being worked on now, one per rack, in request order.</summary>
    public IReadOnlyList<CraftJob> Active(int racks)
    {
        var active = jobs.Where(j => !j.Finished).Take(Math.Max(0, racks)).ToList();
        foreach (var j in active)
            if (j.Status == JobStatus.Queued)
                j.Status = JobStatus.Running;
        return active;
    }

    /// <summary>Record <paramref name="runs"/> finished runs of the job's current step.</summary>
    public void Report(CraftJob job, int runs)
    {
        if (job.Finished || runs <= 0)
            return;
        job.RunsDone += runs;
        while (job.Current is { } step && job.RunsDone >= step.Times)
        {
            job.RunsDone -= step.Times;
            job.StepIndex++;
        }
        if (job.Current == null)
        {
            job.RunsDone = 0;
            job.Status = JobStatus.Done;
        }
    }

    public bool Cancel(int id)
    {
        var job = jobs.FirstOrDefault(j => j.Id == id);
        if (job == null || job.Finished)
            return false;
        job.Status = JobStatus.Cancelled;
        return true;
    }

    /// <summary>Drop finished and cancelled jobs from the list.</summary>
    public int RemoveFinished() => jobs.RemoveAll(j => j.Finished);

    /// <summary>Put back a saved job.</summary>
    public CraftJob Restore(int id, ItemAmount target, IReadOnlyList<CraftStep> steps, JobStatus status, int stepIndex, int runsDone)
    {
        var job = new CraftJob(id, target, steps) { Status = status, StepIndex = stepIndex, RunsDone = runsDone };
        jobs.Add(job);
        nextId = Math.Max(nextId, id + 1);
        return job;
    }
}
