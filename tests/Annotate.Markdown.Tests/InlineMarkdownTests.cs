using Annotate.Markdown;

namespace Annotate.Markdown.Tests;

public sealed class InlineMarkdownTests
{
    [Fact]
    public void ParagraphWithoutMarkupIsOneTextToken()
    {
        const string source = "plain words\n";
        ParagraphBlock paragraph = SingleParagraph(source);

        TextInline text = Assert.IsType<TextInline>(Assert.Single(paragraph.Inlines));
        Assert.Equal(0, text.Start);
        Assert.Equal("plain words".Length, text.End);
        Assert.Equal("plain words", source[text.Start..text.End]);
    }

    [Theory]
    [InlineData("*em*", 1)]
    [InlineData("_em_", 1)]
    [InlineData("**strong**", 2)]
    [InlineData("__strong__", 2)]
    public void WrappedMarkersBecomeEmphasis(string source, int level)
    {
        EmphasisInline emphasis = Assert.IsType<EmphasisInline>(Assert.Single(SingleParagraph(source).Inlines));

        Assert.Equal(level, emphasis.Level);
        Assert.Equal(0, emphasis.Start);
        Assert.Equal(source.Length, emphasis.End);
        TextInline inner = Assert.IsType<TextInline>(Assert.Single(emphasis.Children));
        Assert.Equal(level, inner.Start);
        Assert.Equal(source.Length - level, inner.End);
        Assert.Equal(source[level..^level], source[inner.Start..inner.End]);
    }

    [Fact]
    public void DoubleTildeBecomesStrike()
    {
        const string source = "~~removed~~";
        StrikeInline strike = Assert.IsType<StrikeInline>(Assert.Single(SingleParagraph(source).Inlines));

        Assert.Equal(0, strike.Start);
        Assert.Equal(source.Length, strike.End);
        TextInline inner = Assert.IsType<TextInline>(Assert.Single(strike.Children));
        Assert.Equal(2, inner.Start);
        Assert.Equal(source.Length - 2, inner.End);
        Assert.Equal("removed", source[inner.Start..inner.End]);
    }

    [Theory]
    [InlineData("`a*b*`", "a*b*")]
    [InlineData("`~~x~~`", "~~x~~")]
    [InlineData("`__strong__`", "__strong__")]
    public void CodeSpanKeepsLiteralText(string source, string literal)
    {
        CodeInline code = Assert.IsType<CodeInline>(Assert.Single(SingleParagraph(source).Inlines));

        Assert.Equal(literal, code.Text);
        Assert.Equal(0, code.Start);
        Assert.Equal(source.Length, code.End);
        Assert.Equal(source, source[code.Start..code.End]);
    }

    [Theory]
    [InlineData("[docs](http://example.com/a)", "http://example.com/a")]
    [InlineData("[docs](https://example.com/a)", "https://example.com/a")]
    [InlineData("[docs](/review/abc/)", "/review/abc/")]
    public void LinkKeepsHttpHttpsAndSameSiteDestination(string source, string destination)
    {
        LinkInline link = Assert.IsType<LinkInline>(Assert.Single(SingleParagraph(source).Inlines));

        Assert.Equal(destination, link.Destination);
        Assert.Equal(0, link.Start);
        Assert.Equal(source.Length, link.End);
        TextInline label = Assert.IsType<TextInline>(Assert.Single(link.Children));
        Assert.Equal("docs", source[label.Start..label.End]);
    }

    [Theory]
    [InlineData("[x](javascript:alert(1))")]
    [InlineData("[x](JAVASCRIPT:alert(1))")]
    [InlineData("[x]( javascript:alert(1))")]
    [InlineData("[x](data:text/html,hi)")]
    [InlineData("[x](//evil.example)")]
    public void UnsafeLinkDestinationIsNullAndLabelRemains(string source)
    {
        LinkInline link = Assert.IsType<LinkInline>(Assert.Single(SingleParagraph(source).Inlines));

        Assert.Null(link.Destination);
        TextInline label = Assert.IsType<TextInline>(Assert.Single(link.Children));
        Assert.Equal("x", source[label.Start..label.End]);
    }

