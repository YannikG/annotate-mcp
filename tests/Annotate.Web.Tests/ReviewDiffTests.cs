using AngleSharp.Dom;

using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;
using Annotate.Web.Components.Pages;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using PlanRevisionId = Annotate.Plans.Application.RevisionId;
using ReviewRevisionId = Annotate.Reviews.Application.RevisionId;

namespace Annotate.Web.Tests;

public sealed class ReviewDiffTests
{
    private const string Fences =
        """
        # Storage

        ```csharp
        int x = 1;
        ```

        ```decision
        id: storage
        kind: choice
        prompt: Which store?
        - SQLite
        ```

        ```mermaid
        graph TD
          A-->B
        ```

        ```diff
        - old
        + new
        ```
        """;

    [Fact]
    public async Task NoParentLeavesThePlanAndDoesNotDiff()
    {
        FakePlans plans = new();
        plans.Revisions["rev-1"] = Detail("rev-1", 1, "# Storage\n", null);
        FakeReviews reviews = new();
        reviews.Review = Review("rev-1", ReviewStatus.Pending);
        FakeSelection selection = new();

        using BunitContext context = BrowseHost.Open(plans, reviews);
        IRenderedComponent<ReviewPage> page = Render(context, selection);

        await page.Find("[data-review]").KeyDownAsync("d");

        Assert.NotNull(page.Find("[data-plan]"));
        Assert.Empty(page.FindAll("section[data-diff]"));
        Assert.Empty(page.FindAll("[data-notice]"));
        Assert.Empty(page.FindAll("[data-annotations] li"));
        Assert.Empty(plans.DiffCalls);
        Assert.Equal(["rev-1"], plans.RevisionReads);
    }

    [Fact]
    public async Task ShowsDiffAndKeepsReviewChrome()
    {
        (FakePlans plans, FakeReviews reviews, FakeSelection selection) host = Continued();
        host.plans.DiffResult = SampleLines();

        using BunitContext context = BrowseHost.Open(host.plans, host.reviews);
        IRenderedComponent<ReviewPage> page = Render(context, host.selection);

        host.selection.Next = new TextSelection(0, 2, 9, "Storage");
        await page.Find("[data-review]").KeyDownAsync("d");
        Assert.Equal("Deletion", page.Find("[data-annotations] li").GetAttribute("data-kind"));
        Assert.NotNull(page.Find("[data-plan]"));
        Assert.Empty(page.FindAll("section[data-diff]"));
        Assert.Empty(host.plans.DiffCalls);

        host.selection.Next = null;
        await page.Find("[data-review]").KeyDownAsync("d");

        IElement diff = page.Find("section[data-diff]");
        Assert.Equal("Changes from v4 to v9", diff.QuerySelector("h2")!.TextContent);
        IReadOnlyList<IElement> rows = diff.QuerySelectorAll("[data-line]");
        Assert.Equal(["equal", "removed", "added"], rows.Select(row => row.GetAttribute("data-line")));
        Assert.Equal(
            ["same", "gone", "<script>alert(1)</script>"],
            rows.Select(row => row.TextContent));
        Assert.Empty(rows[2].Children);
        Assert.Empty(diff.QuerySelectorAll("script"));
        Assert.Empty(page.FindAll("[data-plan]"));
        AssertChrome(page);
        Assert.Equal([("rev-4", "rev-9")], host.plans.DiffCalls);
        Assert.Equal(["rev-9", "rev-4"], host.plans.RevisionReads);
    }

    [Fact]
    public async Task SecondDReturnsToPlanSourceWithoutReadingSelection()
    {
        (FakePlans plans, FakeReviews reviews, FakeSelection selection) host = Continued(Fences);
        host.plans.DiffResult = SampleLines();

        using BunitContext context = BrowseHost.Open(host.plans, host.reviews);
        IRenderedComponent<ReviewPage> page = Render(context, host.selection);
        AssertPlanSource(page);

        await page.Find("[data-review]").KeyDownAsync("d");
        Assert.Empty(page.FindAll("[data-plan]"));
        int reads = host.selection.Reads;
        int diffs = host.plans.DiffCalls.Count;

        host.selection.Next = new TextSelection(0, 2, 9, "Storage");
        await page.Find("[data-review]").KeyDownAsync("d");

        Assert.Equal(reads, host.selection.Reads);
        Assert.Equal(diffs, host.plans.DiffCalls.Count);
        Assert.Empty(page.FindAll("section[data-diff]"));
        Assert.Empty(page.FindAll("[data-annotations] li"));
        Assert.Empty(page.FindAll("[data-notice]"));
        AssertPlanSource(page);
        AssertChrome(page, annotations: false);
    }

