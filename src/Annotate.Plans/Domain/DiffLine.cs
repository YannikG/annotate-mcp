namespace Annotate.Plans.Domain;

internal abstract record DiffLine
{
    private DiffLine()
    {
    }

    internal sealed record Equal(string Text) : DiffLine;

    internal sealed record Added(string Text) : DiffLine;

    internal sealed record Removed(string Text) : DiffLine;
}