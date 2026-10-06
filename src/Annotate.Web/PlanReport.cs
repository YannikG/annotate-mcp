using System.Text;
using System.Text.Encodings.Web;

using Annotate.Markdown;

namespace Annotate.Web;

public static class PlanReport
{
    public static string Html(string? summary, string? storyUrl, string? acceptanceCriteria, string markdown)
    {
        string title = Title(summary);
        StringBuilder html = new();
        html.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>");
        AppendText(html, title);
        html.Append("</title><style>").Append(PlanReportStyles.Css);
        html.Append("</style></head><body><main><header class=\"report-header\"><p class=\"report-label\">Plan report</p><h1>");
        AppendText(html, title);
        html.Append("</h1>");
        if (!string.IsNullOrEmpty(storyUrl))
        {
            html.Append("<p class=\"report-story\"><span>Story</span> <a href=\"");
            AppendText(html, storyUrl);
            html.Append("\">");
            AppendText(html, storyUrl);
            html.Append("</a></p>");
        }

        html.Append("</header>");
        if (!string.IsNullOrEmpty(acceptanceCriteria))
        {
            html.Append("<section class=\"report-context\" aria-label=\"Acceptance criteria\"><h2>Acceptance criteria</h2><p class=\"report-criteria\">");
            AppendText(html, acceptanceCriteria);
            html.Append("</p></section>");
        }

        html.Append("<article class=\"report-plan\" aria-label=\"Plan\">");
        ParseOutcome parsed = PlanMarkdown.Parse(markdown);
        if (parsed is ParseOutcome.Invalid invalid)
        {
            html.Append("<pre>");
            AppendText(html, invalid.Error);
            html.Append("</pre>");
        }
        else if (parsed is ParseOutcome.Ok ok)
        {
            PlanReportBlocks.Append(html, markdown, ok.Document.Blocks);
        }

        html.Append("</article></main></body></html>");
        return html.ToString();
    }

    internal static void AppendText(StringBuilder html, string value) =>
        html.Append(HtmlEncoder.Default.Encode(value));

    private static string Title(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return "Plan";
        }

        return summary.Trim();
    }
}