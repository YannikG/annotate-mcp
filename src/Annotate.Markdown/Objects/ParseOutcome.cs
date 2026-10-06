namespace Annotate.Markdown;

public abstract record ParseOutcome
{
    public sealed record Ok(PlanDocument Document) : ParseOutcome;

    public sealed record Invalid(string Error) : ParseOutcome;
}

public sealed record PlanDocument(
    IReadOnlyList<Block> Blocks,
    IReadOnlyList<DecisionPrompt> Decisions);