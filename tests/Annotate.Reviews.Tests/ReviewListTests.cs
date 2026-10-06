using Annotate.Reviews.Application;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Reviews.Tests;

public sealed class ReviewListTests
{
    [Fact]
    public async Task ListCountsAnnotationsAndAnswers()
    {
        await using OpenReviews open = await OpenReviews.Open();
        await using ServiceProvider host = open.Reviews();
        IReviews reviews = host.GetRequiredService<IReviews>();
        ReviewId id = Assert.IsType<OpenOutcome.Opened>(
            await reviews.OpenAsync(new RevisionId("listed"), PlanSamples.Choice, CancellationToken.None)).Id;
        Annotation note = new("note", AnnotationKind.Comment, "Storage", "why", null, 0, 0, 7, "now");
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.SaveAnnotationsAsync(id, [note, note with { Id = "other" }], CancellationToken.None));
        Assert.IsType<SaveAnswerOutcome.Done>(
            await reviews.SaveAnswerAsync(id, new DecisionAnswer("storage", "SQLite", false), CancellationToken.None));

        ListedReview listed = Assert.Single(await reviews.ListAsync(CancellationToken.None));
        Assert.Equal(id, listed.Id);
        Assert.Equal(2, listed.AnnotationCount);
        Assert.Equal(1, listed.AnswerCount);
    }
}