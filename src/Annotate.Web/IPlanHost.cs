namespace Annotate.Web;

public interface IPlanHost
{
    Task<string> SubmitAsync(PlanSubmission submission, string requestHost, CancellationToken cancellationToken);

    Task<string> WaitAsync(string reviewId, int? waitSeconds, CancellationToken cancellationToken);

    Task<string> ArchiveAsync(string planId, CancellationToken cancellationToken);

    Task<string> ListBlocksAsync(string revisionId, CancellationToken cancellationToken);

    Task<string> ReadBlockAsync(string revisionId, string blockId, CancellationToken cancellationToken);

    string Guide();
}

public sealed record PlanSubmission(
    string Plan,
    string? Summary,
    string? PreviousReviewId,
    string? SessionId,
    string? FolderPath,
    string? StoryUrl,
    string? AcceptanceCriteria,
    string? Agent = null,
    string? Model = null,
    string? ClientName = null,
    string? ClientVersion = null);