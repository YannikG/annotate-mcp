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

public sealed class DocumentReviewGuardTests
{
    [Fact]
    public async Task ReadOnlyStopsDraftsAndRequestChangesInAnOpenReview()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 0));
        FakeDocuments documents = new();
        documents.Documents["doc-1"] = Document(readOnly: false);
        documents.Revisions["rev-1"] = new DocumentRevisionDetail(
            new PlanRevisionId("rev-1"),
            new DocumentId("doc-1"),
            1,
            "# Audit\n",
            null,
            [new RevisionBlock("heading", BlockKind.Heading, "Audit", 0, 7, BlockChange.Unchanged, "h")],
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
        FakeSelection selection = new() { Next = new TextSelection(0, 2, 7, "Audit") };
        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<IDocuments>(documents);
        context.Services.AddSingleton<ISelectionReader>(selection);
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-review]")));
        documents.Documents["doc-1"] = Document(readOnly: true);

        await page.Find("[data-review]").KeyDownAsync("d");

        page.WaitForAssertion(() =>
            Assert.Contains("Document is read-only.", page.Find("[data-draft-status]").TextContent, StringComparison.Ordinal));
        Assert.Empty(reviews.DraftSaves);

        await page.Find("[data-request-changes]").ClickAsync();

        Assert.Empty(reviews.Changes);
        Assert.Equal("Document is read-only.", page.Find("[data-notice]").TextContent);
    }

    private static DocumentDetail Document(bool readOnly) =>
        new(
            new DocumentId("doc-1"),
            new ProjectId("project-1"),
            "Audit",
            null,
            [],
            false,
            readOnly,
            []);
}