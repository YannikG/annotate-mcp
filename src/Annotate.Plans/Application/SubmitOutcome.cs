namespace Annotate.Plans.Application;

public abstract record SubmitOutcome
{
    private SubmitOutcome()
    {
    }

    public sealed record Created(ProjectId Project, PlanId Plan, RevisionId Revision, int Number) : SubmitOutcome;

    public sealed record Refused(string Error) : SubmitOutcome;

    public sealed record Archived(string DisplayName) : SubmitOutcome;

    public sealed record PlanArchived(string Title) : SubmitOutcome;
}