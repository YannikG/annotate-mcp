namespace Annotate.Markdown;

internal static class DetailsScanner
{
    public static bool Starts(ReadOnlySpan<char> content) => IsTag(content, "<details>");

    public static bool TryRead(
        string source,
        IReadOnlyList<SourceLine> lines,
        int index,
        int limit,
        SectionPath sections,
        DecisionLog decisions,
        out DetailsBlock? block,
        out int next,
        out string? error)
    {
        block = null;
        next = index;
        error = null;
        if (index >= limit || !Starts(lines[index].Content(source)))
        {
            return false;
        }

        int summaryLine = index + 1;
        while (summaryLine < limit && lines[summaryLine].IsBlank(source))
        {
            summaryLine++;
        }

        if (summaryLine >= limit
            || !TrySummary(source, lines, summaryLine, limit, out int summaryStart, out int summaryEnd, out int afterSummary))
        {
            return false;
        }

        int close = afterSummary;
        while (close < limit && !IsTag(lines[close].Content(source), "</details>"))
        {
            close++;
        }

        if (close >= limit)
        {
            return false;
        }

        string path = sections.Current;
        ReadResult read = BlockReader.Read(source, lines, afterSummary, close, sections, decisions);
        if (read.Error is not null)
        {
            error = read.Error;
            return true;
        }

        block = new DetailsBlock(
            InlineReader.Read(source, summaryStart, summaryEnd),
            read.Blocks,
            lines[index].Start,
            lines[close].End,
            path);
        next = close + 1;
        return true;
    }

    private static bool TrySummary(
        string source,
        IReadOnlyList<SourceLine> lines,
        int index,
        int limit,
        out int summaryStart,
        out int summaryEnd,
        out int next)
    {
        summaryStart = 0;
        summaryEnd = 0;
        next = index;
        SourceLine line = lines[index];
        ReadOnlySpan<char> content = line.Content(source);
        int open = IndexOf(content, "<summary>");
        if (open < 0 || content[..open].Trim().Length != 0)
        {
            return false;
        }

        int afterOpen = open + "<summary>".Length;
        int close = IndexOf(content[afterOpen..], "</summary>");
        if (close >= 0)
        {
            int closeAt = afterOpen + close;
            if (content[(closeAt + "</summary>".Length)..].Trim().Length != 0)
            {
                return false;
            }

            summaryStart = line.Start + afterOpen;
            summaryEnd = line.Start + closeAt;
            Trim(source, ref summaryStart, ref summaryEnd);
            next = index + 1;
            return true;
        }

        int closeLine = index + 1;
        while (closeLine < limit && !IsTag(lines[closeLine].Content(source), "</summary>"))
        {
            closeLine++;
        }

        if (closeLine >= limit)
        {
            return false;
        }

        summaryStart = line.Start + afterOpen;
        summaryEnd = lines[closeLine].Start;
        Trim(source, ref summaryStart, ref summaryEnd);
        next = closeLine + 1;
        return true;
    }

    private static void Trim(string source, ref int start, ref int end)
    {
        while (start < end && source[start] is ' ' or '\t' or '\n' or '\r')
        {
            start++;
        }

        while (end > start && source[end - 1] is ' ' or '\t' or '\n' or '\r')
        {
            end--;
        }
    }

    private static bool IsTag(ReadOnlySpan<char> content, string tag) =>
        content.Trim().Equals(tag, StringComparison.OrdinalIgnoreCase);

    private static int IndexOf(ReadOnlySpan<char> content, string value)
    {
        for (int index = 0; index <= content.Length - value.Length; index++)
        {
            if (content.Slice(index, value.Length).Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }
}