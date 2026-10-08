using Annotate.Markdown;
using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;
using Annotate.Web.Components.Pages;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using PlanRevisionId = Annotate.Plans.Application.RevisionId;
using ReviewRevisionId = Annotate.Reviews.Application.RevisionId;

namespace Annotate.Web.Tests;

public sealed class ReviewThreadTests
{
    [Fact]
    public async Task AgentNoteAcceptsRepliesAndRequestChangesAsksBeforeDroppingIt()
    {
        FakePlans plans = new();
        plans.Revisions["rev-1"] = new RevisionDetail(
            new PlanRevisionId("rev-1"),
            new PlanId("plan-1"),
            1,
            "# Storage\n",
            null,
            null,
            null,
            [new RevisionBlock("block-1", BlockKind.Heading, "Storage", 0, 10, BlockChange.Unchanged, "hash")],
            null);
        FakeReviews reviews = new()
        {
            Review = new ReviewDetail(
                new ReviewId("review-1"),
                new ReviewRevisionId("rev-1"),
                ReviewStatus.Pending,
                null,
                [
                    new Annotation(
                        "note-1",
                        AnnotationKind.Comment,
                        "Use a file.",
                        "Use a file.",
                        null,
                        0,
                        0,
                        0,
                        "t",
                        "block-1",
                        AnnotationAuthor.Agent,
                        false),
                ],
                [],
                [],
                DateTimeOffset.UnixEpoch,
                null),
        };

        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection());
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-bot]")));

        Assert.Empty(page.FindAll("[data-block-comment]"));
        Assert.Equal("Storage", page.Find("[data-block-key]").TextContent.Trim());
        Assert.Equal("block-1", page.Find("[data-block-key]").GetAttribute("data-block-key"));
        Assert.NotNull(page.Find("[data-block-note='true']"));
        Assert.Equal("false", page.Find("[data-accept]").GetAttribute("data-accepted"));
        Assert.Equal("false", page.Find("[data-accept]").GetAttribute("aria-pressed"));
        Assert.True(page.Find("[data-reply-actions]").HasAttribute("hidden"));
        Assert.All(
            page.FindAll("[data-annotations] .btn"),
            button => Assert.Contains("btn-sm", button.ClassList));
        Assert.True(page.Find("[data-approve]").HasAttribute("disabled"));

        await page.Find("[data-request-changes]").ClickAsync();
        Assert.Contains("1 unaccepted agent note will be dropped.", page.Markup, StringComparison.Ordinal);
        await page.Find("[data-cancel]").ClickAsync();
        Assert.Empty(reviews.Changes);

        await page.Find("[data-reply-draft]").InputAsync("Because.");
        Assert.False(page.Find("[data-reply-actions]").HasAttribute("hidden"));
        await page.Find("[data-add-reply]").ClickAsync();
        page.WaitForAssertion(() => Assert.Equal("Because.", page.Find("[data-reply-text]").TextContent));
        await page.Find("[data-delete-reply]").ClickAsync();
        Assert.Contains("Delete this reply?", page.Markup, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("[data-side-panel] [role='dialog']"));
        await page.Find("[data-confirm]").ClickAsync();
        page.WaitForAssertion(() => Assert.Empty(page.FindAll("[data-reply]")));

        await page.Find("[data-accept]").ClickAsync();
        page.WaitForAssertion(() => Assert.Equal("true", page.Find("[data-accept]").GetAttribute("data-accepted")));
        Assert.Equal("true", page.Find("[data-accept]").GetAttribute("aria-pressed"));
        await page.Find("[data-request-changes]").ClickAsync();
        Assert.Single(reviews.Changes);
        Assert.DoesNotContain("will be dropped", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AcceptKeepsAPhraseDraftThatIsStillSaving()
    {
        (FakeReviews reviews, IRenderedComponent<ReviewPage> page) open = await Opened();
        open.reviews.HoldSave = true;
        Task add = open.page.Find("[data-review]").KeyDownAsync("d");
        open.page.WaitForAssertion(() => Assert.Equal("Deletion", open.page.Find("[data-kind='Deletion']").GetAttribute("data-kind")));

        Task accept = open.page.Find("[data-accept]").ClickAsync();
        open.reviews.ReleaseSave.TrySetResult();
        await add;
        await accept;

        Assert.NotNull(open.page.Find("[data-kind='Deletion']"));
        Assert.Equal("true", open.page.Find("[data-accept]").GetAttribute("data-accepted"));
    }

    [Fact]
    public async Task DeletingAPhraseWhileItsSaveIsInFlightStaysDeleted()
    {
        (FakeReviews reviews, IRenderedComponent<ReviewPage> page) open = await Opened();
        open.reviews.HoldSave = true;
        Task add = open.page.Find("[data-review]").KeyDownAsync("d");
        open.page.WaitForAssertion(() => Assert.NotNull(open.page.Find("[data-kind='Deletion'] [data-delete-annotation]")));
        await open.page.Find("[data-kind='Deletion'] [data-delete-annotation]").ClickAsync();
        Task confirm = open.page.Find("[data-confirm]").ClickAsync();
        open.reviews.ReleaseSave.TrySetResult();
        await add;
        await confirm;

        Assert.Empty(open.page.FindAll("[data-kind='Deletion']"));
        Assert.DoesNotContain(open.reviews.Review!.Annotations, item => item.Kind == AnnotationKind.Deletion);
        Assert.NotNull(open.page.Find("[data-accept]"));
    }

    [Fact]
    public async Task AcceptLeavesAnOpenDecisionOpen()
    {
        (FakeReviews reviews, IRenderedComponent<ReviewPage> page) open = await Opened(
            new ReviewPrompt("storage", PromptKind.Choice, "Which store?", ["SQLite"]));
        await open.page.Find("[data-prompt='storage']").ClickAsync();
        Assert.NotNull(open.page.Find("[data-decision]"));

        await open.page.Find("[data-accept]").ClickAsync();

        Assert.NotNull(open.page.Find("[data-decision]"));
        Assert.Equal("true", open.page.Find("[data-accept]").GetAttribute("data-accepted"));
    }

    [Fact]
    public async Task ParagraphBlockKeepsALeadingHashInItsName()
    {
        FakePlans plans = new();
        plans.Revisions["rev-1"] = new RevisionDetail(
            new PlanRevisionId("rev-1"),
            new PlanId("plan-1"),
            1,
            "#tag\n",
            null,
            null,
            null,
            [new RevisionBlock("block-1", BlockKind.Paragraph, "", 0, 5, BlockChange.Unchanged, "hash")],
            null);
        FakeReviews reviews = new()
        {
            Review = new ReviewDetail(
                new ReviewId("review-1"),
                new ReviewRevisionId("rev-1"),
                ReviewStatus.Pending,
                null,
                [
                    new Annotation(
                        "note-1",
                        AnnotationKind.Comment,
                        "#tag",
                        "#tag",
                        null,
                        0,
                        0,
                        0,
                        "t",
                        "block-1",
                        AnnotationAuthor.Agent,
                        false),
                ],
                [],
                [],
                DateTimeOffset.UnixEpoch,
                null),
        };
        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection());
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));
        page.WaitForAssertion(() => Assert.Equal("#tag", page.Find("[data-block-key]").TextContent.Trim()));
    }

    private static async Task<(FakeReviews Reviews, IRenderedComponent<ReviewPage> Page)> Opened(
        params ReviewPrompt[] prompts)
    {
        FakePlans plans = new();
        plans.Revisions["rev-1"] = new RevisionDetail(
            new PlanRevisionId("rev-1"),
            new PlanId("plan-1"),
            1,
            "# Storage\n",
            null,
            null,
            null,
            [new RevisionBlock("block-1", BlockKind.Heading, "Storage", 0, 10, BlockChange.Unchanged, "hash")],
            null);
        FakeReviews reviews = new()
        {
            Review = new ReviewDetail(
                new ReviewId("review-1"),
                new ReviewRevisionId("rev-1"),
                ReviewStatus.Pending,
                null,
                [
                    new Annotation(
                        "note-1",
                        AnnotationKind.Comment,
                        "Use a file.",
                        "Use a file.",
                        null,
                        0,
                        0,
                        0,
                        "t",
                        "block-1",
                        AnnotationAuthor.Agent,
                        false),
                ],
                [],
                prompts,
                DateTimeOffset.UnixEpoch,
                null),
        };
        BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection { Next = new TextSelection(0, 2, 9, "Storage") });
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-accept]")));
        return (reviews, page);
    }
}