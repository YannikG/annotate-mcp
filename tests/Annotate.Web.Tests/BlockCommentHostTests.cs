using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Web.Tests;

public sealed class BlockCommentHostTests
{
    [Fact]
    public async Task AnnotateBlockCommentsOnAStoredBlockOfAPendingReview()
    {
        await using AnnotateApp app = new();
        IPlanHost host = app.Services.GetRequiredService<IPlanHost>();
        IReviews reviews = app.Services.GetRequiredService<IReviews>();
        string submitted = await host.SubmitAsync(
            new PlanSubmission("# Storage\n\nKeep the file local.\n", null, null, null, null, null, null, "Cursor", "claude-opus-4"),
            "localhost",
            CancellationToken.None);
        string reviewId = Value(submitted, "Review ID: ");
        ReviewDetail review = (await reviews.FindAsync(new ReviewId(reviewId), CancellationToken.None))!;
        RevisionDetail revision = (await app.Services.GetRequiredService<IPlans>().RevisionAsync(
            new Annotate.Plans.Application.RevisionId(review.RevisionId.Value),
            CancellationToken.None))!;
        string blockId = revision.Blocks[0].Key;

        Assert.Equal("Error: Review was not found.", await host.AnnotateBlockAsync(" ", blockId, "No", CancellationToken.None));
        Assert.Equal("Error: Review was not found.", await host.AnnotateBlockAsync("missing", blockId, "No", CancellationToken.None));
        Assert.Equal("Error: Block was not found.", await host.AnnotateBlockAsync(reviewId, " ", "No", CancellationToken.None));
        Assert.Equal("Error: Block was not found.", await host.AnnotateBlockAsync(reviewId, "missing-block", "No", CancellationToken.None));
        Assert.Equal("Error: Comment was rejected.", await host.AnnotateBlockAsync(reviewId, blockId, " ", CancellationToken.None));
        Assert.Empty((await reviews.FindAsync(review.Id, CancellationToken.None))!.Annotations);

        Assert.Equal("Block comment saved.", await host.AnnotateBlockAsync(reviewId, blockId, "Use a file.", CancellationToken.None));
        Annotation note = Assert.Single((await reviews.FindAsync(review.Id, CancellationToken.None))!.Annotations);
        Assert.Equal(blockId, note.BlockKey);
        Assert.Equal("Use a file.", note.Comment);
        Assert.Equal(AnnotationAuthor.Agent, note.Author);
        Assert.False(note.Accepted);

        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.DeleteAnnotationAsync(review.Id, note.Id, CancellationToken.None));
        Assert.IsType<DecideOutcome.Done>(await reviews.ApproveAsync(review.Id, CancellationToken.None));
        Assert.Equal(
            "Error: Review already decided.",
            await host.AnnotateBlockAsync(reviewId, blockId, "Later", CancellationToken.None));
        Assert.Empty((await reviews.FindAsync(review.Id, CancellationToken.None))!.Annotations);
    }

    private static string Value(string text, string label)
    {
        int start = text.IndexOf(label, StringComparison.Ordinal) + label.Length;
        int end = text.IndexOf('\n', start);
        return text[start..end].Trim();
    }
}