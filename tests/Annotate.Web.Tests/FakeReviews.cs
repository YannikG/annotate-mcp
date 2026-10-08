using Annotate.Reviews.Application;

namespace Annotate.Web.Tests;

internal sealed class FakeReviews : IReviews
{
    public List<ListedReview> Listed { get; } = [];

    public Dictionary<string, ReviewDetail?> ReviewsByRevision { get; } = new(StringComparer.Ordinal);

    public ReviewDetail? Review { get; set; }

    public Exception? LoadError { get; set; }

    public List<string> Approved { get; } = [];

    public List<RequestedChanges> Changes { get; } = [];

    public DecideOutcome ApproveOutcome { get; set; } = new DecideOutcome.Done();

    public DecideOutcome RequestChangesOutcome { get; set; } = new DecideOutcome.Done();

    public List<SavedDecision> SavedAnswers { get; } = [];

    public List<string> RemovedRevisions { get; } = [];

    public SaveAnswerOutcome SaveOutcome { get; set; } = new SaveAnswerOutcome.Done();

    public Task<OpenOutcome> OpenAsync(RevisionId revision, string markdown, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<ReviewDetail?> FindAsync(ReviewId id, CancellationToken cancellationToken)
    {
        if (LoadError is not null)
        {
            return Task.FromException<ReviewDetail?>(LoadError);
        }

        return Task.FromResult(Review);
    }

    public Task<ReviewDetail?> ForRevisionAsync(RevisionId revision, CancellationToken cancellationToken) =>
        Task.FromResult(ReviewsByRevision.TryGetValue(revision.Value, out ReviewDetail? review) ? review : null);

    public Task<IReadOnlyList<PendingReview>> PendingAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PendingReview>>(
            Listed
                .Where(review => review.Status == ReviewStatus.Pending)
                .Select(review => new PendingReview(review.Id, review.RevisionId, review.CreatedAt))
                .ToArray());

    public Task<IReadOnlyList<ListedReview>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ListedReview>>(Listed);

    public Task<WaitOutcome> WaitAsync(ReviewId id, TimeSpan timeout, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<DecideOutcome> ApproveAsync(ReviewId id, CancellationToken cancellationToken)
    {
        Approved.Add(id.Value);
        return Task.FromResult(ApproveOutcome);
    }

    public Task<DecideOutcome> RequestChangesAsync(
        ReviewId id,
        IReadOnlyList<Annotation> annotations,
        CancellationToken cancellationToken)
    {
        Changes.Add(new RequestedChanges(id.Value, annotations.ToArray()));
        return Task.FromResult(RequestChangesOutcome);
    }

    public SaveAnnotationsOutcome DraftOutcome { get; set; } = new SaveAnnotationsOutcome.Done();

    public List<RequestedChanges> DraftSaves { get; } = [];

    public bool HoldSave { get; set; }

    public TaskCompletionSource ReleaseSave { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task<SaveAnnotationsOutcome> SaveAnnotationsAsync(ReviewId id, IReadOnlyList<Annotation> annotations, CancellationToken cancellationToken)
    {
        DraftSaves.Add(new RequestedChanges(id.Value, annotations.ToArray()));
        if (HoldSave)
        {
            await ReleaseSave.Task;
        }

        if (DraftOutcome is SaveAnnotationsOutcome.Done && Review?.Id == id)
        {
            Annotation[] kept = Review.Annotations.Where(item => item.BlockKey is not null).ToArray();
            Review = Review with { Annotations = [.. annotations, .. kept] };
        }

        return DraftOutcome;
    }

    public Task<SaveAnswerOutcome> SaveAnswerAsync(
        ReviewId id,
        DecisionAnswer answer,
        CancellationToken cancellationToken)
    {
        SavedAnswers.Add(new SavedDecision(id.Value, answer));
        return Task.FromResult(SaveOutcome);
    }

    public Task<SaveAnnotationsOutcome> AddBlockCommentAsync(
        ReviewId id,
        BlockComment comment,
        CancellationToken cancellationToken)
    {
        if (Review?.Id != id || string.IsNullOrWhiteSpace(comment.BlockKey) || string.IsNullOrWhiteSpace(comment.Comment))
        {
            return Task.FromResult<SaveAnnotationsOutcome>(new SaveAnnotationsOutcome.Refused("Comment was rejected."));
        }

        bool agent = comment.Author == AnnotationAuthor.Agent;
        Annotation note = new(
            Guid.NewGuid().ToString(),
            AnnotationKind.Comment,
            comment.Comment.Trim(),
            comment.Comment.Trim(),
            null,
            0,
            0,
            0,
            DateTimeOffset.UtcNow.ToString("u"),
            comment.BlockKey.Trim(),
            comment.Author,
            agent ? false : null);
        Review = Review with { Annotations = [.. Review.Annotations, note] };
        return Task.FromResult<SaveAnnotationsOutcome>(new SaveAnnotationsOutcome.Done());
    }

    public Task<SaveAnnotationsOutcome> AddReplyAsync(
        ReviewId id,
        string annotationId,
        string text,
        CancellationToken cancellationToken)
    {
        if (Review?.Id != id || string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult<SaveAnnotationsOutcome>(new SaveAnnotationsOutcome.Refused("Reply was rejected."));
        }

        Annotation? annotation = Review.Annotations.FirstOrDefault(item => item.Id == annotationId);
        if (annotation is null)
        {
            return Task.FromResult<SaveAnnotationsOutcome>(new SaveAnnotationsOutcome.Refused("Annotation was not found."));
        }

        AnnotationReply reply = new(Guid.NewGuid().ToString(), text.Trim(), DateTimeOffset.UtcNow.ToString("u"));
        AnnotationReply[] replies = annotation.Replies is null ? [reply] : [.. annotation.Replies, reply];
        Review = Review with
        {
            Annotations = Review.Annotations.Select(item => item.Id == annotationId ? item with { Replies = replies } : item).ToArray(),
        };
        return Task.FromResult<SaveAnnotationsOutcome>(new SaveAnnotationsOutcome.Done());
    }

    public Task<SaveAnnotationsOutcome> DeleteAnnotationAsync(
        ReviewId id,
        string annotationId,
        CancellationToken cancellationToken)
    {
        if (Review?.Id == id)
        {
            Review = Review with { Annotations = Review.Annotations.Where(item => item.Id != annotationId).ToArray() };
        }

        return Task.FromResult<SaveAnnotationsOutcome>(new SaveAnnotationsOutcome.Done());
    }

    public Task<SaveAnnotationsOutcome> DeleteReplyAsync(
        ReviewId id,
        string annotationId,
        string replyId,
        CancellationToken cancellationToken)
    {
        if (Review?.Id == id)
        {
            Review = Review with
            {
                Annotations = Review.Annotations.Select(item =>
                {
                    if (item.Id != annotationId || item.Replies is null)
                    {
                        return item;
                    }

                    AnnotationReply[] replies = item.Replies.Where(reply => reply.Id != replyId).ToArray();
                    return item with { Replies = replies.Length == 0 ? null : replies };
                }).ToArray(),
            };
        }

        return Task.FromResult<SaveAnnotationsOutcome>(new SaveAnnotationsOutcome.Done());
    }

    public Task<SaveAnnotationsOutcome> SetAcceptedAsync(
        ReviewId id,
        string annotationId,
        bool accepted,
        CancellationToken cancellationToken)
    {
        if (Review?.Id != id)
        {
            return Task.FromResult<SaveAnnotationsOutcome>(new SaveAnnotationsOutcome.Refused("Review was not found."));
        }

        Annotation? annotation = Review.Annotations.FirstOrDefault(item => item.Id == annotationId);
        if (annotation is null || annotation.Author != AnnotationAuthor.Agent)
        {
            return Task.FromResult<SaveAnnotationsOutcome>(new SaveAnnotationsOutcome.Refused("Accept applies to an agent note."));
        }

        Review = Review with
        {
            Annotations = Review.Annotations.Select(item => item.Id == annotationId ? item with { Accepted = accepted } : item).ToArray(),
        };
        return Task.FromResult<SaveAnnotationsOutcome>(new SaveAnnotationsOutcome.Done());
    }

    public Task RemoveRevisionsAsync(IReadOnlyList<string> revisionIds, CancellationToken cancellationToken)
    {
        RemovedRevisions.AddRange(revisionIds);
        return Task.CompletedTask;
    }
}

internal sealed record RequestedChanges(string ReviewId, IReadOnlyList<Annotation> Annotations);

internal sealed record SavedDecision(string ReviewId, DecisionAnswer Answer);