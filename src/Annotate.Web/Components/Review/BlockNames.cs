using Annotate.Plans.Application;

namespace Annotate.Web.Components.Review;

public static class BlockNames
{
    public static IReadOnlyDictionary<string, string> Of(RevisionDetail revision)
    {
        Dictionary<string, string> names = new(StringComparer.Ordinal);
        foreach (RevisionBlock block in revision.Blocks)
        {
            int start = Math.Clamp(block.Start, 0, revision.Markdown.Length);
            int end = Math.Clamp(block.End, start, revision.Markdown.Length);
            string line = revision.Markdown[start..end].Split('\n', 2)[0].Trim();
            string name = line.TrimStart('#').Trim();
            if (name.Length > 0)
            {
                names[block.Key] = name;
            }
        }

        return names;
    }
}