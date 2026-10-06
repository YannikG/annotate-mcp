namespace Annotate.Markdown;

internal static class TableScanner
{
    public static bool Starts(string source, IReadOnlyList<SourceLine> lines, int index, int limit) =>
        TryRead(source, lines, index, limit, string.Empty, out _, out _);

    public static bool TryRead(
        string source,
        IReadOnlyList<SourceLine> lines,
        int index,
        int limit,
        string sectionPath,
        out TableBlock? block,
        out int next)
    {
        block = null;
        next = index;
        if (index + 1 >= limit || lines[index].IsBlank(source))
        {
            return false;
        }

        if (!TryCells(source, lines[index], out List<CellSpan> header) || header.Count == 0)
        {
            return false;
        }

        if (!TrySeparator(source, lines[index + 1], header.Count, out List<ColumnAlignment> alignments))
        {
            return false;
        }

        int cursor = index + 2;
        List<IReadOnlyList<TableCell>> rows = [];
        while (cursor < limit && !lines[cursor].IsBlank(source) && !StartsBlock(source, lines[cursor]))
        {
            if (!TryCells(source, lines[cursor], out List<CellSpan> cells))
            {
                break;
            }

            rows.Add(Fit(source, cells, header.Count));
            cursor++;
        }

        block = new TableBlock(
            Fit(source, header, header.Count),
            alignments,
            rows,
            lines[index].Start,
            lines[cursor - 1].End,
            sectionPath);
        next = cursor;
        return true;
    }

    private static bool StartsBlock(string source, SourceLine line)
    {
        ReadOnlySpan<char> content = line.Content(source);
        return HeadingScanner.TryAtx(source, line, out _, out _, out _)
            || ListScanner.StartsItem(content)
            || FenceScanner.StartsFence(content)
            || QuoteScanner.Starts(content);
    }

    private static List<TableCell> Fit(string source, List<CellSpan> cells, int width)
    {
        List<TableCell> row = [];
        for (int index = 0; index < width; index++)
        {
            if (index < cells.Count)
            {
                CellSpan cell = cells[index];
                row.Add(new TableCell(InlineReader.Read(source, cell.Start, cell.End)));
            }
            else
            {
                row.Add(new TableCell([]));
            }
        }

        return row;
    }

    private static bool TrySeparator(
        string source,
        SourceLine line,
        int columns,
        out List<ColumnAlignment> alignments)
    {
        alignments = [];
        if (!TryCells(source, line, out List<CellSpan> cells) || cells.Count != columns)
        {
            return false;
        }

        foreach (CellSpan cell in cells)
        {
            if (!TryAlignment(source[cell.Start..cell.End], out ColumnAlignment alignment))
            {
                return false;
            }

            alignments.Add(alignment);
        }

        return true;
    }

    private static bool TryAlignment(string text, out ColumnAlignment alignment)
    {
        alignment = ColumnAlignment.None;
        if (text.Length < 3)
        {
            return false;
        }

        bool left = text[0] == ':';
        bool right = text[^1] == ':';
        int start = left ? 1 : 0;
        int end = right ? text.Length - 1 : text.Length;
        if (end - start < 3)
        {
            return false;
        }

        for (int index = start; index < end; index++)
        {
            if (text[index] != '-')
            {
                return false;
            }
        }

        if (left && right)
        {
            alignment = ColumnAlignment.Center;
        }
        else if (left)
        {
            alignment = ColumnAlignment.Left;
        }
        else if (right)
        {
            alignment = ColumnAlignment.Right;
        }

        return true;
    }

    private static bool TryCells(string source, SourceLine line, out List<CellSpan> cells)
    {
        cells = [];
        ReadOnlySpan<char> content = line.Content(source);
        if (content.IndexOf('|') < 0)
        {
            return false;
        }

        List<CellSpan> raw = [];
        int cellStart = 0;
        for (int index = 0; index <= content.Length; index++)
        {
            if (index == content.Length || content[index] == '|')
            {
                raw.Add(new CellSpan(line.Start + cellStart, line.Start + index));
                cellStart = index + 1;
            }
        }

        int from = 0;
        int to = raw.Count;
        if (to > from && Blank(source, raw[from]))
        {
            from++;
        }

        if (to > from && Blank(source, raw[to - 1]))
        {
            to--;
        }

        for (int index = from; index < to; index++)
        {
            int start = raw[index].Start;
            int end = raw[index].End;
            while (start < end && source[start] is ' ' or '\t')
            {
                start++;
            }

            while (end > start && source[end - 1] is ' ' or '\t')
            {
                end--;
            }

            cells.Add(new CellSpan(start, end));
        }

        return cells.Count > 0;
    }

    private static bool Blank(string source, CellSpan cell)
    {
        for (int index = cell.Start; index < cell.End; index++)
        {
            if (source[index] is not (' ' or '\t'))
            {
                return false;
            }
        }

        return true;
    }

    private readonly record struct CellSpan(int Start, int End);
}