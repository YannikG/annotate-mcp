using Annotate.Plans.Application;
using Annotate.Plans.Domain;

using Microsoft.EntityFrameworkCore;

namespace Annotate.Plans.Infrastructure;

internal sealed class PlanCatalog(IDbContextFactory<PlansDbContext> contexts)
{
    public Task<IReadOnlyList<ProjectSummary>> ProjectsAsync(CancellationToken cancellationToken) =>
        ListProjectsAsync(archived: false, cancellationToken);

    public Task<IReadOnlyList<ProjectSummary>> ArchivedProjectsAsync(CancellationToken cancellationToken) =>
        ListProjectsAsync(archived: true, cancellationToken);

    public async Task<ProjectSummary?> ProjectAsync(ProjectId id, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Project? project = await db.Projects.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (project is null)
        {
            return null;
        }

        int planCount = await db.Plans.CountAsync(plan => plan.ProjectId == project.Id, cancellationToken);
        return Summary(project, planCount);
    }

    public async Task<RenameOutcome> RenameProjectAsync(
        ProjectId id,
        string displayName,
        CancellationToken cancellationToken)
    {
        string? rejection = Project.Rejection(displayName);
        if (rejection is not null)
        {
            return new RenameOutcome.Refused(rejection);
        }

        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Project? project = await db.Projects.SingleOrDefaultAsync(
            item => item.Id == id.Value,
            cancellationToken);
        if (project is null)
        {
            return new RenameOutcome.Refused("Project was not found.");
        }

        project.Rename(displayName);
        await db.SaveChangesAsync(cancellationToken);
        return new RenameOutcome.Renamed();
    }

