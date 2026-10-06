using Annotate.Markdown;

namespace Annotate.Markdown.Tests;

public sealed class PlanMarkdownTests
{
    [Fact]
    public void EmptyStringParsesAsOkWithNoBlocks()
    {
        ParseOutcome outcome = PlanMarkdown.Parse(string.Empty);

        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(outcome);
        Assert.Empty(ok.Document.Blocks);
    }

    [Theory]
    [InlineData("# Title", 1, "Title")]
    [InlineData("## Title", 2, "Title")]
    [InlineData("### Title", 3, "Title")]
    [InlineData("#### Title", 4, "Title")]
    [InlineData("##### Title", 5, "Title")]
    [InlineData("###### Title", 6, "Title")]
    [InlineData("# Title\n", 1, "Title")]
    [InlineData("#   Task 4  ", 1, "Task 4")]
    public void AtxHeadingRecordsLevelTextAndSpan(string source, int level, string text)
    {
        HeadingBlock heading = Single<HeadingBlock>(source);

        Assert.Equal(BlockKind.Heading, heading.Kind);
        Assert.Equal(level, heading.Level);
        Assert.Equal(text, heading.Text);
        Assert.Equal(text, heading.SectionPath);
        Assert.Equal(0, heading.Start);
        Assert.Equal(source.Length, heading.End);
        Assert.Equal(source, source[heading.Start..heading.End]);
        TextInline inline = Assert.IsType<TextInline>(Assert.Single(heading.Inlines));
        Assert.Equal(text, source[inline.Start..inline.End].Trim());
    }

    [Theory]
    [InlineData("Title\n=====\n", 1, "Title")]
    [InlineData("Title\n---\n", 2, "Title")]
    [InlineData("Title\n=", 1, "Title")]
    [InlineData("Title\n-", 2, "Title")]
    [InlineData("  Task 4  \n===\n", 1, "Task 4")]
    public void SetextHeadingRecordsLevelTextAndSpan(string source, int level, string text)
    {
        HeadingBlock heading = Single<HeadingBlock>(source);

        Assert.Equal(BlockKind.Heading, heading.Kind);
        Assert.Equal(level, heading.Level);
        Assert.Equal(text, heading.Text);
        Assert.Equal(text, heading.SectionPath);
        Assert.Equal(0, heading.Start);
        Assert.Equal(source.Length, heading.End);
        Assert.Equal(source, source[heading.Start..heading.End]);
        TextInline inline = Assert.IsType<TextInline>(Assert.Single(heading.Inlines));
        Assert.Equal(text, source[inline.Start..inline.End].Trim());
    }

    [Theory]
    [InlineData("one\ntwo\n=====\n")]
    public void UnderlineAfterSeveralContentLinesStaysInsideTheParagraph(string source)
    {
        ParagraphBlock paragraph = Single<ParagraphBlock>(source);

        Assert.Equal(BlockKind.Paragraph, paragraph.Kind);
        Assert.Equal(source, paragraph.Text);
        Assert.Equal(string.Empty, paragraph.SectionPath);
        Assert.Equal(0, paragraph.Start);
        Assert.Equal(source.Length, paragraph.End);
        Assert.Equal(source, source[paragraph.Start..paragraph.End]);
    }

