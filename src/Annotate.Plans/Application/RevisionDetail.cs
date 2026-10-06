namespace Annotate.Plans.Application;

public sealed record RevisionDetail(
    RevisionId RevisionId,
    PlanId PlanId,
    int Number,
    string Markdown,
    string? Summary,
    string? StoryUrl,
    string? AcceptanceCriteria,
    IReadOnlyList<RevisionBlock> Blocks,
    string? ParentRevisionId,
    Attribution? Attribution = null);