using Annotate.Markdown;

namespace Annotate.Markdown.Tests;

public sealed class ContainerMarkdownTests
{
    [Fact]
    public void BlockquoteChildrenAreBlocksAndTheSpanCoversTheQuoteLines()
    {
        string source = "> outer\n> > inner\n> - item\n\nnext\n";
        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));
        QuoteBlock quote = Assert.IsType<QuoteBlock>(ok.Document.Blocks[0]);

        Assert.Equal(BlockKind.Quote, quote.Kind);
        Assert.Equal(0, quote.Start);
        Assert.Equal("> outer\n> > inner\n> - item\n".Length, quote.End);
        Assert.Equal("> outer\n> > inner\n> - item\n", source[quote.Start..quote.End]);
        Assert.Equal(string.Empty, quote.SectionPath);
        Assert.Equal(3, quote.Children.Count);

        ParagraphBlock outer = Assert.IsType<ParagraphBlock>(quote.Children[0]);
        Assert.Equal("outer", source[Assert.Single(outer.Inlines).Start..Assert.Single(outer.Inlines).End]);

        QuoteBlock nested = Assert.IsType<QuoteBlock>(quote.Children[1]);
        int nestedStart = source.IndexOf("> inner", StringComparison.Ordinal);
        Assert.Equal(nestedStart, nested.Start);
        Assert.Equal(source.IndexOf('\n', nestedStart) + 1, nested.End);
        Assert.Equal("> inner\n", source[nested.Start..nested.End]);
        ParagraphBlock inner = Assert.IsType<ParagraphBlock>(Assert.Single(nested.Children));
        Assert.Equal("inner", source[Assert.Single(inner.Inlines).Start..Assert.Single(inner.Inlines).End]);

        ListBlock list = Assert.IsType<ListBlock>(quote.Children[2]);
        Assert.Equal("item", Assert.Single(list.Items).Text);

        ParagraphBlock next = Assert.IsType<ParagraphBlock>(ok.Document.Blocks[1]);
        Assert.Equal("next\n", source[next.Start..next.End]);
    }

    [Fact]
    public void PipeTableRecordsCellsAlignmentsAndStopsBeforeTheBlankLine()
    {
        string source = "| *a* | b | c | d |\n| :--- | :---: | ---: | --- |\n| 1 | 2 | 3 | 4 |\n\nnext\n";
        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));
        TableBlock table = Assert.IsType<TableBlock>(ok.Document.Blocks[0]);

        Assert.Equal(BlockKind.Table, table.Kind);
        Assert.Equal(0, table.Start);
        Assert.Equal("| *a* | b | c | d |\n| :--- | :---: | ---: | --- |\n| 1 | 2 | 3 | 4 |\n".Length, table.End);
        Assert.Equal("| *a* | b | c | d |\n| :--- | :---: | ---: | --- |\n| 1 | 2 | 3 | 4 |\n", source[table.Start..table.End]);
        Assert.Equal(
            [ColumnAlignment.Left, ColumnAlignment.Center, ColumnAlignment.Right, ColumnAlignment.None],
            table.Alignments);

        EmphasisInline emphasis = Assert.IsType<EmphasisInline>(Assert.Single(table.Header[0].Inlines));
        Assert.Equal("a", source[Assert.Single(emphasis.Children).Start..Assert.Single(emphasis.Children).End]);
        Assert.Equal("b", source[Assert.Single(table.Header[1].Inlines).Start..Assert.Single(table.Header[1].Inlines).End]);
        Assert.Equal("c", source[Assert.Single(table.Header[2].Inlines).Start..Assert.Single(table.Header[2].Inlines).End]);
        Assert.Equal("d", source[Assert.Single(table.Header[3].Inlines).Start..Assert.Single(table.Header[3].Inlines).End]);

        IReadOnlyList<TableCell> row = Assert.Single(table.Rows);
        Assert.Equal("1", source[Assert.Single(row[0].Inlines).Start..Assert.Single(row[0].Inlines).End]);
        Assert.Equal("2", source[Assert.Single(row[1].Inlines).Start..Assert.Single(row[1].Inlines).End]);
        Assert.Equal("3", source[Assert.Single(row[2].Inlines).Start..Assert.Single(row[2].Inlines).End]);
        Assert.Equal("4", source[Assert.Single(row[3].Inlines).Start..Assert.Single(row[3].Inlines).End]);

        ParagraphBlock next = Assert.IsType<ParagraphBlock>(ok.Document.Blocks[1]);
        Assert.Equal("next\n", source[next.Start..next.End]);
    }

    [Fact]
    public void ThematicBreakIsARuleUnlessItIsASetextUnderline()
    {
        string source = "---\n\n* * *\n\n___\n\nTitle\n---\n\nhello\n***\n\n  ---  \n";
        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));
        Block[] blocks = [.. ok.Document.Blocks];

        Assert.Equal(7, blocks.Length);
        Assert.Equal("---\n", source[Assert.IsType<RuleBlock>(blocks[0]).Start..blocks[0].End]);
        Assert.Equal(BlockKind.Rule, blocks[0].Kind);
        Assert.Equal("* * *\n", source[Assert.IsType<RuleBlock>(blocks[1]).Start..blocks[1].End]);
        Assert.Equal("___\n", source[Assert.IsType<RuleBlock>(blocks[2]).Start..blocks[2].End]);

        HeadingBlock heading = Assert.IsType<HeadingBlock>(blocks[3]);
        Assert.Equal(2, heading.Level);
        Assert.Equal("Title", heading.Text);
        Assert.Equal("Title\n---\n", source[heading.Start..heading.End]);

        Assert.Equal("hello\n", source[Assert.IsType<ParagraphBlock>(blocks[4]).Start..blocks[4].End]);
        Assert.Equal("***\n", source[Assert.IsType<RuleBlock>(blocks[5]).Start..blocks[5].End]);
        Assert.Equal("  ---  \n", source[Assert.IsType<RuleBlock>(blocks[6]).Start..blocks[6].End]);
    }

    [Fact]
    public void DetailsKeepsSummaryInlinesAndBlockChildren()
    {
        string source = "<details>\n<summary>See *this*</summary>\n- item\n\npara\n</details>\n\n<script>alert(1)</script>\n\n<div>nope</div>\n";
        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));
        DetailsBlock details = Assert.IsType<DetailsBlock>(ok.Document.Blocks[0]);

        Assert.Equal(BlockKind.Details, details.Kind);
        string detailsText = "<details>\n<summary>See *this*</summary>\n- item\n\npara\n</details>\n";
        Assert.Equal(detailsText, source[details.Start..details.End]);
        Assert.Equal("See ", source[details.Summary[0].Start..details.Summary[0].End]);
        EmphasisInline emphasis = Assert.IsType<EmphasisInline>(details.Summary[1]);
        Assert.Equal("this", source[Assert.Single(emphasis.Children).Start..Assert.Single(emphasis.Children).End]);

        ListBlock list = Assert.IsType<ListBlock>(details.Children[0]);
        Assert.Equal("item", Assert.Single(list.Items).Text);
        ParagraphBlock paragraph = Assert.IsType<ParagraphBlock>(details.Children[1]);
        Assert.Equal("para\n", source[paragraph.Start..paragraph.End]);

        Assert.Equal("<script>alert(1)</script>\n", source[Assert.IsType<ParagraphBlock>(ok.Document.Blocks[1]).Start..ok.Document.Blocks[1].End]);
        Assert.Equal("<div>nope</div>\n", source[Assert.IsType<ParagraphBlock>(ok.Document.Blocks[2]).Start..ok.Document.Blocks[2].End]);
    }

    [Theory]
    [InlineData("diff")]
    [InlineData("patch")]
    [InlineData("mermaid")]
    public void DiagramAndDiffFencesKeepTheLanguageAndALiteralBody(string language)
    {
        string source = "```" + language + "\n*em* ~~x~~\n```\n";
        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));
        CodeBlock code = Assert.IsType<CodeBlock>(Assert.Single(ok.Document.Blocks));

        Assert.Equal(language, code.Language);
        Assert.Equal("*em* ~~x~~\n", code.Body);
        Assert.Equal(source, source[code.Start..code.End]);
        Assert.DoesNotContain(typeof(CodeBlock).GetProperties(), property => property.Name == "Inlines");
    }

    [Fact]
    public void LongerFenceInsideDetailsKeepsTheShorterFenceInTheBody()
    {
        string source = "<details>\n<summary>Note</summary>\n````\n```\nkept\n````\n</details>\n";
        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));
        DetailsBlock details = Assert.IsType<DetailsBlock>(Assert.Single(ok.Document.Blocks));
        CodeBlock code = Assert.IsType<CodeBlock>(Assert.Single(details.Children));

        Assert.Equal(string.Empty, code.Language);
        Assert.Equal("```\nkept\n", code.Body);
        Assert.Equal("````\n```\nkept\n````\n", source[code.Start..code.End]);
        Assert.Equal(source, source[details.Start..details.End]);
    }

    [Fact]
    public void EveryBlockInAMixedDocumentMatchesItsSourceSlice()
    {
        string quote = "> quoted\n> > nested\n";
        string table = "| a | b |\n| --- | ---: |\n| 1 | 2 |\n";
        string rule = "---\n";
        string details = "<details>\n<summary>Sum</summary>\n- item\n</details>\n";
        string diff = "```diff\n+ added\n```\n";
        string decision = "```decision\nid: storage\nkind: text\nprompt: Why?\n```\n";
        string source = "# Tasks\n\n" + quote + "\n" + table + "\n" + rule + "\n" + details + "\n" + diff + "\n" + decision + "\nafter\n";
        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));
        Block[] blocks = [.. ok.Document.Blocks];

        Assert.Equal(8, blocks.Length);
        Assert.Equal("# Tasks\n", source[blocks[0].Start..blocks[0].End]);
        Assert.Equal(quote, source[blocks[1].Start..blocks[1].End]);
        Assert.Equal(table, source[blocks[2].Start..blocks[2].End]);
        Assert.Equal(rule, source[blocks[3].Start..blocks[3].End]);
        Assert.Equal(details, source[blocks[4].Start..blocks[4].End]);
        Assert.Equal(diff, source[blocks[5].Start..blocks[5].End]);
        Assert.Equal(decision, source[blocks[6].Start..blocks[6].End]);
        Assert.Equal("after\n", source[blocks[7].Start..blocks[7].End]);

        QuoteBlock quoted = Assert.IsType<QuoteBlock>(blocks[1]);
        Assert.Equal("quoted\n", source[quoted.Children[0].Start..quoted.Children[0].End]);
        QuoteBlock nested = Assert.IsType<QuoteBlock>(quoted.Children[1]);
        Assert.Equal("> nested\n", source[nested.Start..nested.End]);
        Assert.Equal("nested\n", source[Assert.Single(nested.Children).Start..Assert.Single(nested.Children).End]);

        DetailsBlock folded = Assert.IsType<DetailsBlock>(blocks[4]);
        Assert.Equal("- item\n", source[Assert.Single(folded.Children).Start..Assert.Single(folded.Children).End]);
        Assert.Equal("Sum", source[Assert.Single(folded.Summary).Start..Assert.Single(folded.Summary).End]);

        CodeBlock decisionCode = Assert.IsType<CodeBlock>(blocks[6]);
        DecisionPrompt prompt = Assert.Single(ok.Document.Decisions);
        Assert.Equal(decisionCode.Start, prompt.Start);
        Assert.Equal(decisionCode.End, prompt.End);

        foreach (Block block in blocks)
        {
            AssertBlockSlice(source, block, 0, source.Length);
        }
    }

    [Fact]
    public void GuideNamesTheConstructsThisParserReads()
    {
        string guide = PlanMarkdown.Guide;

        Assert.Contains("blockquote", guide, StringComparison.Ordinal);
        Assert.Contains("pipe table", guide, StringComparison.Ordinal);
        Assert.Contains("horizontal rule", guide, StringComparison.Ordinal);
        Assert.Contains("details", guide, StringComparison.Ordinal);
        Assert.Contains("summary", guide, StringComparison.Ordinal);
        Assert.Contains("diff", guide, StringComparison.Ordinal);
        Assert.Contains("mermaid", guide, StringComparison.Ordinal);
        Assert.Contains("decision", guide, StringComparison.Ordinal);
        Assert.Contains("choice", guide, StringComparison.Ordinal);
        Assert.Contains("text", guide, StringComparison.Ordinal);
        Assert.Contains("Other", guide, StringComparison.Ordinal);
    }

    private static void AssertBlockSlice(string source, Block block, int parentStart, int parentEnd)
    {
        Assert.InRange(block.Start, parentStart, parentEnd);
        Assert.InRange(block.End, block.Start, parentEnd);
        Assert.Equal(source[block.Start..block.End].Length, block.End - block.Start);
        foreach (Block child in ChildBlocks(block))
        {
            AssertBlockSlice(source, child, block.Start, block.End);
        }
    }

    private static IEnumerable<Block> ChildBlocks(Block block) => block switch
    {
        QuoteBlock quote => quote.Children,
        DetailsBlock details => details.Children,
        _ => [],
    };
}