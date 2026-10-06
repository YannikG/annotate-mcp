namespace Annotate.Plans.Application;

public interface IPlans
{
    Task<SubmitOutcome> SubmitAsync(SubmitRevision request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectSummary>> ProjectsAsync(CancellationToken cancellationToken);

    Task<RenameOutcome> RenameProjectAsync(ProjectId id, string displayName, CancellationToken cancellationToken);

    Task<ProjectChange> ArchiveProjectAsync(ProjectId id, CancellationToken cancellationToken);

    Task<ProjectChange> DeleteProjectAsync(ProjectId id, CancellationToken cancellationToken);

    Task<ProjectChange> RestoreProjectAsync(ProjectId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectSummary>> ArchivedProjectsAsync(CancellationToken cancellationToken);

    Task<ProjectSummary?> ProjectAsync(ProjectId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlanSummary>> PlansAsync(ProjectId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlanSummary>> ArchivedPlansAsync(ProjectId id, CancellationToken cancellationToken);

    Task<PlanChange> ArchivePlanAsync(PlanId id, CancellationToken cancellationToken);

    Task<PlanChange> RestorePlanAsync(PlanId id, CancellationToken cancellationToken);

    Task<PlanChange> DeletePlanAsync(PlanId id, CancellationToken cancellationToken);

    Task<PlanDetail?> PlanAsync(PlanId id, CancellationToken cancellationToken);

    Task<RevisionDetail?> RevisionAsync(RevisionId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<RevisionActivity>> RevisionActivityAsync(CancellationToken cancellationToken);

    Task<DiffOutcome> DiffAsync(RevisionId older, RevisionId newer, CancellationToken cancellationToken);
}