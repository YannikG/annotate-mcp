using Annotate.Reviews.Application;
using Annotate.Reviews.Domain;

using Microsoft.EntityFrameworkCore;

namespace Annotate.Reviews.Infrastructure;

internal static class ReviewComments
{
    public static async Task<string?> Add(
        ReviewsDbContext db,
        Review review,
        BlockComment comment,
        TimeProvider time,
        CancellationToken cancellationToken)
    {
        if (comment.Author != AnnotationAuthor.Agent)
        {
            return "A block comment comes from an agent.";
        }

        string blockKey = comment.BlockKey.Trim();
        string body = comment.Comment.Trim();
        if (blockKey.Length is < 1 or > ReviewLimits.Id)
        {
            return "Block was not found.";
        }

        if (body.Length is < 1 or > ReviewLimits.Note)
        {
            return "Comment was rejected.";
        }

        int count = await db.Annotations.CountAsync(item => item.ReviewId == review.Id, cancellationToken);
        if (count >= ReviewLimits.MaxAnnotations)
        {
            return "Too many annotations.";
        }

        int ordinal = await NextOrdinal(db, review.Id, cancellationToken);
        db.Annotations.Add(new StoredAnnotation(
            review.Id,
            ordinal,
            Guid.NewGuid().ToString(),
            "Comment",
            0,
            0,
            0,
            body,
            null,
            body,
            Stamp(time),
            blockKey,
            "agent",
            false));
        return null;
    }

    public static async Task<string?> Reply(
        ReviewsDbContext db,
        Review review,
        string annotationId,
        string text,
        TimeProvider time,
        CancellationToken cancellationToken)
    {
        bool found = await db.Annotations.AnyAsync(
            item => item.ReviewId == review.Id && item.AnnotationId == annotationId,
            cancellationToken);
        if (!found)
        {
            return "Annotation was not found.";
        }

        string body = text.Trim();
        if (body.Length is < 1 or > ReviewLimits.Note)
        {
            return "Reply was rejected.";
        }

        int? max = await db.Replies
            .Where(reply => reply.ReviewId == review.Id && reply.AnnotationId == annotationId)
            .Select(reply => (int?)reply.Ordinal)
            .MaxAsync(cancellationToken);
        db.Replies.Add(new StoredReply(
            review.Id,
            annotationId,
            (max ?? -1) + 1,
            Guid.NewGuid().ToString(),
            body,
            Stamp(time)));
        return null;
    }

    public static async Task<string?> DeleteAnnotation(
        ReviewsDbContext db,
        Review review,
        string annotationId,
        CancellationToken cancellationToken)
    {
        StoredAnnotation? annotation = await db.Annotations.SingleOrDefaultAsync(
            item => item.ReviewId == review.Id && item.AnnotationId == annotationId,
            cancellationToken);
        if (annotation is null)
        {
            return "Annotation was not found.";
        }

        await db.Replies
            .Where(reply => reply.ReviewId == review.Id && reply.AnnotationId == annotationId)
            .ExecuteDeleteAsync(cancellationToken);
        db.Annotations.Remove(annotation);
        return null;
    }

    public static async Task<string?> DeleteReply(
        ReviewsDbContext db,
        Review review,
        string annotationId,
        string replyId,
        CancellationToken cancellationToken)
    {
        StoredReply? reply = await db.Replies.SingleOrDefaultAsync(
            item => item.ReviewId == review.Id && item.AnnotationId == annotationId && item.ReplyId == replyId,
            cancellationToken);
        if (reply is null)
        {
            return "Reply was not found.";
        }

        db.Replies.Remove(reply);
        return null;
    }

    public static async Task<string?> SetAccepted(
        ReviewsDbContext db,
        Review review,
        string annotationId,
        bool accepted,
        CancellationToken cancellationToken)
    {
        StoredAnnotation? annotation = await db.Annotations.SingleOrDefaultAsync(
            item => item.ReviewId == review.Id && item.AnnotationId == annotationId,
            cancellationToken);
        if (annotation is null)
        {
            return "Annotation was not found.";
        }

        if (annotation.Author != "agent")
        {
            return "Accept applies to an agent note.";
        }

        annotation.Accept(accepted);
        return null;
    }

