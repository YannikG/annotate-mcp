namespace Annotate.Markdown;

internal static class QuoteScanner
{
    public static bool Starts(ReadOnlySpan<char> content) => TryMarker(content, out _);

    public static bool TryRead(
        string source,
        IReadOnlyList<SourceLine> lines,
        int index,
        int limit,
        SectionPath sections,
        DecisionLog decisions,
        out QuoteBlock? block,
        out int next,
        out string? error)
    {
        block = null;
        next = index;
        error = null;
        if (index >= limit || !TryMarker(lines[index].Content(source), out _))
        {
            return false;
        }

        int cursor = index;
        List<SourceLine> inner = [];
        while (cursor < limit && TryMarker(lines[cursor].Content(source), out int markerEnd))
        {
            SourceLine line = lines[cursor];
            int start = line.Start + markerEnd;
            if (start > line.ContentEnd)
            {
                start = line.ContentEnd;
            }

            inner.Add(new SourceLine(start, line.ContentEnd, line.End));
            cursor++;
        }

        string path = sections.Current;
        ReadResult read = BlockReader.Read(source, inner, 0, inner.Count, sections, decisions);
        if (read.Error is not null)
        {
            error = read.Error;
            return true;
        }

        block = new QuoteBlock(read.Blocks, lines[index].Start, lines[cursor - 1].End, path);
        next = cursor;
        return true;
    }

    private static bool TryMarker(ReadOnlySpan<char> content, out int markerEnd)
    {
        markerEnd = 0;
        int index = 0;
        int spaces = 0;
        while (index < content.Length && content[index] == ' ')
        {
            spaces++;
            if (spaces > 3)
            {
                return false;
            }

            index++;
        }

        if (index >= content.Length || content[index] != '>')
        {
            return false;
        }

        index++;
        if (index < content.Length && content[index] == ' ')
        {
            index++;
        }

        markerEnd = index;
        return true;
    }
}