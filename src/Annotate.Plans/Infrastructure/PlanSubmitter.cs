using Annotate.Markdown;
using Annotate.Plans.Application;
using Annotate.Plans.Domain;

using Microsoft.EntityFrameworkCore;

namespace Annotate.Plans.Infrastructure;

internal sealed class PlanSubmitter(IDbContextFactory<PlansDbContext> contexts, PlansSettings settings) : IPlans
{
    private readonly PlanCatalog _catalog = new(contexts);
    public async Task<SubmitOutcome> SubmitAsync(SubmitRevision request, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);

        string? error = RevisionLimits.Reject(
                request.Markdown,
                request.Summary,
                request.AcceptanceCriteria,
                request.SessionId)
            ?? StoryLink.Reject(request.StoryUrl, settings.TrustedStoryDomains);
        if (error is not null)
        {
            return new SubmitOutcome.Refused(error);
        }

        ParseOutcome parsed = PlanMarkdown.Parse(request.Markdown);
        if (parsed is ParseOutcome.Invalid invalid)
        {
            return new SubmitOutcome.Refused(invalid.Error);
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        PlanDocument document = ((ParseOutcome.Ok)parsed).Document;
        if (string.IsNullOrWhiteSpace(request.ParentRevisionId))
        {
            return await CreatePlan(db, request, document, now, cancellationToken);
        }

        return await ContinuePlan(db, request, document, now, cancellationToken);
    }

    public Task<IReadOnlyList<ProjectSummary>> ProjectsAsync(CancellationToken cancellationToken) =>
        _catalog.ProjectsAsync(cancellationToken);

    public Task<RenameOutcome> RenameProjectAsync(
        ProjectId id,
        string displayName,
        CancellationToken cancellationToken) =>
        _catalog.RenameProjectAsync(id, displayName, cancellationToken);

    public Task<ProjectChange> ArchiveProjectAsync(ProjectId id, CancellationToken cancellationToken) =>
        _catalog.ArchiveProjectAsync(id, cancellationToken);

    public Task<ProjectChange> DeleteProjectAsync(ProjectId id, CancellationToken cancellationToken) =>
        _catalog.DeleteProjectAsync(id, cancellationToken);

    public Task<ProjectChange> RestoreProjectAsync(ProjectId id, CancellationToken cancellationToken) =>
        _catalog.RestoreProjectAsync(id, cancellationToken);

    public Task<IReadOnlyList<ProjectSummary>> ArchivedProjectsAsync(CancellationToken cancellationToken) =>
        _catalog.ArchivedProjectsAsync(cancellationToken);

    public Task<ProjectSummary?> ProjectAsync(ProjectId id, CancellationToken cancellationToken) =>
        _catalog.ProjectAsync(id, cancellationToken);

    public Task<IReadOnlyList<PlanSummary>> PlansAsync(ProjectId id, CancellationToken cancellationToken) =>
        _catalog.PlansAsync(id, cancellationToken);

    public Task<IReadOnlyList<PlanSummary>> ArchivedPlansAsync(ProjectId id, CancellationToken cancellationToken) =>
        _catalog.ArchivedPlansAsync(id, cancellationToken);

    public Task<PlanChange> ArchivePlanAsync(PlanId id, CancellationToken cancellationToken) =>
        _catalog.ArchivePlanAsync(id, cancellationToken);

    public Task<PlanChange> RestorePlanAsync(PlanId id, CancellationToken cancellationToken) =>
        _catalog.RestorePlanAsync(id, cancellationToken);

    public Task<PlanChange> DeletePlanAsync(PlanId id, CancellationToken cancellationToken) =>
        _catalog.DeletePlanAsync(id, cancellationToken);

    public Task<PlanDetail?> PlanAsync(PlanId id, CancellationToken cancellationToken) =>
        _catalog.PlanAsync(id, cancellationToken);

    public Task<DiffOutcome> DiffAsync(RevisionId older, RevisionId newer, CancellationToken cancellationToken) =>
        _catalog.DiffAsync(older, newer, cancellationToken);

    public Task<IReadOnlyList<RevisionActivity>> RevisionActivityAsync(CancellationToken cancellationToken) =>
        _catalog.RevisionActivityAsync(cancellationToken);

    public async Task<RevisionDetail?> RevisionAsync(RevisionId id, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Revision? revision = await db.Revisions.SingleOrDefaultAsync(
            item => item.Id == id.Value,
            cancellationToken);
        if (revision is null)
        {
            return null;
        }

        List<StoredBlock> blocks = await db.Blocks
            .Where(block => block.RevisionId == revision.Id)
            .OrderBy(block => block.Ordinal)
            .ToListAsync(cancellationToken);
        Dictionary<string, StoredBlock> parent = await ParentBlocks(db, revision.ParentRevisionId, cancellationToken);
        return new RevisionDetail(
            new RevisionId(revision.Id),
            new PlanId(revision.PlanId),
            revision.Number,
            revision.Markdown,
            revision.Summary,
            revision.StoryUrl,
            revision.AcceptanceCriteria,
            blocks.Select(block => ToBlock(block, parent)).ToArray(),
            revision.ParentRevisionId,
            new Attribution(revision.Agent, revision.Model, revision.ClientName, revision.ClientVersion));
    }

