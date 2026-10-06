using Annotate.Plans.Application;

namespace Annotate.Web.Tests;

internal sealed class FakePlans : IPlans
{
    public List<ProjectSummary> Projects { get; } = [];

    public List<ProjectSummary> ArchivedProjects { get; } = [];

    public Dictionary<string, IReadOnlyList<PlanSummary>> PlansByProject { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, IReadOnlyList<PlanSummary>> ArchivedPlansByProject { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, PlanDetail?> PlansById { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, RevisionDetail?> Revisions { get; } = new(StringComparer.Ordinal);

    public List<string> RevisionReads { get; } = [];

    public Dictionary<string, Exception> RevisionErrors { get; } = new(StringComparer.Ordinal);

    public List<RevisionActivity> Activity { get; } = [];

    public List<(string Older, string Newer)> DiffCalls { get; } = [];

    public DiffOutcome? DiffResult { get; set; }

    public Exception? DiffError { get; set; }

    public Func<ProjectId, string, RenameOutcome> Rename { get; set; } =
        (_, _) => throw new NotImplementedException();

    public string? RenamedName { get; private set; }

    public List<string> Archived { get; } = [];

    public List<string> Deleted { get; } = [];

    public List<string> Restored { get; } = [];

    public ProjectChange ArchiveResult { get; set; } = new ProjectChange.Done([]);

    public ProjectChange DeleteResult { get; set; } = new ProjectChange.Done([]);

    public ProjectChange RestoreResult { get; set; } = new ProjectChange.Done([]);

    public List<string> ArchivedPlanIds { get; } = [];

    public List<string> RestoredPlanIds { get; } = [];

    public List<string> DeletedPlanIds { get; } = [];

    public PlanChange ArchivePlanResult { get; set; } = new PlanChange.Done([]);

    public PlanChange RestorePlanResult { get; set; } = new PlanChange.Done([]);

    public PlanChange DeletePlanResult { get; set; } = new PlanChange.Done([]);

    public Task<SubmitOutcome> SubmitAsync(SubmitRevision request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<ProjectSummary>> ProjectsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProjectSummary>>(Projects);

    public Task<IReadOnlyList<ProjectSummary>> ArchivedProjectsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProjectSummary>>(ArchivedProjects);

    public Task<ProjectSummary?> ProjectAsync(ProjectId id, CancellationToken cancellationToken) =>
        Task.FromResult(
            Projects.Concat(ArchivedProjects).FirstOrDefault(project => project.ProjectId == id));

    public Task<RenameOutcome> RenameProjectAsync(ProjectId id, string displayName, CancellationToken cancellationToken)
    {
        RenamedName = displayName;
        return Task.FromResult(Rename(id, displayName));
    }

    public Task<IReadOnlyList<PlanSummary>> PlansAsync(ProjectId id, CancellationToken cancellationToken) =>
        Task.FromResult(
            PlansByProject.TryGetValue(id.Value, out IReadOnlyList<PlanSummary>? plans)
                ? plans
                : []);

    public Task<IReadOnlyList<PlanSummary>> ArchivedPlansAsync(ProjectId id, CancellationToken cancellationToken) =>
        Task.FromResult(
            ArchivedPlansByProject.TryGetValue(id.Value, out IReadOnlyList<PlanSummary>? plans)
                ? plans
                : []);

    public Task<PlanChange> ArchivePlanAsync(PlanId id, CancellationToken cancellationToken)
    {
        ArchivedPlanIds.Add(id.Value);
        if (ArchivePlanResult is PlanChange.Done
            && PlansById.TryGetValue(id.Value, out PlanDetail? plan)
            && plan is not null)
        {
            PlansById[id.Value] = plan with { Archived = true };
        }

        return Task.FromResult(ArchivePlanResult);
    }

    public Task<PlanChange> RestorePlanAsync(PlanId id, CancellationToken cancellationToken)
    {
        RestoredPlanIds.Add(id.Value);
        if (RestorePlanResult is PlanChange.Done
            && PlansById.TryGetValue(id.Value, out PlanDetail? plan)
            && plan is not null)
        {
            PlansById[id.Value] = plan with { Archived = false };
        }

        return Task.FromResult(RestorePlanResult);
    }

    public Task<PlanChange> DeletePlanAsync(PlanId id, CancellationToken cancellationToken)
    {
        DeletedPlanIds.Add(id.Value);
        return Task.FromResult(DeletePlanResult);
    }

    public Task<PlanDetail?> PlanAsync(PlanId id, CancellationToken cancellationToken) =>
        Task.FromResult(PlansById.TryGetValue(id.Value, out PlanDetail? plan) ? plan : null);

    public Task<RevisionDetail?> RevisionAsync(RevisionId id, CancellationToken cancellationToken)
    {
        RevisionReads.Add(id.Value);
        if (RevisionErrors.TryGetValue(id.Value, out Exception? error))
        {
            return Task.FromException<RevisionDetail?>(error);
        }

        return Task.FromResult(Revisions.TryGetValue(id.Value, out RevisionDetail? revision) ? revision : null);
    }

    public Task<ProjectChange> ArchiveProjectAsync(ProjectId id, CancellationToken cancellationToken)
    {
        Archived.Add(id.Value);
        return Task.FromResult(ArchiveResult);
    }

    public Task<ProjectChange> DeleteProjectAsync(ProjectId id, CancellationToken cancellationToken)
    {
        Deleted.Add(id.Value);
        return Task.FromResult(DeleteResult);
    }

    public Task<ProjectChange> RestoreProjectAsync(ProjectId id, CancellationToken cancellationToken)
    {
        Restored.Add(id.Value);
        if (RestoreResult is ProjectChange.Done)
        {
            ProjectSummary? archived = ArchivedProjects.FirstOrDefault(project => project.ProjectId == id);
            if (archived is not null)
            {
                ArchivedProjects.Remove(archived);
                Projects.Add(archived with { Archived = false });
            }
        }

        return Task.FromResult(RestoreResult);
    }

    public Task<IReadOnlyList<RevisionActivity>> RevisionActivityAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RevisionActivity>>(Activity);

    public Task<DiffOutcome> DiffAsync(RevisionId older, RevisionId newer, CancellationToken cancellationToken)
    {
        DiffCalls.Add((older.Value, newer.Value));
        if (DiffError is not null)
        {
            return Task.FromException<DiffOutcome>(DiffError);
        }

        return DiffResult is null
            ? Task.FromException<DiffOutcome>(new NotImplementedException())
            : Task.FromResult(DiffResult);
    }
}