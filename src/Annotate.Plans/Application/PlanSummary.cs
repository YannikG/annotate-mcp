namespace Annotate.Plans.Application;

public sealed record PlanSummary(PlanId PlanId, string Title, DateTimeOffset UpdatedAt, int RevisionCount);