    [Fact]
    public void DashRuleAfterTwoContentLinesEndsTheParagraph()
    {
        const string source = "one\ntwo\n---\n";
        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));
        Block[] blocks = [.. ok.Document.Blocks];

        Assert.Equal(2, blocks.Length);
        ParagraphBlock paragraph = Assert.IsType<ParagraphBlock>(blocks[0]);
        Assert.Equal("one\ntwo\n", paragraph.Text);
        Assert.Equal("one\ntwo\n", source[paragraph.Start..paragraph.End]);
        RuleBlock rule = Assert.IsType<RuleBlock>(blocks[1]);
        Assert.Equal(BlockKind.Rule, rule.Kind);
        Assert.Equal("---\n", source[rule.Start..rule.End]);
    }

    [Theory]
    [InlineData("one\ntwo\n\n", "one\ntwo\n")]
    [InlineData("one\ntwo\n\n\n", "one\ntwo\n")]
    public void ParagraphOfSeveralLinesExcludesFollowingBlankLine(string source, string text)
    {
        ParagraphBlock paragraph = Single<ParagraphBlock>(source);

        Assert.Equal(BlockKind.Paragraph, paragraph.Kind);
        Assert.Equal(text, paragraph.Text);
        Assert.Equal(string.Empty, paragraph.SectionPath);
        Assert.Equal(0, paragraph.Start);
        Assert.Equal(text.Length, paragraph.End);
        Assert.Equal(text, source[paragraph.Start..paragraph.End]);
    }

    [Theory]
    [InlineData("- item", "-", null, "item")]
    [InlineData("* item", "*", null, "item")]
    [InlineData("+ item", "+", null, "item")]
    [InlineData("1. item", "1.", null, "item")]
    [InlineData("- item\n", "-", null, "item")]
    [InlineData("- [ ] item", "-", false, "item")]
    [InlineData("- [x] item", "-", true, "item")]
    public void ListItemRecordsMarkerCheckedStateAndText(
        string source,
        string marker,
        bool? checkedState,
        string text)
    {
        ListBlock list = Single<ListBlock>(source);

        Assert.Equal(BlockKind.List, list.Kind);
        Assert.Equal(0, list.Start);
        Assert.Equal(source.Length, list.End);
        Assert.Equal(source, source[list.Start..list.End]);
        ListItem item = Assert.Single(list.Items);
        Assert.Equal(marker, item.Marker);
        Assert.Equal(checkedState, item.Checked);
        Assert.Equal(text, item.Text);
        Assert.Empty(item.Items);
        TextInline inline = Assert.IsType<TextInline>(Assert.Single(item.Inlines));
        Assert.Equal(text, source[inline.Start..inline.End]);
    }

    [Fact]
    public void NestedListHangsOffParentItem()
    {
        string source = "- parent\n  - child\n    - grandchild\n  - other\n- next\n";
        ListBlock list = Single<ListBlock>(source);

        Assert.Equal(source, source[list.Start..list.End]);
        Assert.Equal(2, list.Items.Count);

        ListItem parent = list.Items[0];
        Assert.Equal("-", parent.Marker);
        Assert.Null(parent.Checked);
        Assert.Equal("parent", parent.Text);
        Assert.Equal(parent.Text, source[Assert.Single(parent.Inlines).Start..Assert.Single(parent.Inlines).End]);
        Assert.Equal(2, parent.Items.Count);
        Assert.Equal("child", parent.Items[0].Text);
        Assert.Equal(
            parent.Items[0].Text,
            source[Assert.Single(parent.Items[0].Inlines).Start..Assert.Single(parent.Items[0].Inlines).End]);
        Assert.Equal("-", parent.Items[0].Marker);
        ListItem grandchild = Assert.Single(parent.Items[0].Items);
        Assert.Equal("grandchild", grandchild.Text);
        Assert.Equal(grandchild.Text, source[Assert.Single(grandchild.Inlines).Start..Assert.Single(grandchild.Inlines).End]);
        Assert.Empty(grandchild.Items);
        Assert.Equal("other", parent.Items[1].Text);
        Assert.Equal(
            parent.Items[1].Text,
            source[Assert.Single(parent.Items[1].Inlines).Start..Assert.Single(parent.Items[1].Inlines).End]);
        Assert.Empty(parent.Items[1].Items);

        ListItem next = list.Items[1];
        Assert.Equal("next", next.Text);
        Assert.Equal(next.Text, source[Assert.Single(next.Inlines).Start..Assert.Single(next.Inlines).End]);
        Assert.Empty(next.Items);
    }

    [Theory]
    [InlineData("```csharp\nbody\n```\n", "csharp", "body\n")]
    [InlineData("~~~js\nbody\n~~~\n", "js", "body\n")]
    [InlineData("````\n```\ntext\n````\n", "", "```\ntext\n")]
    [InlineData("```\nbody\n````\n", "", "body\n")]
    [InlineData("```\n~~~\n```\n", "", "~~~\n")]
    [InlineData("```python\nbody", "python", "body")]
    public void FenceKeepsLanguageAndClosesOnAMatchingRun(string source, string language, string body)
    {
        CodeBlock code = Single<CodeBlock>(source);

        Assert.Equal(BlockKind.Code, code.Kind);
        Assert.Equal(language, code.Language);
        Assert.Equal(body, code.Body);
        Assert.Equal(string.Empty, code.SectionPath);
        Assert.Equal(0, code.Start);
        Assert.Equal(source.Length, code.End);
        Assert.Equal(source, source[code.Start..code.End]);
    }

    [Fact]
    public void HeadingInsideFenceStaysFenceContent()
    {
        string source = "```\n# Not a heading\nTitle\n=====\n```\n";
        CodeBlock code = Single<CodeBlock>(source);

        Assert.Equal(BlockKind.Code, code.Kind);
        Assert.Equal(string.Empty, code.Language);
        Assert.Equal("# Not a heading\nTitle\n=====\n", code.Body);
        Assert.Equal(source, source[code.Start..code.End]);
    }

    [Fact]
    public void SectionPathsNestAndResetWhenAHigherHeadingAppears()
    {
        string source = "# Tasks\n## Task 4\n### Detail\n## Task 5\nbody\n\nDone\n=====\nafter\n";
        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));
        Block[] blocks = [.. ok.Document.Blocks];

        Assert.Equal(
            [
                "Tasks",
                "Tasks/Task 4",
                "Tasks/Task 4/Detail",
                "Tasks/Task 5",
                "Tasks/Task 5",
                "Done",
                "Done",
            ],
            blocks.Select(block => block.SectionPath).ToArray());
        Assert.Equal("Tasks", Assert.IsType<HeadingBlock>(blocks[0]).Text);
        Assert.Equal("Task 4", Assert.IsType<HeadingBlock>(blocks[1]).Text);
        Assert.Equal("Detail", Assert.IsType<HeadingBlock>(blocks[2]).Text);
        Assert.Equal("Task 5", Assert.IsType<HeadingBlock>(blocks[3]).Text);
        Assert.Equal("body\n", Assert.IsType<ParagraphBlock>(blocks[4]).Text);
        Assert.Equal("Done", Assert.IsType<HeadingBlock>(blocks[5]).Text);
        Assert.Equal("after\n", Assert.IsType<ParagraphBlock>(blocks[6]).Text);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void EveryBlockSpanEqualsItsSourceText(string newline)
    {
        string[] parts =
        [
            "# Tasks",
            "## Task 4",
            "A paragraph" + newline + "that wraps.",
            "- [ ] one" + newline + "  - nested" + newline + "- [x] two",
            "```cs" + newline + "# still code" + newline + "```",
            "Title" + newline + "=====",
        ];
        string[] expected = parts.Select(part => part + newline).ToArray();
        string source = string.Join(newline + newline, parts) + newline;
        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));
        IReadOnlyList<Block> blocks = ok.Document.Blocks;

        Assert.Equal(expected.Length, blocks.Count);
        int cursor = 0;
        for (int index = 0; index < expected.Length; index++)
        {
            Block block = blocks[index];
            if (index == 0)
            {
                Assert.Equal(0, block.Start);
            }
            else
            {
                Assert.Equal(newline, source[cursor..block.Start]);
            }

            Assert.Equal(expected[index], source[block.Start..block.End]);
            cursor = block.End;
        }

        Assert.Equal(source.Length, cursor);
        Assert.Equal("Tasks", Assert.IsType<HeadingBlock>(blocks[0]).Text);
        Assert.Equal("Tasks", blocks[0].SectionPath);
        Assert.Equal("Task 4", Assert.IsType<HeadingBlock>(blocks[1]).Text);
        Assert.Equal("Tasks/Task 4", blocks[1].SectionPath);
        Assert.Equal(expected[2], Assert.IsType<ParagraphBlock>(blocks[2]).Text);
        Assert.Equal("Tasks/Task 4", blocks[2].SectionPath);

        ListBlock list = Assert.IsType<ListBlock>(blocks[3]);
        Assert.Equal("Tasks/Task 4", list.SectionPath);
        Assert.Equal(2, list.Items.Count);
        Assert.False(list.Items[0].Checked);
        Assert.Equal("one", list.Items[0].Text);
        Assert.Equal("nested", Assert.Single(list.Items[0].Items).Text);
        Assert.True(list.Items[1].Checked);
        Assert.Equal("two", list.Items[1].Text);

        CodeBlock code = Assert.IsType<CodeBlock>(blocks[4]);
        Assert.Equal("cs", code.Language);
        Assert.Equal("# still code" + newline, code.Body);
        Assert.Equal("Tasks/Task 4", code.SectionPath);

        HeadingBlock title = Assert.IsType<HeadingBlock>(blocks[5]);
        Assert.Equal(1, title.Level);
        Assert.Equal("Title", title.Text);
        Assert.Equal("Title", title.SectionPath);
    }

    private static T Single<T>(string source)
        where T : Block
    {
        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));
        return Assert.IsType<T>(Assert.Single(ok.Document.Blocks));
    }
}