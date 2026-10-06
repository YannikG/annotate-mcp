namespace Annotate.Reviews.Application;

public abstract record DecideOutcome
{
    private DecideOutcome()
    {
    }

    public sealed record Done : DecideOutcome;

    public sealed record Refused(string Error) : DecideOutcome;
}

public abstract record SaveAnswerOutcome
{
    private SaveAnswerOutcome()
    {
    }

    public sealed record Done : SaveAnswerOutcome;

    public sealed record Refused(string Error) : SaveAnswerOutcome;
}