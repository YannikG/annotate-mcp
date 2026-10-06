namespace Annotate.Markdown;

internal readonly record struct SourceLine(int Start, int ContentEnd, int End)
{
    public ReadOnlySpan<char> Content(string source) => source.AsSpan(Start, ContentEnd - Start);

    public bool IsBlank(string source)
    {
        ReadOnlySpan<char> content = Content(source);
        foreach (char character in content)
        {
            if (character is not (' ' or '\t'))
            {
                return false;
            }
        }

        return true;
    }
}

internal static class LineReader
{
    public static List<SourceLine> Read(string source)
    {
        List<SourceLine> lines = [];
        int index = 0;
        while (index < source.Length)
        {
            int start = index;
            while (index < source.Length && source[index] != '\n')
            {
                index++;
            }

            int contentEnd = index;
            if (contentEnd > start && source[contentEnd - 1] == '\r')
            {
                contentEnd--;
            }

            if (index < source.Length)
            {
                index++;
            }

            lines.Add(new SourceLine(start, contentEnd, index));
        }

        return lines;
    }
}