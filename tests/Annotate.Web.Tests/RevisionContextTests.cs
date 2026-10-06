using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web.Components.Pages;

using Bunit;

namespace Annotate.Web.Tests;

public sealed class RevisionContextTests
{
    [Theory]
    [InlineData("plan")]
    [InlineData("revision")]
    public void RevisionShowsCriteriaAnnotationsAndCollapsedChanges(string surface)
    {
        FakePlans plans = new();
        plans.Revisions["rev-1"] = new RevisionDetail(
            new Annotate.Plans.Application.RevisionId("rev-1"), new PlanId("plan-1"), 1,
            "Keep this text\n", "Context summary", "https://example.com/story", "All criteria are visible.",
            [new("block", Annotate.Markdown.BlockKind.Paragraph, "", 0, 14, BlockChange.Added, "hash")], null);
        plans.PlansById["plan-1"] = new PlanDetail(new PlanId("plan-1"), new ProjectId("project-1"), "Storage", null,
            [new(new Annotate.Plans.Application.RevisionId("rev-1"), 1, DateTimeOffset.UnixEpoch)]);
        FakeReviews reviews = new();
        reviews.ReviewsByRevision["rev-1"] = new ReviewDetail(new ReviewId("review-1"),
            new Annotate.Reviews.Application.RevisionId("rev-1"), ReviewStatus.ChangesRequested, null,
            [new("note", AnnotationKind.Replacement, "Keep", "Clearer wording", "Retain", 0, 0, 4, "")], [], [], DateTimeOffset.UnixEpoch, null);
        using BunitContext context = BrowseHost.Open(plans, reviews);
        if (surface == "plan") Check(context.Render<PlanPage>(p => p.Add(c => c.Id, "plan-1")));
        else Check(context.Render<RevisionPage>(p => p.Add(c => c.Id, "rev-1")));
    }

    private static void Check<T>(IRenderedComponent<T> page) where T : Microsoft.AspNetCore.Components.IComponent
    {
        page.WaitForAssertion(() =>
        {
            Assert.Equal("All criteria are visible.", page.Find("[data-criteria] .criteria").TextContent);
            Assert.Empty(page.FindAll("[data-criteria] [data-block-index]"));
            Assert.DoesNotContain("Context summary", page.Find("[data-review-context]").TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain(">Summary<", page.Markup, StringComparison.Ordinal);
            Assert.NotNull(page.Find("[data-review-context] a[href='https://example.com/story']"));
            var annotations = page.Find("[data-side-panel='annotations']");
            Assert.Equal("Keep", annotations.QuerySelector("[data-text]")!.TextContent);
            Assert.Equal("Retain", annotations.QuerySelector("[data-replacement]")!.TextContent);
            Assert.Equal("Clearer wording", annotations.QuerySelector("[data-comment]")!.TextContent);
            Assert.Equal("Keep", page.Find("[data-plan] del").TextContent);
            var changes = page.Find("details[data-revision-changes]");
            Assert.False(changes.HasAttribute("open"));
            Assert.Equal("Changes", changes.QuerySelector("summary")!.TextContent);
            Assert.Equal("Keep this text", changes.QuerySelector("[data-changes] p")!.TextContent);
            Assert.NotNull(changes.PreviousElementSibling!.QuerySelector("[data-plan]") ??
                (changes.PreviousElementSibling.HasAttribute("data-plan") ? changes.PreviousElementSibling : null));
            Assert.Empty(page.FindAll("[data-side-panel='changes'], [data-panel-trigger='changes']"));
        });
    }
}