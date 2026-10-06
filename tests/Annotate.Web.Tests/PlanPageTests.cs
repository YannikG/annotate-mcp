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

public sealed class PlanPageTests
{
    [Fact]
    public async Task PlanShowsRevisionNumbersAndReviewStatus()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 1));
        plans.PlansById["plan-1"] = new PlanDetail(
            new PlanId("plan-1"),
            new ProjectId("project-1"),
            "Storage",
            null,
            [
                new PlanRevision(new PlanRevisionId("rev-1"), 1, At(1, 8)),
                new PlanRevision(new PlanRevisionId("rev-2"), 2, At(2, 9)),
                new PlanRevision(new PlanRevisionId("rev-3"), 3, At(2, 10)),
                new PlanRevision(new PlanRevisionId("rev-4"), 4, At(2, 11)),
            ]);
        FakeReviews reviews = new();
        reviews.ReviewsByRevision["rev-1"] = Review("rev-1", ReviewStatus.Pending);
        reviews.ReviewsByRevision["rev-2"] = Review("rev-2", ReviewStatus.Approved);
        reviews.ReviewsByRevision["rev-3"] = Review("rev-3", ReviewStatus.ChangesRequested);
        plans.Revisions["rev-1"] = Detail("rev-1", 1, "First\n");
        plans.Revisions["rev-4"] = Detail("rev-4", 4, "Fourth\n");

        using BunitContext context = BrowseHost.Open(plans, reviews);
        IRenderedComponent<PlanPage> page = context.Render<PlanPage>(parameters =>
            parameters.Add(component => component.Id, "plan-1"));

        page.WaitForAssertion(() =>
        {
            IElement picker = page.Find("[data-revision]");
            Assert.Equal("v4 · No review", picker.QuerySelector("[data-select-trigger]")!.TextContent.Trim());
            Assert.Equal("Storage", page.Find(".page-title h1").TextContent);
            Assert.Equal("Projects", page.Find(".breadcrumb a[href='/projects']").TextContent);
            Assert.Equal("Atlas", page.Find(".breadcrumb a[href='/projects/project-1']").TextContent);
            Assert.Contains("Fourth", page.Find("[data-plan]").TextContent, StringComparison.Ordinal);
        });

        await page.Find("[data-revision] [data-select-trigger]").ClickAsync();
        Assert.Equal(
            ["v1 · Pending", "v2 · Approved", "v3 · Changes requested", "v4 · No review"],
            page.FindAll("[role='option']").Select(option => option.TextContent.Trim()));
        await page.FindAll("[role='option']")[0].ClickAsync();
        page.WaitForAssertion(() =>
            Assert.Contains("First", page.Find("[data-plan]").TextContent, StringComparison.Ordinal));
    }

    [Fact]
    public void PlanWithPendingReviewLinksToTheReview()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 1));
        plans.PlansById["plan-1"] = new PlanDetail(
            new PlanId("plan-1"),
            new ProjectId("project-1"),
            "Storage",
            null,
            [new PlanRevision(new PlanRevisionId("rev-1"), 1, At(1, 8))]);
        FakeReviews reviews = new();
        reviews.ReviewsByRevision["rev-1"] = Review("rev-1", ReviewStatus.Pending);
        plans.Revisions["rev-1"] = Detail("rev-1", 1, "First\n");

        using BunitContext context = BrowseHost.Open(plans, reviews);
        IRenderedComponent<PlanPage> page = context.Render<PlanPage>(parameters =>
            parameters.Add(component => component.Id, "plan-1"));

        page.WaitForAssertion(() =>
        {
            IElement link = page.Find("a[data-continue-review]");
            Assert.Equal("/review/review-rev-1", link.GetAttribute("href"));
            Assert.Equal("Continue review", link.TextContent.Trim());
        });
    }

    [Fact]
    public void PlanWithoutPendingReviewHidesTheContinueLink()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 1));
        plans.PlansById["plan-1"] = new PlanDetail(
            new PlanId("plan-1"),
            new ProjectId("project-1"),
            "Storage",
            null,
            [new PlanRevision(new PlanRevisionId("rev-1"), 1, At(1, 8))]);
        FakeReviews reviews = new();
        reviews.ReviewsByRevision["rev-1"] = Review("rev-1", ReviewStatus.Approved);
        plans.Revisions["rev-1"] = Detail("rev-1", 1, "First\n");

        using BunitContext context = BrowseHost.Open(plans, reviews);
        IRenderedComponent<PlanPage> page = context.Render<PlanPage>(parameters =>
            parameters.Add(component => component.Id, "plan-1"));

        page.WaitForAssertion(() =>
        {
            Assert.NotNull(page.Find("[data-revision]"));
            Assert.Empty(page.FindAll("[data-continue-review]"));
        });
    }

    [Fact]
    public void PlanWithApprovedReviewOffersTheReportDownload()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 1));
        plans.PlansById["plan-1"] = new PlanDetail(
            new PlanId("plan-1"),
            new ProjectId("project-1"),
            "Storage",
            null,
            [new PlanRevision(new PlanRevisionId("rev-1"), 1, At(1, 8))]);
        FakeReviews reviews = new();
        reviews.ReviewsByRevision["rev-1"] = Review("rev-1", ReviewStatus.Approved);
        plans.Revisions["rev-1"] = Detail("rev-1", 1, "First\n");

        using BunitContext context = BrowseHost.Open(plans, reviews);
        IRenderedComponent<PlanPage> page = context.Render<PlanPage>(parameters =>
            parameters.Add(component => component.Id, "plan-1"));

        page.WaitForAssertion(() =>
        {
            IElement button = page.Find("[data-editor-toolbar] [data-download-report]");
            Assert.Equal("Download report", button.TextContent.Trim());
        });
    }

    [Fact]
    public void PlanWithPendingReviewHidesTheReportDownload()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 1));
        plans.PlansById["plan-1"] = new PlanDetail(
            new PlanId("plan-1"),
            new ProjectId("project-1"),
            "Storage",
            null,
            [new PlanRevision(new PlanRevisionId("rev-1"), 1, At(1, 8))]);
        FakeReviews reviews = new();
        reviews.ReviewsByRevision["rev-1"] = Review("rev-1", ReviewStatus.Pending);
        plans.Revisions["rev-1"] = Detail("rev-1", 1, "First\n");

        using BunitContext context = BrowseHost.Open(plans, reviews);
        IRenderedComponent<PlanPage> page = context.Render<PlanPage>(parameters =>
            parameters.Add(component => component.Id, "plan-1"));

        page.WaitForAssertion(() =>
        {
            Assert.NotNull(page.Find("[data-revision]"));
            Assert.Empty(page.FindAll("[data-download-report]"));
        });
    }

    [Fact]
    public async Task SwitchingRevisionsTogglesTheReportDownload()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 1));
        plans.PlansById["plan-1"] = new PlanDetail(
            new PlanId("plan-1"),
            new ProjectId("project-1"),
            "Storage",
            null,
            [
                new PlanRevision(new PlanRevisionId("rev-1"), 1, At(1, 8)),
                new PlanRevision(new PlanRevisionId("rev-2"), 2, At(2, 9)),
            ]);
        FakeReviews reviews = new();
        reviews.ReviewsByRevision["rev-1"] = Review("rev-1", ReviewStatus.Approved);
        plans.Revisions["rev-1"] = Detail("rev-1", 1, "First\n");
        plans.Revisions["rev-2"] = Detail("rev-2", 2, "Second\n");

        using BunitContext context = BrowseHost.Open(plans, reviews);
        IRenderedComponent<PlanPage> page = context.Render<PlanPage>(parameters =>
            parameters.Add(component => component.Id, "plan-1"));

        page.WaitForAssertion(() => Assert.Empty(page.FindAll("[data-download-report]")));
        await ChooseRevision(page, "v1 · Approved");
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-download-report]")));
        await ChooseRevision(page, "v2 · No review");
        page.WaitForAssertion(() => Assert.Empty(page.FindAll("[data-download-report]")));
    }

    private static async Task ChooseRevision(IRenderedComponent<PlanPage> page, string label)
    {
        await page.Find("[data-revision] [data-select-trigger]").ClickAsync();
        await page.FindAll("[role='option']").Single(option => option.TextContent.Trim() == label).ClickAsync();
    }

    [Fact]
    public async Task DownloadingTheReportSavesTheSelectedRevision()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 1));
        plans.PlansById["plan-1"] = new PlanDetail(
            new PlanId("plan-1"),
            new ProjectId("project-1"),
            "Storage",
            null,
            [new PlanRevision(new PlanRevisionId("rev-1"), 1, At(1, 8))]);
        FakeReviews reviews = new();
        reviews.ReviewsByRevision["rev-1"] = Review("rev-1", ReviewStatus.Approved);
        plans.Revisions["rev-1"] = Detail(
            "rev-1", 1, "First\n", "Keep one file", "https://example.com/story", "The file stays on disk");

        using BunitContext context = BrowseHost.Open(plans, reviews);
        RecordingReport report = new();
        context.Services.AddSingleton<IReportScript>(report);
        IRenderedComponent<PlanPage> page = context.Render<PlanPage>(parameters =>
            parameters.Add(component => component.Id, "plan-1"));

        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-download-report]")));
        await page.Find("[data-download-report]").ClickAsync();

        DownloadedReport download = Assert.Single(report.Downloads);
        Assert.Equal("plan.html", download.FileName);
        Assert.Equal(
            PlanReport.Html("Keep one file", "https://example.com/story", "The file stays on disk", "First\n"),
            download.Html);
        Assert.Empty(page.FindAll("[data-report-error]"));
    }

    [Fact]
    public async Task FailedReportDownloadShowsAnAlert()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 1));
        plans.PlansById["plan-1"] = new PlanDetail(
            new PlanId("plan-1"),
            new ProjectId("project-1"),
            "Storage",
            null,
            [new PlanRevision(new PlanRevisionId("rev-1"), 1, At(1, 8))]);
        FakeReviews reviews = new();
        reviews.ReviewsByRevision["rev-1"] = Review("rev-1", ReviewStatus.Approved);
        plans.Revisions["rev-1"] = Detail("rev-1", 1, "First\n");

        using BunitContext context = BrowseHost.Open(plans, reviews);
        RecordingReport report = new() { Failure = "disk full" };
        context.Services.AddSingleton<IReportScript>(report);
        IRenderedComponent<PlanPage> page = context.Render<PlanPage>(parameters =>
            parameters.Add(component => component.Id, "plan-1"));

        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-download-report]")));
        await page.Find("[data-download-report]").ClickAsync();

        IElement alert = page.Find("[data-report-error]");
        Assert.Equal("alert", alert.GetAttribute("role"));
        Assert.Equal("disk full", alert.TextContent);
    }

    [Fact]
    public async Task PlanNamesTheAgentAndModelForEachRevision()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 1));
        plans.PlansById["plan-1"] = new PlanDetail(
            new PlanId("plan-1"),
            new ProjectId("project-1"),
            "Storage",
            null,
            [
                new PlanRevision(new PlanRevisionId("rev-1"), 1, At(1, 8), new Attribution("Cursor", "opus", "cursor-vscode", "1.7.2")),
                new PlanRevision(new PlanRevisionId("rev-2"), 2, At(2, 9), new Attribution("Claude Code", "gpt-5", null, null)),
            ]);
        plans.Revisions["rev-2"] = Detail("rev-2", 2, "Second\n") with
        {
            Attribution = new Attribution("Claude Code", "gpt-5", null, null),
        };

        using BunitContext context = BrowseHost.Open(plans, new FakeReviews());
        IRenderedComponent<PlanPage> page = context.Render<PlanPage>(parameters =>
            parameters.Add(component => component.Id, "plan-1"));

        page.WaitForAssertion(() =>
        {
            Assert.Equal("2 agents · 2 models", page.Find("[data-attribution]").TextContent);
            Assert.Contains("Claude Code", page.Find("[data-select-trigger]").TextContent, StringComparison.Ordinal);
            Assert.Contains("gpt-5", page.Find("[data-select-trigger]").TextContent, StringComparison.Ordinal);
            Assert.Contains("Claude Code · gpt-5", page.Find("[data-written-by]").TextContent, StringComparison.Ordinal);
        });
        await page.Find("[data-revision] [data-select-trigger]").ClickAsync();
        Assert.Contains("Cursor", page.FindAll("[role='option']")[0].TextContent, StringComparison.Ordinal);
    }

    private static RevisionDetail Detail(
        string revisionId, int number, string markdown,
        string? summary = null, string? story = null, string? criteria = null) =>
        new(
            new PlanRevisionId(revisionId),
            new PlanId("plan-1"),
            number,
            markdown,
            summary,
            story,
            criteria,
            [],
            null);

    private static ReviewDetail Review(string revisionId, ReviewStatus status) =>
        new(
            new ReviewId("review-" + revisionId),
            new ReviewRevisionId(revisionId),
            status,
            null,
            [],
            [],
            [],
            DateTimeOffset.UnixEpoch,
            null);

    private static DateTimeOffset At(int day, int hour) =>
        new(2026, 10, day, hour, 0, 0, TimeSpan.Zero);
}