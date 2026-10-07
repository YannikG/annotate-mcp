using AngleSharp.Dom;

using Annotate.Markdown;
using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;
using Annotate.Web.Components.Browse;
using Annotate.Web.Components.Pages;
using Annotate.Web.Components.Review;
using Annotate.Web.Components.Ui;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using PlanRevisionId = Annotate.Plans.Application.RevisionId;
using ReviewRevisionId = Annotate.Reviews.Application.RevisionId;

namespace Annotate.Web.Tests;

public sealed class CopyTests
{
    [Fact]
    public async Task HeadingCopiesReviewAndBlockKey()
    {
        (BunitContext context, FakeClipboard clipboard, ToastHub toast) host = Host();
        using BunitContext context = host.context;
        IRenderedComponent<PlanText> page = context.Render<PlanText>(parameters => parameters
            .Add(component => component.Markdown, "# Title\n\nWords here.\n")
            .Add(component => component.ReviewId, "rev")
            .Add(component => component.BlockKeys, (IReadOnlyList<string>)["key", "para"]));

        Assert.Equal("Title", page.Find("h1").TextContent.Replace("Copies the review id and this block id", "").Trim());
        IElement copy = page.Find("[data-copy-block]");
        Assert.Equal("Copy block ID", copy.GetAttribute("aria-label"));
        Assert.Contains("btn-quiet", copy.ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("btn-icon", copy.ClassName, StringComparison.Ordinal);
        Assert.Equal("Copies the review id and this block id", page.Find("[data-copy-tip]").TextContent);
        Assert.Empty(page.FindAll("p [data-copy-block]"));
        await page.Find("[data-copy-block]").ClickAsync();

        Assert.Equal("reviewId: rev, blockId: key", host.clipboard.Text);
        Assert.Equal("Block ID copied", host.toast.Message);
    }

    [Fact]
    public void QuotedHeadingHasNoCopyButton()
    {
        using BunitContext context = Host().context;
        IRenderedComponent<PlanText> page = context.Render<PlanText>(parameters => parameters
            .Add(component => component.Markdown, "> # Nested\n")
            .Add(component => component.ReviewId, "rev")
            .Add(component => component.BlockKeys, (IReadOnlyList<string>)["quote-key"]));

        Assert.Empty(page.FindAll("[data-copy-block]"));
    }

    [Fact]
    public void HeadingCopyIsAbsentWithoutAReview()
    {
        using BunitContext context = Host().context;
        IRenderedComponent<PlanText> page = context.Render<PlanText>(parameters => parameters
            .Add(component => component.Markdown, "# Title\n")
            .Add(component => component.BlockKeys, (IReadOnlyList<string>)["key"]));

        Assert.Empty(page.FindAll("[data-copy-block]"));
    }

    [Fact]
    public async Task ToolbarCopiesPlanAndReview()
    {
        (BunitContext context, FakeClipboard clipboard, ToastHub toast) host = Host();
        using BunitContext context = host.context;
        IRenderedComponent<CopyIds> menu = context.Render<CopyIds>(parameters => parameters
            .Add(component => component.PlanId, "plan-1")
            .Add(component => component.ReviewId, "rev-1"));

        await menu.Find("[data-copy-plan]").ClickAsync();
        Assert.Equal("planId: plan-1", host.clipboard.Text);
        Assert.Equal("Plan ID copied", host.toast.Message);

        await menu.Find("[data-copy-review]").ClickAsync();
        Assert.Equal("reviewId: rev-1", host.clipboard.Text);
        Assert.Equal("Review ID copied", host.toast.Message);
    }

    [Fact]
    public void ToolbarMenuUsesTheSharedDropdown()
    {
        using BunitContext context = Host().context;
        IRenderedComponent<CopyIds> menu = context.Render<CopyIds>(parameters => parameters
            .Add(component => component.PlanId, "plan-1")
            .Add(component => component.ReviewId, "rev-1"));

        IElement list = menu.Find(".split-menu");
        Assert.Equal("menu", list.GetAttribute("role"));
        Assert.Equal("menu", menu.Find("[data-copy-trigger]").GetAttribute("aria-haspopup"));
        foreach (IElement item in list.QuerySelectorAll("[data-copy-plan], [data-copy-review]"))
        {
            Assert.Equal("split-item", item.ClassName);
            Assert.Equal("menuitem", item.GetAttribute("role"));
        }
    }

    [Fact]
    public void ToolbarOmitsReviewWhenTheRevisionHasNone()
    {
        using BunitContext context = Host().context;
        IRenderedComponent<CopyIds> menu = context.Render<CopyIds>(parameters => parameters
            .Add(component => component.PlanId, "plan-1"));

        Assert.NotNull(menu.Find("[data-copy-plan]"));
        Assert.Empty(menu.FindAll("[data-copy-review]"));
    }

    [Fact]
    public async Task ClipboardFailureShowsCouldNotCopy()
    {
        (BunitContext context, FakeClipboard clipboard, ToastHub toast) host = Host();
        using BunitContext context = host.context;
        host.clipboard.Fails = true;
        IRenderedComponent<CopyIds> menu = context.Render<CopyIds>(parameters => parameters
            .Add(component => component.PlanId, "plan-1")
            .Add(component => component.ReviewId, "rev-1"));

        await menu.Find("[data-copy-plan]").ClickAsync();

        Assert.Null(host.clipboard.Text);
        Assert.Equal("Could not copy", host.toast.Message);
    }

    [Fact]
    public void SecondToastReplacesTheFirst()
    {
        ToastHub hub = new(TimeSpan.FromHours(1));
        using BunitContext context = new();
        context.Services.AddSingleton<IToast>(hub);
        IRenderedComponent<ToastHost> host = context.Render<ToastHost>();

        hub.Show("Plan ID copied");
        host.WaitForAssertion(() => Assert.Equal("Plan ID copied", host.Find("[data-toast]").TextContent));
        hub.Show("Review ID copied");
        host.WaitForAssertion(() => Assert.Equal("Review ID copied", host.Find("[data-toast]").TextContent));
        Assert.Equal("status", host.Find("[data-toast]").GetAttribute("role"));
    }

    [Fact]
    public async Task ToastLeavesOnItsOwn()
    {
        Assert.Equal(TimeSpan.FromSeconds(4), ToastHub.DefaultLifetime);
        ToastHub hub = new(TimeSpan.FromMilliseconds(400));
        hub.Show("Plan ID copied");
        await Task.Delay(250);
        hub.Show("Review ID copied");
        await Task.Delay(250);
        Assert.Equal("Review ID copied", hub.Message);
        await Task.Delay(300);
        Assert.Null(hub.Message);
    }

    [Fact]
    public void CopyStylesAreHoverOnly()
    {
        string css = File.ReadAllText(Path.Combine(Root(), "src", "Annotate.Web", "wwwroot", "css", "copy.css"));
        string app = File.ReadAllText(Path.Combine(Root(), "src", "Annotate.Web", "Components", "App.razor"));

        Assert.Contains("@media (hover: hover) and (pointer: fine)", css, StringComparison.Ordinal);
        Assert.Contains(".copy-ids", css, StringComparison.Ordinal);
        Assert.Contains(".copy-anchor", css, StringComparison.Ordinal);
        Assert.Contains("color: var(--nav-accent)", css, StringComparison.Ordinal);
        Assert.Contains("opacity: 0.5", css, StringComparison.Ordinal);
        Assert.Contains(".copy-anchor svg {\n        width: var(--space-5)", css, StringComparison.Ordinal);
        Assert.DoesNotContain(":focus", css, StringComparison.Ordinal);
        Assert.Contains("css/copy.css", app, StringComparison.Ordinal);
        Assert.Contains("js/clipboard.js", app, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlanPageCopiesTheReviewOnScreen()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 1));
        plans.PlansById["plan-1"] = new PlanDetail(
            new PlanId("plan-1"),
            new ProjectId("project-1"),
            "Storage",
            null,
            [
                new PlanRevision(new PlanRevisionId("rev-1"), 1, At(1)),
                new PlanRevision(new PlanRevisionId("rev-2"), 2, At(2)),
            ]);
        plans.Revisions["rev-1"] = Revision("rev-1", 1, "# One\n", "key-1");
        plans.Revisions["rev-2"] = Revision("rev-2", 2, "# Two\n", "key-2");
        FakeReviews reviews = new();
        reviews.ReviewsByRevision["rev-1"] = PlanReview("rev-1");
        reviews.ReviewsByRevision["rev-2"] = PlanReview("rev-2");

        (BunitContext context, FakeClipboard clipboard, ToastHub _) host = Host(plans, reviews);
        using BunitContext context = host.context;
        IRenderedComponent<PlanPage> page = context.Render<PlanPage>(parameters =>
            parameters.Add(component => component.Id, "plan-1"));

        page.WaitForAssertion(() => Assert.Contains("Two", page.Find("[data-plan]").TextContent, StringComparison.Ordinal));
        await page.Find("[data-copy-review]").ClickAsync();
        Assert.Equal("reviewId: review-rev-2", host.clipboard.Text);
        await page.Find("[data-copy-block]").ClickAsync();
        Assert.Equal("reviewId: review-rev-2, blockId: key-2", host.clipboard.Text);

        await page.Find("[data-revision] [data-select-trigger]").ClickAsync();
        await page.FindAll("[role='option']")[0].ClickAsync();
        page.WaitForAssertion(() => Assert.Contains("One", page.Find("[data-plan]").TextContent, StringComparison.Ordinal));
        await page.Find("[data-copy-review]").ClickAsync();
        Assert.Equal("reviewId: review-rev-1", host.clipboard.Text);
        await page.Find("[data-copy-block]").ClickAsync();
        Assert.Equal("reviewId: review-rev-1, blockId: key-1", host.clipboard.Text);
    }

