namespace Annotate.Plans.Application;

public abstract record DocumentChange
{
    private DocumentChange()
    {
    }

    public sealed record Done(IReadOnlyList<string> RevisionIds) : DocumentChange;

    public sealed record AlreadyArchived : DocumentChange;

    public sealed record Refused(string Error) : DocumentChange;
}