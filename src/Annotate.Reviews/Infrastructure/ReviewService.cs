using Annotate.Markdown;
using Annotate.Reviews.Application;
using Annotate.Reviews.Domain;

using Microsoft.EntityFrameworkCore;

namespace Annotate.Reviews.Infrastructure;

internal sealed class ReviewService(
    IDbContextFactory<ReviewsDbContext> contexts,
    TimeProvider time,
    ReviewSignals signals) : IReviews
{
    public async Task<OpenOutcome> OpenAsync(
        RevisionId revision,
        string markdown,
        CancellationToken cancellationToken)
    {
        await using ReviewsDbContext db = await ReviewsSchema.Open(contexts, cancellationToken);
        ParseOutcome parsed = PlanMarkdown.Parse(markdown);
        if (parsed is ParseOutcome.Invalid invalid)
        {
            return new OpenOutcome.Refused(invalid.Error);
        }

        Review? existing = await db.Reviews.SingleOrDefaultAsync(
            review => review.RevisionId == revision.Value,
            cancellationToken);
        if (existing is not null)
        {
            return new OpenOutcome.Opened(new ReviewId(existing.Id));
        }

        PlanDocument document = ((ParseOutcome.Ok)parsed).Document;
        IReadOnlyList<CapturedFence> fences = DecisionCapture.Read(document);
        string id = Guid.NewGuid().ToString();
        db.Reviews.Add(new Review(id, revision.Value, time.GetUtcNow()));
        for (int ordinal = 0; ordinal < fences.Count; ordinal++)
        {
            CapturedFence fence = fences[ordinal];
            db.Fences.Add(new ReviewFence(id, ordinal, fence.Id, fence.Kind, fence.Prompt));
            for (int option = 0; option < fence.Options.Count; option++)
            {
                db.Options.Add(new ReviewOption(id, fence.Id, option, fence.Options[option]));
            }
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            Review? raced = await db.Reviews.SingleOrDefaultAsync(
                review => review.RevisionId == revision.Value,
                cancellationToken);
            if (raced is null)
            {
                throw;
            }

            return new OpenOutcome.Opened(new ReviewId(raced.Id));
        }

        return new OpenOutcome.Opened(new ReviewId(id));
    }

    public async Task<ReviewDetail?> FindAsync(ReviewId id, CancellationToken cancellationToken)
    {
        await using ReviewsDbContext db = await ReviewsSchema.Open(contexts, cancellationToken);
        return await ReviewReader.ById(db, id.Value, cancellationToken);
    }

    public async Task<ReviewDetail?> ForRevisionAsync(RevisionId revision, CancellationToken cancellationToken)
    {
        await using ReviewsDbContext db = await ReviewsSchema.Open(contexts, cancellationToken);
        return await ReviewReader.ByRevision(db, revision.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<PendingReview>> PendingAsync(CancellationToken cancellationToken)
    {
        await using ReviewsDbContext db = await ReviewsSchema.Open(contexts, cancellationToken);
        return await ReviewReader.Pending(db, cancellationToken);
    }

    public async Task<IReadOnlyList<ListedReview>> ListAsync(CancellationToken cancellationToken)
    {
        await using ReviewsDbContext db = await ReviewsSchema.Open(contexts, cancellationToken);
        return await ReviewReader.List(db, cancellationToken);
    }

    public async Task<WaitOutcome> WaitAsync(ReviewId id, TimeSpan timeout, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using CancellationTokenSource timer = new();
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timer.Token);
        Task delay = Task.Delay(timeout, time, linked.Token);
        try
        {
            WaitOutcome? immediate = await Read(id, cancellationToken);
            if (immediate is not null)
            {
                return immediate;
            }

            Task signal = signals.Wait(id.Value, cancellationToken);
            immediate = await Read(id, cancellationToken);
            if (immediate is not null)
            {
                return immediate;
            }

            await Task.WhenAny(signal, delay);
            cancellationToken.ThrowIfCancellationRequested();
            return await Read(id, cancellationToken) ?? new WaitOutcome.Pending();
        }
        finally
        {
            timer.Cancel();
        }
    }

    private async Task<WaitOutcome?> Read(ReviewId id, CancellationToken cancellationToken)
    {
        await using ReviewsDbContext db = await ReviewsSchema.Open(contexts, cancellationToken);
        Review? review = await db.Reviews.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == id.Value,
            cancellationToken);
        if (review is null)
        {
            return new WaitOutcome.Missing();
        }

        if (review.IsPending)
        {
            return null;
        }

        return new WaitOutcome.Decided(review.Status switch
        {
            Review.Approved => new ReviewDecision.Approved(),
            Review.ChangesRequested => new ReviewDecision.ChangesRequested(review.Feedback ?? ""),
            _ => throw new InvalidOperationException("Review status was not recognised."),
        });
    }

    public async Task<DecideOutcome> ApproveAsync(ReviewId id, CancellationToken cancellationToken)
    {
        await using ReviewsDbContext db = await ReviewsSchema.Open(contexts, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        Review? review = await db.Reviews.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (review is null)
        {
            return new DecideOutcome.Refused("Review was not found.");
        }

        if (!review.IsPending)
        {
            return new DecideOutcome.Refused("Review already decided.");
        }

        bool fenced = await db.Fences.AnyAsync(fence => fence.ReviewId == review.Id, cancellationToken);
        if (fenced)
        {
            return new DecideOutcome.Refused("A plan with a decision fence cannot be approved.");
        }

        await db.Annotations.Where(annotation => annotation.ReviewId == review.Id).ExecuteDeleteAsync(cancellationToken);
        review.Approve(time.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        signals.Pulse(review.Id);
        return new DecideOutcome.Done();
    }

    public async Task<DecideOutcome> RequestChangesAsync(
        ReviewId id,
        IReadOnlyList<Annotation> annotations,
        CancellationToken cancellationToken)
    {
        await using ReviewsDbContext db = await ReviewsSchema.Open(contexts, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        Review? review = await db.Reviews.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (review is null)
        {
            return new DecideOutcome.Refused("Review was not found.");
        }

        if (!review.IsPending)
        {
            return new DecideOutcome.Refused("Review already decided.");
        }

        List<AnnotationDraft> drafts = annotations.Select(ToDraft).ToList();
        string? rejected = AnnotationRules.Reject(drafts);
        if (rejected is not null)
        {
            return new DecideOutcome.Refused(rejected);
        }

        List<ReviewFence> fences = await db.Fences
            .Where(fence => fence.ReviewId == review.Id)
            .OrderBy(fence => fence.Ordinal)
            .ToListAsync(cancellationToken);
        if (drafts.Count == 0 && fences.Count == 0)
        {
            return new DecideOutcome.Refused("Nothing to send.");
        }

        List<StoredAnswer> answers = await db.Answers
            .Where(answer => answer.ReviewId == review.Id)
            .ToListAsync(cancellationToken);
        Dictionary<string, string> saved = answers.ToDictionary(
            answer => answer.FenceId,
            answer => answer.Answer,
            StringComparer.Ordinal);
        review.RequestChanges(
            FeedbackText.Write(drafts, fences.Select(fence => fence.FenceId).ToArray(), saved),
            time.GetUtcNow());
        await ReplaceAnnotations(db, review.Id, drafts, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        signals.Pulse(review.Id);
        return new DecideOutcome.Done();
    }

    private static AnnotationDraft ToDraft(Annotation annotation) =>
        new(
            annotation.Id,
            annotation.Kind switch
            {
                AnnotationKind.Deletion => "Deletion",
                AnnotationKind.Replacement => "Replacement",
                AnnotationKind.Insertion => "Insertion",
                AnnotationKind.Comment => "Comment",
                _ => "unknown",
            },
            annotation.Text,
            annotation.Comment,
            annotation.Replacement,
            annotation.BlockOrdinal,
            annotation.StartOffset,
            annotation.EndOffset,
            annotation.CreatedAt);

    public async Task<SaveAnnotationsOutcome> SaveAnnotationsAsync(
        ReviewId id, IReadOnlyList<Annotation> annotations, CancellationToken cancellationToken)
    {
        await using ReviewsDbContext db = await ReviewsSchema.Open(contexts, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        Review? review = await db.Reviews.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (review is null) return new SaveAnnotationsOutcome.Refused("Review was not found.");
        if (!review.IsPending) return new SaveAnnotationsOutcome.Refused("Review already decided.");
        List<AnnotationDraft> drafts = annotations.Select(ToDraft).ToList();
        string? rejected = AnnotationRules.Reject(drafts);
        if (rejected is not null) return new SaveAnnotationsOutcome.Refused(rejected);
        await ReplaceAnnotations(db, review.Id, drafts, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new SaveAnnotationsOutcome.Done();
    }

    private static async Task ReplaceAnnotations(
        ReviewsDbContext db, string reviewId, List<AnnotationDraft> drafts, CancellationToken cancellationToken)
    {
        await db.Annotations.Where(annotation => annotation.ReviewId == reviewId).ExecuteDeleteAsync(cancellationToken);
        for (int ordinal = 0; ordinal < drafts.Count; ordinal++)
        {
            AnnotationDraft draft = drafts[ordinal];
            db.Annotations.Add(new StoredAnnotation(
                reviewId,
                ordinal,
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

    }

    public async Task<SaveAnswerOutcome> SaveAnswerAsync(
        ReviewId id,
        DecisionAnswer answer,
        CancellationToken cancellationToken)
    {
        await using ReviewsDbContext db = await ReviewsSchema.Open(contexts, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        Review? review = await db.Reviews.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (review is null)
        {
            return new SaveAnswerOutcome.Refused("Review was not found.");
        }

        if (!review.IsPending)
        {
            return new SaveAnswerOutcome.Refused("Review already decided.");
        }

        ReviewFence? fence = await db.Fences.SingleOrDefaultAsync(
            item => item.ReviewId == review.Id && item.FenceId == answer.FenceId,
            cancellationToken);
        if (fence is null)
        {
            return new SaveAnswerOutcome.Refused("Decision was not found.");
        }

        List<string> options = await db.Options
            .Where(option => option.ReviewId == review.Id && option.FenceId == fence.FenceId)
            .OrderBy(option => option.Ordinal)
            .Select(option => option.Label)
            .ToListAsync(cancellationToken);
        if (!AnswerRules.Accepts(fence.Kind, options, answer.Answer, answer.IsOther, out string stored))
        {
            return new SaveAnswerOutcome.Refused("Decision answer was rejected.");
        }

        StoredAnswer? existing = await db.Answers.SingleOrDefaultAsync(
            item => item.ReviewId == review.Id && item.FenceId == fence.FenceId,
            cancellationToken);
        if (existing is null)
        {
            db.Answers.Add(new StoredAnswer(review.Id, fence.FenceId, stored, answer.IsOther));
        }
        else
        {
            existing.Replace(stored, answer.IsOther);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new SaveAnswerOutcome.Done();
    }

    public async Task RemoveRevisionsAsync(IReadOnlyList<string> revisionIds, CancellationToken cancellationToken)
    {
        if (revisionIds.Count == 0)
        {
            return;
        }

        await using ReviewsDbContext db = await ReviewsSchema.Open(contexts, cancellationToken);
        List<Review> reviews = await db.Reviews
            .Where(review => revisionIds.Contains(review.RevisionId))
            .ToListAsync(cancellationToken);
        db.Reviews.RemoveRange(reviews);
        await db.SaveChangesAsync(cancellationToken);
    }
}