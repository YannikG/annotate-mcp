using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;
using Annotate.Web.Components.Pages;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using PlanRevisionId = Annotate.Plans.Application.RevisionId;
using ReviewRevisionId = Annotate.Reviews.Application.RevisionId;

namespace Annotate.Web.Tests;

public sealed class AnnotationRenderingTests
{
    [Theory]
    [InlineData("d", "Deletion")]
    [InlineData("r", "Replacement")]
    [InlineData("s", "Insertion")]
    [InlineData("c", "Comment")]
    public async Task DraftEditsAppearInThePlanAndUndoRemovesThem(string key, string kind)
    {
        FakePlans plans = Plans();
        FakeReviews reviews = Reviews([]);
        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection { Next = new(0, 2, 6, "Keep") });
        var page = context.Render<ReviewPage>(p => p.Add(c => c.Id, "review"));
        await page.Find("[data-review]").KeyDownAsync(key);
        if (key != "d")
        {
            page.Find("[data-note]").Input("new <script>text</script>");
            await page.Find("[data-save-note]").ClickAsync();
        }

        var mark = page.Find($"[data-plan] [data-annotation-kinds~='{kind}']");
        Assert.Equal("Keep", mark.TextContent);
        Assert.Equal("2", mark.QuerySelector("[data-seg-start]")!.GetAttribute("data-seg-start"));
        Assert.Equal("6", mark.QuerySelector("[data-seg-end]")!.GetAttribute("data-seg-end"));
        if (key is "d" or "r") Assert.NotNull(mark.QuerySelector("del"));
        if (key is "r" or "s")
        {
            Assert.Equal("new <script>text</script>", page.Find("[data-plan] ins[data-annotation-extra]").TextContent);
            Assert.Empty(page.FindAll("[data-plan] ins [data-block-index]"));
        }
        if (key == "c") Assert.Contains("new <script>text</script>", mark.GetAttribute("title"), StringComparison.Ordinal);
        Assert.Empty(page.FindAll("[data-plan] script"));

        await page.Find("[data-review]").KeyDownAsync("u");
        Assert.Empty(page.FindAll("[data-plan] [data-annotation-kinds], [data-plan] [data-annotation-extra]"));
        Assert.Equal("Keep me", page.Find("[data-plan] h1").TextContent);
    }

    [Fact]
    public void SavedAnnotationsSpanFormattingAndAppearOnReviewPlanAndRevision()
    {
        FakePlans plans = Plans();
        string source = plans.Revisions["revision"]!.Markdown;
        int start = source.IndexOf("Keep", 10, StringComparison.Ordinal);
        int end = source.IndexOf("this", start, StringComparison.Ordinal) + 4;
        Annotation annotation = new("saved", AnnotationKind.Replacement, "Keep this", null, "Use that", 1, start, end, "");
        FakeReviews reviews = Reviews([annotation]);
        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection());

        var review = context.Render<ReviewPage>(p => p.Add(c => c.Id, "review"));
        Assert.Equal(2, review.FindAll("[data-plan] [data-annotation-kinds~='Replacement']").Count);
        Assert.Single(review.FindAll("[data-plan] ins[data-annotation-extra]"));
        Assert.Equal("Use that", review.Find("[data-plan] ins").TextContent);
        Assert.NotNull(review.Find("[data-plan] strong del"));

        var plan = context.Render<PlanPage>(p => p.Add(c => c.Id, "plan"));
        Assert.Equal("Use that", plan.Find("[data-plan] ins[data-annotation-extra]").TextContent);
        var revision = context.Render<RevisionPage>(p => p.Add(c => c.Id, "revision"));
        Assert.Equal("Use that", revision.Find("[data-plan] ins[data-annotation-extra]").TextContent);
    }

    private static FakePlans Plans()
    {
        FakePlans plans = new();
        plans.Revisions["revision"] = new(new PlanRevisionId("revision"), new PlanId("plan"), 1,
            "# Keep me\n\nKeep **this** and `code`.\n", null, null, null, [], null);
        plans.PlansById["plan"] = new(new PlanId("plan"), new ProjectId("project"), "Plan", null,
            [new(new PlanRevisionId("revision"), 1, DateTimeOffset.UnixEpoch)]);
        return plans;
    }

    private static FakeReviews Reviews(IReadOnlyList<Annotation> annotations)
    {
        ReviewDetail detail = new(new ReviewId("review"), new ReviewRevisionId("revision"), ReviewStatus.Pending,
            null, annotations, [], [], DateTimeOffset.UnixEpoch, null);
        FakeReviews reviews = new() { Review = detail };
        reviews.ReviewsByRevision["revision"] = detail;
        return reviews;
    }
}