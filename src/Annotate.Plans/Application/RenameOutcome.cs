namespace Annotate.Plans.Application;

public abstract record RenameOutcome
{
    private RenameOutcome()
    {
    }

    public sealed record Renamed : RenameOutcome;

    public sealed record Refused(string Error) : RenameOutcome;
}