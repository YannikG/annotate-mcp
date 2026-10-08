namespace Annotate.Reviews.Domain;

internal sealed class StoredAnnotation
{
    public string ReviewId { get; private set; }

    public int Ordinal { get; private set; }

    public string AnnotationId { get; private set; }

    public string Kind { get; private set; }

    public int BlockOrdinal { get; private set; }

    public int StartOffset { get; private set; }

    public int EndOffset { get; private set; }

    public string Text { get; private set; }

    public string? Replacement { get; private set; }

    public string? Comment { get; private set; }

    public string? BlockKey { get; private set; }

    public string Author { get; private set; }

    public bool? Accepted { get; private set; }

    public string CreatedAt { get; private set; }

    public StoredAnnotation(
        string reviewId,
        int ordinal,
        string annotationId,
        string kind,
        int blockOrdinal,
        int startOffset,
        int endOffset,
        string text,
        string? replacement,
        string? comment,
        string createdAt,
        string? blockKey = null,
        string author = "operator",
        bool? accepted = null)
    {
        ReviewId = reviewId;
        Ordinal = ordinal;
        AnnotationId = annotationId;
        Kind = kind;
        BlockOrdinal = blockOrdinal;
        StartOffset = startOffset;
        EndOffset = endOffset;
        Text = text;
        Replacement = replacement;
        Comment = comment;
        CreatedAt = createdAt;
        BlockKey = blockKey;
        Author = author;
        Accepted = accepted;
    }

    public void Accept(bool accepted) => Accepted = accepted;

    private StoredAnnotation()
    {
        ReviewId = "";
        AnnotationId = "";
        Kind = "";
        Text = "";
        CreatedAt = "";
        Author = "operator";
    }
}