using Annotate.Markdown;
using Annotate.Plans.Application;
using Annotate.Plans.Domain;

using Microsoft.EntityFrameworkCore;

namespace Annotate.Plans.Infrastructure;

internal sealed class DocumentSubmitter(IDbContextFactory<PlansDbContext> contexts) : IDocuments
{
    private readonly DocumentCatalog _catalog = new(contexts);

    public async Task<DocumentSubmitOutcome> SubmitAsync(SubmitDocument request, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        string? error = RevisionLimits.Reject(request.Markdown, request.Summary, null, request.SessionId);
        if (error is not null)
        {
            return new DocumentSubmitOutcome.Refused(error);
        }

        ParseOutcome parsed = PlanMarkdown.Parse(request.Markdown);
        if (parsed is ParseOutcome.Invalid invalid)
        {
            return new DocumentSubmitOutcome.Refused(invalid.Error);
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        PlanDocument markdown = ((ParseOutcome.Ok)parsed).Document;
        if (string.IsNullOrWhiteSpace(request.ParentRevisionId))
        {
            return await Create(db, request, markdown, now, cancellationToken);
        }

        return await Continue(db, request, markdown, now, cancellationToken);
    }

    public Task<IReadOnlyList<DocumentSummary>> DocumentsAsync(ProjectId id, CancellationToken cancellationToken) =>
        _catalog.ListAsync(id, archived: false, cancellationToken);

    public Task<IReadOnlyList<DocumentSummary>> ArchivedDocumentsAsync(ProjectId id, CancellationToken cancellationToken) =>
        _catalog.ListAsync(id, archived: true, cancellationToken);

    public Task<DocumentChange> ArchiveAsync(DocumentId id, CancellationToken cancellationToken) =>
        _catalog.ArchiveAsync(id, cancellationToken);

    public Task<DocumentChange> RestoreAsync(DocumentId id, CancellationToken cancellationToken) =>
        _catalog.RestoreAsync(id, cancellationToken);

    public Task<DocumentChange> DeleteAsync(DocumentId id, CancellationToken cancellationToken) =>
        _catalog.DeleteAsync(id, cancellationToken);

    public Task<DocumentChange> SetReadOnlyAsync(DocumentId id, bool makeReadOnly, CancellationToken cancellationToken) =>
        _catalog.SetReadOnlyAsync(id, makeReadOnly, cancellationToken);

    public Task<DocumentChange> SetBlockLockAsync(
        DocumentId id,
        string blockKey,
        bool locked,
        CancellationToken cancellationToken) =>
        _catalog.SetBlockLockAsync(id, blockKey, locked, cancellationToken);

    public Task<DocumentDetail?> DocumentAsync(DocumentId id, CancellationToken cancellationToken) =>
        _catalog.DocumentAsync(id, cancellationToken);

    public Task<DocumentRevisionDetail?> RevisionAsync(RevisionId id, CancellationToken cancellationToken) =>
        _catalog.RevisionAsync(id, cancellationToken);

    private static async Task<DocumentSubmitOutcome> Create(
        PlansDbContext db,
        SubmitDocument request,
        PlanDocument markdown,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        (Project? project, string? archivedName) = await FindOrAddProject(db, request.FolderPath, now, cancellationToken);
        if (archivedName is not null)
        {
            return new DocumentSubmitOutcome.ProjectArchived(archivedName);
        }

        DocumentSubmitOutcome.Refused? refused = RefuseAttribution(request);
        if (refused is not null)
        {
            return refused;
        }

        Document document = new(
            NewId(),
            project!.Id,
            PlanTitle.Choose(request.Summary, markdown),
            EmptyToNull(request.SessionId),
            now);
        Revision revision = NewRevision(request, document.Id, 1, null, now);
        db.Add(document);
        db.Add(revision);
        AddBlocks(db, revision.Id, request.Markdown, markdown, []);
        await db.SaveChangesAsync(cancellationToken);
        return Created(project.Id, document.Id, revision);
    }

    private static async Task<DocumentSubmitOutcome> Continue(
        PlansDbContext db,
        SubmitDocument request,
        PlanDocument markdown,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        Revision? parent = await db.Revisions.SingleOrDefaultAsync(
            revision => revision.Id == request.ParentRevisionId,
            cancellationToken);
        if (parent?.DocumentId is null)
        {
            return new DocumentSubmitOutcome.Refused("Previous revision was not found.");
        }

        Document document = await db.Documents.SingleAsync(item => item.Id == parent.DocumentId, cancellationToken);
        Project project = await db.Projects.SingleAsync(item => item.Id == document.ProjectId, cancellationToken);
        if (project.ArchivedAt is not null)
        {
            return new DocumentSubmitOutcome.ProjectArchived(project.DisplayName);
        }

        if (document.ArchivedAt is not null)
        {
            return new DocumentSubmitOutcome.DocumentArchived(document.Title);
        }

        if (document.ReadOnlyAt is not null)
        {
            return new DocumentSubmitOutcome.ReadOnlyDocument(document.Title);
        }

        if (!string.IsNullOrWhiteSpace(request.FolderPath)
            && ProjectFolder.Normalize(request.FolderPath) != project.FolderPath)
        {
            return new DocumentSubmitOutcome.Refused("Folder does not match the document's project.");
        }

        DocumentSubmitOutcome.Refused? refused = RefuseAttribution(request);
        if (refused is not null)
        {
            return refused;
        }

        List<StoredBlock> parentBlocks = await db.Blocks
            .Where(block => block.RevisionId == parent.Id)
            .OrderBy(block => block.Ordinal)
            .ToListAsync(cancellationToken);
        List<string> locks = await db.BlockLocks
            .Where(item => item.DocumentId == document.Id)
            .Select(item => item.BlockKey)
            .ToListAsync(cancellationToken);
        IReadOnlyList<StoredBlock> next = BlockKeys.Assign(NewId(), request.Markdown, markdown.Blocks, parentBlocks);
        if (LockBroken(locks, parentBlocks, next))
        {
            return new DocumentSubmitOutcome.Refused("A locked block was changed.");
        }

        int number = await db.Revisions
            .Where(revision => revision.DocumentId == document.Id)
            .MaxAsync(revision => revision.Number, cancellationToken) + 1;
        document.Touch(now);
        document.Retitle(PlanTitle.Choose(request.Summary, markdown));
        Revision revision = NewRevision(request, document.Id, number, parent.Id, now);
        foreach (StoredBlock block in BlockKeys.Assign(revision.Id, request.Markdown, markdown.Blocks, parentBlocks))
        {
            db.Add(block);
        }

        db.Add(revision);
        await db.SaveChangesAsync(cancellationToken);
        return Created(project.Id, document.Id, revision);
    }

    private static bool LockBroken(
        IReadOnlyList<string> locks,
        IReadOnlyList<StoredBlock> parent,
        IReadOnlyList<StoredBlock> next)
    {
        foreach (string key in locks)
        {
            StoredBlock? previous = parent.FirstOrDefault(block => block.BlockKey == key);
            StoredBlock? current = next.FirstOrDefault(block => block.BlockKey == key);
            if (previous is null
                || current is null
                || previous.ContentHash != current.ContentHash
                || previous.SectionPath != current.SectionPath)
            {
                return true;
            }
        }

        return false;
    }

    private static void AddBlocks(
        PlansDbContext db,
        string revisionId,
        string markdown,
        PlanDocument document,
        IReadOnlyList<StoredBlock> parentBlocks)
    {
        foreach (StoredBlock block in BlockKeys.Assign(revisionId, markdown, document.Blocks, parentBlocks))
        {
            db.Add(block);
        }
    }

    private static async Task<(Project? Project, string? ArchivedName)> FindOrAddProject(
        PlansDbContext db,
        string? folderPath,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        string? folder = ProjectFolder.Normalize(folderPath);
        Project? existing = await db.Projects.SingleOrDefaultAsync(
            project => project.FolderPath == folder && project.ArchivedAt == null,
            cancellationToken);
        if (existing is not null)
        {
            return (existing, null);
        }

        if (folder is not null)
        {
            List<Project> archived = await db.Projects
                .Where(project => project.FolderPath == folder && project.ArchivedAt != null)
                .ToListAsync(cancellationToken);
            Project? chosen = archived.OrderByDescending(project => project.ArchivedAt).FirstOrDefault();
            if (chosen is not null)
            {
                return (null, chosen.DisplayName);
            }
        }

        Project created = new(NewId(), folder, ProjectFolder.DisplayName(folder), now);
        db.Add(created);
        return (created, null);
    }

    private static Revision NewRevision(
        SubmitDocument request,
        string documentId,
        int number,
        string? parentRevisionId,
        DateTimeOffset now) =>
        new(
            NewId(),
            null,
            number,
            parentRevisionId,
            request.Markdown,
            EmptyToNull(request.Summary),
            null,
            null,
            now,
            AttributionRules.Trim(request.Agent),
            AttributionRules.Trim(request.Model),
            AttributionRules.Trim(request.ClientName),
            AttributionRules.Trim(request.ClientVersion),
            documentId);

    private static DocumentSubmitOutcome.Refused? RefuseAttribution(SubmitDocument request)
    {
        string? error = AttributionRules.Reject(request.Agent, request.Model, request.ClientName, request.ClientVersion);
        return error is null ? null : new DocumentSubmitOutcome.Refused(error);
    }

    private static DocumentSubmitOutcome.Created Created(string projectId, string documentId, Revision revision) =>
        new(
            new ProjectId(projectId),
            new DocumentId(documentId),
            new RevisionId(revision.Id),
            revision.Number);

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NewId() => Guid.NewGuid().ToString();
}