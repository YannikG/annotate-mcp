using AngleSharp.Dom;
using AngleSharp.Html.Parser;

using Annotate.Markdown;
using Annotate.Web;

namespace Annotate.Web.Tests;

public sealed class PlanReportTests
{
    [Fact]
    public void ReportEmbedsStandaloneStylesAndResponsiveLayout()
    {
        string html = PlanReport.Html("Storage plan", "https://example.com/story", "First criterion\nSecond criterion",
            "# Storage\n\nKeep **one file**.\n\n```csharp\nint count = 1;\n```\n\n| Store | Status |\n| --- | --- |\n| SQLite | Local |\n");
        IDocument document = new HtmlParser().ParseDocument(html);

        IElement stylesheet = Assert.Single(document.QuerySelectorAll("head style"));
        Assert.Contains("font-family:", stylesheet.TextContent, StringComparison.Ordinal);
        Assert.Contains("@media print", stylesheet.TextContent, StringComparison.Ordinal);
        Assert.Contains("pre", stylesheet.TextContent, StringComparison.Ordinal);
        Assert.Contains("table", stylesheet.TextContent, StringComparison.Ordinal);
        Assert.Equal("width=device-width, initial-scale=1", document.QuerySelector("meta[name='viewport']")!.GetAttribute("content"));
        Assert.Empty(document.QuerySelectorAll("link[rel='stylesheet'], script"));
        Assert.DoesNotContain("@import", stylesheet.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("url(", stylesheet.TextContent, StringComparison.Ordinal);
        Assert.Equal("Storage plan", document.QuerySelector("main > header h1")!.TextContent);
        Assert.Equal("First criterion\nSecond criterion", document.QuerySelector(".report-criteria")!.TextContent);
        Assert.Equal("Storage", document.QuerySelector("article h1")!.TextContent);
        Assert.NotNull(document.QuerySelector("article pre code"));
        Assert.NotNull(document.QuerySelector("article .table-wrap table"));
    }

    [Fact]
    public void ReportUsesSummaryStoryCriteriaAndEscapedTokens()
    {
        string markdown =
            """
            # Storage

            Keep the file local. See [docs](https://example.com/docs) and [pending]() and ![mark](https://example.com/a.png) and ![plain]().

            <script>alert(1)</script>

            ```mermaid
            graph TD
              A-->B
            ```

            ```diff
            - old
            + new
            ```
            """;
        ParseOutcome.Ok parsed = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(markdown));
        ParagraphBlock paragraph = Assert.Single(
            parsed.Document.Blocks.OfType<ParagraphBlock>(),
            block => block.Inlines.OfType<LinkInline>().Any());
        LinkInline docs = Assert.Single(paragraph.Inlines.OfType<LinkInline>(), link => link.Destination is not null);
        LinkInline pending = Assert.Single(paragraph.Inlines.OfType<LinkInline>(), link => link.Destination is null);
        ImageInline mark = Assert.Single(paragraph.Inlines.OfType<ImageInline>(), image => image.Destination is not null);
        ImageInline plain = Assert.Single(paragraph.Inlines.OfType<ImageInline>(), image => image.Destination is null);
        CodeBlock mermaid = Assert.Single(parsed.Document.Blocks.OfType<CodeBlock>(), block => block.Language == "mermaid");
        CodeBlock diff = Assert.Single(parsed.Document.Blocks.OfType<CodeBlock>(), block => block.Language == "diff");

        string html = PlanReport.Html(
            "  Keep one file  ",
            "https://example.com/story",
            "<script>alert(2)</script>",
            markdown);
        IDocument document = new HtmlParser().ParseDocument(html);

        Assert.Equal("Keep one file", document.Title);
        Assert.Equal("Keep one file", document.QuerySelector("h1")!.TextContent);
        IElement story = document.QuerySelector("a[href='https://example.com/story']")!;
        Assert.Equal("https://example.com/story", story.TextContent);
        IElement main = document.QuerySelector("main")!;
        Assert.Contains("<script>alert(2)</script>", main.TextContent, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;alert(2)&lt;/script&gt;", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", document.QuerySelector("article")!.InnerHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-diagram-viewer", html, StringComparison.Ordinal);
        Assert.DoesNotContain("annotateDiagrams", html, StringComparison.Ordinal);
        Assert.Single(document.QuerySelectorAll("script"));

        Assert.Equal("docs", document.QuerySelector($"a[href='{docs.Destination}']")!.TextContent);
        Assert.Null(pending.Destination);
        Assert.All(
            document.QuerySelectorAll("a"),
            anchor => Assert.DoesNotContain("pending", anchor.TextContent, StringComparison.Ordinal));
        Assert.Contains("pending", main.TextContent, StringComparison.Ordinal);

        IElement image = Assert.Single(document.QuerySelectorAll("img"));
        Assert.Equal(mark.Destination, image.GetAttribute("src"));
        Assert.Null(plain.Destination);
        Assert.Contains("plain", main.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("plain", image.GetAttribute("alt") ?? "", StringComparison.Ordinal);
        Assert.Contains("alert(1)", main.TextContent, StringComparison.Ordinal);

        IElement diagram = document.QuerySelector("[data-mermaid]")!;
        Assert.Equal(mermaid.Body, diagram.QuerySelector("[data-mermaid-body]")!.TextContent);
        Assert.Empty(diagram.QuerySelectorAll("[data-code-fence]"));
        IHtmlCollection<IElement> fences = document.QuerySelectorAll("[data-code-fence] pre code");
        Assert.Contains(fences, node => node.TextContent == diff.Body && node.ClassList.Contains("language-diff"));
        Assert.Contains("graph TD", main.TextContent, StringComparison.Ordinal);
        Assert.Contains("+ new", main.TextContent, StringComparison.Ordinal);
        Assert.Empty(document.QuerySelectorAll("svg"));

        string text = main.TextContent;
        int storage = text.IndexOf("Storage", StringComparison.Ordinal);
        int keep = text.IndexOf("Keep the file local.", StringComparison.Ordinal);
        int graph = text.IndexOf("graph TD", StringComparison.Ordinal);
        int added = text.IndexOf("+ new", StringComparison.Ordinal);
        Assert.True(storage < keep && keep < graph && graph < added);
    }

    [Fact]
    public void EmptyTitleAndInvalidPlanStayText()
    {
        string markdown =
            """
            <script>alert(1)</script>
            ```decision
            id: storage
            ```
            """;
        ParseOutcome.Invalid invalid = Assert.IsType<ParseOutcome.Invalid>(PlanMarkdown.Parse(markdown));

        string html = PlanReport.Html("  A <b> plan  ", null, null, markdown);
        IDocument document = new HtmlParser().ParseDocument(html);

        Assert.Equal("A <b> plan", document.Title);
        Assert.Equal("A <b> plan", document.QuerySelector("h1")!.TextContent);
        Assert.Empty(document.QuerySelectorAll("b"));
        Assert.Empty(document.QuerySelectorAll("a"));
        Assert.Empty(document.QuerySelectorAll("script"));
        Assert.Contains(invalid.Error, document.QuerySelector("pre")!.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(invalid.Error, html, StringComparison.Ordinal);
        Assert.DoesNotContain("alert(1)", document.Body!.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);

        string blank = PlanReport.Html("   ", null, null, "Hello");
        IDocument blankDocument = new HtmlParser().ParseDocument(blank);
        Assert.Equal("Plan", blankDocument.Title);
        Assert.Equal("Plan", blankDocument.QuerySelector("h1")!.TextContent);
        Assert.Contains("Hello", blankDocument.Body!.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportDrawsMermaidAndLeavesOtherFencesAsSource()
    {
        string diagram = PlanReport.Html(
            "Flow",
            null,
            null,
            """
            ```mermaid
            flowchart LR
              A-->B
            ```
            """);
        IDocument document = new HtmlParser().ParseDocument(diagram);
        IElement block = Assert.Single(document.QuerySelectorAll("[data-mermaid]"));
        Assert.Empty(block.QuerySelectorAll("[data-code-fence]"));
        IElement enlarge = block.QuerySelector("[data-mermaid-enlarge]")!;
        Assert.Equal("Enlarge", enlarge.TextContent.Trim());
        Assert.Equal("button", enlarge.TagName, ignoreCase: true);
        Assert.Contains("btn-quiet", enlarge.ClassName, StringComparison.Ordinal);
        Assert.True(enlarge.HasAttribute("hidden"));
        Assert.Null(enlarge.Closest("[data-block-index]"));
        Assert.NotNull(block.QuerySelector("[data-mermaid-canvas]"));
        Assert.NotNull(block.QuerySelector("[data-mermaid-error][hidden]"));
        Assert.Contains("A-->B", block.QuerySelector("[data-mermaid-body]")!.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("```", block.QuerySelector("[data-mermaid-body]")!.TextContent, StringComparison.Ordinal);
        Assert.Equal("0", block.QuerySelector("[data-block-index]")!.GetAttribute("data-block-index"));
        Assert.Equal("Mermaid source", block.QuerySelector("summary")!.TextContent);
        Assert.Contains("data-diagram-viewer", diagram, StringComparison.Ordinal);
        Assert.DoesNotContain("annotateDiagrams", diagram, StringComparison.Ordinal);
        Assert.Single(document.QuerySelectorAll("script"));
        Assert.True(diagram.Length < 150_000, "The report must store a diagram slot, not the Mermaid bundle.");
        Assert.Empty(document.QuerySelectorAll("svg"));
        Assert.Contains(".dialog-diagram", document.QuerySelector("style")!.TextContent, StringComparison.Ordinal);

        string plain = PlanReport.Html(
            "Note",
            null,
            null,
            """
            ```text
            hello
            ```
            """);
        IDocument plainDocument = new HtmlParser().ParseDocument(plain);
        Assert.Contains("hello", plainDocument.QuerySelector("[data-code-fence] code")!.TextContent, StringComparison.Ordinal);
        Assert.Empty(plainDocument.QuerySelectorAll("[data-mermaid], script"));
        Assert.DoesNotContain("annotateDiagrams", plain, StringComparison.Ordinal);
    }
}