    [Fact]
    public async Task ReviewPageCopiesPlanReviewAndBlock()
    {
        FakePlans plans = new();
        plans.Revisions["rev-1"] = Revision("rev-1", 1, "# Storage\n", "key-9");
        FakeReviews reviews = new()
        {
            Review = new ReviewDetail(
                new ReviewId("review-9"),
                new ReviewRevisionId("rev-1"),
                ReviewStatus.Approved,
                null,
                [],
                [],
                [],
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch),
        };

        (BunitContext context, FakeClipboard clipboard, ToastHub _) host = Host(plans, reviews);
        using BunitContext context = host.context;
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection());
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/review/review-9");
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-9"));

        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-copy-block]")));
        await page.Find("[data-copy-plan]").ClickAsync();
        Assert.Equal("planId: plan-1", host.clipboard.Text);
        await page.Find("[data-copy-block]").ClickAsync();
        Assert.Equal("reviewId: review-9, blockId: key-9", host.clipboard.Text);
    }

    [Fact]
    public void RevisionPageWithoutAReviewCopiesOnlyThePlan()
    {
        FakePlans plans = new();
        plans.Revisions["rev-1"] = Revision("rev-1", 1, "# Storage\n", "key-1");

        using BunitContext context = Host(plans, new FakeReviews()).context;
        IRenderedComponent<RevisionPage> page = context.Render<RevisionPage>(parameters =>
            parameters.Add(component => component.Id, "rev-1"));

        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-copy-plan]")));
        Assert.Empty(page.FindAll("[data-copy-review]"));
        Assert.Empty(page.FindAll("[data-copy-block]"));
    }

    private static (BunitContext context, FakeClipboard clipboard, ToastHub toast) Host(
        IPlans? plans = null, IReviews? reviews = null)
    {
        BunitContext context = plans is null
            ? new BunitContext()
            : BrowseHost.Open(plans, reviews ?? new FakeReviews());
        FakeClipboard clipboard = new();
        ToastHub toast = new(TimeSpan.FromHours(1));
        context.Services.AddSingleton<IClipboard>(clipboard);
        context.Services.AddSingleton<IToast>(toast);
        return (context, clipboard, toast);
    }

    private static RevisionDetail Revision(string id, int number, string markdown, string key) =>
        new(
            new PlanRevisionId(id),
            new PlanId("plan-1"),
            number,
            markdown,
            null,
            null,
            null,
            [new RevisionBlock(key, BlockKind.Heading, "", 0, markdown.Length, BlockChange.Added, key)],
            null);

    private static ReviewDetail PlanReview(string revisionId) =>
        new(
            new ReviewId("review-" + revisionId),
            new ReviewRevisionId(revisionId),
            ReviewStatus.Pending,
            null,
            [],
            [],
            [],
            DateTimeOffset.UnixEpoch,
            null);

    private static DateTimeOffset At(int day) => new(2026, 10, day, 8, 0, 0, TimeSpan.Zero);

    private static string Root()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Annotate.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate Annotate.slnx by walking up from the test output.");
    }

    private sealed class FakeClipboard : IClipboard
    {
        public string? Text { get; private set; }

        public bool Fails { get; set; }

        public Task<bool> CopyAsync(string text)
        {
            if (Fails)
            {
                return Task.FromResult(false);
            }

            Text = text;
            return Task.FromResult(true);
        }
    }
}