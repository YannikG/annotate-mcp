namespace Annotate.Web;

public interface IDocumentHost
{
    Task<string> SubmitAsync(DocumentSubmission submission, string requestHost, CancellationToken cancellationToken);

    Task<string> WaitAsync(string reviewId, int? waitSeconds, CancellationToken cancellationToken);

    Task<string> ArchiveAsync(string documentId, CancellationToken cancellationToken);
}

public sealed record DocumentSubmission(
    string Document,
    string? Summary,
    string? PreviousReviewId,
    string? SessionId,
    string? FolderPath,
    string? Agent,
    string? Model,
    string? ClientName,
    string? ClientVersion);