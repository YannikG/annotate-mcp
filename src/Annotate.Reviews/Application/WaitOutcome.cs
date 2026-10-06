namespace Annotate.Reviews.Application;

public abstract record WaitOutcome
{
    private WaitOutcome()
    {
    }

    public sealed record Pending : WaitOutcome;

    public sealed record Missing : WaitOutcome;

    public sealed record Decided(ReviewDecision Decision) : WaitOutcome;
}

public abstract record ReviewDecision
{
    private ReviewDecision()
    {
    }

    public sealed record Approved : ReviewDecision;

    public sealed record ChangesRequested(string Feedback) : ReviewDecision;
}