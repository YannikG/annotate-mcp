namespace Annotate.Plans.Application;

public abstract record ProjectChange
{
    private ProjectChange()
    {
    }

    public sealed record Done(IReadOnlyList<string> RevisionIds) : ProjectChange;

    public sealed record Refused(string Error) : ProjectChange;
}