    private static async Task<SubmitOutcome> CreatePlan(
        PlansDbContext db,
        SubmitRevision request,
        PlanDocument document,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        (Project? project, string? archivedName) = await FindOrAddProject(
            db,
            request.FolderPath,
            now,
            cancellationToken);
        if (archivedName is not null)
        {
            return new SubmitOutcome.Archived(archivedName);
        }

        SubmitOutcome.Refused? refused = RefuseAttribution(request);
        if (refused is not null)
        {
            return refused;
        }

        Plan plan = new(NewId(), project!.Id, PlanTitle.Choose(request.Summary, document), EmptyToNull(request.SessionId), now);
        Revision revision = NewRevision(request, plan.Id, 1, null, now);
        db.Add(plan);
        db.Add(revision);
        AddBlocks(db, revision.Id, request.Markdown, document, []);
        await db.SaveChangesAsync(cancellationToken);
        return Created(project.Id, plan.Id, revision);
    }

    private static async Task<SubmitOutcome> ContinuePlan(
        PlansDbContext db,
        SubmitRevision request,
        PlanDocument document,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        Revision? parent = await db.Revisions.SingleOrDefaultAsync(
            revision => revision.Id == request.ParentRevisionId,
            cancellationToken);
        if (parent is null)
        {
            return new SubmitOutcome.Refused("Previous revision was not found.");
        }

        Plan plan = await db.Plans.SingleAsync(item => item.Id == parent.PlanId, cancellationToken);
        Project project = await db.Projects.SingleAsync(item => item.Id == plan.ProjectId, cancellationToken);
        if (project.ArchivedAt is not null)
        {
            return new SubmitOutcome.Archived(project.DisplayName);
        }

        if (plan.ArchivedAt is not null)
        {
            return new SubmitOutcome.PlanArchived(plan.Title);
        }

        if (!string.IsNullOrWhiteSpace(request.FolderPath)
            && ProjectFolder.Normalize(request.FolderPath) != project.FolderPath)
        {
            return new SubmitOutcome.Refused("Folder does not match the plan's project.");
        }

        SubmitOutcome.Refused? refused = RefuseAttribution(request);
        if (refused is not null)
        {
            return refused;
        }

        int number = await db.Revisions
            .Where(revision => revision.PlanId == plan.Id)
            .MaxAsync(revision => revision.Number, cancellationToken) + 1;
        List<StoredBlock> parentBlocks = await db.Blocks
            .Where(block => block.RevisionId == parent.Id)
            .OrderBy(block => block.Ordinal)
            .ToListAsync(cancellationToken);
        plan.Touch(now);
        Revision revision = NewRevision(request, plan.Id, number, parent.Id, now);
        db.Add(revision);
        AddBlocks(db, revision.Id, request.Markdown, document, parentBlocks);
        await db.SaveChangesAsync(cancellationToken);
        return Created(plan.ProjectId, plan.Id, revision);
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

    private static async Task<Dictionary<string, StoredBlock>> ParentBlocks(
        PlansDbContext db,
        string? parentRevisionId,
        CancellationToken cancellationToken)
    {
        Dictionary<string, StoredBlock> parent = new(StringComparer.Ordinal);
        if (parentRevisionId is null)
        {
            return parent;
        }

        List<StoredBlock> blocks = await db.Blocks
            .Where(block => block.RevisionId == parentRevisionId)
            .ToListAsync(cancellationToken);
        foreach (StoredBlock block in blocks)
        {
            parent[block.BlockKey] = block;
        }

        return parent;
    }

    private static RevisionBlock ToBlock(StoredBlock block, Dictionary<string, StoredBlock> parent) =>
        new(
            block.BlockKey,
            Enum.Parse<BlockKind>(block.Kind),
            block.SectionPath,
            block.SourceStart,
            block.SourceEnd,
            Change(block, parent),
            block.ContentHash);

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
            Project? chosen = archived
                .OrderByDescending(project => project.ArchivedAt)
                .FirstOrDefault();
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
        SubmitRevision request,
        string planId,
        int number,
        string? parentRevisionId,
        DateTimeOffset now) =>
        new(
            NewId(),
            planId,
            number,
            parentRevisionId,
            request.Markdown,
            EmptyToNull(request.Summary),
            EmptyToNull(request.StoryUrl),
            EmptyToNull(request.AcceptanceCriteria),
            now,
            AttributionRules.Trim(request.Agent),
            AttributionRules.Trim(request.Model),
            AttributionRules.Trim(request.ClientName),
            AttributionRules.Trim(request.ClientVersion));

    private static SubmitOutcome.Refused? RefuseAttribution(SubmitRevision request)
    {
        string? error = AttributionRules.Reject(
            request.Agent,
            request.Model,
            request.ClientName,
            request.ClientVersion);
        return error is null ? null : new SubmitOutcome.Refused(error);
    }

    private static SubmitOutcome.Created Created(string projectId, string planId, Revision revision) =>
        new SubmitOutcome.Created(
            new ProjectId(projectId),
            new PlanId(planId),
            new RevisionId(revision.Id),
            revision.Number);

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NewId() => Guid.NewGuid().ToString();
}