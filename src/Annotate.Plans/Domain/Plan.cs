namespace Annotate.Plans.Domain;

internal sealed class Plan
{
    public string Id { get; private set; }

    public string ProjectId { get; private set; }

    public string Title { get; private set; }

    public string? SessionId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public Plan(
        string id,
        string projectId,
        string title,
        string? sessionId,
        DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        Title = title;
        SessionId = sessionId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public void Touch(DateTimeOffset updatedAt) => UpdatedAt = updatedAt;

    public void Archive(DateTimeOffset at) => ArchivedAt = at;

    public void Restore() => ArchivedAt = null;

    private Plan()
    {
        Id = "";
        ProjectId = "";
        Title = "";
    }
}