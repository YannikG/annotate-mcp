namespace Annotate.Markdown;

internal static class ListScanner
{
    public static bool TryRead(
        string source,
        IReadOnlyList<SourceLine> lines,
        int index,
        int limit,
        string sectionPath,
        out ListBlock? block,
        out int next)
    {
        block = null;
        next = index;
        if (index >= limit || !TryMarker(lines[index].Content(source), out ListMarker first))
        {
            return false;
        }

        int cursor = index;
        List<ListItem> items = ReadItems(source, lines, limit, ref cursor, first.Indent);
        block = new ListBlock(items, lines[index].Start, lines[cursor - 1].End, sectionPath);
        next = cursor;
        return true;
    }

    private static List<ListItem> ReadItems(
        string source,
        IReadOnlyList<SourceLine> lines,
        int limit,
        ref int cursor,
        int indent)
    {
        List<ListItem> items = [];
        while (cursor < limit)
        {
            if (lines[cursor].IsBlank(source))
            {
                if (!TryPeekMarker(source, lines, limit, cursor, out ListMarker next) || next.Indent != indent)
                {
                    break;
                }

                cursor++;
                continue;
            }

            if (Interrupts(source, lines[cursor]))
            {
                break;
            }

            if (!TryMarker(lines[cursor].Content(source), out ListMarker marker) || marker.Indent != indent)
            {
                break;
            }

            items.Add(ReadItem(source, lines, limit, ref cursor, marker));
        }

        return items;
    }

    private static ListItem ReadItem(
        string source,
        IReadOnlyList<SourceLine> lines,
        int limit,
        ref int cursor,
        ListMarker marker)
    {
        SourceLine line = lines[cursor];
        int textStart = line.Start + marker.TextStart;
        int textEnd = line.ContentEnd;
        string text = source[textStart..textEnd];
        cursor++;
        List<ListItem> nested = [];
        while (cursor < limit)
        {
            if (lines[cursor].IsBlank(source))
            {
                if (!TryPeekMarker(source, lines, limit, cursor, out ListMarker next) || next.Indent <= marker.Indent)
                {
                    break;
                }

                cursor++;
                continue;
            }

            if (Interrupts(source, lines[cursor]))
            {
                break;
            }

            if (!TryMarker(lines[cursor].Content(source), out ListMarker child) || child.Indent <= marker.Indent)
            {
                break;
            }

            nested.AddRange(ReadItems(source, lines, limit, ref cursor, child.Indent));
        }

        return new ListItem(
            marker.Marker,
            marker.Checked,
            text,
            nested,
            InlineReader.Read(source, textStart, textEnd));
    }

    private static bool TryPeekMarker(
        string source,
        IReadOnlyList<SourceLine> lines,
        int limit,
        int cursor,
        out ListMarker marker)
    {
        marker = default;
        int peek = cursor + 1;
        while (peek < limit && lines[peek].IsBlank(source))
        {
            peek++;
        }

        return peek < limit && TryMarker(lines[peek].Content(source), out marker);
    }

    private static bool Interrupts(string source, SourceLine line) =>
        HeadingScanner.TryAtx(source, line, out _, out _, out _)
        || FenceScanner.StartsFence(line.Content(source));

    public static bool StartsItem(ReadOnlySpan<char> content) => TryMarker(content, out _);

    private static bool TryMarker(ReadOnlySpan<char> content, out ListMarker marker)
    {
        marker = default;
        if (RuleScanner.IsRule(content))
        {
            return false;
        }

        int index = 0;
        while (index < content.Length && content[index] == ' ')
        {
            index++;
        }

        int indent = index;
        if (index >= content.Length)
        {
            return false;
        }

        int markerStart = index;
        if (content[index] is '-' or '*' or '+')
        {
            index++;
        }
        else if (!TryOrderedMarker(content, ref index))
        {
            return false;
        }

        if (index < content.Length && content[index] is not (' ' or '\t'))
        {
            return false;
        }

        string markerText = content[markerStart..index].ToString();
        if (index < content.Length)
        {
            index++;
        }

        bool? checkedState = ReadChecked(content, ref index);
        marker = new ListMarker(indent, markerText, checkedState, index);
        return true;
    }

    private static bool TryOrderedMarker(ReadOnlySpan<char> content, ref int index)
    {
        if (!char.IsAsciiDigit(content[index]))
        {
            return false;
        }

        int digits = 0;
        while (index < content.Length && char.IsAsciiDigit(content[index]) && digits < 9)
        {
            index++;
            digits++;
        }

        if (index >= content.Length || content[index] != '.')
        {
            return false;
        }

        index++;
        return true;
    }

    private static bool? ReadChecked(ReadOnlySpan<char> content, ref int index)
    {
        if (index + 2 >= content.Length || content[index] != '[' || content[index + 2] != ']')
        {
            return null;
        }

        char flag = content[index + 1];
        if (flag is not (' ' or 'x' or 'X'))
        {
            return null;
        }

        int after = index + 3;
        if (after < content.Length && content[after] is not (' ' or '\t'))
        {
            return null;
        }

        index = after;
        if (index < content.Length && content[index] is ' ' or '\t')
        {
            index++;
        }

        return flag is 'x' or 'X';
    }

    private readonly record struct ListMarker(int Indent, string Marker, bool? Checked, int TextStart);
}