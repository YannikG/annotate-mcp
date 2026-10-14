using Annotate.Plans.Application;
using Annotate.Plans.Domain;

using Microsoft.EntityFrameworkCore;

namespace Annotate.Plans.Infrastructure;

internal sealed class DocumentCatalog(IDbContextFactory<PlansDbContext> contexts)
{
    public async Task<IReadOnlyList<DocumentSummary>> ListAsync(
        ProjectId id,
        bool archived,
        CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        List<Document> documents = await db.Documents
            .Where(document => document.ProjectId == id.Value && (archived ? document.ArchivedAt != null : document.ArchivedAt == null))
            .ToListAsync(cancellationToken);
        Dictionary<string, int> counts = await Counts(db, documents, cancellationToken);
        return documents
            .OrderByDescending(document => document.UpdatedAt)
            .ThenBy(document => document.Id, StringComparer.Ordinal)
            .Select(document => new DocumentSummary(
                new DocumentId(document.Id),
                document.Title,
                document.UpdatedAt,
                counts.GetValueOrDefault(document.Id),
                document.ReadOnlyAt is not null,
                document.ArchivedAt is not null))
            .ToArray();
    }

    public async Task<DocumentChange> ArchiveAsync(DocumentId id, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Document? document = await db.Documents.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (document is null)
        {
            return new DocumentChange.Refused("Document was not found.");
        }

        if (document.ArchivedAt is not null)
        {
            return new DocumentChange.AlreadyArchived();
        }

        document.Archive(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return new DocumentChange.Done([]);
    }

    public async Task<DocumentChange> RestoreAsync(DocumentId id, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Document? document = await db.Documents.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (document is null)
        {
            return new DocumentChange.Refused("Document was not found.");
        }

        document.Restore();
        await db.SaveChangesAsync(cancellationToken);
        return new DocumentChange.Done([]);
    }

    public async Task<DocumentChange> DeleteAsync(DocumentId id, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Document? document = await db.Documents.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (document is null)
        {
            return new DocumentChange.Refused("Document was not found.");
        }

        List<Revision> revisions = await db.Revisions
            .Where(revision => revision.DocumentId == document.Id)
            .OrderBy(revision => revision.Number)
            .ToListAsync(cancellationToken);
        foreach (Revision revision in revisions)
        {
            revision.DetachParent();
        }

        await db.SaveChangesAsync(cancellationToken);
        db.Documents.Remove(document);
        await db.SaveChangesAsync(cancellationToken);
        return new DocumentChange.Done(revisions.Select(revision => revision.Id).ToArray());
    }

    public async Task<DocumentChange> SetReadOnlyAsync(DocumentId id, bool makeReadOnly, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Document? document = await db.Documents.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (document is null)
        {
            return new DocumentChange.Refused("Document was not found.");
        }

        document.SetReadOnly(makeReadOnly ? DateTimeOffset.UtcNow : null);
        await db.SaveChangesAsync(cancellationToken);
        return new DocumentChange.Done([]);
    }

    public async Task<DocumentChange> SetBlockLockAsync(
        DocumentId id,
        string blockKey,
        bool locked,
        CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Document? document = await db.Documents.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (document is null)
        {
            return new DocumentChange.Refused("Document was not found.");
        }

        if (document.ReadOnlyAt is not null)
        {
            return new DocumentChange.Refused("Document is read-only.");
        }

        string key = blockKey.Trim();
        Revision? newest = await db.Revisions
            .Where(revision => revision.DocumentId == document.Id)
            .OrderByDescending(revision => revision.Number)
            .FirstOrDefaultAsync(cancellationToken);
        bool present = newest is not null && await db.Blocks.AnyAsync(
            block => block.RevisionId == newest.Id && block.BlockKey == key,
            cancellationToken);
        if (!present)
        {
            return new DocumentChange.Refused("Block was not found.");
        }

        BlockLock? existing = await db.BlockLocks.SingleOrDefaultAsync(
            item => item.DocumentId == document.Id && item.BlockKey == key,
            cancellationToken);
        if (locked && existing is null)
        {
            db.Add(new BlockLock(document.Id, key));
        }
        else if (!locked && existing is not null)
        {
            db.BlockLocks.Remove(existing);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new DocumentChange.Done([]);
    }

    public async Task<DocumentDetail?> DocumentAsync(DocumentId id, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Document? document = await db.Documents.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var rows = await db.Revisions
            .Where(revision => revision.DocumentId == document.Id)
            .OrderBy(revision => revision.Number)
            .Select(revision => new
            {
                revision.Id,
                revision.Number,
                revision.CreatedAt,
                revision.Agent,
                revision.Model,
                revision.ClientName,
                revision.ClientVersion,
            })
            .ToListAsync(cancellationToken);
        List<string> locks = await db.BlockLocks
            .Where(item => item.DocumentId == document.Id)
            .Select(item => item.BlockKey)
            .ToListAsync(cancellationToken);
        return new DocumentDetail(
            new DocumentId(document.Id),
            new ProjectId(document.ProjectId),
            document.Title,
            document.SessionId,
            rows.Select(row => new PlanRevision(
                new RevisionId(row.Id),
                row.Number,
                row.CreatedAt,
                new Attribution(row.Agent, row.Model, row.ClientName, row.ClientVersion))).ToArray(),
            document.ArchivedAt is not null,
            document.ReadOnlyAt is not null,
            locks);
    }

    public async Task<DocumentRevisionDetail?> RevisionAsync(RevisionId id, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Revision? revision = await db.Revisions.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (revision?.DocumentId is null)
        {
            return null;
        }

        Document? document = await db.Documents.SingleOrDefaultAsync(
            item => item.Id == revision.DocumentId,
            cancellationToken);
        if (document is null)
        {
            return null;
        }

        List<StoredBlock> blocks = await db.Blocks
            .Where(block => block.RevisionId == revision.Id)
            .OrderBy(block => block.Ordinal)
            .ToListAsync(cancellationToken);
        HashSet<string> locks = SectionLocks.Covered(
            blocks,
            (await db.BlockLocks
                .Where(item => item.DocumentId == document.Id)
                .Select(item => item.BlockKey)
                .ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal));
        List<StoredBlock> parent = revision.ParentRevisionId is null
            ? []
            : await db.Blocks.Where(block => block.RevisionId == revision.ParentRevisionId).ToListAsync(cancellationToken);
        Dictionary<string, StoredBlock> previous = parent.ToDictionary(block => block.BlockKey, StringComparer.Ordinal);
        return new DocumentRevisionDetail(
            new RevisionId(revision.Id),
            new DocumentId(document.Id),
            revision.Number,
            revision.Markdown,
            revision.Summary,
            blocks.Select(block => new RevisionBlock(
                block.BlockKey,
                Enum.Parse<Annotate.Markdown.BlockKind>(block.Kind),
                block.SectionPath,
                block.SourceStart,
                block.SourceEnd,
                Change(block, previous),
                block.ContentHash,
                locks.Contains(block.BlockKey))).ToArray(),
            revision.ParentRevisionId,
            new Attribution(revision.Agent, revision.Model, revision.ClientName, revision.ClientVersion),
            document.ReadOnlyAt is not null);
    }

    private static BlockChange Change(StoredBlock block, Dictionary<string, StoredBlock> parent)
    {
        if (!parent.TryGetValue(block.BlockKey, out StoredBlock? previous))
        {
            return BlockChange.Added;
        }

        if (previous.ContentHash == block.ContentHash && previous.SectionPath == block.SectionPath)
        {
            return BlockChange.Unchanged;
        }

        return BlockChange.Changed;
    }

    private static async Task<Dictionary<string, int>> Counts(
        PlansDbContext db,
        List<Document> documents,
        CancellationToken cancellationToken)
    {
        if (documents.Count == 0)
        {
            return [];
        }

        string[] ids = documents.Select(document => document.Id).ToArray();
        return await db.Revisions
            .Where(revision => revision.DocumentId != null && ids.Contains(revision.DocumentId))
            .GroupBy(revision => revision.DocumentId!)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);
    }
}