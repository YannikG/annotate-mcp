using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web.Components.Pages;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Web.Tests;

public sealed class NotePauseTests
{
    [Fact]
    public async Task TypingANoteSavesTheLatestTextOnce()
    {
        (BunitContext context, IRenderedComponent<ReviewPage> page, FakeReviews reviews) = Open();
        await using BunitContext _ = context;
        await page.Find("[data-review]").KeyDownAsync("c");
        await page.Find("[data-note]").InputAsync("a");
        await page.Find("[data-note]").InputAsync("ab");
        await page.Find("[data-note]").InputAsync("abc");

        Assert.Empty(reviews.DraftSaves);
        await WaitForSaves(reviews, 1);
        Assert.Equal("abc", Assert.Single(reviews.DraftSaves[0].Annotations).Comment);

        await page.Find("[data-note]").InputAsync("abc");
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        Assert.Single(reviews.DraftSaves);
    }

    [Fact]
    public async Task LeavingTheReviewSavesTheNoteWithoutWaiting()
    {
        (BunitContext context, IRenderedComponent<ReviewPage> page, FakeReviews reviews) = Open();
        await page.Find("[data-review]").KeyDownAsync("c");
        await page.Find("[data-note]").InputAsync("kept");

        Assert.Empty(reviews.DraftSaves);
        await context.DisposeAsync();

        Assert.Equal("kept", Assert.Single(reviews.DraftSaves[0].Annotations).Comment);
    }

    private static async Task WaitForSaves(FakeReviews reviews, int count)
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        while (reviews.DraftSaves.Count < count && started.Elapsed < TimeSpan.FromSeconds(2))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20));
        }
    }

    private static (BunitContext Context, IRenderedComponent<ReviewPage> Page, FakeReviews Reviews) Open()
    {
        FakePlans plans = new();
        plans.Revisions["revision"] = new(new("revision"), new PlanId("plan"), 1, "# Keep me", null, null, null, [], null);
        FakeReviews reviews = new()
        {
            Review = new(new("review"), new("revision"), ReviewStatus.Pending, null, [], [], [], DateTimeOffset.UnixEpoch, null),
        };
        BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection { Next = new(0, 2, 6, "Keep") });
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review"));
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-review]")));
        return (context, page, reviews);
    }
}