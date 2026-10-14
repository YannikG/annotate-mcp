namespace Annotate.Plans.Application;

public sealed record SubmitDocument(
    string Markdown,
    string? Summary,
    string? FolderPath,
    string? SessionId,
    string? ParentRevisionId,
    string? Agent = null,
    string? Model = null,
    string? ClientName = null,
    string? ClientVersion = null);