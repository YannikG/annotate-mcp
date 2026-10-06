using Annotate.Reviews.Application;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Reviews.Tests;

public sealed class SaveAnswerTests
{
    [Fact]
    public async Task SaveAnswerKeepsALegalChoiceOrTextAndShowsItInFeedback()
    {
        await using OpenReviews open = await OpenReviews.Open();
        await using ServiceProvider host = open.Reviews();
        IReviews reviews = host.GetRequiredService<IReviews>();
        ReviewId id = await Opened(reviews, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", PlanSamples.Decisions);

        Assert.Equal("Decision was not found.", Error(await reviews.SaveAnswerAsync(id, new DecisionAnswer("missing", "x", false), CancellationToken.None)));
        Assert.Equal("Decision answer was rejected.", Error(await reviews.SaveAnswerAsync(id, new DecisionAnswer("storage", "nope", false), CancellationToken.None)));
        Assert.Equal("Decision answer was rejected.", Error(await reviews.SaveAnswerAsync(id, new DecisionAnswer("storage", " SQLite ", false), CancellationToken.None)));
        Assert.Equal("Decision answer was rejected.", Error(await reviews.SaveAnswerAsync(id, new DecisionAnswer("storage", "   ", true), CancellationToken.None)));
        Assert.Equal("Decision answer was rejected.", Error(await reviews.SaveAnswerAsync(id, new DecisionAnswer("note", "free", true), CancellationToken.None)));
        Assert.Equal("Decision answer was rejected.", Error(await reviews.SaveAnswerAsync(id, new DecisionAnswer("note", "   ", false), CancellationToken.None)));
        Assert.Empty((await Required(reviews, id)).Answers);

        Assert.IsType<SaveAnswerOutcome.Done>(
            await reviews.SaveAnswerAsync(id, new DecisionAnswer("storage", "SQLite", false), CancellationToken.None));
        Assert.Equal("Decision answer was rejected.", Error(await reviews.SaveAnswerAsync(id, new DecisionAnswer("storage", "nope", false), CancellationToken.None)));
        Assert.Equal([new DecisionAnswer("storage", "SQLite", false)], (await Required(reviews, id)).Answers);

        Assert.IsType<SaveAnswerOutcome.Done>(
            await reviews.SaveAnswerAsync(id, new DecisionAnswer("storage", "  custom  ", true), CancellationToken.None));
        Assert.IsType<SaveAnswerOutcome.Done>(
            await reviews.SaveAnswerAsync(id, new DecisionAnswer("note", "  hello  ", false), CancellationToken.None));
        Assert.Equal(
            [new DecisionAnswer("storage", "custom", true), new DecisionAnswer("note", "hello", false)],
            (await Required(reviews, id)).Answers);

        Assert.IsType<DecideOutcome.Done>(await reviews.RequestChangesAsync(id, [], CancellationToken.None));
        Assert.Equal(AnsweredFeedback, (await Required(reviews, id)).Feedback);
        Assert.Equal(
            "Review already decided.",
            Error(await reviews.SaveAnswerAsync(id, new DecisionAnswer("note", "later", false), CancellationToken.None)));
        Assert.Equal(
            [new DecisionAnswer("storage", "custom", true), new DecisionAnswer("note", "hello", false)],
            (await Required(reviews, id)).Answers);

        Assert.Equal(
            "Review was not found.",
            Error(await reviews.SaveAnswerAsync(new ReviewId("missing"), new DecisionAnswer("storage", "SQLite", false), CancellationToken.None)));
    }

    private const string AnsweredFeedback =
        """
        Plan changes requested.

        ### Decisions

        - storage: custom
        - note: hello
        """;

    private static string Error(SaveAnswerOutcome outcome) =>
        Assert.IsType<SaveAnswerOutcome.Refused>(outcome).Error;

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