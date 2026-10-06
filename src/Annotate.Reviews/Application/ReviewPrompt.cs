namespace Annotate.Reviews.Application;

public enum PromptKind
{
    Choice,
    Text,
}

public sealed record ReviewPrompt(
    string Id,
    PromptKind Kind,
    string Prompt,
    IReadOnlyList<string> Options);