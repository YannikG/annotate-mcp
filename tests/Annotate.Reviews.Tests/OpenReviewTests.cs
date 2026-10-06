using Annotate.Reviews.Application;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Annotate.Reviews.Tests;

public sealed class OpenReviewTests
{
    [Fact]
    public async Task OpenAndFindReturnsTheSameReviewForOneRevision()
    {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 10, 2, 12, 0, 0, TimeSpan.Zero));
        await using OpenReviews open = await OpenReviews.Open();
        await using ServiceProvider host = open.Reviews(time);
        IReviews reviews = host.GetRequiredService<IReviews>();
        RevisionId firstRevision = new("11111111-1111-1111-1111-111111111111");
        RevisionId secondRevision = new("22222222-2222-2222-2222-222222222222");

        OpenOutcome.Opened opened = Assert.IsType<OpenOutcome.Opened>(
            await reviews.OpenAsync(firstRevision, PlanSamples.Decisions, CancellationToken.None));
        OpenOutcome.Opened again = Assert.IsType<OpenOutcome.Opened>(
            await reviews.OpenAsync(firstRevision, "# Different\n", CancellationToken.None));
        Assert.Equal(opened.Id, again.Id);

        ReviewDetail? found = await reviews.FindAsync(opened.Id, CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal(opened.Id, found.Id);
        Assert.Equal(firstRevision, found.RevisionId);
        Assert.Equal(ReviewStatus.Pending, found.Status);
        Assert.Null(found.Feedback);
        Assert.Null(found.DecidedAt);
        Assert.Equal(time.GetUtcNow(), found.CreatedAt);
        Assert.Empty(found.Annotations);
        Assert.Empty(found.Answers);
        Assert.Equal(2, found.Prompts.Count);
        Assert.Equal("storage", found.Prompts[0].Id);
        Assert.Equal(PromptKind.Choice, found.Prompts[0].Kind);
        Assert.Equal("Which store?", found.Prompts[0].Prompt);
        Assert.Equal(["SQLite", "Postgres"], found.Prompts[0].Options);
        Assert.Equal("note", found.Prompts[1].Id);
        Assert.Equal(PromptKind.Text, found.Prompts[1].Kind);
        Assert.Equal("Anything else?", found.Prompts[1].Prompt);
        Assert.Empty(found.Prompts[1].Options);

        ReviewDetail? byRevision = await reviews.ForRevisionAsync(firstRevision, CancellationToken.None);
        Assert.NotNull(byRevision);
        Assert.Equal(found.Id, byRevision.Id);

        OpenOutcome.Refused invalid = Assert.IsType<OpenOutcome.Refused>(
            await reviews.OpenAsync(secondRevision, PlanSamples.InvalidDecision, CancellationToken.None));
        Assert.Equal("Error: decision block 1: missing id", invalid.Error);
        Assert.Null(await reviews.ForRevisionAsync(secondRevision, CancellationToken.None));
        Assert.Null(await reviews.FindAsync(new ReviewId("missing"), CancellationToken.None));

        time.Advance(TimeSpan.FromMinutes(1));
        OpenOutcome.Opened second = Assert.IsType<OpenOutcome.Opened>(
            await reviews.OpenAsync(secondRevision, PlanSamples.Plain, CancellationToken.None));
        Assert.NotEqual(opened.Id, second.Id);

        IReadOnlyList<PendingReview> pending = await reviews.PendingAsync(CancellationToken.None);
        Assert.Equal([second.Id, opened.Id], pending.Select(item => item.Id).ToArray());
        Assert.Equal([secondRevision, firstRevision], pending.Select(item => item.RevisionId).ToArray());
        Assert.Equal(time.GetUtcNow(), pending[0].CreatedAt);
    }
}