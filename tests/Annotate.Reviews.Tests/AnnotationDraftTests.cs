using Annotate.Reviews.Application;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Reviews.Tests;

public sealed class AnnotationDraftTests
{
    [Fact]
    public async Task DraftsSurviveAnotherServiceAndCanBeEditedAndUndone()
    {
        await using OpenReviews open = await OpenReviews.Open();
        await using ServiceProvider host = open.Reviews();
        IReviews reviews = host.GetRequiredService<IReviews>();
        ReviewId id = Assert.IsType<OpenOutcome.Opened>(await reviews.OpenAsync(
            new RevisionId("draft-revision"), "# Plan", CancellationToken.None)).Id;
        Annotation annotation = Mark();
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.SaveAnnotationsAsync(id, [annotation], CancellationToken.None));
        await using ServiceProvider reopened = open.Reviews();
        IReviews reader = reopened.GetRequiredService<IReviews>();
        ReviewDetail saved = Assert.IsType<ReviewDetail>(await reader.FindAsync(id, CancellationToken.None));
        Assert.Equal([annotation], saved.Annotations);
        Assert.Equal(ReviewStatus.Pending, saved.Status);
        Assert.Null(saved.Feedback);
        Assert.Null(saved.DecidedAt);
        Assert.IsType<WaitOutcome.Pending>(await reader.WaitAsync(id, TimeSpan.Zero, CancellationToken.None));
        ReviewId other = Assert.IsType<OpenOutcome.Opened>(await reviews.OpenAsync(
            new RevisionId("other-revision"), "# Other", CancellationToken.None)).Id;
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.SaveAnnotationsAsync(other, [annotation with { Id = "other" }], CancellationToken.None));
        Assert.Equal([annotation], (await reader.FindAsync(id, CancellationToken.None))!.Annotations);
        Annotation edited = annotation with { Replacement = "edited" };
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.SaveAnnotationsAsync(id, [edited], CancellationToken.None));
        Assert.Equal([edited], (await reader.FindAsync(id, CancellationToken.None))!.Annotations);
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.SaveAnnotationsAsync(id, [], CancellationToken.None));
        Assert.Empty((await reader.FindAsync(id, CancellationToken.None))!.Annotations);
    }

    [Fact]
    public async Task SubmittingDraftsStoresThemOnceAndLocksFurtherEdits()
    {
        await using OpenReviews open = await OpenReviews.Open();
        await using ServiceProvider host = open.Reviews();
        IReviews reviews = host.GetRequiredService<IReviews>();
        ReviewId id = Assert.IsType<OpenOutcome.Opened>(await reviews.OpenAsync(
            new RevisionId("submit-revision"), "# Plan", CancellationToken.None)).Id;
        Annotation annotation = Mark();
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.SaveAnnotationsAsync(id, [annotation], CancellationToken.None));
        Assert.IsType<DecideOutcome.Done>(await reviews.RequestChangesAsync(id, [annotation], CancellationToken.None));
        ReviewDetail saved = (await reviews.FindAsync(id, CancellationToken.None))!;
        Assert.Equal([annotation], saved.Annotations);
        Assert.Equal(ReviewStatus.ChangesRequested, saved.Status);
        Assert.Contains("new", saved.Feedback, StringComparison.Ordinal);
        Assert.IsType<SaveAnnotationsOutcome.Refused>(await reviews.SaveAnnotationsAsync(id, [], CancellationToken.None));
        Assert.Equal([annotation], (await reviews.FindAsync(id, CancellationToken.None))!.Annotations);
    }

    [Fact]
    public async Task InvalidDraftLeavesSavedAnnotationsIntactAndApprovalWaitsUntilTheyAreGone()
    {
        await using OpenReviews open = await OpenReviews.Open();
        await using ServiceProvider host = open.Reviews();
        IReviews reviews = host.GetRequiredService<IReviews>();
        ReviewId id = Assert.IsType<OpenOutcome.Opened>(await reviews.OpenAsync(
            new RevisionId("invalid-revision"), "# Plan", CancellationToken.None)).Id;
        Annotation annotation = Mark();
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.SaveAnnotationsAsync(id, [annotation], CancellationToken.None));
        Assert.IsType<SaveAnnotationsOutcome.Refused>(await reviews.SaveAnnotationsAsync(id, [annotation with { Replacement = null }], CancellationToken.None));
        Assert.Equal([annotation], (await reviews.FindAsync(id, CancellationToken.None))!.Annotations);
        Assert.IsType<SaveAnnotationsOutcome.Refused>(await reviews.SaveAnnotationsAsync(new ReviewId("missing"), [], CancellationToken.None));
        Assert.Equal(
            "Annotations must be removed before approval.",
            Assert.IsType<DecideOutcome.Refused>(await reviews.ApproveAsync(id, CancellationToken.None)).Error);
        Assert.Equal([annotation], (await reviews.FindAsync(id, CancellationToken.None))!.Annotations);
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.SaveAnnotationsAsync(id, [], CancellationToken.None));
        Assert.IsType<DecideOutcome.Done>(await reviews.ApproveAsync(id, CancellationToken.None));
        Assert.Empty((await reviews.FindAsync(id, CancellationToken.None))!.Annotations);
        Assert.IsType<SaveAnnotationsOutcome.Refused>(await reviews.SaveAnnotationsAsync(id, [annotation], CancellationToken.None));
    }

    private static Annotation Mark() => new("note", AnnotationKind.Replacement, "Plan", null, "new", 0, 2, 6, "now");
}