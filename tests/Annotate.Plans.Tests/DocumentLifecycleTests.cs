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
    public async Task LockingAHeadingKeepsTheTextUnderIt()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IDocuments documents = host.GetRequiredService<IDocuments>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));
        const string markdown = """
            # Audit

            Intro stays.

            ## Method

            Read the notes.

            ## Next

            Other text.
            """;

        DocumentSubmitOutcome.Created created = Assert.IsType<DocumentSubmitOutcome.Created>(
            await documents.SubmitAsync(Request(markdown, folder, null), CancellationToken.None));
        DocumentRevisionDetail first = Assert.IsType<DocumentRevisionDetail>(
            await documents.RevisionAsync(created.Revision, CancellationToken.None));
        string method = first.Blocks.Single(block => Slice(first, block) == "## Method").Key;
        Assert.IsType<DocumentChange.Done>(
            await documents.SetBlockLockAsync(created.Document, method, true, CancellationToken.None));

        DocumentRevisionDetail locked = Assert.IsType<DocumentRevisionDetail>(
            await documents.RevisionAsync(created.Revision, CancellationToken.None));
        Assert.True(locked.Blocks.Single(block => Slice(locked, block) == "## Method").Locked);
        Assert.True(locked.Blocks.Single(block => Slice(locked, block) == "Read the notes.").Locked);
        Assert.False(locked.Blocks.Single(block => Slice(locked, block) == "Intro stays.").Locked);
        Assert.False(locked.Blocks.Single(block => Slice(locked, block) == "Other text.").Locked);

        DocumentSubmitOutcome changedBody = await documents.SubmitAsync(
            Request(markdown.Replace("Read the notes.", "Read the site.", StringComparison.Ordinal), folder, created.Revision.Value),
            CancellationToken.None);
        Assert.Equal("A locked block was changed.", Assert.IsType<DocumentSubmitOutcome.Refused>(changedBody).Error);

        DocumentSubmitOutcome.Created second = Assert.IsType<DocumentSubmitOutcome.Created>(
            await documents.SubmitAsync(
                Request(markdown.Replace("Other text.", "Other words.", StringComparison.Ordinal), folder, created.Revision.Value),
                CancellationToken.None));
        DocumentRevisionDetail revised = Assert.IsType<DocumentRevisionDetail>(
            await documents.RevisionAsync(second.Revision, CancellationToken.None));
        Assert.Equal("Read the notes.", Slice(revised, revised.Blocks.Single(block => Slice(revised, block) == "Read the notes.")));
        Assert.True(revised.Blocks.Single(block => Slice(revised, block) == "Read the notes.").Locked);
        Assert.Equal("Other words.", Slice(revised, revised.Blocks.Single(block => Slice(revised, block) == "Other words.")));
    }

    [Fact]
    public async Task LockingAHeadingDoesNotLockALaterHeadingWithTheSameTitle()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IDocuments documents = host.GetRequiredService<IDocuments>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));
        const string markdown = "# Notes\n\nFirst.\n\n# Notes\n\nSecond.\n";
        DocumentSubmitOutcome.Created created = Assert.IsType<DocumentSubmitOutcome.Created>(
            await documents.SubmitAsync(Request(markdown, folder, null), CancellationToken.None));
        DocumentRevisionDetail first = Assert.IsType<DocumentRevisionDetail>(
            await documents.RevisionAsync(created.Revision, CancellationToken.None));
        string firstHeading = first.Blocks.First(block => Slice(first, block) == "# Notes").Key;
        Assert.IsType<DocumentChange.Done>(
            await documents.SetBlockLockAsync(created.Document, firstHeading, true, CancellationToken.None));

        DocumentRevisionDetail locked = Assert.IsType<DocumentRevisionDetail>(
            await documents.RevisionAsync(created.Revision, CancellationToken.None));
        Assert.True(locked.Blocks.Single(block => Slice(locked, block) == "First.").Locked);
        Assert.False(locked.Blocks.Single(block => Slice(locked, block) == "Second.").Locked);

        DocumentSubmitOutcome.Created second = Assert.IsType<DocumentSubmitOutcome.Created>(
            await documents.SubmitAsync(
                Request(markdown.Replace("Second.", "Changed.", StringComparison.Ordinal), folder, created.Revision.Value),
                CancellationToken.None));
        Assert.Equal(2, second.Number);
    }

    [Fact]
    public async Task ANewerLockDoesNotBlockContinuingAnOlderRevision()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IDocuments documents = host.GetRequiredService<IDocuments>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));
        DocumentSubmitOutcome.Created first = Assert.IsType<DocumentSubmitOutcome.Created>(
            await documents.SubmitAsync(Request("# Audit\n\nFirst.\n", folder, null), CancellationToken.None));
        DocumentSubmitOutcome.Created second = Assert.IsType<DocumentSubmitOutcome.Created>(
            await documents.SubmitAsync(
                Request("# Audit\n\nFirst.\n\n## New\n\nLater.\n", folder, first.Revision.Value),
                CancellationToken.None));
        DocumentRevisionDetail newer = Assert.IsType<DocumentRevisionDetail>(
            await documents.RevisionAsync(second.Revision, CancellationToken.None));
        string newHeading = newer.Blocks.Single(block => Slice(newer, block) == "## New").Key;
        Assert.IsType<DocumentChange.Done>(
            await documents.SetBlockLockAsync(first.Document, newHeading, true, CancellationToken.None));

        DocumentSubmitOutcome.Created branch = Assert.IsType<DocumentSubmitOutcome.Created>(
            await documents.SubmitAsync(
                Request("# Audit\n\nChanged from first.\n", folder, first.Revision.Value),
                CancellationToken.None));

        Assert.Equal(3, branch.Number);
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

    private static string Slice(DocumentRevisionDetail revision, RevisionBlock block) =>
        revision.Markdown[block.Start..block.End].Trim();

    private static SubmitDocument Request(string markdown, string folder, string? parent) =>
        new(markdown, null, folder, null, parent, "Cursor", "claude-opus-4");
}