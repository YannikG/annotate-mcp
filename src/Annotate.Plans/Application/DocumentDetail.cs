namespace Annotate.Plans.Application;

public sealed record DocumentSummary(
    DocumentId DocumentId,
    string Title,
    DateTimeOffset UpdatedAt,
    int RevisionCount,
    bool ReadOnly,
    bool Archived = false);

public sealed record DocumentDetail(
    DocumentId DocumentId,
    ProjectId ProjectId,
    string Title,
    string? SessionId,
    IReadOnlyList<PlanRevision> Revisions,
    bool Archived,
    bool ReadOnly,
    IReadOnlyList<string> LockedBlockKeys);

public sealed record DocumentRevisionDetail(
    RevisionId RevisionId,
    DocumentId DocumentId,
    int Number,
    string Markdown,
    string? Summary,
    IReadOnlyList<RevisionBlock> Blocks,
    string? ParentRevisionId,
    Attribution? Attribution,
    bool ReadOnly);