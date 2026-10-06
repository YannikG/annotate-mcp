namespace Annotate.Plans.Application;

public sealed record RevisionActivity(
    RevisionId RevisionId,
    PlanId PlanId,
    string PlanTitle,
    ProjectId ProjectId,
    string ProjectName,
    bool ProjectArchived,
    int Number,
    DateTimeOffset CreatedAt,
    Attribution Attribution,
    bool PlanArchived = false);