using Annotate.Reviews.Application;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Reviews.Tests;

public sealed class RemoveRevisionTests
{
    [Fact]
    public async Task RemovingARevisionDropsItsReview()
    {
        await using OpenReviews open = await OpenReviews.Open();
        await using ServiceProvider host = open.Reviews();
        IReviews reviews = host.GetRequiredService<IReviews>();
        const string Revision = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
        ReviewId id = await Opened(reviews, Revision, PlanSamples.Plain);
        Assert.NotNull(await reviews.FindAsync(id, CancellationToken.None));

        await reviews.RemoveRevisionsAsync([Revision], CancellationToken.None);
        await reviews.RemoveRevisionsAsync([], CancellationToken.None);

        Assert.Null(await reviews.FindAsync(id, CancellationToken.None));
        Assert.Empty(await reviews.PendingAsync(CancellationToken.None));
    }

    private static async Task<ReviewId> Opened(IReviews reviews, string revision, string markdown)
    {
        OpenOutcome.Opened opened = Assert.IsType<OpenOutcome.Opened>(
            await reviews.OpenAsync(new RevisionId(revision), markdown, CancellationToken.None));
        return opened.Id;
    }
}