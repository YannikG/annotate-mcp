namespace Annotate.Reviews.Domain;

internal sealed class StoredReply
{
    public string ReviewId { get; private set; }

    public string AnnotationId { get; private set; }

    public int Ordinal { get; private set; }

    public string ReplyId { get; private set; }

    public string Text { get; private set; }

    public string CreatedAt { get; private set; }

    public StoredReply(
        string reviewId,
        string annotationId,
        int ordinal,
        string replyId,
        string text,
        string createdAt)
    {
        ReviewId = reviewId;
        AnnotationId = annotationId;
        Ordinal = ordinal;
        ReplyId = replyId;
        Text = text;
        CreatedAt = createdAt;
    }

    private StoredReply()
    {
        ReviewId = "";
        AnnotationId = "";
        ReplyId = "";
        Text = "";
        CreatedAt = "";
    }
}