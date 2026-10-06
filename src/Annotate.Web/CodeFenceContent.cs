using Annotate.Markdown;

namespace Annotate.Web;

internal static class CodeFenceContent
{
    public static string Language(CodeBlock fence) =>
        fence.Language.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToLowerInvariant() ?? "";

    public static IEnumerable<CodeSourceLine> Lines(string source, CodeBlock fence)
    {
        int indent = 0;
        while (indent < 3 && fence.Start + indent < source.Length && source[fence.Start + indent] == ' ') indent++;
        int cursor = source.IndexOfAny(['\r', '\n'], fence.Start);
        if (cursor < 0) yield break;
        if (source[cursor++] == '\r' && cursor < source.Length && source[cursor] == '\n') cursor++;
        int bodyEnd = cursor + fence.Body.Length;
        while (cursor < bodyEnd)
        {
            int end = source.IndexOfAny(['\r', '\n'], cursor);
            if (end < 0 || end > bodyEnd) end = bodyEnd;
            int start = cursor;
            while (start < end && start - cursor < indent && source[start] == ' ') start++;
            yield return new(start, end, end < bodyEnd);
            cursor = end;
            if (cursor < bodyEnd && source[cursor++] == '\r' && cursor < bodyEnd && source[cursor] == '\n') cursor++;
        }
    }
}

internal sealed record CodeSourceLine(int Start, int End, bool HasNewline);