    [Fact]
    public async Task DeletionDoesNotOpenTheDiff()
    {
        (FakePlans plans, FakeReviews reviews, FakeSelection selection) host = Continued();
        host.plans.DiffResult = SampleLines();
        host.selection.Next = new TextSelection(0, 2, 9, "Storage");

        using BunitContext context = BrowseHost.Open(host.plans, host.reviews);
        IRenderedComponent<ReviewPage> page = Render(context, host.selection);

        await page.Find("[data-review]").KeyDownAsync("d");

        Assert.Equal("Deletion", page.Find("[data-annotations] li").GetAttribute("data-kind"));
        Assert.Equal("Storage", page.Find("[data-text]").TextContent);
        Assert.NotNull(page.Find("[data-plan]"));
        Assert.Empty(page.FindAll("section[data-diff]"));
        Assert.Empty(host.plans.DiffCalls);
        Assert.Equal(["rev-9"], host.plans.RevisionReads);
    }

    [Fact]
    public async Task RefusedDiffLeavesThePlan()
    {
        (FakePlans plans, FakeReviews reviews, FakeSelection selection) host = Continued(Fences);
        host.plans.DiffResult = new DiffOutcome.Refused("Revisions belong to different plans.");

        using BunitContext context = BrowseHost.Open(host.plans, host.reviews);
        IRenderedComponent<ReviewPage> page = Render(context, host.selection);

        await page.Find("[data-review]").KeyDownAsync("d");

        Assert.Equal("Revisions belong to different plans.", page.Find("[data-notice]").TextContent);
        Assert.Empty(page.FindAll("section[data-diff]"));
        AssertPlanSource(page);
        AssertChrome(page, annotations: false);
    }

    [Fact]
    public async Task ThrownDiffLeavesThePlan()
    {
        (FakePlans plans, FakeReviews reviews, FakeSelection selection) host = Continued();
        host.plans.DiffError = new InvalidOperationException("down");

        using BunitContext context = BrowseHost.Open(host.plans, host.reviews);
        IRenderedComponent<ReviewPage> page = Render(context, host.selection);

        await page.Find("[data-review]").KeyDownAsync("d");

        Assert.Equal("Could not load this review", page.Find("[data-notice]").TextContent);
        Assert.NotNull(page.Find("[data-plan]"));
        Assert.NotNull(page.Find("[data-review]"));
        Assert.Empty(page.FindAll("section[data-diff]"));
        Assert.Equal("Pending", page.Find("[data-status]").TextContent);
        Assert.Equal([("rev-4", "rev-9")], host.plans.DiffCalls);
    }

    [Fact]
    public async Task MissingParentRevisionLeavesThePlan()
    {
        FakePlans plans = new();
        plans.Revisions["rev-9"] = Detail("rev-9", 9, "# Storage\n", "rev-missing");
        plans.DiffResult = SampleLines();
        FakeReviews reviews = new();
        reviews.Review = Review("rev-9", ReviewStatus.Pending);
        FakeSelection selection = new();

        using BunitContext context = BrowseHost.Open(plans, reviews);
        IRenderedComponent<ReviewPage> page = Render(context, selection);

        await page.Find("[data-review]").KeyDownAsync("d");

        Assert.Equal("Revision was not found.", page.Find("[data-notice]").TextContent);
        Assert.NotNull(page.Find("[data-plan]"));
        Assert.Empty(page.FindAll("section[data-diff]"));
        Assert.Empty(plans.DiffCalls);
        Assert.Equal(["rev-9", "rev-missing"], plans.RevisionReads);
    }

    [Fact]
    public async Task ThrownParentReadLeavesThePlan()
    {
        (FakePlans plans, FakeReviews reviews, FakeSelection selection) host = Continued();
        host.plans.RevisionErrors["rev-4"] = new InvalidOperationException("down");
        host.plans.DiffResult = SampleLines();

        using BunitContext context = BrowseHost.Open(host.plans, host.reviews);
        IRenderedComponent<ReviewPage> page = Render(context, host.selection);

        await page.Find("[data-review]").KeyDownAsync("d");

        Assert.Equal("Could not load this review", page.Find("[data-notice]").TextContent);
        Assert.NotNull(page.Find("[data-plan]"));
        Assert.NotNull(page.Find("[data-review]"));
        Assert.Empty(page.FindAll("section[data-diff]"));
        Assert.Empty(host.plans.DiffCalls);
        Assert.Equal(["rev-9", "rev-4"], host.plans.RevisionReads);
    }

