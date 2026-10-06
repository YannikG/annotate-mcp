namespace Annotate.Plans.Application;

public sealed record PlanRevision(
    RevisionId RevisionId,
    int Number,
    DateTimeOffset CreatedAt,
    Attribution? Attribution = null);

public sealed record PlanDetail(
    PlanId PlanId,
    ProjectId ProjectId,
    string Title,
    string? SessionId,
    IReadOnlyList<PlanRevision> Revisions,
    bool Archived = false);