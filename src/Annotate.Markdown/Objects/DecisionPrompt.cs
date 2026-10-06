namespace Annotate.Markdown;

public enum DecisionKind
{
    Choice,
    Text,
}

public sealed record DecisionPrompt(
    string Id,
    DecisionKind Kind,
    IReadOnlyList<string> Options,
    int Start,
    int End);