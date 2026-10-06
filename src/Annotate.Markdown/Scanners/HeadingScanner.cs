namespace Annotate.Markdown;

internal static class HeadingScanner
{
    public static bool TryAtx(string source, SourceLine line, out int level, out string text, out int contentStart)
    {
        level = 0;
        text = "";
        contentStart = line.Start;
        ReadOnlySpan<char> content = line.Content(source);
        int index = CountLeadingSpaces(content);
        if (index < content.Length && content[index] == ' ')
        {
            return false;
        }

        if (index >= content.Length || content[index] != '#')
        {
            return false;
        }

        int hashes = 0;
        while (index < content.Length && content[index] == '#')
        {
            hashes++;
            index++;
        }

        if (hashes is < 1 or > 6)
        {
            return false;
        }

        if (index < content.Length && content[index] is not (' ' or '\t'))
        {
            return false;
        }

        if (index < content.Length)
        {
            index++;
        }

        contentStart = line.Start + index;
        text = content[index..].Trim().ToString();
        level = hashes;
        return true;
    }

    public static bool TrySetextLevel(string source, SourceLine line, out int level)
    {
        level = 0;
        ReadOnlySpan<char> content = line.Content(source);
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

        if (index >= content.Length)
        {
            return false;
        }

        char mark = content[index];
        if (mark is not ('=' or '-'))
        {
            return false;
        }

        do
        {
            index++;
        }
        while (index < content.Length && content[index] == mark);

        while (index < content.Length && content[index] == ' ')
        {
            index++;
        }

        if (index != content.Length)
        {
            return false;
        }

        level = mark == '=' ? 1 : 2;
        return true;
    }

    private static int CountLeadingSpaces(ReadOnlySpan<char> content)
    {
        int index = 0;
        while (index < content.Length && index < 3 && content[index] == ' ')
        {
            index++;
        }

        return index;
    }
}