    [Theory]
    [InlineData("![alt](https://example.com/a.png)", "https://example.com/a.png")]
    [InlineData("![alt](data:image/png,xx)", null)]
    [InlineData("![alt](javascript:alert(1))", null)]
    public void ImageKeepsOnlySafeDestination(string source, string? destination)
    {
        ImageInline image = Assert.IsType<ImageInline>(Assert.Single(SingleParagraph(source).Inlines));

        Assert.Equal(destination, image.Destination);
        Assert.Equal(0, image.Start);
        Assert.Equal(source.Length, image.End);
        TextInline alt = Assert.IsType<TextInline>(Assert.Single(image.Children));
        Assert.Equal("alt", source[alt.Start..alt.End]);
    }

    [Fact]
    public void RawScriptTagIsText()
    {
        const string source = "<script>alert(1)</script>";
        TextInline text = Assert.IsType<TextInline>(Assert.Single(SingleParagraph(source).Inlines));

        Assert.Equal(0, text.Start);
        Assert.Equal(source.Length, text.End);
        Assert.Equal(source, source[text.Start..text.End]);
    }

    [Fact]
    public void EmphasisNestsInsideStrong()
    {
        const string source = "**a *b* c**";
        EmphasisInline strong = Assert.IsType<EmphasisInline>(Assert.Single(SingleParagraph(source).Inlines));

        Assert.Equal(2, strong.Level);
        Assert.Equal(0, strong.Start);
        Assert.Equal(source.Length, strong.End);
        Assert.Equal(3, strong.Children.Count);
        Assert.Equal("a ", source[strong.Children[0].Start..strong.Children[0].End]);
        EmphasisInline emphasis = Assert.IsType<EmphasisInline>(strong.Children[1]);
        Assert.Equal(1, emphasis.Level);
        Assert.Equal("b", source[Assert.Single(emphasis.Children).Start..Assert.Single(emphasis.Children).End]);
        Assert.Equal(" c", source[strong.Children[2].Start..strong.Children[2].End]);
    }

    [Fact]
    public void LinkLabelCanContainEmphasis()
    {
        const string source = "[*em*](https://example.com)";
        LinkInline link = Assert.IsType<LinkInline>(Assert.Single(SingleParagraph(source).Inlines));

        Assert.Equal("https://example.com", link.Destination);
        EmphasisInline emphasis = Assert.IsType<EmphasisInline>(Assert.Single(link.Children));
        Assert.Equal(1, emphasis.Level);
        Assert.Equal("*em*", source[emphasis.Start..emphasis.End]);
        Assert.Equal("em", source[Assert.Single(emphasis.Children).Start..Assert.Single(emphasis.Children).End]);
    }

    [Theory]
    [InlineData("*em")]
    [InlineData("**em")]
    [InlineData("_em")]
    [InlineData("~~em")]
    [InlineData("[em")]
    public void UnmatchedOpenerStaysText(string source)
    {
        TextInline text = Assert.IsType<TextInline>(Assert.Single(SingleParagraph(source).Inlines));

        Assert.Equal(0, text.Start);
        Assert.Equal(source.Length, text.End);
        Assert.Equal(source, source[text.Start..text.End]);
    }

