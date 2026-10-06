namespace Annotate.Reviews.Application;

public interface IReviews
{
    Task<OpenOutcome> OpenAsync(RevisionId revision, string markdown, CancellationToken cancellationToken);

    Task<ReviewDetail?> FindAsync(ReviewId id, CancellationToken cancellationToken);

    Task<ReviewDetail?> ForRevisionAsync(RevisionId revision, CancellationToken cancellationToken);

    Task<IReadOnlyList<PendingReview>> PendingAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ListedReview>> ListAsync(CancellationToken cancellationToken);

    Task<WaitOutcome> WaitAsync(ReviewId id, TimeSpan timeout, CancellationToken cancellationToken);

    Task<DecideOutcome> ApproveAsync(ReviewId id, CancellationToken cancellationToken);

    Task<DecideOutcome> RequestChangesAsync(
        ReviewId id,
        IReadOnlyList<Annotation> annotations,
        CancellationToken cancellationToken);

    Task<SaveAnnotationsOutcome> SaveAnnotationsAsync(
        ReviewId id,
        IReadOnlyList<Annotation> annotations,
        CancellationToken cancellationToken);

    Task<SaveAnswerOutcome> SaveAnswerAsync(
        ReviewId id,
        DecisionAnswer answer,
        CancellationToken cancellationToken);

    Task RemoveRevisionsAsync(IReadOnlyList<string> revisionIds, CancellationToken cancellationToken);
}