namespace Annotate.Plans.Application;

public sealed record ProjectSummary(
    ProjectId ProjectId,
    string DisplayName,
    string? FolderPath,
    int PlanCount,
    bool Archived = false);