    public async Task<ProjectChange> ArchiveProjectAsync(ProjectId id, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Project? project = await db.Projects.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (project is null)
        {
            return new ProjectChange.Refused("Project was not found.");
        }

        project.Archive(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return new ProjectChange.Done([]);
    }

    public async Task<ProjectChange> RestoreProjectAsync(ProjectId id, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Project? project = await db.Projects.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (project is null)
        {
            return new ProjectChange.Refused("Project was not found.");
        }

        if (project.ArchivedAt is null)
        {
            return new ProjectChange.Refused("Project is not archived.");
        }

        if (project.FolderPath is not null)
        {
            bool taken = await db.Projects.AnyAsync(
                other => other.Id != project.Id
                    && other.ArchivedAt == null
                    && other.FolderPath == project.FolderPath,
                cancellationToken);
            if (taken)
            {
                return new ProjectChange.Refused("Folder is already used by another project.");
            }
        }

        project.Restore();
        await db.SaveChangesAsync(cancellationToken);
        return new ProjectChange.Done([]);
    }

    public async Task<ProjectChange> DeleteProjectAsync(ProjectId id, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Project? project = await db.Projects.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (project is null)
        {
            return new ProjectChange.Refused("Project was not found.");
        }

        List<string> planIds = await db.Plans
            .Where(plan => plan.ProjectId == project.Id)
            .Select(plan => plan.Id)
            .ToListAsync(cancellationToken);
        List<Revision> revisions = await db.Revisions
            .Where(revision => planIds.Contains(revision.PlanId))
            .OrderBy(revision => revision.Number)
            .ToListAsync(cancellationToken);
        foreach (Revision revision in revisions)
        {
            revision.DetachParent();
        }

        await db.SaveChangesAsync(cancellationToken);
        db.Projects.Remove(project);
        await db.SaveChangesAsync(cancellationToken);
        return new ProjectChange.Done(revisions.Select(revision => revision.Id).ToArray());
    }

    public Task<IReadOnlyList<PlanSummary>> PlansAsync(ProjectId id, CancellationToken cancellationToken) =>
        ListPlansAsync(id, archived: false, cancellationToken);

    public Task<IReadOnlyList<PlanSummary>> ArchivedPlansAsync(ProjectId id, CancellationToken cancellationToken) =>
        ListPlansAsync(id, archived: true, cancellationToken);

    public async Task<PlanChange> ArchivePlanAsync(PlanId id, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Plan? plan = await db.Plans.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (plan is null)
        {
            return new PlanChange.Refused("Plan was not found.");
        }

        if (plan.ArchivedAt is not null)
        {
            return new PlanChange.AlreadyArchived();
        }

        plan.Archive(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return new PlanChange.Done([]);
    }

    public async Task<PlanChange> RestorePlanAsync(PlanId id, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Plan? plan = await db.Plans.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (plan is null)
        {
            return new PlanChange.Refused("Plan was not found.");
        }

        plan.Restore();
        await db.SaveChangesAsync(cancellationToken);
        return new PlanChange.Done([]);
    }

    public async Task<PlanChange> DeletePlanAsync(PlanId id, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Plan? plan = await db.Plans.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (plan is null)
        {
            return new PlanChange.Refused("Plan was not found.");
        }

        List<Revision> revisions = await db.Revisions
            .Where(revision => revision.PlanId == plan.Id)
            .OrderBy(revision => revision.Number)
            .ToListAsync(cancellationToken);
        foreach (Revision revision in revisions)
        {
            revision.DetachParent();
        }

        await db.SaveChangesAsync(cancellationToken);
        db.Plans.Remove(plan);
        await db.SaveChangesAsync(cancellationToken);
        return new PlanChange.Done(revisions.Select(revision => revision.Id).ToArray());
    }

    private async Task<IReadOnlyList<PlanSummary>> ListPlansAsync(
        ProjectId id,
        bool archived,
        CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        List<Plan> plans = await db.Plans
            .Where(plan => plan.ProjectId == id.Value && (archived ? plan.ArchivedAt != null : plan.ArchivedAt == null))
            .ToListAsync(cancellationToken);
        Dictionary<string, int> revisions = await RevisionCounts(db, plans, cancellationToken);
        return plans
            .OrderByDescending(plan => plan.UpdatedAt)
            .ThenBy(plan => plan.Id, StringComparer.Ordinal)
            .Select(plan => new PlanSummary(
                new PlanId(plan.Id),
                plan.Title,
                plan.UpdatedAt,
                revisions.GetValueOrDefault(plan.Id)))
            .ToArray();
    }

    public async Task<DiffOutcome> DiffAsync(RevisionId older, RevisionId newer, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Revision? left = await db.Revisions.SingleOrDefaultAsync(
            item => item.Id == older.Value,
            cancellationToken);
        Revision? right = await db.Revisions.SingleOrDefaultAsync(
            item => item.Id == newer.Value,
            cancellationToken);
        if (left is null || right is null)
        {
            return new DiffOutcome.Refused("Revision was not found.");
        }

        if (left.PlanId != right.PlanId)
        {
            return new DiffOutcome.Refused("Revisions belong to different plans.");
        }

        if (left.Id == right.Id)
        {
            return new DiffOutcome.Lines([]);
        }

        return new DiffOutcome.Lines(
            LineDiff.Compare(left.Markdown, right.Markdown).Select(ToLine).ToArray());
    }

    public async Task<PlanDetail?> PlanAsync(PlanId id, CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        Plan? plan = await db.Plans.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var rows = await db.Revisions
            .Where(revision => revision.PlanId == plan.Id)
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
        List<PlanRevision> revisions = rows
            .Select(row => new PlanRevision(
                new RevisionId(row.Id),
                row.Number,
                row.CreatedAt,
                new Attribution(row.Agent, row.Model, row.ClientName, row.ClientVersion)))
            .ToList();
        return new PlanDetail(
            new PlanId(plan.Id),
            new ProjectId(plan.ProjectId),
            plan.Title,
            plan.SessionId,
            revisions,
            plan.ArchivedAt is not null);
    }

    public async Task<IReadOnlyList<RevisionActivity>> RevisionActivityAsync(CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        var rows = await (
            from revision in db.Revisions
            join plan in db.Plans on revision.PlanId equals plan.Id
            join project in db.Projects on plan.ProjectId equals project.Id
            select new
            {
                revision.Id,
                revision.PlanId,
                plan.Title,
                plan.ProjectId,
                project.DisplayName,
                ProjectArchivedAt = project.ArchivedAt,
                PlanArchivedAt = plan.ArchivedAt,
                revision.Number,
                revision.CreatedAt,
                revision.Agent,
                revision.Model,
                revision.ClientName,
                revision.ClientVersion,
            }).ToListAsync(cancellationToken);
        return rows
            .OrderBy(row => row.CreatedAt)
            .ThenBy(row => row.Id, StringComparer.Ordinal)
            .Select(row => new RevisionActivity(
                new RevisionId(row.Id),
                new PlanId(row.PlanId),
                row.Title,
                new ProjectId(row.ProjectId),
                row.DisplayName,
                row.ProjectArchivedAt is not null,
                row.Number,
                row.CreatedAt,
                new Attribution(row.Agent, row.Model, row.ClientName, row.ClientVersion),
                row.PlanArchivedAt is not null))
            .ToArray();
    }

    private static Application.DiffLine ToLine(Domain.DiffLine row) =>
        row switch
        {
            Domain.DiffLine.Equal equal => new Application.DiffLine.Equal(equal.Text),
            Domain.DiffLine.Added added => new Application.DiffLine.Added(added.Text),
            Domain.DiffLine.Removed removed => new Application.DiffLine.Removed(removed.Text),
            _ => throw new InvalidOperationException("Line diff returned an unknown row."),
        };

    private static async Task<Dictionary<string, int>> RevisionCounts(
        PlansDbContext db,
        List<Plan> plans,
        CancellationToken cancellationToken)
    {
        if (plans.Count == 0)
        {
            return [];
        }

        string[] ids = plans.Select(plan => plan.Id).ToArray();
        return await db.Revisions
            .Where(revision => ids.Contains(revision.PlanId))
            .GroupBy(revision => revision.PlanId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);
    }

    private async Task<IReadOnlyList<ProjectSummary>> ListProjectsAsync(
        bool archived,
        CancellationToken cancellationToken)
    {
        await using PlansDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        List<Project> projects = await db.Projects
            .Where(project => archived ? project.ArchivedAt != null : project.ArchivedAt == null)
            .ToListAsync(cancellationToken);
        List<Plan> plans = await db.Plans.ToListAsync(cancellationToken);
        Dictionary<string, int> counts = plans
            .Where(plan => plan.ArchivedAt == null)
            .GroupBy(plan => plan.ProjectId)
            .ToDictionary(group => group.Key, group => group.Count());
        Dictionary<string, DateTimeOffset> newest = plans
            .GroupBy(plan => plan.ProjectId)
            .ToDictionary(group => group.Key, group => group.Max(plan => plan.UpdatedAt));

        return projects
            .OrderByDescending(project => newest.TryGetValue(project.Id, out DateTimeOffset updated)
                ? updated
                : project.CreatedAt)
            .ThenBy(project => project.DisplayName, StringComparer.Ordinal)
            .ThenBy(project => project.Id, StringComparer.Ordinal)
            .Select(project => Summary(project, counts.GetValueOrDefault(project.Id)))
            .ToArray();
    }

    private static ProjectSummary Summary(Project project, int planCount) =>
        new(
            new ProjectId(project.Id),
            project.DisplayName,
            project.FolderPath,
            planCount,
            project.ArchivedAt is not null);
}