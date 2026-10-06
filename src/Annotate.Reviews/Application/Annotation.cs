namespace Annotate.Reviews.Application;

public enum AnnotationKind
{
    Deletion,
    Replacement,
    Insertion,
    Comment,
}

public sealed record Annotation(
    string Id,
    AnnotationKind Kind,
    string Text,
    string? Comment,
    string? Replacement,
    int BlockOrdinal,
    int StartOffset,
    int EndOffset,
    string CreatedAt);