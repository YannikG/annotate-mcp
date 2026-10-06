namespace Annotate.Markdown;

internal static class InlineReader
{
    public static IReadOnlyList<Inline> Read(string source, int start, int end)
    {
        if (start >= end)
        {
            return [];
        }

        List<Piece> pieces = Scan(source, start, end);
        CloseMarkers(pieces);
        return Materialize(pieces);
    }

    private static List<Piece> Scan(string source, int start, int end)
    {
        List<Piece> pieces = [];
        int index = start;
        int textStart = start;
        while (index < end)
        {
            char current = source[index];
            if (current == '`')
            {
                if (TryCodeSpan(source, index, end, out int codeEnd, out string literal))
                {
                    AddText(pieces, textStart, index);
                    pieces.Add(new Piece(PieceKind.Code, index, codeEnd) { Literal = literal });
                    index = codeEnd;
                    textStart = index;
                }
                else
                {
                    while (index < end && source[index] == '`')
                    {
                        index++;
                    }
                }

                continue;
            }

            if (current == '!'
                && index + 1 < end
                && source[index + 1] == '['
                && TryLink(source, index, end, image: true, out int imageEnd, out Piece? image)
                && image is not null)
            {
                AddText(pieces, textStart, index);
                pieces.Add(image);
                index = imageEnd;
                textStart = index;
                continue;
            }

            if (current == '[' && TryLink(source, index, end, image: false, out int linkEnd, out Piece? link) && link is not null)
            {
                AddText(pieces, textStart, index);
                pieces.Add(link);
                index = linkEnd;
                textStart = index;
                continue;
            }

            if (current is '*' or '_' or '~')
            {
                int run = index + 1;
                while (run < end && source[run] == current)
                {
                    run++;
                }

                int length = run - index;
                bool marker = current == '~' ? length == 2 : length is 1 or 2;
                if (marker)
                {
                    AddText(pieces, textStart, index);
                    pieces.Add(Delimiter(source, index, run, current, length, start, end));
                    textStart = run;
                }

                index = run;
                continue;
            }

            index++;
        }

        AddText(pieces, textStart, end);
        return pieces;
    }

    private static void CloseMarkers(List<Piece> pieces)
    {
        List<int> openers = [];
        for (int index = 0; index < pieces.Count; index++)
        {
            Piece piece = pieces[index];
            if (piece.Kind != PieceKind.Delimiter)
            {
                continue;
            }

            int match = piece.CanClose ? FindOpener(pieces, openers, piece) : -1;
            if (match >= 0)
            {
                Piece opener = pieces[match];
                opener.End = piece.End;
                opener.Children = Materialize(pieces.GetRange(match + 1, index - match - 1));
                if (opener.Marker == '~')
                {
                    opener.Kind = PieceKind.Strike;
                }
                else
                {
                    opener.Kind = PieceKind.Emphasis;
                    opener.Level = piece.MarkerLength;
                }
                for (int inner = match + 1; inner <= index; inner++)
                {
                    pieces[inner].Kind = PieceKind.Removed;
                }
            }
            else if (piece.CanOpen)
            {
                openers.Add(index);
            }
        }
    }

    private static int FindOpener(List<Piece> pieces, List<int> openers, Piece closer)
    {
        for (int slot = openers.Count - 1; slot >= 0; slot--)
        {
            Piece opener = pieces[openers[slot]];
            if (opener.Marker != closer.Marker || opener.MarkerLength != closer.MarkerLength || !opener.CanOpen)
            {
                continue;
            }

            int match = openers[slot];
            openers.RemoveRange(slot, openers.Count - slot);
            return match;
        }

        return -1;
    }

    private static List<Inline> Materialize(List<Piece> pieces)
    {
        List<Inline> result = [];
        int textStart = -1;
        int textEnd = -1;
        foreach (Piece piece in pieces)
        {
            if (piece.Kind is PieceKind.Text or PieceKind.Delimiter)
            {
                if (piece.Start >= piece.End)
                {
                    continue;
                }

                if (textStart >= 0 && textEnd == piece.Start)
                {
                    textEnd = piece.End;
                }
                else
                {
                    AppendText(result, textStart, textEnd);
                    textStart = piece.Start;
                    textEnd = piece.End;
                }
            }
            else if (piece.Kind == PieceKind.Emphasis)
            {
                AppendText(result, textStart, textEnd);
                textStart = -1;
                result.Add(new EmphasisInline(piece.Level, piece.Children ?? [], piece.Start, piece.End));
            }
            else if (piece.Kind == PieceKind.Strike)
            {
                AppendText(result, textStart, textEnd);
                textStart = -1;
                result.Add(new StrikeInline(piece.Children ?? [], piece.Start, piece.End));
            }
            else if (piece.Kind == PieceKind.Code)
            {
                AppendText(result, textStart, textEnd);
                textStart = -1;
                result.Add(new CodeInline(piece.Literal ?? "", piece.Start, piece.End));
            }
            else if (piece.Kind == PieceKind.Link)
            {
                AppendText(result, textStart, textEnd);
                textStart = -1;
                result.Add(new LinkInline(piece.Children ?? [], piece.Destination, piece.Start, piece.End));
            }
            else if (piece.Kind == PieceKind.Image)
            {
                AppendText(result, textStart, textEnd);
                textStart = -1;
                result.Add(new ImageInline(piece.Children ?? [], piece.Destination, piece.Start, piece.End));
            }
        }

        AppendText(result, textStart, textEnd);
        return result;
    }

