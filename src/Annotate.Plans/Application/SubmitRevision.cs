namespace Annotate.Plans.Application;

public sealed record SubmitRevision(
    string Markdown,
    string? Summary,
    string? FolderPath,
    string? SessionId,
    string? ParentRevisionId,
    string? StoryUrl,
    string? AcceptanceCriteria,
    string? Agent = null,
    string? Model = null,
    string? ClientName = null,
    string? ClientVersion = null);