    [Fact]
    public void CodeFenceBodyKeepsMarkersAndHasNoInlineList()
    {
        const string source = "```\n*em* ~~x~~ [a](javascript:alert(1))\n# heading\n```\n";
        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));
        CodeBlock code = Assert.IsType<CodeBlock>(Assert.Single(ok.Document.Blocks));

        Assert.Equal("*em* ~~x~~ [a](javascript:alert(1))\n# heading\n", code.Body);
        Assert.Equal(source, source[code.Start..code.End]);
        Assert.DoesNotContain(typeof(CodeBlock).GetProperties(), property => property.Name == "Inlines");
    }

    [Fact]
    public void MixedParagraphTokensCoverTheMarkupRange()
    {
        const string source = "Go *fast* to [docs](https://example.com/a) and `code`.\n";
        ParagraphBlock paragraph = SingleParagraph(source);
        int markupEnd = source.Length - 1;

        AssertTokensCover(source, paragraph.Inlines, 0, markupEnd);
        Assert.Equal("Go ", source[paragraph.Inlines[0].Start..paragraph.Inlines[0].End]);
        EmphasisInline emphasis = Assert.IsType<EmphasisInline>(paragraph.Inlines[1]);
        Assert.Equal(1, emphasis.Level);
        Assert.Equal("fast", source[Assert.Single(emphasis.Children).Start..Assert.Single(emphasis.Children).End]);
        Assert.Equal(" to ", source[paragraph.Inlines[2].Start..paragraph.Inlines[2].End]);
        LinkInline link = Assert.IsType<LinkInline>(paragraph.Inlines[3]);
        Assert.Equal("https://example.com/a", link.Destination);
        Assert.Equal(" and ", source[paragraph.Inlines[4].Start..paragraph.Inlines[4].End]);
        CodeInline code = Assert.IsType<CodeInline>(paragraph.Inlines[5]);
        Assert.Equal("code", code.Text);
        Assert.Equal("`code`", source[code.Start..code.End]);
        Assert.Equal(".", source[paragraph.Inlines[6].Start..paragraph.Inlines[6].End]);
    }

    [Theory]
    [InlineData("# Title\n", "Title")]
    [InlineData("# Title #", "Title #")]
    [InlineData("#   Task 4  ", "  Task 4  ")]
    [InlineData("Title\n=====\n", "Title")]
    [InlineData("  Task 4  \n===\n", "  Task 4  ")]
    public void HeadingInlinesCoverRawContent(string source, string raw)
    {
        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));
        HeadingBlock heading = Assert.IsType<HeadingBlock>(Assert.Single(ok.Document.Blocks));

        AssertTokensCover(source, heading.Inlines, heading.Inlines[0].Start, heading.Inlines[0].Start + raw.Length);
        Assert.Equal(raw, string.Concat(heading.Inlines.Select(token => source[token.Start..token.End])));
    }

    [Theory]
    [InlineData("- *item*\n", "*item*")]
    [InlineData("- [x] **done**", "**done**")]
    public void ListItemInlinesCoverTheMarkerLineText(string source, string raw)
    {
        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));
        ListItem item = Assert.Single(Assert.IsType<ListBlock>(Assert.Single(ok.Document.Blocks)).Items);

        Assert.Equal(raw, item.Text);
        AssertTokensCover(source, item.Inlines, item.Inlines[0].Start, item.Inlines[0].Start + raw.Length);
        Assert.Equal(raw, string.Concat(item.Inlines.Select(token => source[token.Start..token.End])));
    }

    private static void AssertTokensCover(string source, IReadOnlyList<Inline> tokens, int start, int end)
    {
        Assert.Equal(source[start..end], string.Concat(tokens.Select(token => source[token.Start..token.End])));
        int cursor = start;
        foreach (Inline token in tokens)
        {
            Assert.Equal(cursor, token.Start);
            Assert.True(token.End >= token.Start);
            cursor = token.End;
            switch (token)
            {
                case EmphasisInline emphasis:
                    AssertTokensCover(source, emphasis.Children, emphasis.Start + emphasis.Level, emphasis.End - emphasis.Level);
                    break;
                case StrikeInline strike:
                    AssertTokensCover(source, strike.Children, strike.Start + 2, strike.End - 2);
                    break;
                case LinkInline link:
                    AssertLabel(source, link.Start + 1, link.Children);
                    break;
                case ImageInline image:
                    AssertLabel(source, image.Start + 2, image.Children);
                    break;
                case CodeInline code:
                    int width = 0;
                    while (source[code.Start + width] == '`')
                    {
                        width++;
                    }

                    Assert.Equal(code.Text, source[(code.Start + width)..(code.End - width)]);
                    break;
            }
        }

        Assert.Equal(end, cursor);
    }

    private static void AssertLabel(string source, int labelStart, IReadOnlyList<Inline> children)
    {
        int labelEnd = source.IndexOf(']', labelStart);
        AssertTokensCover(source, children, labelStart, labelEnd);
    }

    private static ParagraphBlock SingleParagraph(string source)
    {
        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));
        return Assert.IsType<ParagraphBlock>(Assert.Single(ok.Document.Blocks));
    }
}