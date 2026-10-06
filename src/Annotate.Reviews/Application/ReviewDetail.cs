namespace Annotate.Reviews.Application;

public sealed record ReviewDetail(
    ReviewId Id,
    RevisionId RevisionId,
    ReviewStatus Status,
    string? Feedback,
    IReadOnlyList<Annotation> Annotations,
    IReadOnlyList<DecisionAnswer> Answers,
    IReadOnlyList<ReviewPrompt> Prompts,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DecidedAt);

public sealed record PendingReview(ReviewId Id, RevisionId RevisionId, DateTimeOffset CreatedAt);

public sealed record ListedReview(
    ReviewId Id,
    RevisionId RevisionId,
    ReviewStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DecidedAt = null,
    int AnnotationCount = 0,
    int AnswerCount = 0);