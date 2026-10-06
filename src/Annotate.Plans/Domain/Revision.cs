namespace Annotate.Plans.Domain;

internal sealed class Revision
{
    public string Id { get; private set; }

    public string PlanId { get; private set; }

    public int Number { get; private set; }

    public string? ParentRevisionId { get; private set; }

    public string Markdown { get; private set; }

    public string? Summary { get; private set; }

    public string? StoryUrl { get; private set; }

    public string? AcceptanceCriteria { get; private set; }

    public string? Agent { get; private set; }

    public string? Model { get; private set; }

    public string? ClientName { get; private set; }

    public string? ClientVersion { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Revision(
        string id,
        string planId,
        int number,
        string? parentRevisionId,
        string markdown,
        string? summary,
        string? storyUrl,
        string? acceptanceCriteria,
        DateTimeOffset createdAt,
        string? agent = null,
        string? model = null,
        string? clientName = null,
        string? clientVersion = null)
    {
        Id = id;
        PlanId = planId;
        Number = number;
        ParentRevisionId = parentRevisionId;
        Markdown = markdown;
        Summary = summary;
        StoryUrl = storyUrl;
        AcceptanceCriteria = acceptanceCriteria;
        CreatedAt = createdAt;
        Agent = agent;
        Model = model;
        ClientName = clientName;
        ClientVersion = clientVersion;
    }

    public void DetachParent() => ParentRevisionId = null;

    private Revision()
    {
        Id = "";
        PlanId = "";
        Markdown = "";
    }
}