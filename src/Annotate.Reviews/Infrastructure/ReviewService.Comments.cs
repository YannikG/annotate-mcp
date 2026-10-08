using Annotate.Reviews.Application;
using Annotate.Reviews.Domain;

using Microsoft.EntityFrameworkCore;

namespace Annotate.Reviews.Infrastructure;

internal sealed partial class ReviewService
{
    public Task<SaveAnnotationsOutcome> AddBlockCommentAsync(
        ReviewId id,
        BlockComment comment,
        CancellationToken cancellationToken) =>
        Edit(id, (db, review, token) => ReviewComments.Add(db, review, comment, time, token), cancellationToken);

    public Task<SaveAnnotationsOutcome> AddReplyAsync(
        ReviewId id,
        string annotationId,
        string text,
        CancellationToken cancellationToken) =>
        Edit(id, (db, review, token) => ReviewComments.Reply(db, review, annotationId, text, time, token), cancellationToken);

    public Task<SaveAnnotationsOutcome> DeleteAnnotationAsync(
        ReviewId id,
        string annotationId,
        CancellationToken cancellationToken) =>
        Edit(id, (db, review, token) => ReviewComments.DeleteAnnotation(db, review, annotationId, token), cancellationToken);

    public Task<SaveAnnotationsOutcome> DeleteReplyAsync(
        ReviewId id,
        string annotationId,
        string replyId,
        CancellationToken cancellationToken) =>
        Edit(id, (db, review, token) => ReviewComments.DeleteReply(db, review, annotationId, replyId, token), cancellationToken);

    public Task<SaveAnnotationsOutcome> SetAcceptedAsync(
        ReviewId id,
        string annotationId,
        bool accepted,
        CancellationToken cancellationToken) =>
        Edit(id, (db, review, token) => ReviewComments.SetAccepted(db, review, annotationId, accepted, token), cancellationToken);

    private async Task<SaveAnnotationsOutcome> Edit(
        ReviewId id,
        Func<ReviewsDbContext, Review, CancellationToken, Task<string?>> change,
        CancellationToken cancellationToken)
    {
        await using ReviewsDbContext db = await ReviewsSchema.Open(contexts, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        Review? review = await db.Reviews.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (review is null)
        {
            return new SaveAnnotationsOutcome.Refused("Review was not found.");
        }

        if (!review.IsPending)
        {
            return new SaveAnnotationsOutcome.Refused("Review already decided.");
        }

        string? error = await change(db, review, cancellationToken);
        if (error is not null)
        {
            return new SaveAnnotationsOutcome.Refused(error);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new SaveAnnotationsOutcome.Done();
    }
}