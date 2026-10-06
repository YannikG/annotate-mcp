using Annotate.Markdown;

namespace Annotate.Reviews.Infrastructure;

internal readonly record struct CapturedFence(
    string Id,
    string Kind,
    string Prompt,
    IReadOnlyList<string> Options);

internal static class DecisionCapture
{
    public static IReadOnlyList<CapturedFence> Read(PlanDocument document)
    {
        Dictionary<(int Start, int End), CodeBlock> fences = [];
        Collect(document.Blocks, fences);
        List<CapturedFence> captured = [];
        foreach (DecisionPrompt prompt in document.Decisions)
        {
            if (!fences.TryGetValue((prompt.Start, prompt.End), out CodeBlock? block))
            {
                throw new InvalidOperationException("Decision fence was missing from the document.");
            }

            captured.Add(new CapturedFence(
                prompt.Id,
                prompt.Kind == DecisionKind.Choice ? "choice" : "text",
                Question(block.Body),
                prompt.Options));
        }

        return captured;
    }

    private static void Collect(IReadOnlyList<Block> blocks, Dictionary<(int Start, int End), CodeBlock> fences)
    {
        foreach (Block block in blocks)
        {
            switch (block)
            {
                case CodeBlock code when code.Language.Equals("decision", StringComparison.OrdinalIgnoreCase):
                    fences[(code.Start, code.End)] = code;
                    break;
                case QuoteBlock quote:
                    Collect(quote.Children, fences);
                    break;
                case DetailsBlock details:
                    Collect(details.Children, fences);
                    break;
            }
        }
    }

    private static string Question(string body)
    {
        foreach (string line in Lines(body))
        {
            if (TryPrompt(line, out string value))
            {
                return value.Trim();
            }
        }

        throw new InvalidOperationException("Decision fence has no prompt.");
    }

    private static bool TryPrompt(string line, out string value)
    {
        value = "";
        int colon = line.IndexOf(':');
        if (colon <= 0 || line[..colon] != "prompt")
        {
            return false;
        }

        int index = colon + 1;
        while (index < line.Length && line[index] is ' ' or '\t')
        {
            index++;
        }

        value = line[index..];
        return true;
    }

    private static IEnumerable<string> Lines(string body)
    {
        int start = 0;
        for (int index = 0; index < body.Length; index++)
        {
            if (body[index] != '\n')
            {
                continue;
            }

            yield return StripCr(body[start..index]);
            start = index + 1;
        }

        if (start < body.Length)
        {
            yield return StripCr(body[start..]);
        }
    }

    private static string StripCr(string line) =>
        line.Length > 0 && line[^1] == '\r' ? line[..^1] : line;
}