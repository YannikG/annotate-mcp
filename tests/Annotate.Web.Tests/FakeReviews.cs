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

    public Task<SaveAnnotationsOutcome> SaveAnnotationsAsync(ReviewId id, IReadOnlyList<Annotation> annotations, CancellationToken cancellationToken)
    {
        DraftSaves.Add(new RequestedChanges(id.Value, annotations.ToArray()));
        if (DraftOutcome is SaveAnnotationsOutcome.Done && Review?.Id == id)
        {
            Review = Review with { Annotations = annotations.ToArray() };
        }
        return Task.FromResult(DraftOutcome);
    }

    public Task<SaveAnswerOutcome> SaveAnswerAsync(
        ReviewId id,
        DecisionAnswer answer,
        CancellationToken cancellationToken)
    {
        SavedAnswers.Add(new SavedDecision(id.Value, answer));
        return Task.FromResult(SaveOutcome);
    }

    public Task RemoveRevisionsAsync(IReadOnlyList<string> revisionIds, CancellationToken cancellationToken)
    {
        RemovedRevisions.AddRange(revisionIds);
        return Task.CompletedTask;
    }
}

internal sealed record RequestedChanges(string ReviewId, IReadOnlyList<Annotation> Annotations);

internal sealed record SavedDecision(string ReviewId, DecisionAnswer Answer);