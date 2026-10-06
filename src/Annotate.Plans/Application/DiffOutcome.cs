namespace Annotate.Plans.Application;

public abstract record DiffOutcome
{
    private DiffOutcome()
    {
    }

    public sealed record Lines(IReadOnlyList<DiffLine> Rows) : DiffOutcome;

    public sealed record Refused(string Error) : DiffOutcome;
}