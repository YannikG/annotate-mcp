using Annotate.Reviews.Application;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Reviews.Tests;

public sealed class RequestChangesTests
{
    [Fact]
    public async Task RequestChangesStoresCriticMarkupOnce()
    {
        await using OpenReviews open = await OpenReviews.Open();
        await using ServiceProvider host = open.Reviews();
        IReviews reviews = host.GetRequiredService<IReviews>();

        ReviewId plain = await Opened(reviews, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", PlanSamples.Plain);
        Assert.Equal(
            "Nothing to send.",
            Refused(await reviews.RequestChangesAsync(plain, [], CancellationToken.None)));
        Assert.Equal(
            "Annotation was rejected.",
            Refused(await reviews.RequestChangesAsync(plain, [Mark(AnnotationKind.Deletion, id: "")], CancellationToken.None)));
        Assert.Equal(
            "Annotation was rejected.",
            Refused(await reviews.RequestChangesAsync(
                plain,
                [Mark(AnnotationKind.Replacement, replacement: null)],
                CancellationToken.None)));
        Assert.Equal(
            "Too many annotations.",
            Refused(await reviews.RequestChangesAsync(plain, Many(), CancellationToken.None)));
        ReviewDetail stillPending = await Required(reviews, plain);
        Assert.Equal(ReviewStatus.Pending, stillPending.Status);
        Assert.Null(stillPending.Feedback);

        Annotation[] annotations =
        [
            Mark(AnnotationKind.Deletion, "old line", id: "del", createdAt: "t1"),
            Mark(AnnotationKind.Replacement, "old", replacement: "new", id: "rep", createdAt: "t2"),
            Mark(AnnotationKind.Insertion, "anchor", replacement: "added", id: "ins", createdAt: "t3"),
            Mark(AnnotationKind.Comment, "note", comment: "why", id: "com", createdAt: "t4"),
            Mark(AnnotationKind.Deletion, "a--b", id: "say \"hi\"\\x", createdAt: "t\"5"),
        ];
        Assert.IsType<DecideOutcome.Done>(
            await reviews.RequestChangesAsync(plain, annotations, CancellationToken.None));
        ReviewDetail changed = await Required(reviews, plain);
        Assert.Equal(ReviewStatus.ChangesRequested, changed.Status);
        Assert.Equal(Feedback, changed.Feedback);
        Assert.Equal(annotations, changed.Annotations);
        Assert.Equal(
            "Review already decided.",
            Refused(await reviews.RequestChangesAsync(plain, [Mark(AnnotationKind.Comment, "later", comment: "no")], CancellationToken.None)));
        Assert.Equal(Feedback, (await Required(reviews, plain)).Feedback);

        ReviewId fenced = await Opened(reviews, "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", PlanSamples.Decisions);
        Assert.IsType<DecideOutcome.Done>(await reviews.RequestChangesAsync(fenced, [], CancellationToken.None));
        Assert.Equal(FenceFeedback, (await Required(reviews, fenced)).Feedback);

        Assert.Equal(
            "Review was not found.",
            Refused(await reviews.RequestChangesAsync(new ReviewId("missing"), [], CancellationToken.None)));
    }

    private const string Feedback =
        """
        ## Plan Review Feedback

        Apply the following anchored review comments before proceeding.

        ### Suggested Changes

        1. {--old line--}}{id="del" by="user" at="t1"}
        from: operator
        2. {~~old~>new~~}}{id="rep" by="user" at="t2"}
        from: operator
        3. After {==anchor==}}, insert {++added++}}{id="ins" by="user" at="t3"}
        from: operator
        4. {==note==}}{{>>why<<}}{id="com" by="user" at="t4"}
        from: operator
        5. {--a[escaped --]b--}}{id="say \"hi\"\\x" by="user" at="t\"5"}
        from: operator

        Please revise the plan to address this feedback and submit the revised draft again.
        """;

    private const string FenceFeedback =
        """
        Plan changes requested.

        ### Decisions

        - storage: (unanswered)
        - note: (unanswered)
        """;

    private static string Refused(DecideOutcome outcome) =>
        Assert.IsType<DecideOutcome.Refused>(outcome).Error;

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

    private static Annotation Mark(
        AnnotationKind kind,
        string text = "text",
        string? replacement = null,
        string? comment = null,
        string id = "a",
        string createdAt = "t") =>
        new(id, kind, text, comment, replacement, 1, 2, 3, createdAt);

    private static Annotation[] Many()
    {
        Annotation[] annotations = new Annotation[101];
        for (int index = 0; index < annotations.Length; index++)
        {
            annotations[index] = Mark(AnnotationKind.Deletion, id: "a" + index);
        }

        return annotations;
    }
}