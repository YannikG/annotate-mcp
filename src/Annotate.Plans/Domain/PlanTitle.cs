using Annotate.Markdown;

namespace Annotate.Plans.Domain;

internal static class PlanTitle
{
    public const int MaxLength = 300;

    public static string Choose(string? summary, PlanDocument document)
    {
        if (!string.IsNullOrWhiteSpace(summary))
        {
            return summary.Trim();
        }

        string? heading = FirstHeading(document.Blocks);
        if (!string.IsNullOrWhiteSpace(heading))
        {
            string trimmed = heading.Trim();
            return trimmed.Length <= MaxLength ? trimmed : trimmed[..MaxLength];
        }

        return "Untitled";
    }

    private static string? FirstHeading(IReadOnlyList<Block> blocks)
    {
        foreach (Block block in blocks)
        {
            if (block is HeadingBlock heading)
            {
                return heading.Text;
            }

            string? nested = FirstHeading(Children(block));
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static IReadOnlyList<Block> Children(Block block) =>
        block switch
        {
            QuoteBlock quote => quote.Children,
            DetailsBlock details => details.Children,
            _ => [],
        };
}