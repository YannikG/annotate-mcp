using Annotate.Reviews.Application;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Annotate.Reviews.Tests;

public sealed class WaitReviewTests
{
    [Fact]
    public async Task WaitTimesOutReturnsStoredDecisionAndWakesWhenDecided()
    {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 10, 2, 12, 0, 0, TimeSpan.Zero));
        await using OpenReviews open = await OpenReviews.Open();
        await using ServiceProvider host = open.Reviews(time);
        IReviews reviews = host.GetRequiredService<IReviews>();

        Assert.IsType<WaitOutcome.Missing>(
            await reviews.WaitAsync(new ReviewId("missing"), TimeSpan.FromSeconds(1), CancellationToken.None));

        ReviewId pending = await Opened(reviews, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Task<WaitOutcome> waiting = reviews.WaitAsync(pending, TimeSpan.FromSeconds(30), CancellationToken.None);
        time.Advance(TimeSpan.FromSeconds(30));
        Task timeoutFinished = await Task.WhenAny(waiting, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(waiting, timeoutFinished);
        Assert.IsType<WaitOutcome.Pending>(await waiting);

        ReviewId stored = await Opened(reviews, "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Assert.IsType<DecideOutcome.Done>(await reviews.ApproveAsync(stored, CancellationToken.None));
        WaitOutcome.Decided decided = Assert.IsType<WaitOutcome.Decided>(
            await reviews.WaitAsync(stored, TimeSpan.FromMinutes(5), CancellationToken.None));
        Assert.IsType<ReviewDecision.Approved>(decided.Decision);

        ReviewId live = await Opened(reviews, "cccccccc-cccc-cccc-cccc-cccccccccccc");
        Task<WaitOutcome> during = reviews.WaitAsync(live, TimeSpan.FromMinutes(5), CancellationToken.None);
        Assert.IsType<DecideOutcome.Done>(await reviews.RequestChangesAsync(
            live,
            [new Annotation("del", AnnotationKind.Deletion, "old line", null, null, 0, 0, 1, "t1")],
            CancellationToken.None));
        Task wokeFinished = await Task.WhenAny(during, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(during, wokeFinished);
        WaitOutcome.Decided woke = Assert.IsType<WaitOutcome.Decided>(await during);
        ReviewDecision.ChangesRequested changes = Assert.IsType<ReviewDecision.ChangesRequested>(woke.Decision);
        Assert.Contains("{--old line--}}{id=\"del\" by=\"user\" at=\"t1\"}", changes.Feedback, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WaitOnANewProviderReturnsAStoredDecision()
    {
        await using OpenReviews open = await OpenReviews.Open();
        await using ServiceProvider first = open.Reviews();
        IReviews reviews = first.GetRequiredService<IReviews>();
        ReviewId id = await Opened(reviews, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Assert.IsType<DecideOutcome.Done>(await reviews.ApproveAsync(id, CancellationToken.None));

        await using ServiceProvider second = open.Reviews();
        IReviews again = second.GetRequiredService<IReviews>();
        Task<WaitOutcome> waiting = again.WaitAsync(id, TimeSpan.FromMinutes(5), CancellationToken.None);
        Task finished = await Task.WhenAny(waiting, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(waiting, finished);
        WaitOutcome.Decided decided = Assert.IsType<WaitOutcome.Decided>(await waiting);
        Assert.IsType<ReviewDecision.Approved>(decided.Decision);
    }

    private static async Task<ReviewId> Opened(IReviews reviews, string revision)
    {
        OpenOutcome.Opened opened = Assert.IsType<OpenOutcome.Opened>(
            await reviews.OpenAsync(new RevisionId(revision), PlanSamples.Plain, CancellationToken.None));
        return opened.Id;
    }
}