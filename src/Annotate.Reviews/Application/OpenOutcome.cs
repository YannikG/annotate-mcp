namespace Annotate.Reviews.Application;

public abstract record OpenOutcome
{
    private OpenOutcome()
    {
    }

    public sealed record Opened(ReviewId Id) : OpenOutcome;

    public sealed record Refused(string Error) : OpenOutcome;
}