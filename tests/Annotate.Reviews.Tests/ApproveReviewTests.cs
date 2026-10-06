using Annotate.Reviews.Application;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Reviews.Tests;

public sealed class ApproveReviewTests
{
    [Fact]
    public async Task ApproveSucceedsOnceAndRefusesADecisionFence()
    {
        await using OpenReviews open = await OpenReviews.Open();
        await using ServiceProvider host = open.Reviews();
        IReviews reviews = host.GetRequiredService<IReviews>();

        ReviewId plain = await Opened(reviews, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", PlanSamples.Plain);
        Assert.IsType<DecideOutcome.Done>(await reviews.ApproveAsync(plain, CancellationToken.None));
        ReviewDetail approved = await Required(reviews, plain);
        DecideOutcome.Refused second = Assert.IsType<DecideOutcome.Refused>(
            await reviews.ApproveAsync(plain, CancellationToken.None));
        Assert.Equal("Review already decided.", second.Error);
        ReviewDetail stayed = await Required(reviews, plain);
        Assert.Equal(ReviewStatus.Approved, stayed.Status);
        Assert.Null(stayed.Feedback);
        Assert.Equal(approved.DecidedAt, stayed.DecidedAt);
        Assert.NotNull(stayed.DecidedAt);

        ReviewId fenced = await Opened(reviews, "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", PlanSamples.Choice);
        DecideOutcome.Refused blocked = Assert.IsType<DecideOutcome.Refused>(
            await reviews.ApproveAsync(fenced, CancellationToken.None));
        Assert.Equal("A plan with a decision fence cannot be approved.", blocked.Error);
        ReviewDetail pending = await Required(reviews, fenced);
        Assert.Equal(ReviewStatus.Pending, pending.Status);
        Assert.Null(pending.Feedback);
        Assert.Null(pending.DecidedAt);
        Assert.Equal([fenced], (await reviews.PendingAsync(CancellationToken.None)).Select(item => item.Id).ToArray());
        IReadOnlyList<ListedReview> listed = await reviews.ListAsync(CancellationToken.None);
        Assert.Equal(ReviewStatus.Approved, Assert.Single(listed, item => item.Id == plain).Status);
        Assert.Equal(ReviewStatus.Pending, Assert.Single(listed, item => item.Id == fenced).Status);
        Assert.Equal(stayed.DecidedAt, Assert.Single(listed, item => item.Id == plain).DecidedAt);
        Assert.Null(Assert.Single(listed, item => item.Id == fenced).DecidedAt);

        DecideOutcome.Refused missing = Assert.IsType<DecideOutcome.Refused>(
            await reviews.ApproveAsync(new ReviewId("missing"), CancellationToken.None));
        Assert.Equal("Review was not found.", missing.Error);
    }

    private static async Task<ReviewId> Opened(IReviews reviews, string revision, string markdown)
    {
        OpenOutcome.Opened opened = Assert.IsType<OpenOutcome.Opened>(
            await reviews.OpenAsync(new RevisionId(revision), markdown, CancellationToken.None));
        return opened.Id;
    }

    private static async Task<ReviewDetail> Required(IReviews reviews, ReviewId id)
    {
        ReviewDetail? detail = await reviews.FindAsync(id, CancellationToken.None);
        Assert.NotNull(detail);
        return detail;
    }
}