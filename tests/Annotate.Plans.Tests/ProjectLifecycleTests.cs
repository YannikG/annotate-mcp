using Annotate.Plans.Application;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Plans.Tests;

public sealed class ProjectLifecycleTests
{
    [Fact]
    public async Task ArchiveHidesTheProjectAndLeavesTheRow()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"), "Repo");
        SubmitOutcome.Created created = await Submit(plans, "# Plan\n", folder, null);

        Assert.IsType<ProjectChange.Done>(
            await plans.ArchiveProjectAsync(created.Project, CancellationToken.None));
        Assert.DoesNotContain(
            await plans.ProjectsAsync(CancellationToken.None),
            project => project.ProjectId == created.Project);
        Assert.IsType<RenameOutcome.Renamed>(
            await plans.RenameProjectAsync(created.Project, "Kept", CancellationToken.None));

        SubmitOutcome again = await plans.SubmitAsync(
            new SubmitRevision("# Next\n", null, folder, null, null, null, null, "Cursor", "claude-opus-4"),
            CancellationToken.None);
        Assert.Equal("Kept", Assert.IsType<SubmitOutcome.Archived>(again).DisplayName);
        Assert.Empty(await plans.ProjectsAsync(CancellationToken.None));
        Assert.Equal(
            [created.Project],
            (await plans.ArchivedProjectsAsync(CancellationToken.None)).Select(project => project.ProjectId));
    }

    [Fact]
    public async Task DeleteRemovesTheProjectAndItsRevisions()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"), "Repo");
        SubmitOutcome.Created first = await Submit(plans, "# Plan\n", folder, null);
        SubmitOutcome.Created second = await Submit(plans, "# Plan\n\nNext\n", folder, first.Revision.Value);

        ProjectChange.Done deleted = Assert.IsType<ProjectChange.Done>(
            await plans.DeleteProjectAsync(first.Project, CancellationToken.None));
        Assert.Equal([first.Revision.Value, second.Revision.Value], deleted.RevisionIds);
        Assert.DoesNotContain(
            await plans.ProjectsAsync(CancellationToken.None),
            project => project.ProjectId == first.Project);
        Assert.Null(await plans.PlanAsync(first.Plan, CancellationToken.None));
        Assert.Null(await plans.RevisionAsync(first.Revision, CancellationToken.None));
        Assert.Null(await plans.RevisionAsync(second.Revision, CancellationToken.None));

        SubmitOutcome.Created again = await Submit(plans, "# Fresh\n", folder, null);
        Assert.NotEqual(first.Project, again.Project);
    }

    [Fact]
    public async Task MissingProjectIsRefused()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        ProjectId missing = new(Guid.NewGuid().ToString());

        Assert.Equal(
            "Project was not found.",
            Assert.IsType<ProjectChange.Refused>(
                await plans.ArchiveProjectAsync(missing, CancellationToken.None)).Error);
        Assert.Equal(
            "Project was not found.",
            Assert.IsType<ProjectChange.Refused>(
                await plans.DeleteProjectAsync(missing, CancellationToken.None)).Error);
        Assert.Equal(
            "Project was not found.",
            Assert.IsType<ProjectChange.Refused>(
                await plans.RestoreProjectAsync(missing, CancellationToken.None)).Error);
    }

    [Fact]
    public async Task RestoreReturnsAnArchivedProjectUnlessTheFolderIsTaken()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"), "Repo");
        SubmitOutcome.Created created = await Submit(plans, "# Plan\n", folder, null);
        Assert.IsType<ProjectChange.Done>(
            await plans.ArchiveProjectAsync(created.Project, CancellationToken.None));
        Assert.Equal(
            [created.Project],
            (await plans.ArchivedProjectsAsync(CancellationToken.None)).Select(project => project.ProjectId));

        Assert.IsType<ProjectChange.Done>(
            await plans.RestoreProjectAsync(created.Project, CancellationToken.None));
        Assert.Empty(await plans.ArchivedProjectsAsync(CancellationToken.None));
        Assert.Contains(
            created.Project,
            (await plans.ProjectsAsync(CancellationToken.None)).Select(project => project.ProjectId));

        Assert.IsType<ProjectChange.Done>(
            await plans.ArchiveProjectAsync(created.Project, CancellationToken.None));
        Assert.IsType<SubmitOutcome.Archived>(
            await plans.SubmitAsync(
                new SubmitRevision("# Next\n", null, folder, null, created.Revision.Value, null, null),
                CancellationToken.None));
        Assert.IsType<ProjectChange.Done>(
            await plans.RestoreProjectAsync(created.Project, CancellationToken.None));
        SubmitOutcome.Created next = await Submit(plans, "# Next\n", folder, created.Revision.Value);
        Assert.Equal(created.Project, next.Project);
        Assert.Equal(2, next.Number);
    }

    private static async Task<SubmitOutcome.Created> Submit(
        IPlans plans,
        string markdown,
        string folder,
        string? parentRevisionId)
    {
        SubmitOutcome outcome = await plans.SubmitAsync(
            new SubmitRevision(markdown, null, folder, null, parentRevisionId, null, null, "Cursor", "claude-opus-4"),
            CancellationToken.None);
        return Assert.IsType<SubmitOutcome.Created>(outcome);
    }
}