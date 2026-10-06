using System.Globalization;

using AngleSharp.Dom;

using Annotate.Markdown;
using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;
using Annotate.Web.Components.Pages;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

using PlanRevisionId = Annotate.Plans.Application.RevisionId;
using ReviewRevisionId = Annotate.Reviews.Application.RevisionId;

namespace Annotate.Web.Tests;

public sealed class ReviewPageTests
{
    [Fact]
    public void ReviewShowsContextAndPlan()
    {
        string markdown =
            """
            # Storage

            Keep the file local. See [docs](https://example.com/docs) and [pending]() and ![logo](https://example.com/a.png) and ![plain]() and <script>alert(1)</script>.

            - first item

            > quoted

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
        HeadingBlock heading = Assert.IsType<HeadingBlock>(parsed.Document.Blocks[0]);
        TextInline title = Assert.IsType<TextInline>(Assert.Single(heading.Inlines));
        ParagraphBlock paragraph = Assert.Single(parsed.Document.Blocks.OfType<ParagraphBlock>());
        LinkInline docs = Assert.Single(paragraph.Inlines.OfType<LinkInline>(), link => link.Destination is not null);
        LinkInline pending = Assert.Single(paragraph.Inlines.OfType<LinkInline>(), link => link.Destination is null);
        ImageInline logo = Assert.Single(paragraph.Inlines.OfType<ImageInline>(), image => image.Destination is not null);
        ImageInline plain = Assert.Single(paragraph.Inlines.OfType<ImageInline>(), image => image.Destination is null);
        CodeBlock mermaid = Assert.Single(parsed.Document.Blocks.OfType<CodeBlock>(), block => block.Language == "mermaid");

        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 1));
        plans.PlansById["plan-1"] = new PlanDetail(new PlanId("plan-1"), new ProjectId("project-1"), "Storage", null, []);
        plans.Revisions["rev-1"] = new RevisionDetail(
            new PlanRevisionId("rev-1"),
            new PlanId("plan-1"),
            1,
            markdown,
            "Keep one file",
            "https://example.com/story",
            "The file stays on disk",
            [],
            null);
        FakeReviews reviews = new();
        reviews.Review = Review("review-1", "rev-1", ReviewStatus.Pending, []);

        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection());
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/review/review-1");
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));

        page.WaitForAssertion(() =>
        {
            IElement contextBlock = page.Find("[data-review-context]");
            Assert.DoesNotContain("Keep one file", contextBlock.TextContent, StringComparison.Ordinal);
            Assert.Contains("The file stays on disk", contextBlock.QuerySelector("[data-criteria]")!.TextContent, StringComparison.Ordinal);
            Assert.Equal("https://example.com/story", contextBlock.QuerySelector("a")!.GetAttribute("href"));
            Assert.Equal("Projects", page.Find(".breadcrumb a[href='/projects']").TextContent);
            Assert.Equal("Atlas", page.Find(".breadcrumb a[href='/projects/project-1']").TextContent);
            Assert.Empty(contextBlock.QuerySelectorAll("[data-block-index]"));
            Assert.Empty(contextBlock.QuerySelectorAll("[data-seg-start]"));
            Assert.Empty(contextBlock.QuerySelectorAll("[data-seg-end]"));

            IElement plan = page.Find("[data-plan]");
            int contextAt = page.Markup.IndexOf("data-review-context", StringComparison.Ordinal);
            int planAt = page.Markup.IndexOf("data-plan", StringComparison.Ordinal);
            Assert.True(contextAt >= 0 && planAt > contextAt);

            IElement titleSegment = plan.QuerySelector(
                $"[data-block-index='0'][data-seg-start='{title.Start}'][data-seg-end='{title.End}']")!;
            Assert.NotNull(titleSegment);
            Assert.Equal(markdown[title.Start..title.End], titleSegment.TextContent);
            Assert.Equal("Storage", titleSegment.TextContent);
            IElement contents = page.Find("[data-contents] a[href='/review/review-1#b-0']");
            Assert.Equal("Storage", contents.TextContent);

            Assert.Contains("Keep the file local.", plan.TextContent, StringComparison.Ordinal);
            Assert.Equal("docs", plan.QuerySelector($"a[href='{docs.Destination}']")!.TextContent);
            Assert.Null(pending.Destination);
            Assert.All(
                plan.QuerySelectorAll("a"),
                anchor => Assert.DoesNotContain("pending", anchor.TextContent, StringComparison.Ordinal));
            Assert.Contains("pending", plan.TextContent, StringComparison.Ordinal);

            IElement image = Assert.Single(plan.QuerySelectorAll("img"));
            Assert.Equal(logo.Destination, image.GetAttribute("src"));
            Assert.Null(plain.Destination);
            Assert.Contains("plain", plan.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("plain", image.GetAttribute("alt") ?? "", StringComparison.Ordinal);

            Assert.Contains("alert(1)", plan.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("<script", page.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(plan.QuerySelectorAll("script"));

            IElement mermaidNode = plan.QuerySelector(
                $"code[data-block-index][data-seg-start='{mermaid.Start}'][data-seg-end='{mermaid.End}']")!;
            Assert.Equal(markdown[mermaid.Start..mermaid.End], mermaidNode.TextContent);
            Assert.Contains("graph TD", mermaidNode.TextContent, StringComparison.Ordinal);
            IElement diagram = plan.QuerySelector("[data-mermaid]")!;
            Assert.NotNull(diagram.QuerySelector("[data-mermaid-canvas]"));
            Assert.Contains("graph TD", diagram.QuerySelector("[data-mermaid-body]")!.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("```", diagram.QuerySelector("[data-mermaid-body]")!.TextContent, StringComparison.Ordinal);
            IElement diffNode = plan.QuerySelector("[data-diff]")!;
            Assert.NotNull(diffNode);
            Assert.Equal("Unified diff", diffNode.QuerySelector(".diff-title")!.TextContent.Trim());
            Assert.Equal(" old", diffNode.QuerySelector(".diff-del .diff-content")!.TextContent);
            Assert.Equal(" new", diffNode.QuerySelector(".diff-add .diff-content")!.TextContent);
            Assert.Empty(diffNode.QuerySelectorAll("pre, code"));
            Assert.Empty(plan.QuerySelectorAll("svg"));

            string text = plan.TextContent;
            int storage = text.IndexOf("Storage", StringComparison.Ordinal);
            int keep = text.IndexOf("Keep the file local.", StringComparison.Ordinal);
            int item = text.IndexOf("first item", StringComparison.Ordinal);
            int quoted = text.IndexOf("quoted", StringComparison.Ordinal);
            int graph = text.IndexOf("graph TD", StringComparison.Ordinal);
            int added = text.IndexOf(" new", StringComparison.Ordinal);
            Assert.True(storage < keep && keep < item && item < quoted && quoted < graph && graph < added);
        });
    }

    [Fact]
    public async Task KeysAddAnnotationKindsAndUndo()
    {
        FakePlans plans = new();
        plans.Revisions["rev-1"] = Revision("# Storage\n\nKeep the file local.");
        FakeReviews reviews = new();
        reviews.Review = Review("review-1", "rev-1", ReviewStatus.Pending, []);
        FakeSelection selection = new();

        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(selection);
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-review]")));

        selection.Next = null;
        await page.Find("[data-review]").KeyDownAsync("d");
        Assert.Empty(page.FindAll("[data-annotations] li"));

        selection.Next = new TextSelection(0, 2, 9, "Storage");
        await page.Find("[data-review]").KeyDownAsync("d");
        await SaveNote(page, "r", "disk");
        await SaveNote(page, "s", "line");
        await SaveNote(page, "c", "why");

        IReadOnlyList<IElement> items = page.FindAll("[data-annotations] li");
        Assert.Equal(
            ["Deletion", "Replacement", "Insertion", "Comment"],
            items.Select(item => item.GetAttribute("data-kind")));
        AssertAnnotation(items[0], "Storage", "", "", 0, 2, 9);
        AssertAnnotation(items[1], "Storage", "disk", "", 0, 2, 9);
        AssertAnnotation(items[2], "Storage", "line", "", 0, 2, 9);
        AssertAnnotation(items[3], "Storage", "", "why", 0, 2, 9);
        Assert.Equal("5", page.Find("[data-countdown]").TextContent);
        Assert.Equal("true", page.Find("[data-countdown]").GetAttribute("data-running"));
        Assert.Empty(reviews.Approved);
        Assert.Empty(reviews.Changes);

        await page.Find("[data-review]").KeyDownAsync("u");
        items = page.FindAll("[data-annotations] li");
        Assert.Equal(["Deletion", "Replacement", "Insertion"], items.Select(item => item.GetAttribute("data-kind")));
        Assert.Empty(reviews.Approved);
        Assert.Empty(reviews.Changes);

        selection.Next = null;
        await page.Find("[data-review]").KeyDownAsync("d");
        await page.Find("[data-review]").KeyDownAsync("c");
        selection.Next = new TextSelection(0, 2, 9, "Storage");
        await page.Find("[data-review]").KeyDownAsync("r");
        await page.Find("[data-note]").InputAsync("");
        await page.Find("[data-save-note]").ClickAsync();
        Assert.Equal(3, page.FindAll("[data-annotations] li").Count);
        await page.Find("[data-review]").KeyDownAsync(Key.Escape);
        Assert.Equal(3, page.FindAll("[data-annotations] li").Count);
        Assert.Equal("5", page.Find("[data-countdown]").TextContent);
        Assert.Equal("false", page.Find("[data-countdown]").GetAttribute("data-running"));
        Assert.Empty(reviews.Changes);
    }

    [Fact]
    public async Task SelectionOpensAnInlineAnnotatePopover()
    {
        FakePlans plans = new();
        plans.Revisions["rev-1"] = Revision("# Storage\n\nKeep the file local.");
        FakeReviews reviews = new();
        reviews.Review = Review("review-1", "rev-1", ReviewStatus.Pending, []);
        FakeSelection selection = new()
        {
            Next = new TextSelection(0, 2, 9, "Storage", new SelectionBox(120, 40, 140, 80)),
        };

        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(selection);
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-review]")));

        await page.Find(".document").TriggerEventAsync("onmouseup", new MouseEventArgs());

        IElement toolbar = page.Find("[data-annotate-toolbar]");
        Assert.Equal("76", toolbar.GetAttribute("data-top"));
        Assert.Equal("40", toolbar.GetAttribute("data-left"));
        Assert.Empty(page.FindAll("[data-note-dialog]"));

        await toolbar.QuerySelector("[data-annotate='c']")!.ClickAsync();

        IElement dialog = page.Find("[data-note-dialog]");
        Assert.Equal("148", dialog.GetAttribute("data-top"));
        Assert.Equal("40", dialog.GetAttribute("data-left"));
        Assert.Equal("Storage", dialog.QuerySelector("[data-note-quote]")!.TextContent);
        Assert.Empty(page.FindAll("[data-decision]"));
        Assert.Empty(page.FindAll("[data-annotate-toolbar]"));
    }

    [Fact]
    public async Task OpenNoteKeepsTheQuoteMarked()
    {
        FakePlans plans = new();
        plans.Revisions["rev-1"] = Revision("# Storage\n\nKeep the file local.");
        FakeReviews reviews = new();
        reviews.Review = Review("review-1", "rev-1", ReviewStatus.Pending, []);
        FakeSelection selection = new()
        {
            Next = new TextSelection(0, 2, 9, "Storage", new SelectionBox(120, 40, 140, 80)),
        };

        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(selection);
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-review]")));

        await page.Find(".document").TriggerEventAsync("onmouseup", new MouseEventArgs());
        await page.Find("[data-annotate='c']").ClickAsync();

        IElement mark = page.Find("[data-plan] mark.annotation-mark");
        Assert.Equal("Storage", mark.TextContent);
        Assert.Contains("Comment", mark.GetAttribute("data-annotation-kinds"));
        Assert.Empty(page.FindAll("[data-annotations] li"));

        await page.Find("[data-cancel]").ClickAsync();
        Assert.Empty(page.FindAll("[data-plan] mark.annotation-mark"));
    }

    [Fact]
    public async Task ApproveAndRequestChanges()
    {
        FakePlans plans = new();
        plans.Revisions["rev-1"] = Revision("# Storage\n");
        FakeReviews reviews = new();
        reviews.Review = Review("review-1", "rev-1", ReviewStatus.Pending, []);
        FakeSelection selection = new() { Next = new TextSelection(0, 2, 9, "Storage") };

        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(selection);
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-review]")));

        await page.Find("[data-review]").KeyDownAsync("d");
        await page.Find("[data-approve]").ClickAsync();

        Assert.Equal(["review-1"], reviews.Approved);
        Assert.Empty(reviews.Changes);
        Assert.Empty(page.FindAll("[data-annotations] li"));
        Assert.Equal("Approved", page.Find("[data-status]").TextContent);
        Assert.Equal("false", page.Find("[data-countdown]").GetAttribute("data-running"));
        Assert.True(page.Find("[data-approve]").HasAttribute("disabled"));
        Assert.True(page.Find("[data-request-changes]").HasAttribute("disabled"));

        await page.Find("[data-review]").KeyDownAsync("d");
        await page.Find("[data-approve]").ClickAsync();
        Assert.Equal(["review-1"], reviews.Approved);
        Assert.Empty(page.FindAll("[data-annotations] li"));

        FakeReviews prompted = new();
        prompted.Review = Review(
            "review-2",
            "rev-1",
            ReviewStatus.Pending,
            [new ReviewPrompt("storage", PromptKind.Choice, "Which store?", ["SQLite"])]);
        using BunitContext promptedContext = BrowseHost.Open(plans, prompted);
        promptedContext.Services.AddSingleton<ISelectionReader>(new FakeSelection());
        IRenderedComponent<ReviewPage> promptedPage = promptedContext.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-2"));
        promptedPage.WaitForAssertion(() =>
            Assert.True(promptedPage.Find("[data-approve]").HasAttribute("disabled")));
        await promptedPage.Find("[data-approve]").ClickAsync();
        Assert.Empty(prompted.Approved);

        FakeReviews changes = new();
        changes.Review = Review("review-3", "rev-1", ReviewStatus.Pending, []);
        FakeSelection changeSelection = new() { Next = new TextSelection(0, 2, 9, "Storage") };
        using BunitContext changeContext = BrowseHost.Open(plans, changes);
        changeContext.Services.AddSingleton<ISelectionReader>(changeSelection);
        IRenderedComponent<ReviewPage> changePage = changeContext.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-3"));
        changePage.WaitForAssertion(() => Assert.NotNull(changePage.Find("[data-review]")));
        await changePage.Find("[data-review]").KeyDownAsync("d");
        await SaveNote(changePage, "r", "disk");
        await changePage.Find("[data-request-changes]").ClickAsync();

        RequestedChanges sent = Assert.Single(changes.Changes);
        Assert.Equal("review-3", sent.ReviewId);
        Assert.Equal(AnnotationKind.Deletion, sent.Annotations[0].Kind);
        Assert.Equal("Storage", sent.Annotations[0].Text);
        Assert.Null(sent.Annotations[0].Replacement);
        Assert.Null(sent.Annotations[0].Comment);
        Assert.Equal(0, sent.Annotations[0].BlockOrdinal);
        Assert.Equal(2, sent.Annotations[0].StartOffset);
        Assert.Equal(9, sent.Annotations[0].EndOffset);
        Assert.Equal(AnnotationKind.Replacement, sent.Annotations[1].Kind);
        Assert.Equal("disk", sent.Annotations[1].Replacement);
        Assert.Empty(changes.Approved);
        Assert.Empty(changePage.FindAll("[data-annotations] li"));
        Assert.Equal("Changes requested", changePage.Find("[data-status]").TextContent);
        Assert.Equal("false", changePage.Find("[data-countdown]").GetAttribute("data-running"));
    }

    [Fact]
    public async Task RefusedKeepsAnnotationsAndLoadErrors()
    {
        FakePlans plans = new();
        plans.Revisions["rev-1"] = Revision("# Storage\n");
        FakeReviews reviews = new();
        reviews.Review = Review("review-1", "rev-1", ReviewStatus.Pending, []);
        reviews.RequestChangesOutcome = new DecideOutcome.Refused("Annotation was rejected.");
        FakeSelection selection = new() { Next = new TextSelection(0, 2, 9, "Storage") };

        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(selection);
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-review]")));
        await page.Find("[data-review]").KeyDownAsync("d");
        await page.Find("[data-request-changes]").ClickAsync();

        Assert.Equal("Deletion", page.Find("[data-annotations] li").GetAttribute("data-kind"));
        Assert.Equal("Storage", page.Find("[data-text]").TextContent);
        Assert.Equal("Pending", page.Find("[data-status]").TextContent);
        Assert.False(page.Find("[data-approve]").HasAttribute("disabled"));
        Assert.False(page.Find("[data-request-changes]").HasAttribute("disabled"));
        Assert.Equal("false", page.Find("[data-countdown]").GetAttribute("data-running"));
        await page.Find("[data-review]").KeyDownAsync("d");
        Assert.Equal(2, page.FindAll("[data-annotations] li").Count);

        FakeReviews missing = new();
        using BunitContext missingContext = BrowseHost.Open(plans, missing);
        missingContext.Services.AddSingleton<ISelectionReader>(new FakeSelection());
        IRenderedComponent<ReviewPage> missingPage = missingContext.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "missing"));
        missingPage.WaitForAssertion(() => Assert.Equal("Review not found", missingPage.Find("h1").TextContent));
        Assert.DoesNotContain("Could not load this review", missingPage.Markup, StringComparison.Ordinal);

        FakeReviews broken = new();
        broken.LoadError = new InvalidOperationException("down");
        using BunitContext brokenContext = BrowseHost.Open(plans, broken);
        brokenContext.Services.AddSingleton<ISelectionReader>(new FakeSelection());
        IRenderedComponent<ReviewPage> brokenPage = brokenContext.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));
        brokenPage.WaitForAssertion(() => Assert.Equal("Could not load this review", brokenPage.Find("h1").TextContent));
        Assert.DoesNotContain("Review not found", brokenPage.Markup, StringComparison.Ordinal);
    }

    private static void AssertAnnotation(
        IElement item,
        string text,
        string replacement,
        string comment,
        int block,
        int start,
        int end)
    {
        Assert.Equal(text, item.QuerySelector("[data-text]")!.TextContent);
        Assert.Equal(replacement, item.QuerySelector("[data-replacement]")!.TextContent);
        Assert.Equal(comment, item.QuerySelector("[data-comment]")!.TextContent);
        Assert.Equal(block.ToString(CultureInfo.InvariantCulture), item.QuerySelector("[data-block]")!.TextContent);
        Assert.Equal(start.ToString(CultureInfo.InvariantCulture), item.QuerySelector("[data-start]")!.TextContent);
        Assert.Equal(end.ToString(CultureInfo.InvariantCulture), item.QuerySelector("[data-end]")!.TextContent);
        Assert.True(Guid.TryParse(item.QuerySelector("[data-id]")!.TextContent, out _));
        Assert.True(DateTimeOffset.TryParseExact(
            item.QuerySelector("[data-created]")!.TextContent,
            "u",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTimeOffset created));
        Assert.Equal(TimeSpan.Zero, created.Offset);
    }

    private static async Task SaveNote(IRenderedComponent<ReviewPage> page, string key, string text)
    {
        await page.Find("[data-review]").KeyDownAsync(key);
        IElement dialog = page.Find("[data-note-dialog]");
        Assert.Equal("Storage", dialog.QuerySelector("[data-note-quote]")!.TextContent);
        await dialog.QuerySelector("[data-note]")!.InputAsync(text);
        await dialog.QuerySelector("[data-save-note]")!.ClickAsync();
    }

    private static RevisionDetail Revision(string markdown) =>
        new(
            new PlanRevisionId("rev-1"),
            new PlanId("plan-1"),
            1,
            markdown,
            null,
            null,
            null,
            [],
            null);

    private static ReviewDetail Review(
        string id,
        string revision,
        ReviewStatus status,
        IReadOnlyList<ReviewPrompt> prompts) =>
        new(
            new ReviewId(id),
            new ReviewRevisionId(revision),
            status,
            null,
            [],
            [],
            prompts,
            DateTimeOffset.UnixEpoch,
            status == ReviewStatus.Pending ? null : DateTimeOffset.UnixEpoch);
}