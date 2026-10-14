using AngleSharp.Dom;

using Annotate.Markdown;
using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;
using Annotate.Web.Components.Pages;
using Annotate.Web.Components.Ui;

using Bunit;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

using PlanRevisionId = Annotate.Plans.Application.RevisionId;
using ReviewRevisionId = Annotate.Reviews.Application.RevisionId;

namespace Annotate.Web.Tests;

public sealed class LockedBlockToastTests
{
    [Fact]
    public async Task SelectingALockedBlockShowsAToast()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 0));
        FakeDocuments documents = new();
        documents.Documents["doc-1"] = new DocumentDetail(
            new DocumentId("doc-1"),
            new ProjectId("project-1"),
            "Audit",
            null,
            [],
            false,
            false,
            ["heading"]);
        documents.Revisions["rev-1"] = new DocumentRevisionDetail(
            new PlanRevisionId("rev-1"),
            new DocumentId("doc-1"),
            1,
            "# Method\n\nOpen the notes.\n",
            null,
            [
                new RevisionBlock("heading", BlockKind.Heading, "Method", 0, 9, BlockChange.Unchanged, "h", true),
                new RevisionBlock("body", BlockKind.Paragraph, "Method", 11, 26, BlockChange.Unchanged, "b", true),
            ],
            null,
            null,
            false);
        FakeReviews reviews = new()
        {
            Review = new ReviewDetail(
                new ReviewId("review-1"),
                new ReviewRevisionId("rev-1"),
                ReviewStatus.Pending,
                null,
                [],
                [],
                [],
                DateTimeOffset.UnixEpoch,
                null),
        };
        FakeSelection selection = new()
        {
            Next = new TextSelection(1, 0, 4, "Open", new SelectionBox(180, 48, 200, 60)),
        };
        ToastHub toast = new(TimeSpan.FromHours(1));

        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<IDocuments>(documents);
        context.Services.AddSingleton<ISelectionReader>(selection);
        context.Services.AddSingleton<IToast>(toast);
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));
        IRenderedComponent<ToastHost> host = context.Render<ToastHost>();
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-review]")));

        await page.Find(".document").TriggerEventAsync("onmouseup", new MouseEventArgs());

        host.WaitForAssertion(() => Assert.Equal("This block is locked.", host.Find("[data-toast]").TextContent));
        Assert.Empty(page.FindAll("[data-annotate-toolbar]"));
        Assert.Empty(page.FindAll("[data-notice]"));
        Assert.Empty(page.FindAll("[data-note-dialog]"));

        await page.Find("[data-review]").KeyDownAsync("c");

        Assert.Equal("This block is locked.", host.Find("[data-toast]").TextContent);
        Assert.Empty(page.FindAll("[data-note-dialog]"));
    }
}