namespace Annotate.Plans.Domain;

internal static class RevisionLimits
{
    public const int MaxMarkdown = 256000;

    public const int MaxSummary = 300;

    public const int MaxAcceptanceCriteria = 8000;

    public const int MaxSessionId = 200;

    public static string? Reject(
        string markdown,
        string? summary,
        string? acceptanceCriteria,
        string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return "Plan is empty.";
        }

        if (markdown.Length > MaxMarkdown)
        {
            return "Plan exceeds 256000 characters.";
        }

        if (summary is { Length: > MaxSummary })
        {
            return "Summary exceeds 300 characters.";
        }

        if (acceptanceCriteria is { Length: > MaxAcceptanceCriteria })
        {
            return "Acceptance criteria exceed 8000 characters.";
        }

        if (sessionId is { Length: > MaxSessionId })
        {
            return "Session id exceeds 200 characters.";
        }

        return null;
    }
}