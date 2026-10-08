namespace Annotate.Reviews.Application;

public enum AnnotationKind
{
    Deletion,
    Replacement,
    Insertion,
    Comment,
}

public enum AnnotationAuthor
{
    Operator,
    Agent,
}

public sealed record AnnotationReply(string Id, string Text, string CreatedAt);

public sealed record BlockComment(string BlockKey, string Comment, AnnotationAuthor Author);

public sealed record Annotation(
    string Id,
    AnnotationKind Kind,
    string Text,
    string? Comment,
    string? Replacement,
    int BlockOrdinal,
    int StartOffset,
    int EndOffset,
    string CreatedAt,
    string? BlockKey = null,
    AnnotationAuthor Author = AnnotationAuthor.Operator,
    bool? Accepted = null,
    IReadOnlyList<AnnotationReply>? Replies = null);