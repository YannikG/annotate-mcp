using System.Net;

using Annotate.Plans.Application;
using Annotate.Web;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Web.Tests;

public sealed class DocumentHostTests
{
    [Fact]
    public async Task DocumentReviewReturnsReadOnlyAndHidesApprove()
    {
        await using AnnotateApp app = new();
        IDocumentHost host = app.Services.GetRequiredService<IDocumentHost>();
        IPlanHost planHost = app.Services.GetRequiredService<IPlanHost>();
        IDocuments documents = app.Services.GetRequiredService<IDocuments>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));
        string submitted = await host.SubmitAsync(
            new DocumentSubmission("# Audit\n\nScope stays.\n", null, null, null, folder, "Cursor", "grok-4.7", null, null),
            "localhost",
            CancellationToken.None);
        Assert.Contains("Document review status: pending.", submitted, StringComparison.Ordinal);
        string reviewId = Line(submitted, "Review ID: ");
        string documentId = Line(submitted, "Document ID: ");

        string waiting = await host.WaitAsync(reviewId, 0, CancellationToken.None);
        Assert.Contains("Call `await_document_review` again.", waiting, StringComparison.Ordinal);

        DocumentDetail detail = Assert.IsType<DocumentDetail>(
            await documents.DocumentAsync(new DocumentId(documentId), CancellationToken.None));
        string revisionId = Assert.Single(detail.Revisions).RevisionId.Value;
        string list = await planHost.ListBlocksAsync(revisionId, CancellationToken.None);
        Assert.Contains("locked: false", list, StringComparison.Ordinal);

        string blockId = list.Split('\n')[0]["blockId: ".Length..];
        Assert.IsType<DocumentChange.Done>(
            await documents.SetBlockLockAsync(new DocumentId(documentId), blockId, true, CancellationToken.None));
        Assert.Contains("locked: true", await planHost.ListBlocksAsync(revisionId, CancellationToken.None), StringComparison.Ordinal);
        Assert.Equal(
            "Error: Block is locked.",
            await planHost.AnnotateBlockAsync(reviewId, blockId, "leave this", CancellationToken.None));

        string planSubmitted = await planHost.SubmitAsync(
            new PlanSubmission("# Plan\n", null, null, null, folder, null, null, "Cursor", "grok-4.7"),
            "localhost",
            CancellationToken.None);
        Assert.Contains(
            "This review belongs to a plan.",
            await host.WaitAsync(Line(planSubmitted, "Review ID: "), 0, CancellationToken.None),
            StringComparison.Ordinal);
        Assert.Contains(
            "This review belongs to a document.",
            await planHost.WaitAsync(reviewId, 0, CancellationToken.None),
            StringComparison.Ordinal);

        using HttpClient client = app.CreateClient(new() { AllowAutoRedirect = true });
        string reviewHtml = await client.GetStringAsync("/review/" + reviewId);
        Assert.Contains("data-request-changes", reviewHtml, StringComparison.Ordinal);
        Assert.Contains("data-lock-block", reviewHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("data-approve", reviewHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("data-auto-close", reviewHtml, StringComparison.Ordinal);
        string documentHtml = await client.GetStringAsync("/documents/" + documentId);
        Assert.DoesNotContain("data-lock-block", documentHtml, StringComparison.Ordinal);

        string documentsHtml = await client.GetStringAsync("/projects/" + detail.ProjectId.Value + "/documents");
        Assert.Contains("Audit", documentsHtml, StringComparison.Ordinal);
        Assert.Contains("data-tab=\"archived\"", documentsHtml, StringComparison.Ordinal);
        Assert.Contains("href=\"/projects/" + detail.ProjectId.Value + "\"", documentsHtml, StringComparison.Ordinal);
        Assert.Contains("Documents", documentsHtml, StringComparison.Ordinal);
        Assert.Contains("data-edit", documentsHtml, StringComparison.Ordinal);
        Assert.Contains("data-archive", documentsHtml, StringComparison.Ordinal);
        Assert.Contains("data-delete", documentsHtml, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/projects/" + detail.ProjectId.Value)).StatusCode);

        Assert.IsType<DocumentChange.Done>(
            await documents.SetReadOnlyAsync(new DocumentId(documentId), true, CancellationToken.None));
        Assert.Contains("read_only", await host.WaitAsync(reviewId, 0, CancellationToken.None), StringComparison.Ordinal);
        Assert.Contains("read-only", await host.SubmitAsync(
            new DocumentSubmission("# Audit\n\nScope stays.\n\nMore.\n", null, reviewId, null, folder, "Cursor", "grok-4.7", null, null),
            "localhost",
            CancellationToken.None), StringComparison.Ordinal);
    }

    private static string Line(string text, string label)
    {
        foreach (string line in text.Split('\n'))
        {
            if (line.StartsWith(label, StringComparison.Ordinal))
            {
                return line[label.Length..].Trim();
            }
        }

        throw new InvalidOperationException($"Missing {label}");
    }
}