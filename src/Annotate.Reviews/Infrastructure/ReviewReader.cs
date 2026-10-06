using Annotate.Reviews.Application;
using Annotate.Reviews.Domain;

using Microsoft.EntityFrameworkCore;

namespace Annotate.Reviews.Infrastructure;

internal static class ReviewReader
{
    public static async Task<ReviewDetail?> ById(
        ReviewsDbContext db,
        string id,
        CancellationToken cancellationToken)
    {
        Review? review = await db.Reviews.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return review is null ? null : await Detail(db, review, cancellationToken);
    }

    public static async Task<ReviewDetail?> ByRevision(
        ReviewsDbContext db,
        string revisionId,
        CancellationToken cancellationToken)
    {
        Review? review = await db.Reviews.SingleOrDefaultAsync(
            item => item.RevisionId == revisionId,
            cancellationToken);
        return review is null ? null : await Detail(db, review, cancellationToken);
    }

    public static async Task<IReadOnlyList<PendingReview>> Pending(
        ReviewsDbContext db,
        CancellationToken cancellationToken)
    {
        List<Review> reviews = await db.Reviews
            .Where(review => review.Status == Review.Pending)
            .ToListAsync(cancellationToken);
        return reviews
            .OrderByDescending(review => review.CreatedAt)
            .ThenByDescending(review => review.Id, StringComparer.Ordinal)
            .Select(review => new PendingReview(new ReviewId(review.Id), new RevisionId(review.RevisionId), review.CreatedAt))
            .ToArray();
    }

    public static async Task<IReadOnlyList<ListedReview>> List(
        ReviewsDbContext db,
        CancellationToken cancellationToken)
    {
        List<Review> reviews = await db.Reviews.ToListAsync(cancellationToken);
        Dictionary<string, int> annotations = await db.Annotations
            .GroupBy(annotation => annotation.ReviewId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);
        Dictionary<string, int> answers = await db.Answers
            .GroupBy(answer => answer.ReviewId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);
        return reviews
            .OrderByDescending(review => review.CreatedAt)
            .ThenByDescending(review => review.Id, StringComparer.Ordinal)
            .Select(review => new ListedReview(
                new ReviewId(review.Id),
                new RevisionId(review.RevisionId),
                Status(review.Status),
                review.CreatedAt,
                review.DecidedAt,
                annotations.GetValueOrDefault(review.Id),
                answers.GetValueOrDefault(review.Id)))
            .ToArray();
    }

    private static async Task<ReviewDetail> Detail(
        ReviewsDbContext db,
        Review review,
        CancellationToken cancellationToken)
    {
        List<ReviewFence> fences = await db.Fences
            .Where(fence => fence.ReviewId == review.Id)
            .OrderBy(fence => fence.Ordinal)
            .ToListAsync(cancellationToken);
        List<ReviewOption> options = await db.Options
            .Where(option => option.ReviewId == review.Id)
            .OrderBy(option => option.Ordinal)
            .ToListAsync(cancellationToken);
        List<StoredAnswer> answers = await db.Answers
            .Where(answer => answer.ReviewId == review.Id)
            .ToListAsync(cancellationToken);
        List<StoredAnnotation> annotations = await db.Annotations
            .Where(annotation => annotation.ReviewId == review.Id)
            .OrderBy(annotation => annotation.Ordinal)
            .ToListAsync(cancellationToken);
        Dictionary<string, List<string>> labels = Labels(options);
        Dictionary<string, StoredAnswer> saved = answers.ToDictionary(answer => answer.FenceId, StringComparer.Ordinal);

        return new ReviewDetail(
            new ReviewId(review.Id),
            new RevisionId(review.RevisionId),
            Status(review.Status),
            review.Feedback,
            annotations.Select(ToAnnotation).ToArray(),
            fences
                .Where(fence => saved.ContainsKey(fence.FenceId))
                .Select(fence => ToAnswer(saved[fence.FenceId]))
                .ToArray(),
            fences.Select(fence => ToPrompt(fence, labels)).ToArray(),
            review.CreatedAt,
            review.DecidedAt);
    }

    private static Dictionary<string, List<string>> Labels(List<ReviewOption> options)
    {
        Dictionary<string, List<string>> labels = new(StringComparer.Ordinal);
        foreach (ReviewOption option in options)
        {
            if (!labels.TryGetValue(option.FenceId, out List<string>? list))
            {
                list = [];
                labels[option.FenceId] = list;
            }

            list.Add(option.Label);
        }

        return labels;
    }

    private static ReviewPrompt ToPrompt(ReviewFence fence, Dictionary<string, List<string>> labels) =>
        new(
            fence.FenceId,
            fence.Kind switch
            {
                "choice" => PromptKind.Choice,
                "text" => PromptKind.Text,
                _ => throw new InvalidOperationException("Decision kind was not recognised."),
            },
            fence.Prompt,
            labels.TryGetValue(fence.FenceId, out List<string>? stored) ? stored : []);

    private static DecisionAnswer ToAnswer(StoredAnswer answer) =>
        new(answer.FenceId, answer.Answer, answer.IsOther);

    private static Annotation ToAnnotation(StoredAnnotation annotation) =>
        new(
            annotation.AnnotationId,
            annotation.Kind switch
            {
                "Deletion" => AnnotationKind.Deletion,
                "Replacement" => AnnotationKind.Replacement,
                "Insertion" => AnnotationKind.Insertion,
                "Comment" => AnnotationKind.Comment,
                _ => throw new InvalidOperationException("Annotation kind was not recognised."),
            },
            annotation.Text,
            annotation.Comment,
            annotation.Replacement,
            annotation.BlockOrdinal,
            annotation.StartOffset,
            annotation.EndOffset,
            annotation.CreatedAt);

    private static ReviewStatus Status(string status) => status switch
    {
        Review.Pending => ReviewStatus.Pending,
        Review.Approved => ReviewStatus.Approved,
        Review.ChangesRequested => ReviewStatus.ChangesRequested,
        _ => throw new InvalidOperationException("Review status was not recognised."),
    };
}