using System.Text;

using Annotate.Markdown;
using Annotate.Web.Components.Review;

namespace Annotate.Web;

internal static class PlanReportBlocks
{
    public static void Append(StringBuilder html, string source, IReadOnlyList<Block> blocks)
    {
        foreach (Block block in blocks)
        {
            Append(html, source, block);
        }
    }

    private static void Append(StringBuilder html, string source, Block block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                int level = Math.Clamp(heading.Level, 1, 6);
                html.Append("<h").Append(level).Append('>');
                AppendInlines(html, source, heading.Inlines);
                html.Append("</h").Append(level).Append('>');
                break;
            case ParagraphBlock paragraph:
                html.Append("<p>");
                AppendInlines(html, source, paragraph.Inlines);
                html.Append("</p>");
                break;
            case ListBlock list:
                AppendList(html, source, list.Items);
                break;
            case CodeBlock code:
                string language = CodeFenceContent.Language(code);
                html.Append("<div class=\"code-block\" data-code-fence>");
                if (language.Length > 0)
                {
                    html.Append("<div class=\"code-language\">");
                    PlanReport.AppendText(html, language);
                    html.Append("</div>");
                }
                html.Append("<pre><code class=\"language-");
                PlanReport.AppendText(html, language);
                html.Append("\">");
                foreach (CodeSourceLine line in CodeFenceContent.Lines(source, code))
                {
                    PlanReport.AppendText(html, source[line.Start..line.End]);
                    if (line.HasNewline) html.Append('\n');
                }
                html.Append("</code></pre></div>");
                break;
            case QuoteBlock quote:
                html.Append("<blockquote>");
                Append(html, source, quote.Children);
                html.Append("</blockquote>");
                break;
            case TableBlock table:
                AppendTable(html, source, table);
                break;
            case RuleBlock:
                html.Append("<hr>");
                break;
            case DetailsBlock details:
                html.Append("<details><summary>");
                AppendInlines(html, source, details.Summary);
                html.Append("</summary>");
                Append(html, source, details.Children);
                html.Append("</details>");
                break;
            default:
                throw new InvalidOperationException($"Block {block.GetType().Name} was not recognised.");
        }
    }

    private static void AppendList(StringBuilder html, string source, IReadOnlyList<ListItem> items)
    {
        bool ordered = ListMarkers.Ordered(items);
        html.Append(ordered ? "<ol>" : "<ul>");
        foreach (ListItem item in items)
        {
            html.Append("<li>");
            if (item.Checked is bool done)
            {
                html.Append(done
                    ? "<input type=\"checkbox\" disabled checked>"
                    : "<input type=\"checkbox\" disabled>");
            }

            AppendInlines(html, source, item.Inlines);
            if (item.Items.Count > 0)
            {
                AppendList(html, source, item.Items);
            }

            html.Append("</li>");
        }

        html.Append(ordered ? "</ol>" : "</ul>");
    }

    private static void AppendTable(StringBuilder html, string source, TableBlock table)
    {
        html.Append("<div class=\"table-wrap\"><table><thead><tr>");
        for (int index = 0; index < table.Header.Count; index++)
        {
            AppendCell(html, source, "th", table.Header[index], Alignment(table, index));
        }

        html.Append("</tr></thead><tbody>");
        foreach (IReadOnlyList<TableCell> row in table.Rows)
        {
            html.Append("<tr>");
            for (int index = 0; index < row.Count; index++)
            {
                AppendCell(html, source, "td", row[index], Alignment(table, index));
            }

            html.Append("</tr>");
        }

        html.Append("</tbody></table></div>");
    }

    private static void AppendCell(
        StringBuilder html,
        string source,
        string tag,
        TableCell cell,
        ColumnAlignment alignment)
    {
        html.Append('<').Append(tag);
        if (alignment != ColumnAlignment.None)
        {
            html.Append(" align=\"");
            html.Append(alignment switch
            {
                ColumnAlignment.Left => "left",
                ColumnAlignment.Center => "center",
                ColumnAlignment.Right => "right",
                _ => "left",
            });
            html.Append('"');
        }

        html.Append('>');
        AppendInlines(html, source, cell.Inlines);
        html.Append("</").Append(tag).Append('>');
    }

    private static ColumnAlignment Alignment(TableBlock table, int index) =>
        index < table.Alignments.Count ? table.Alignments[index] : ColumnAlignment.None;

    private static void AppendInlines(StringBuilder html, string source, IReadOnlyList<Inline> inlines)
    {
        foreach (Inline inline in inlines)
        {
            switch (inline)
            {
                case TextInline text:
                    PlanReport.AppendText(html, SourceSlice.Read(source, text.Start, text.End));
                    break;
                case CodeInline span:
                    html.Append("<code>");
                    PlanReport.AppendText(html, SourceSlice.Read(source, span.Start, span.End));
                    html.Append("</code>");
                    break;
                case EmphasisInline emphasis:
                    html.Append(emphasis.Level >= 2 ? "<strong>" : "<em>");
                    AppendInlines(html, source, emphasis.Children);
                    html.Append(emphasis.Level >= 2 ? "</strong>" : "</em>");
                    break;
                case StrikeInline strike:
                    html.Append("<del>");
                    AppendInlines(html, source, strike.Children);
                    html.Append("</del>");
                    break;
                case LinkInline link when link.Destination is not null:
                    html.Append("<a href=\"");
                    PlanReport.AppendText(html, link.Destination);
                    html.Append("\">");
                    AppendInlines(html, source, link.Children);
                    html.Append("</a>");
                    break;
                case LinkInline link:
                    AppendInlines(html, source, link.Children);
                    break;
                case ImageInline image when image.Destination is not null:
                    html.Append("<img src=\"");
                    PlanReport.AppendText(html, image.Destination);
                    html.Append("\" alt=\"");
                    PlanReport.AppendText(html, Alt(source, image));
                    html.Append("\">");
                    break;
                case ImageInline image:
                    AppendInlines(html, source, image.Children);
                    break;
                default:
                    throw new InvalidOperationException($"Inline {inline.GetType().Name} was not recognised.");
            }
        }
    }

    private static string Alt(string source, ImageInline image)
    {
        if (image.Children.Count == 0)
        {
            return "";
        }

        return SourceSlice.Read(source, image.Children[0].Start, image.Children[^1].End);
    }
}