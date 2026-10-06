namespace Annotate.Markdown;

internal static class RuleScanner
{
    public static bool IsRule(ReadOnlySpan<char> content) => TryMark(content, out _);

    public static bool TryRead(
        string source,
        SourceLine line,
        string sectionPath,
        out RuleBlock? block)
    {
        block = null;
        if (!IsRule(line.Content(source)))
        {
            return false;
        }

        block = new RuleBlock(line.Start, line.End, sectionPath);
        return true;
    }

    private static bool TryMark(ReadOnlySpan<char> content, out char mark)
    {
        mark = default;
        int count = 0;
        foreach (char character in content)
        {
            if (character is ' ' or '\t')
            {
                continue;
            }

            if (character is not ('-' or '*' or '_'))
            {
                return false;
            }

            if (count == 0)
            {
                mark = character;
            }
            else if (character != mark)
            {
                return false;
            }

            count++;
        }

        return count >= 3;
    }
}