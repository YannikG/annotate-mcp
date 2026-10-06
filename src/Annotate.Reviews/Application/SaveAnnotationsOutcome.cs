namespace Annotate.Reviews.Application;

public abstract record SaveAnnotationsOutcome
{
    private SaveAnnotationsOutcome() { }

    public sealed record Done : SaveAnnotationsOutcome;

    public sealed record Refused(string Error) : SaveAnnotationsOutcome;
}