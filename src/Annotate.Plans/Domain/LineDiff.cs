namespace Annotate.Plans.Domain;

internal static class LineDiff
{
    public static IReadOnlyList<DiffLine> Compare(string older, string newer)
    {
        string[] left = Split(older);
        string[] right = Split(newer);
        List<DiffLine> rows = [];
        foreach ((Kind kind, string text) in Script(left, right))
        {
            rows.Add(kind switch
            {
                Kind.Equal => new DiffLine.Equal(text),
                Kind.Added => new DiffLine.Added(text),
                _ => new DiffLine.Removed(text),
            });
        }

        return rows;
    }

    private static string[] Split(string markdown)
    {
        if (markdown.Length == 0)
        {
            return [];
        }

        List<string> lines = [];
        int start = 0;
        for (int index = 0; index < markdown.Length; index++)
        {
            if (markdown[index] != '\n')
            {
                continue;
            }

            lines.Add(Take(markdown, start, index));
            start = index + 1;
        }

        if (start < markdown.Length)
        {
            lines.Add(Take(markdown, start, markdown.Length));
        }

        return lines.ToArray();
    }

    private static string Take(string markdown, int start, int end)
    {
        if (end > start && markdown[end - 1] == '\r')
        {
            end--;
        }

        return markdown[start..end];
    }

    private static List<(Kind Kind, string Text)> Script(string[] older, string[] newer)
    {
        int leftCount = older.Length;
        int rightCount = newer.Length;
        int max = leftCount + rightCount;
        int[] furthest = new int[(2 * max) + 1];
        int offset = max;
        List<int[]> trace = [];

        for (int depth = 0; depth <= max; depth++)
        {
            trace.Add((int[])furthest.Clone());
            for (int diagonal = -depth; diagonal <= depth; diagonal += 2)
            {
                int x = NextX(furthest, offset, depth, diagonal);
                int y = x - diagonal;
                while (x < leftCount && y < rightCount && older[x] == newer[y])
                {
                    x++;
                    y++;
                }

                furthest[offset + diagonal] = x;
                if (x >= leftCount && y >= rightCount)
                {
                    return Walk(trace, older, newer, depth, offset);
                }
            }
        }

        throw new InvalidOperationException("Line diff did not finish.");
    }

    private static int NextX(int[] furthest, int offset, int depth, int diagonal)
    {
        if (diagonal == -depth || (diagonal != depth && furthest[offset + diagonal - 1] < furthest[offset + diagonal + 1]))
        {
            return furthest[offset + diagonal + 1];
        }

        return furthest[offset + diagonal - 1] + 1;
    }

    private static List<(Kind Kind, string Text)> Walk(
        List<int[]> trace,
        string[] older,
        string[] newer,
        int depth,
        int offset)
    {
        List<(Kind Kind, string Text)> rows = [];
        int x = older.Length;
        int y = newer.Length;
        for (int step = depth; step >= 0; step--)
        {
            int[] furthest = trace[step];
            int diagonal = x - y;
            int previous = diagonal == -step
                || (diagonal != step && furthest[offset + diagonal - 1] < furthest[offset + diagonal + 1])
                    ? diagonal + 1
                    : diagonal - 1;
            int previousX = furthest[offset + previous];
            int previousY = previousX - previous;
            while (x > previousX && y > previousY)
            {
                rows.Add((Kind.Equal, older[x - 1]));
                x--;
                y--;
            }

            if (step > 0)
            {
                rows.Add(x == previousX
                    ? (Kind.Added, newer[previousY])
                    : (Kind.Removed, older[previousX]));
            }

            x = previousX;
            y = previousY;
        }

        rows.Reverse();
        return rows;
    }

    private enum Kind
    {
        Equal,
        Added,
        Removed,
    }
}