namespace Annotate.Plans.Application;

public abstract record DocumentSubmitOutcome
{
    private DocumentSubmitOutcome()
    {
    }

    public sealed record Created(ProjectId Project, DocumentId Document, RevisionId Revision, int Number)
        : DocumentSubmitOutcome;

    public sealed record Refused(string Error) : DocumentSubmitOutcome;

    public sealed record ProjectArchived(string DisplayName) : DocumentSubmitOutcome;

    public sealed record DocumentArchived(string Title) : DocumentSubmitOutcome;

    public sealed record ReadOnlyDocument(string Title) : DocumentSubmitOutcome;
}