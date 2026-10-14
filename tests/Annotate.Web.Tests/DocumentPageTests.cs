using AngleSharp.Dom;

using Annotate.Plans.Application;
using Annotate.Reviews.Application;
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