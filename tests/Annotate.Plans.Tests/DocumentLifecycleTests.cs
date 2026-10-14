using Annotate.Plans.Application;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Plans.Tests;

public sealed class DocumentLifecycleTests
{
    [Fact]
    public async Task SubmitRevisesOneDocumentUntilItIsReadOnlyOrALockedBlockChanges()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IDocuments documents = host.GetRequiredService<IDocuments>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));

        DocumentSubmitOutcome.Created created = Assert.IsType<DocumentSubmitOutcome.Created>(
            await documents.SubmitAsync(Request("# Audit\n\nScope stays.\n\nNext section.\n", folder, null), CancellationToken.None));
        Assert.Equal(1, created.Number);

        DocumentRevisionDetail first = Assert.IsType<DocumentRevisionDetail>(
            await documents.RevisionAsync(created.Revision, CancellationToken.None));
        string locked = first.Blocks[1].Key;
        Assert.IsType<DocumentChange.Done>(
            await documents.SetBlockLockAsync(created.Document, locked, true, CancellationToken.None));

        DocumentSubmitOutcome changedLock = await documents.SubmitAsync(
            Request("# Audit\n\nScope moved.\n\nNext section.\n", folder, created.Revision.Value),
            CancellationToken.None);
        Assert.Equal("A locked block was changed.", Assert.IsType<DocumentSubmitOutcome.Refused>(changedLock).Error);
        Assert.Equal(1, Assert.Single((await documents.DocumentAsync(created.Document, CancellationToken.None))!.Revisions).Number);

        DocumentSubmitOutcome.Created second = Assert.IsType<DocumentSubmitOutcome.Created>(
            await documents.SubmitAsync(
                Request("# Audit\n\nScope stays.\n\nNext section changed.\n", folder, created.Revision.Value),
                CancellationToken.None));
        Assert.Equal(2, second.Number);
        Assert.Equal(created.Document, second.Document);
        DocumentRevisionDetail revised = Assert.IsType<DocumentRevisionDetail>(
            await documents.RevisionAsync(second.Revision, CancellationToken.None));
        Assert.Equal(locked, revised.Blocks[1].Key);
        Assert.True(revised.Blocks[1].Locked);
        Assert.False(revised.Blocks[2].Locked);

        Assert.IsType<DocumentChange.Done>(
            await documents.SetReadOnlyAsync(created.Document, true, CancellationToken.None));
        DocumentSubmitOutcome readOnly = await documents.SubmitAsync(
            Request("# Audit\n\nScope stays.\n\nNext section changed.\n\nMore.\n", folder, second.Revision.Value),
            CancellationToken.None);
        Assert.IsType<DocumentSubmitOutcome.ReadOnlyDocument>(readOnly);
        Assert.Equal(2, (await documents.DocumentAsync(created.Document, CancellationToken.None))!.Revisions.Count);

        Assert.IsType<DocumentChange.Done>(
            await documents.SetReadOnlyAsync(created.Document, false, CancellationToken.None));
        DocumentSubmitOutcome.Created third = Assert.IsType<DocumentSubmitOutcome.Created>(
            await documents.SubmitAsync(
                Request("# Audit\n\nScope stays.\n\nNext section changed.\n\nMore.\n", folder, second.Revision.Value),
                CancellationToken.None));
        Assert.Equal(3, third.Number);
    }

    [Fact]
    public async Task ArchiveAndProjectDeleteRemoveDocumentRevisions()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        IDocuments documents = host.GetRequiredService<IDocuments>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));
        DocumentSubmitOutcome.Created created = Assert.IsType<DocumentSubmitOutcome.Created>(
            await documents.SubmitAsync(Request("# Audit\n", folder, null), CancellationToken.None));

        Assert.IsType<DocumentChange.AlreadyArchived>(
            await documents.ArchiveAsync(created.Document, CancellationToken.None) is DocumentChange.Done
                ? await documents.ArchiveAsync(created.Document, CancellationToken.None)
                : throw new InvalidOperationException("Archive did not succeed."));
        Assert.IsType<DocumentSubmitOutcome.DocumentArchived>(
            await documents.SubmitAsync(Request("# Audit\n\nAgain.\n", folder, created.Revision.Value), CancellationToken.None));
        Assert.Contains(
            await documents.ArchivedDocumentsAsync(created.Project, CancellationToken.None),
            document => document.DocumentId == created.Document);
        Assert.DoesNotContain(
            await documents.DocumentsAsync(created.Project, CancellationToken.None),
            document => document.DocumentId == created.Document);

        ProjectChange.Done deleted = Assert.IsType<ProjectChange.Done>(
            await plans.DeleteProjectAsync(created.Project, CancellationToken.None));
        Assert.Contains(created.Revision.Value, deleted.RevisionIds);
        Assert.Null(await documents.DocumentAsync(created.Document, CancellationToken.None));
        Assert.Null(await documents.RevisionAsync(created.Revision, CancellationToken.None));
    }

    private static SubmitDocument Request(string markdown, string folder, string? parent) =>
        new(markdown, null, folder, null, parent, "Cursor", "claude-opus-4");
}