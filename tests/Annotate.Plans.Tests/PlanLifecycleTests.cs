using Annotate.Plans.Application;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Plans.Tests;

public sealed class PlanLifecycleTests
{
    [Fact]
    public async Task ArchiveHidesThePlanUntilItIsRestored()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"), "Repo");
        SubmitOutcome.Created created = await Submit(plans, "# Plan\n", folder, null);
        Assert.Equal(1, Assert.Single(await plans.ProjectsAsync(CancellationToken.None)).PlanCount);

        Assert.IsType<PlanChange.Done>(await plans.ArchivePlanAsync(created.Plan, CancellationToken.None));
        Assert.Equal(0, Assert.Single(await plans.ProjectsAsync(CancellationToken.None)).PlanCount);
        Assert.Empty(await plans.PlansAsync(created.Project, CancellationToken.None));
        Assert.Equal(
            [created.Plan],
            (await plans.ArchivedPlansAsync(created.Project, CancellationToken.None)).Select(plan => plan.PlanId));
        Assert.True((await plans.PlanAsync(created.Plan, CancellationToken.None))!.Archived);
        Assert.IsType<PlanChange.AlreadyArchived>(
            await plans.ArchivePlanAsync(created.Plan, CancellationToken.None));

        Assert.IsType<PlanChange.Done>(await plans.RestorePlanAsync(created.Plan, CancellationToken.None));
        Assert.Equal(
            [created.Plan],
            (await plans.PlansAsync(created.Project, CancellationToken.None)).Select(plan => plan.PlanId));
        Assert.Empty(await plans.ArchivedPlansAsync(created.Project, CancellationToken.None));
        Assert.False((await plans.PlanAsync(created.Plan, CancellationToken.None))!.Archived);
        Assert.Equal(1, Assert.Single(await plans.ProjectsAsync(CancellationToken.None)).PlanCount);
    }

    [Fact]
    public async Task DeleteRemovesThePlanAndLeavesTheProject()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"), "Repo");
        SubmitOutcome.Created first = await Submit(plans, "# Plan\n", folder, null);
        SubmitOutcome.Created second = await Submit(plans, "# Plan\n\nNext\n", folder, first.Revision.Value);

        PlanChange.Done deleted = Assert.IsType<PlanChange.Done>(
            await plans.DeletePlanAsync(first.Plan, CancellationToken.None));
        Assert.Equal([first.Revision.Value, second.Revision.Value], deleted.RevisionIds);
        Assert.Null(await plans.PlanAsync(first.Plan, CancellationToken.None));
        Assert.Null(await plans.RevisionAsync(first.Revision, CancellationToken.None));
        Assert.Null(await plans.RevisionAsync(second.Revision, CancellationToken.None));
        Assert.Equal(first.Project, Assert.Single(await plans.ProjectsAsync(CancellationToken.None)).ProjectId);
        Assert.Equal(0, Assert.Single(await plans.ProjectsAsync(CancellationToken.None)).PlanCount);
    }

    [Fact]
    public async Task MissingPlanIsRefused()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        PlanId missing = new(Guid.NewGuid().ToString());

        Assert.Equal(
            "Plan was not found.",
            Assert.IsType<PlanChange.Refused>(await plans.ArchivePlanAsync(missing, CancellationToken.None)).Error);
        Assert.Equal(
            "Plan was not found.",
            Assert.IsType<PlanChange.Refused>(await plans.DeletePlanAsync(missing, CancellationToken.None)).Error);
        Assert.Equal(
            "Plan was not found.",
            Assert.IsType<PlanChange.Refused>(await plans.RestorePlanAsync(missing, CancellationToken.None)).Error);
    }

    [Fact]
    public async Task ContinuingAnArchivedPlanInsertsNothing()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"), "Repo");
        SubmitOutcome.Created created = await Submit(plans, "# Plan\n", folder, null);
        string title = (await plans.PlanAsync(created.Plan, CancellationToken.None))!.Title;
        Assert.IsType<PlanChange.Done>(await plans.ArchivePlanAsync(created.Plan, CancellationToken.None));

        SubmitOutcome again = await plans.SubmitAsync(
            new SubmitRevision("# Plan\n\nNext\n", null, folder, null, created.Revision.Value, null, null, "Cursor", "claude-opus-4"),
            CancellationToken.None);
        Assert.Equal(title, Assert.IsType<SubmitOutcome.PlanArchived>(again).Title);
        Assert.Single((await plans.PlanAsync(created.Plan, CancellationToken.None))!.Revisions);

        SubmitOutcome.Created fresh = await Submit(plans, "# Fresh\n", folder, null);
        Assert.Equal(created.Project, fresh.Project);
        Assert.NotEqual(created.Plan, fresh.Plan);
        Assert.Equal(
            [created.Plan],
            (await plans.ArchivedPlansAsync(created.Project, CancellationToken.None)).Select(plan => plan.PlanId));
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