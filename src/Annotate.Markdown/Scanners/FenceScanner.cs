namespace Annotate.Markdown;

internal static class FenceScanner
{
    public static bool TryRead(
        string source,
        IReadOnlyList<SourceLine> lines,
        int index,
        int limit,
        string sectionPath,
        out CodeBlock? block,
        out int next)
    {
        block = null;
        next = index;
        if (index >= limit || !TryOpen(lines[index].Content(source), out FenceOpen open))
        {
            return false;
        }

        int cursor = index + 1;
        while (cursor < limit && !IsClose(lines[cursor].Content(source), open))
        {
            cursor++;
        }

        int bodyStart = lines[index].End;
        int bodyEnd;
        int blockEnd;
        if (cursor < limit)
        {
            bodyEnd = lines[cursor].Start;
            blockEnd = lines[cursor].End;
            next = cursor + 1;
        }
        else
        {
            blockEnd = lines[limit - 1].End;
            bodyEnd = blockEnd;
            next = limit;
        }

        block = new CodeBlock(open.Language, source[bodyStart..bodyEnd], lines[index].Start, blockEnd, sectionPath);
        return true;
    }

    public static bool StartsFence(ReadOnlySpan<char> content) => TryOpen(content, out _);

    private static bool TryOpen(ReadOnlySpan<char> content, out FenceOpen open)
    {
        open = default;
        int index = 0;
        while (index < content.Length && content[index] == ' ')
        {
            index++;
        }

        if (index > 3 || index >= content.Length || content[index] is not ('`' or '~'))
        {
            return false;
        }

        char marker = content[index];
        int length = 0;
        while (index < content.Length && content[index] == marker)
        {
            length++;
            index++;
        }

        if (length < 3)
        {
            return false;
        }

        ReadOnlySpan<char> info = content[index..];
        if (marker == '`' && info.Contains('`'))
        {
            return false;
        }

        open = new FenceOpen(marker, length, info.Trim().ToString());
        return true;
    }

    private static bool IsClose(ReadOnlySpan<char> content, FenceOpen open)
    {
        int index = 0;
        while (index < content.Length && content[index] == ' ')
        {
            index++;
        }

        if (index > 3)
        {
            return false;
        }

        int length = 0;
        while (index < content.Length && content[index] == open.Marker)
        {
            length++;
            index++;
        }

        if (length < open.Length)
        {
            return false;
        }

        while (index < content.Length && content[index] == ' ')
        {
            index++;
        }

        return index == content.Length;
    }

    private readonly record struct FenceOpen(char Marker, int Length, string Language);
}