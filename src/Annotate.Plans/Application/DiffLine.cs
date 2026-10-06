namespace Annotate.Plans.Application;

public abstract record DiffLine
{
    private DiffLine()
    {
    }

    public sealed record Equal(string Text) : DiffLine;

    public sealed record Added(string Text) : DiffLine;

    public sealed record Removed(string Text) : DiffLine;
}