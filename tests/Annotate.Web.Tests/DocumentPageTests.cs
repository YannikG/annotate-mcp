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

public sealed class DocumentPageTests
{
    [Fact]
    public async Task DocumentShowsTheRevisionDropdownInTheToolbar()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 0));
        FakeDocuments documents = new();
        documents.Documents["doc-1"] = new DocumentDetail(
            new DocumentId("doc-1"),
            new ProjectId("project-1"),
            "Audit",
            null,
            [
                new PlanRevision(new PlanRevisionId("rev-1"), 1, At(1), new Attribution("Cursor", "grok-4.7", null, null)),
                new PlanRevision(new PlanRevisionId("rev-2"), 2, At(2), new Attribution("Cursor", "grok-4.7", null, null)),
            ],
            false,
            false,
            []);
        documents.Revisions["rev-1"] = Revision("rev-1", 1, "# Audit\n\nFirst draft.\n");
        documents.Revisions["rev-2"] = Revision("rev-2", 2, "# Audit\n\nSecond draft.\n");
        FakeReviews reviews = new();
        reviews.ReviewsByRevision["rev-1"] = Review("rev-1", ReviewStatus.ChangesRequested);
        reviews.ReviewsByRevision["rev-2"] = Review("rev-2", ReviewStatus.Pending);

        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<IDocuments>(documents);
        IRenderedComponent<DocumentPage> page = context.Render<DocumentPage>(parameters =>
            parameters.Add(component => component.Id, "doc-1"));

        page.WaitForAssertion(() =>
        {
            IElement toolbar = page.Find("[data-editor-toolbar]");
            IElement picker = toolbar.QuerySelector(".editor-toolbar-leading [data-revision]")!;
            Assert.Equal("v2 · Pending · Cursor · grok-4.7", picker.QuerySelector("[data-select-trigger]")!.TextContent.Trim());
            Assert.Equal("Pending", page.Find(".page-title [data-status]").TextContent.Trim());
            Assert.Equal("pending", page.Find(".page-title [data-status]").GetAttribute("data-state"));
            Assert.Empty(page.FindAll(".page-title [data-revision]"));
            Assert.Contains("Second draft.", page.Find("[data-plan]").TextContent, StringComparison.Ordinal);
        });

        await page.Find("[data-revision] [data-select-trigger]").ClickAsync();
        Assert.Equal(
            ["v1 · Changes requested · Cursor · grok-4.7", "v2 · Pending · Cursor · grok-4.7"],
            page.FindAll("[role='option']").Select(option => option.TextContent.Trim()));
        await page.FindAll("[role='option']")[0].ClickAsync();
        page.WaitForAssertion(() =>
        {
            Assert.Contains("First draft.", page.Find("[data-plan]").TextContent, StringComparison.Ordinal);
            Assert.Equal("Changes requested", page.Find(".page-title [data-status]").TextContent.Trim());
            Assert.Equal("changes", page.Find(".page-title [data-status]").GetAttribute("data-state"));
        });
    }

    [Fact]
    public async Task DownloadingTheReportSavesTheSelectedRevision()
    {
        (FakePlans plans, FakeDocuments documents, FakeReviews reviews) = Host();
        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<IDocuments>(documents);
        RecordingReport report = new();
        context.Services.AddSingleton<IReportScript>(report);
        IRenderedComponent<DocumentPage> page = context.Render<DocumentPage>(parameters =>
            parameters.Add(component => component.Id, "doc-1"));
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-editor-toolbar] [data-download-report]")));

        await page.Find("[data-download-report]").ClickAsync();

        DownloadedReport newest = Assert.Single(report.Downloads);
        Assert.Equal("document.html", newest.FileName);
        Assert.Equal(
            PlanReport.Html("Audit", null, null, "# Audit\n\nSecond draft.\n", "Document report", "Document", "Document"),
            newest.Html);
        Assert.Empty(page.FindAll("[data-report-error]"));

        await page.Find("[data-revision] [data-select-trigger]").ClickAsync();
        await page.FindAll("[role='option']")[0].ClickAsync();
        page.WaitForAssertion(() =>
            Assert.Contains("First draft.", page.Find("[data-plan]").TextContent, StringComparison.Ordinal));
        await page.Find("[data-download-report]").ClickAsync();

        Assert.Equal(
            PlanReport.Html("Audit", null, null, "# Audit\n\nFirst draft.\n", "Document report", "Document", "Document"),
            report.Downloads[1].Html);
    }

    [Fact]
    public async Task FailedReportDownloadShowsAnAlert()
    {
        (FakePlans plans, FakeDocuments documents, FakeReviews reviews) = Host();
        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<IDocuments>(documents);
        context.Services.AddSingleton<IReportScript>(new RecordingReport { Failure = "disk full" });
        IRenderedComponent<DocumentPage> page = context.Render<DocumentPage>(parameters =>
            parameters.Add(component => component.Id, "doc-1"));
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-download-report]")));

        await page.Find("[data-download-report]").ClickAsync();

        IElement alert = page.Find("[data-report-error]");
        Assert.Equal("alert", alert.GetAttribute("role"));
        Assert.Equal("disk full", alert.TextContent);
    }

    private static (FakePlans Plans, FakeDocuments Documents, FakeReviews Reviews) Host()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 0));
        FakeDocuments documents = new();
        documents.Documents["doc-1"] = new DocumentDetail(
            new DocumentId("doc-1"),
            new ProjectId("project-1"),
            "Audit",
            null,
            [
                new PlanRevision(new PlanRevisionId("rev-1"), 1, At(1), new Attribution("Cursor", "grok-4.7", null, null)),
                new PlanRevision(new PlanRevisionId("rev-2"), 2, At(2), new Attribution("Cursor", "grok-4.7", null, null)),
            ],
            false,
            false,
            []);
        documents.Revisions["rev-1"] = Revision("rev-1", 1, "# Audit\n\nFirst draft.\n");
        documents.Revisions["rev-2"] = Revision("rev-2", 2, "# Audit\n\nSecond draft.\n");
        FakeReviews reviews = new();
        reviews.ReviewsByRevision["rev-1"] = Review("rev-1", ReviewStatus.ChangesRequested);
        reviews.ReviewsByRevision["rev-2"] = Review("rev-2", ReviewStatus.Pending);
        return (plans, documents, reviews);
    }

    private static DocumentRevisionDetail Revision(string revisionId, int number, string markdown) =>
        new(
            new PlanRevisionId(revisionId),
            new DocumentId("doc-1"),
            number,
            markdown,
            null,
            [],
            null,
            new Attribution("Cursor", "grok-4.7", null, null),
            false);

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

    private static DateTimeOffset At(int day) => new(2026, 10, day, 8, 0, 0, TimeSpan.Zero);
}