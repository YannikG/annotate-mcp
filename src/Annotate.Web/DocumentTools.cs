using System.ComponentModel;

using ModelContextProtocol.Server;

namespace Annotate.Web;

[McpServerToolType]
internal sealed class DocumentTools
{
    [McpServerTool(Name = "annotate_document"), Description("Returns immediately with a pending document review and a review URL. Send that URL to the user before await_document_review. There is no approval. The user requests changes, or sets the document read-only. agent and model are required on every call.")]
    public static Task<string> AnnotateDocument(
        IDocumentHost host,
        IHttpContextAccessor http,
        McpServer server,
        string document,
        string? summary = null,
        string? previousReviewId = null,
        string? sessionId = null,
        string? cwd = null,
        [Description("Required. Agent product making this call, for example Cursor, Claude Code, or Codex CLI.")] string? agent = null,
        [Description("Required. Exact model id currently generating this document. Send it on every call.")] string? model = null,
        CancellationToken cancellationToken = default)
    {
        string requestHost = http.HttpContext?.Request.Host.Value ?? "";
        return host.SubmitAsync(
            new DocumentSubmission(
                document,
                summary,
                previousReviewId,
                sessionId,
                cwd,
                agent,
                model,
                server.ClientInfo?.Name,
                server.ClientInfo?.Version),
            requestHost,
            cancellationToken);
    }

    [McpServerTool(Name = "await_document_review"), Description("Call only after the user has the review URL. Returns changes_requested or read_only. A pending result is normal. Call again with the same review id. Never returns approved.")]
    public static Task<string> AwaitDocumentReview(
        IDocumentHost host,
        string reviewId,
        int? waitSeconds = null,
        CancellationToken cancellationToken = default) =>
        host.WaitAsync(reviewId, waitSeconds, cancellationToken);

    [McpServerTool(Name = "archive_document"), Description(ArchiveDocumentText.Notice)]
    public static Task<string> ArchiveDocument(
        IDocumentHost host,
        [Description("Document ID from annotate_document. This does not submit a revision.")] string documentId,
        CancellationToken cancellationToken = default) =>
        host.ArchiveAsync(documentId, cancellationToken);
}