    [Fact]
    public async Task DecidedReviewDoesNotToggleTheDiff()
    {
        (FakePlans plans, FakeReviews reviews, FakeSelection selection) host = Continued();
        host.reviews.Review = Review("rev-9", ReviewStatus.Approved);
        host.plans.DiffResult = SampleLines();

        using BunitContext context = BrowseHost.Open(host.plans, host.reviews);
        IRenderedComponent<ReviewPage> page = Render(context, host.selection);

        await page.Find("[data-review]").KeyDownAsync("d");

        Assert.NotNull(page.Find("[data-plan]"));
        Assert.Empty(page.FindAll("section[data-diff]"));
        Assert.Empty(page.FindAll("[data-notice]"));
        Assert.Empty(host.plans.DiffCalls);
        Assert.Equal(["rev-9"], host.plans.RevisionReads);
    }

    private static (FakePlans plans, FakeReviews reviews, FakeSelection selection) Continued(string? markdown = null)
    {
        FakePlans plans = new();
        plans.Revisions["rev-4"] = Detail("rev-4", 4, "# Old\n", null);
        plans.Revisions["rev-9"] = Detail("rev-9", 9, markdown ?? "# Storage\n", "rev-4");
        FakeReviews reviews = new();
        reviews.Review = Review(
            "rev-9",
            ReviewStatus.Pending,
            [new ReviewPrompt("storage", PromptKind.Choice, "Which store?", ["SQLite"])]);
        return (plans, reviews, new FakeSelection());
    }

    private static IRenderedComponent<ReviewPage> Render(BunitContext context, FakeSelection selection)
    {
        context.Services.AddSingleton<ISelectionReader>(selection);
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-review]")));
        return page;
    }

    private static void AssertPlanSource(IRenderedComponent<ReviewPage> page)
    {
        IElement plan = page.Find("[data-plan]");
        Assert.Contains("int x = 1;", plan.TextContent, StringComparison.Ordinal);
        IElement decision = plan.QuerySelector("[data-decision-block='storage']")!;
        Assert.NotNull(decision);
        Assert.Equal("Which store?", decision.QuerySelector("[data-decision-question]")!.TextContent);
        Assert.Equal(["SQLite", "Other"], decision.QuerySelectorAll("[data-decision-option]").Select(option => option.TextContent));
        Assert.Empty(decision.QuerySelectorAll("pre, code"));
        Assert.Contains("graph TD", plan.TextContent, StringComparison.Ordinal);
        Assert.Equal("Unified diff", plan.QuerySelector("[data-diff] .diff-title")!.TextContent);
        Assert.Equal(" new", plan.QuerySelector(".diff-add .diff-content")!.TextContent);
        Assert.Equal("+", plan.QuerySelector(".diff-add .diff-marker")!.TextContent);
        Assert.Empty(plan.QuerySelectorAll("svg"));
    }

    private static void AssertChrome(IRenderedComponent<ReviewPage> page, bool annotations = true)
    {
        IElement context = page.Find("[data-review-context]");
        Assert.DoesNotContain("Keep one file", context.TextContent, StringComparison.Ordinal);
        Assert.Contains("The file stays on disk", context.TextContent, StringComparison.Ordinal);
        Assert.Equal("https://example.com/story", context.QuerySelector("a")!.GetAttribute("href"));
        Assert.Equal("Which store?", page.Find("[data-prompt-text]").TextContent);
        Assert.NotNull(page.Find("[data-approve]"));
        Assert.NotNull(page.Find("[data-request-changes]"));
        if (annotations)
        {
            Assert.Equal("Deletion", page.Find("[data-annotations] li").GetAttribute("data-kind"));
        }
    }

    private static DiffOutcome.Lines SampleLines() =>
        new(
        [
            new DiffLine.Equal("same"),
            new DiffLine.Removed("gone"),
            new DiffLine.Added("<script>alert(1)</script>"),
        ]);

    private static RevisionDetail Detail(string id, int number, string markdown, string? parent) =>
        new(
            new PlanRevisionId(id),
            new PlanId("plan-1"),
            number,
            markdown,
            "Keep one file",
            "https://example.com/story",
            "The file stays on disk",
            [],
            parent);

    private static ReviewDetail Review(
        string revision,
        ReviewStatus status,
        IReadOnlyList<ReviewPrompt>? prompts = null) =>
        new(
            new ReviewId("review-1"),
            new ReviewRevisionId(revision),
            status,
            null,
            [],
            [],
            prompts ?? [],
            DateTimeOffset.UnixEpoch,
            status == ReviewStatus.Pending ? null : DateTimeOffset.UnixEpoch);
}