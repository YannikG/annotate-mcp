namespace Annotate.Reviews.Domain;

internal sealed class Review
{
    public const string Pending = "pending";

    public const string Approved = "approved";

    public const string ChangesRequested = "changes_requested";

    public string Id { get; private set; }

    public string RevisionId { get; private set; }

    public string Status { get; private set; }

    public string? Feedback { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    public Review(string id, string revisionId, DateTimeOffset createdAt)
    {
        Id = id;
        RevisionId = revisionId;
        Status = Pending;
        CreatedAt = createdAt;
    }

    public bool IsPending => Status == Pending;

    public void Approve(DateTimeOffset decidedAt)
    {
        Status = Approved;
        Feedback = null;
        DecidedAt = decidedAt;
    }

    public void RequestChanges(string feedback, DateTimeOffset decidedAt)
    {
        Status = ChangesRequested;
        Feedback = feedback;
        DecidedAt = decidedAt;
    }

    private Review()
    {
        Id = "";
        RevisionId = "";
        Status = "";
    }
}