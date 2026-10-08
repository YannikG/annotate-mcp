namespace Annotate.Reviews.Domain;

internal readonly record struct AnnotationDraft(
    string Id,
    string Kind,
    string Text,
    string? Comment,
    string? Replacement,
    int BlockOrdinal,
    int StartOffset,
    int EndOffset,
    string CreatedAt,
    string? BlockKey = null,
    string Author = "operator",
    IReadOnlyList<string>? Replies = null);