    public static async Task<string?> ReplacePhrase(
        ReviewsDbContext db,
        string reviewId,
        List<AnnotationDraft> drafts,
        CancellationToken cancellationToken)
    {
        List<string> blockIds = await db.Annotations
            .Where(item => item.ReviewId == reviewId && item.BlockKey != null)
            .Select(item => item.AnnotationId)
            .ToListAsync(cancellationToken);
        if (drafts.Count + blockIds.Count > ReviewLimits.MaxAnnotations)
        {
            return "Too many annotations.";
        }

        HashSet<string> keep = new(drafts.Select(draft => draft.Id), StringComparer.Ordinal);
        foreach (string blockId in blockIds)
        {
            keep.Add(blockId);
        }

        int? max = await db.Annotations
            .Where(item => item.ReviewId == reviewId && item.BlockKey != null)
            .Select(item => (int?)item.Ordinal)
            .MaxAsync(cancellationToken);
        await db.Annotations
            .Where(item => item.ReviewId == reviewId && item.BlockKey == null)
            .ExecuteDeleteAsync(cancellationToken);
        await db.Replies
            .Where(reply => reply.ReviewId == reviewId && !keep.Contains(reply.AnnotationId))
            .ExecuteDeleteAsync(cancellationToken);
        int ordinal = (max ?? -1) + 1;
        for (int index = 0; index < drafts.Count; index++)
        {
            AnnotationDraft draft = drafts[index];
            db.Annotations.Add(new StoredAnnotation(
                reviewId,
                ordinal + index,
                draft.Id,
                draft.Kind,
                draft.BlockOrdinal,
                draft.StartOffset,
                draft.EndOffset,
                draft.Text,
                draft.Replacement,
                draft.Comment,
                draft.CreatedAt));
        }

        return null;
    }

    public static async Task DropUnaccepted(ReviewsDbContext db, string reviewId, CancellationToken cancellationToken)
    {
        string[] ids = await db.Annotations
            .Where(item => item.ReviewId == reviewId && item.Author == "agent" && item.Accepted != true)
            .Select(item => item.AnnotationId)
            .ToArrayAsync(cancellationToken);
        if (ids.Length == 0)
        {
            return;
        }

        await db.Replies
            .Where(reply => reply.ReviewId == reviewId && ids.Contains(reply.AnnotationId))
            .ExecuteDeleteAsync(cancellationToken);
        await db.Annotations
            .Where(item => item.ReviewId == reviewId && ids.Contains(item.AnnotationId))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public static async Task<List<AnnotationDraft>> LoadDrafts(
        ReviewsDbContext db,
        string reviewId,
        CancellationToken cancellationToken)
    {
        List<StoredAnnotation> annotations = await db.Annotations
            .Where(item => item.ReviewId == reviewId)
            .ToListAsync(cancellationToken);
        List<StoredReply> replies = await db.Replies
            .Where(reply => reply.ReviewId == reviewId)
            .OrderBy(reply => reply.Ordinal)
            .ToListAsync(cancellationToken);
        return annotations
            .OrderBy(item => item.BlockKey is null ? 0 : 1)
            .ThenBy(item => item.Ordinal)
            .Select(item => ToDraft(item, replies))
            .ToList();
    }

    private static AnnotationDraft ToDraft(StoredAnnotation annotation, List<StoredReply> replies)
    {
        string[] lines = replies
            .Where(reply => reply.AnnotationId == annotation.AnnotationId)
            .Select(reply => reply.Text)
            .ToArray();
        return new AnnotationDraft(
            annotation.AnnotationId,
            annotation.Kind,
            annotation.Text,
            annotation.Comment,
            annotation.Replacement,
            annotation.BlockOrdinal,
            annotation.StartOffset,
            annotation.EndOffset,
            annotation.CreatedAt,
            annotation.BlockKey,
            annotation.Author,
            lines.Length == 0 ? null : lines);
    }

    private static async Task<int> NextOrdinal(ReviewsDbContext db, string reviewId, CancellationToken cancellationToken)
    {
        int? max = await db.Annotations
            .Where(item => item.ReviewId == reviewId)
            .Select(item => (int?)item.Ordinal)
            .MaxAsync(cancellationToken);
        return (max ?? -1) + 1;
    }

    private static string Stamp(TimeProvider time) => time.GetUtcNow().ToString("u");
}