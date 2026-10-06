namespace Annotate.Markdown;

internal sealed class DecisionLog
{
    private readonly List<DecisionPrompt> _prompts = [];

    private readonly HashSet<string> _ids = new(StringComparer.Ordinal);

    private int _count;

    public IReadOnlyList<DecisionPrompt> Prompts => _prompts;

    public string? Accept(CodeBlock code)
    {
        if (!code.Language.Equals("decision", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        _count++;
        if (!DecisionReader.TryRead(code, _count, _ids, out DecisionPrompt? prompt, out string? error))
        {
            return error;
        }

        _ids.Add(prompt!.Id);
        _prompts.Add(prompt);
        return null;
    }
}