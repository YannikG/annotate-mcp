namespace Annotate.Plans.Application;

public abstract record PlanChange
{
    private PlanChange()
    {
    }

    public sealed record Done(IReadOnlyList<string> RevisionIds) : PlanChange;

    public sealed record AlreadyArchived : PlanChange;

    public sealed record Refused(string Error) : PlanChange;
}