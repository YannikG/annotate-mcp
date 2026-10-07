using Annotate.Web;

namespace Annotate.Web.Components.Browse;

public sealed record HomeViewModel(
    IReadOnlyList<ReviewLink> Pending,
    IReadOnlyList<ReviewLink> Approved,
    IReadOnlyList<ReviewLink> Changes);

public sealed record ReviewLink(
    string ReviewId, string Title, string PlanId, string? ProjectId, string? ProjectName,
    int RevisionNumber, DateTimeOffset Date);

public sealed record ProjectsViewModel(IReadOnlyList<ProjectRow> Projects, IReadOnlyList<ProjectRow> Archived);

public sealed record ProjectRow(string ProjectId, string DisplayName, int PlanCount, string? FolderPath = null);

public sealed record ProjectViewModel(
    string DisplayName,
    string? FolderPath,
    string? Error,
    ProjectPlanListing Listing,
    bool Archived = false,
    ProjectPlanTab Tab = ProjectPlanTab.Pending,
    int Page = 1);

public sealed record PlanViewModel(
    string Title,
    IReadOnlyList<RevisionRow> Revisions,
    string? SelectedId = null,
    RevisionViewModel? Selected = null,
    ProjectRow? Project = null,
    string? PendingReviewId = null,
    string? ReportError = null,
    string? AttributionNote = null,
    bool Archived = false,
    string? Error = null);

public sealed record RevisionRow(
    string RevisionId, int Number, string CreatedAt, string Status, string? Agent = null, string? Model = null);

public sealed record RevisionViewModel(
    int Number, IReadOnlyList<BlockRow> Blocks, string Markdown = "",
    IReadOnlyList<Annotate.Reviews.Application.Annotation>? Annotations = null,
    string? Summary = null, string? StoryUrl = null, string? AcceptanceCriteria = null,
    bool Approved = false,
    Annotate.Plans.Application.Attribution? Attribution = null,
    string? PlanId = null,
    string? ReviewId = null);

public sealed record BlockRow(string Text, int Start, int End, string Change, string Key = "");