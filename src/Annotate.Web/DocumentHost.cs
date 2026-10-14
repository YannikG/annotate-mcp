using System.ComponentModel;

using Annotate.Plans.Application;
using Annotate.Reviews.Application;

using ReviewRevisionId = Annotate.Reviews.Application.RevisionId;

namespace Annotate.Web;

internal sealed class DocumentHost(
    IDocuments documents,
    IReviews reviews,
    IConfiguration configuration,
    IReviewBrowser browser) : IDocumentHost
{
    public async Task<string> SubmitAsync(
        DocumentSubmission submission,
        string requestHost,
        CancellationToken cancellationToken)
    {
        if (!LoopbackHost.Accepts(requestHost))
        {
            return "Error: the review host must be loopback.";
        }

        string? parentRevisionId = null;
        if (!string.IsNullOrWhiteSpace(submission.PreviousReviewId))
        {
            ReviewDetail? previous = await reviews.FindAsync(new ReviewId(submission.PreviousReviewId), cancellationToken);
            if (previous is null)
            {
                return Unknown(submission.PreviousReviewId);
            }

            parentRevisionId = previous.RevisionId.Value;
        }

        DocumentSubmitOutcome outcome = await documents.SubmitAsync(
            new SubmitDocument(
                submission.Document,
                submission.Summary,
                submission.FolderPath,
                submission.SessionId,
                parentRevisionId,
                submission.Agent,
                submission.Model,
                submission.ClientName,
                submission.ClientVersion),
            cancellationToken);
        return outcome switch
        {
            DocumentSubmitOutcome.ProjectArchived archived => ArchivedProject(archived.DisplayName),
            DocumentSubmitOutcome.DocumentArchived archived => ArchivedDocument(archived.Title),
            DocumentSubmitOutcome.ReadOnlyDocument readOnly => ReadOnly(readOnly.Title),
            DocumentSubmitOutcome.Refused refused => Error(refused.Error),
            DocumentSubmitOutcome.Created created => await Opened(created, submission.Document, requestHost, cancellationToken),
            _ => throw new InvalidOperationException("Document submit was not recognised."),
        };
    }

    public async Task<string> WaitAsync(string reviewId, int? waitSeconds, CancellationToken cancellationToken)
    {
        int seconds = WaitSeconds(waitSeconds);
        ReviewDetail? review = string.IsNullOrWhiteSpace(reviewId)
            ? null
            : await reviews.FindAsync(new ReviewId(reviewId.Trim()), cancellationToken);
        if (review is null)
        {
            return Unknown(reviewId);
        }

        DocumentRevisionDetail? revision = await documents.RevisionAsync(
            new Annotate.Plans.Application.RevisionId(review.RevisionId.Value),
            cancellationToken);
        if (revision is null)
        {
            return Error("This review belongs to a plan.");
        }

        string? decided = Decided(review, revision);
        if (decided is not null)
        {
            return decided;
        }

        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(seconds);
        while (DateTimeOffset.UtcNow < deadline)
        {
            TimeSpan slice = deadline - DateTimeOffset.UtcNow;
            if (slice > TimeSpan.FromSeconds(1))
            {
                slice = TimeSpan.FromSeconds(1);
            }

            await reviews.WaitAsync(review.Id, slice, cancellationToken);
            review = await reviews.FindAsync(review.Id, cancellationToken);
            revision = review is null
                ? null
                : await documents.RevisionAsync(new Annotate.Plans.Application.RevisionId(review.RevisionId.Value), cancellationToken);
            if (review is null || revision is null)
            {
                return Unknown(reviewId);
            }

            decided = Decided(review, revision);
            if (decided is not null)
            {
                return decided;
            }
        }

        return StillPending(review.Id.Value);
    }

    public async Task<string> ArchiveAsync(string documentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            return Error("Document was not found.");
        }

        DocumentChange outcome = await documents.ArchiveAsync(new DocumentId(documentId.Trim()), cancellationToken);
        return outcome switch
        {
            DocumentChange.Refused refused => Error(refused.Error),
            DocumentChange.AlreadyArchived => ArchiveResult(documentId.Trim(), already: true),
            DocumentChange.Done => ArchiveResult(documentId.Trim(), already: false),
            _ => throw new InvalidOperationException("Document archive was not recognised."),
        };
    }

    private async Task<string> Opened(
        DocumentSubmitOutcome.Created created,
        string markdown,
        string requestHost,
        CancellationToken cancellationToken)
    {
        OpenOutcome opened = await reviews.OpenAsync(
            new ReviewRevisionId(created.Revision.Value),
            markdown,
            cancellationToken);
        if (opened is OpenOutcome.Refused refused)
        {
            return Error(refused.Error);
        }

        OpenOutcome.Opened review = (OpenOutcome.Opened)opened;
        if (configuration.GetValue("Annotate:OpenBrowser", false))
        {
            try
            {
                browser.TryOpen($"http://{requestHost}/review/{review.Id.Value}");
            }
            catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or PlatformNotSupportedException)
            {
                // Opening a browser is optional.
            }
        }

        return Pending(review.Id.Value, created.Document.Value, created.Number, requestHost);
    }

    private int WaitSeconds(int? waitSeconds)
    {
        int limit = Math.Max(0, configuration.GetValue("Annotate:MaxWaitSeconds", 600));
        int seconds = waitSeconds ?? configuration.GetValue("Annotate:DefaultWaitSeconds", 50);
        if (seconds < 0)
        {
            return 0;
        }

        return seconds > limit ? limit : seconds;
    }

    private static string? Decided(ReviewDetail review, DocumentRevisionDetail revision)
    {
        if (review.Status == ReviewStatus.ChangesRequested)
        {
            return Changes(review.Id.Value, revision.DocumentId.Value, review.Feedback ?? "");
        }

        if (review.Status == ReviewStatus.Approved)
        {
            return Error("Documents have no approval.");
        }

        return revision.ReadOnly ? ReadOnlyResult(revision.DocumentId.Value) : null;
    }

    private static string Pending(string reviewId, string documentId, int number, string requestHost) =>
        $"""
        Document review status: pending.
        Review ID: {reviewId}
        Document ID: {documentId}
        Revision: {number}
        Review URL: http://{requestHost}/review/{reviewId}

        Send the Review URL to the user now, before you wait. Do not call `await_document_review` until the user has that URL.
        """;

    private static string StillPending(string reviewId) =>
        $"""
        Document review status: pending.
        Review ID: {reviewId}

        The review is still pending. Call `await_document_review` again.
        """;

    private static string Changes(string reviewId, string documentId, string feedback) =>
        $"""
        Document review status: changes_requested.
        Document ID: {documentId}

        ## User feedback

        {feedback}

        Revise the document using this feedback, then submit the revised draft once via `annotate_document` with previousReviewId="{reviewId}".
        """;

    private static string ReadOnlyResult(string documentId) =>
        $"""
        Document review status: read_only.
        Document ID: {documentId}

        The document is read-only. Do not submit a revision. Ask the user before changing it.
        """;

    private static string ReadOnly(string title) =>
        $"""
        This document is read-only.
        Document: {title}

        Ask the user to lift read-only before submitting another revision.
        """;

    private static string ArchiveResult(string documentId, bool already) =>
        $"""
        {(already ? "This document is already archived." : "Document archived.")}
        Document ID: {documentId}

        {ArchiveDocumentText.Notice}
        """;

    private static string ArchivedDocument(string title) =>
        $"""
        This document is archived.
        Document: {title}

        Archive is not a new revision. Ask the user whether to restore this document or start a new one. Do not submit again until they answer.
        """;

    private static string ArchivedProject(string displayName) =>
        $"""
        This project got archived.
        Project: {displayName}

        Ask the user what to do. Do not submit the document again until they answer.
        """;

    private static string Unknown(string reviewId) =>
        $"Error: unknown or expired reviewId \"{reviewId}\". Submit the document again with annotate_document.";

    private static string Error(string message) =>
        message.StartsWith("Error: ", StringComparison.Ordinal) ? message : "Error: " + message;
}