    private static void AppendText(List<Inline> result, int start, int end)
    {
        if (start >= 0 && start < end)
        {
            result.Add(new TextInline(start, end));
        }
    }

    private static bool TryLink(
        string source,
        int start,
        int limit,
        bool image,
        out int end,
        out Piece? piece)
    {
        piece = null;
        end = start;
        int bracket = image ? start + 1 : start;
        if (start >= limit || bracket >= limit || source[bracket] != '[')
        {
            return false;
        }

        int labelEnd = FindCloser(source, bracket + 1, limit, '[', ']');
        if (labelEnd < 0 || labelEnd + 1 >= limit || source[labelEnd + 1] != '(')
        {
            return false;
        }

        int destinationEnd = FindDestinationEnd(source, labelEnd + 2, limit);
        if (destinationEnd < 0)
        {
            return false;
        }

        end = destinationEnd + 1;
        piece = new Piece(image ? PieceKind.Image : PieceKind.Link, start, end)
        {
            Children = Read(source, bracket + 1, labelEnd),
            Destination = KeepDestination(source[(labelEnd + 2)..destinationEnd]),
        };
        return true;
    }

    private static int FindCloser(string source, int start, int limit, char open, char close)
    {
        int depth = 1;
        int index = start;
        while (index < limit)
        {
            if (source[index] == '`' && TryCodeSpan(source, index, limit, out int codeEnd, out _))
            {
                index = codeEnd;
                continue;
            }

            if (source[index] == open)
            {
                depth++;
            }
            else if (source[index] == close)
            {
                depth--;
                if (depth == 0)
                {
                    return index;
                }
            }

            index++;
        }

        return -1;
    }

    private static int FindDestinationEnd(string source, int start, int limit)
    {
        int depth = 0;
        int index = start;
        while (index < limit)
        {
            if (source[index] == '(')
            {
                depth++;
            }
            else if (source[index] == ')')
            {
                if (depth == 0)
                {
                    return index;
                }

                depth--;
            }

            index++;
        }

        return -1;
    }

    private static string? KeepDestination(string raw)
    {
        string value = raw.Trim();
        if (value.Length == 0)
        {
            return null;
        }

        if (value[0] == '/')
        {
            return value.Length > 1 && value[1] == '/' ? null : value;
        }

        int schemeEnd = 0;
        while (schemeEnd < value.Length && IsSchemeCharacter(value[schemeEnd]))
        {
            schemeEnd++;
        }

        if (schemeEnd == 0 || schemeEnd >= value.Length || value[schemeEnd] != ':')
        {
            return null;
        }

        ReadOnlySpan<char> scheme = value.AsSpan(0, schemeEnd);
        if (scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
            || scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        return null;
    }

    private static bool IsSchemeCharacter(char character) =>
        char.IsAsciiLetter(character) || char.IsAsciiDigit(character) || character is '+' or '.' or '-';

    private static bool TryCodeSpan(string source, int start, int limit, out int end, out string text)
    {
        end = start;
        text = "";
        int open = start;
        while (open < limit && source[open] == '`')
        {
            open++;
        }

        int width = open - start;
        int index = open;
        while (index < limit)
        {
            if (source[index] != '`')
            {
                index++;
                continue;
            }

            int close = index;
            while (close < limit && source[close] == '`')
            {
                close++;
            }

            if (close - index == width)
            {
                text = source[open..index];
                end = close;
                return true;
            }

            index = close;
        }

        return false;
    }

    private static void AddText(List<Piece> pieces, int start, int end)
    {
        if (start < end)
        {
            pieces.Add(new Piece(PieceKind.Text, start, end));
        }
    }

    private static Piece Delimiter(
        string source,
        int start,
        int end,
        char marker,
        int length,
        int rangeStart,
        int rangeEnd)
    {
        bool canOpen = end < rangeEnd && !char.IsWhiteSpace(source[end]);
        bool canClose = start > rangeStart && !char.IsWhiteSpace(source[start - 1]);
        return new Piece(PieceKind.Delimiter, start, end)
        {
            Marker = marker,
            MarkerLength = length,
            CanOpen = canOpen,
            CanClose = canClose,
        };
    }

    private enum PieceKind
    {
        Text,
        Delimiter,
        Emphasis,
        Strike,
        Code,
        Link,
        Image,
        Removed,
    }

    private sealed class Piece
    {
        public Piece(PieceKind kind, int start, int end)
        {
            Kind = kind;
            Start = start;
            End = end;
        }

        public PieceKind Kind { get; set; }

        public int Start { get; }

        public int End { get; set; }

        public char Marker { get; init; }

        public int MarkerLength { get; init; }

        public bool CanOpen { get; init; }

        public bool CanClose { get; init; }

        public int Level { get; set; }

        public string? Literal { get; init; }

        public string? Destination { get; init; }

        public IReadOnlyList<Inline>? Children { get; set; }
    }
}