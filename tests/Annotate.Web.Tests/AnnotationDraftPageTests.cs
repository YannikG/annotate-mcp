using Annotate.Plans.Application;
using Annotate.Reviews;
using Annotate.Reviews.Application;
using Annotate.Web;
using Annotate.Web.Components.Pages;

using Bunit;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Web.Tests;

public sealed class AnnotationDraftPageTests
{
    [Theory]
    [InlineData("d")]
    [InlineData("r")]
    [InlineData("s")]
    [InlineData("c")]
    public async Task EditsPersistBeforeSubmitAndSurviveNavigationAndUndo(string key)
    {
        string connectionString = $"Data Source=file:drafts-{Guid.NewGuid():N}?mode=memory&cache=shared";
        await using SqliteConnection connection = new(connectionString);
        await connection.OpenAsync();
        using BunitContext context = new();
        context.Services.AddReviews(new ReviewsSettings(connectionString));
        FakePlans plans = new();
        plans.Revisions["revision"] = new(new("revision"), new PlanId("plan"), 1, "# Keep me", null, null, null, [], null);
        context.Services.AddSingleton<IPlans>(plans);
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection { Next = new(0, 2, 6, "Keep") });
        context.Services.AddSingleton<IAutoClosePreference>(new MemoryAutoClose());
        context.Services.AddSingleton<IReportScript>(new RecordingReport());
        IReviews reviews = context.Services.GetRequiredService<IReviews>();
        ReviewId id = Assert.IsType<OpenOutcome.Opened>(await reviews.OpenAsync(new("revision"), "# Keep me", CancellationToken.None)).Id;
        var page = context.Render<ReviewPage>(p => p.Add(c => c.Id, id.Value));
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-review]")));
        await page.Find("[data-review]").KeyDownAsync(key);
        if (key != "d")
        {
            await page.Find("[data-note]").InputAsync("first draft");
            string firstId = Assert.Single((await reviews.FindAsync(id, CancellationToken.None))!.Annotations).Id;
            await page.Find("[data-note]").InputAsync("");
            Assert.Empty((await reviews.FindAsync(id, CancellationToken.None))!.Annotations);
            await page.Find("[data-note]").InputAsync("edited draft");
            Assert.Equal(firstId, Assert.Single((await reviews.FindAsync(id, CancellationToken.None))!.Annotations).Id);
        }
        ReviewDetail saved = (await reviews.FindAsync(id, CancellationToken.None))!;
        Annotation annotation = Assert.Single(saved.Annotations);
        Assert.Equal(ReviewStatus.Pending, saved.Status);
        Assert.Null(saved.Feedback);
        Assert.Null(saved.DecidedAt);
        if (key != "d") Assert.Equal("edited draft", key == "c" ? annotation.Comment : annotation.Replacement);
        Assert.Equal("Draft saved", page.Find("[data-draft-status]").TextContent);
        page.Dispose();
        var returned = context.Render<ReviewPage>(p => p.Add(c => c.Id, id.Value));
        returned.WaitForAssertion(() => Assert.Single(returned.FindAll("[data-plan] [data-annotation-kinds]")));
        await returned.Find("[data-review]").KeyDownAsync("u");
        Assert.Empty((await reviews.FindAsync(id, CancellationToken.None))!.Annotations);
        returned.Dispose();
        var afterUndo = context.Render<ReviewPage>(p => p.Add(c => c.Id, id.Value));
        afterUndo.WaitForAssertion(() => Assert.Empty(afterUndo.FindAll("[data-plan] [data-annotation-kinds]")));
    }

    [Fact]
    public async Task FailedSaveRetainsLocalDraftAndDoesNotClaimItIsSaved()
    {
        FakePlans plans = new();
        plans.Revisions["revision"] = new(new("revision"), new PlanId("plan"), 1, "# Keep me", null, null, null, [], null);
        FakeReviews reviews = new()
        {
            Review = new(new("review"), new("revision"), ReviewStatus.Pending, null, [], [], [], DateTimeOffset.UtcNow, null),
            DraftOutcome = new SaveAnnotationsOutcome.Refused("Review already decided.")
        };
        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection { Next = new(0, 2, 6, "Keep") });
        var page = context.Render<ReviewPage>(p => p.Add(c => c.Id, "review"));
        await page.Find("[data-review]").KeyDownAsync("d");
        Assert.Single(page.FindAll("[data-plan] del"));
        Assert.Contains("Could not save draft", page.Find("[data-draft-status]").TextContent, StringComparison.Ordinal);
        reviews.DraftOutcome = new SaveAnnotationsOutcome.Done();
        await page.Find("[data-retry-draft]").ClickAsync();
        Assert.Equal("Draft saved", page.Find("[data-draft-status]").TextContent);
        Assert.Single(reviews.Review!.Annotations);
    }
}