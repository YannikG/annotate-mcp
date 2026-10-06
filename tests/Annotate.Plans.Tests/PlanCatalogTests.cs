using Annotate.Plans.Application;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Plans.Tests;

public sealed class PlanCatalogTests
{
    [Fact]
    public async Task CatalogListsProjectsPlansAndRevisionText()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans(["example.com"]);
        IPlans plans = host.GetRequiredService<IPlans>();
        string root = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));
        string mangoFolder = Path.Combine(root, "mango");
        string appleFolder = Path.Combine(root, "apple");
        string bananaFolder = Path.Combine(root, "banana");
        string heading = new string('H', 301);

        SubmitOutcome.Created mango = await Submit(
            plans,
            "# " + heading + "\n",
            null,
            mangoFolder,
            " session-z ",
            null,
            null,
            null);
        await Task.Delay(40);
        SubmitOutcome.Created apple = await Submit(
            plans,
            "Just a paragraph\n",
            null,
            appleFolder,
            null,
            null,
            null,
            null);
        await Task.Delay(40);
        SubmitOutcome.Created banana = await Submit(
            plans,
            "# Ignored heading\n",
            "  Kept summary  ",
            bananaFolder,
            "session-m",
            null,
            "https://example.com/story",
            "  Ship it  ");
        await Task.Delay(40);
        SubmitOutcome.Created mangoLater = await Submit(
            plans,
            "# Later\n",
            null,
            mangoFolder,
            "later-session",
            null,
            null,
            null);

        IReadOnlyList<ProjectSummary> projects = await plans.ProjectsAsync(CancellationToken.None);
        Assert.Equal(
            [mango.Project, banana.Project, apple.Project],
            projects.Select(project => project.ProjectId).ToArray());
        Assert.Equal(["mango", "banana", "apple"], projects.Select(project => project.DisplayName).ToArray());
        Assert.Equal([2, 1, 1], projects.Select(project => project.PlanCount).ToArray());
        Assert.Equal(FullPath(mangoFolder), projects[0].FolderPath);
        Assert.Equal(FullPath(appleFolder), projects[2].FolderPath);

        IReadOnlyList<PlanSummary> mangoPlans = await plans.PlansAsync(mango.Project, CancellationToken.None);
        Assert.Equal([mangoLater.Plan, mango.Plan], mangoPlans.Select(plan => plan.PlanId).ToArray());
        Assert.True(mangoPlans[0].UpdatedAt > mangoPlans[1].UpdatedAt);
        Assert.Equal([1, 1], mangoPlans.Select(plan => plan.RevisionCount).ToArray());
        Assert.Empty(await plans.PlansAsync(new ProjectId(Guid.NewGuid().ToString()), CancellationToken.None));

        await Task.Delay(40);
        SubmitOutcome.Created mangoNext = await Submit(
            plans,
            "# Next\n",
            null,
            mangoFolder,
            "session-2",
            mango.Revision.Value,
            null,
            null);
        mangoPlans = await plans.PlansAsync(mango.Project, CancellationToken.None);
        Assert.Equal([mango.Plan, mangoLater.Plan], mangoPlans.Select(plan => plan.PlanId).ToArray());
        Assert.Equal([2, 1], mangoPlans.Select(plan => plan.RevisionCount).ToArray());
        Assert.Equal(new string('H', 300), mangoPlans[0].Title);
        Assert.Equal("Later", mangoPlans[1].Title);

        PlanDetail mangoDetail = Assert.IsType<PlanDetail>(await plans.PlanAsync(mango.Plan, CancellationToken.None));
        Assert.Equal(mango.Project, mangoDetail.ProjectId);
        Assert.Equal(new string('H', 300), mangoDetail.Title);
        Assert.Equal("session-z", mangoDetail.SessionId);
        Assert.Equal([1, 2], mangoDetail.Revisions.Select(revision => revision.Number).ToArray());
        Assert.Equal(mango.Revision, mangoDetail.Revisions[0].RevisionId);
        Assert.Equal(mangoNext.Revision, mangoDetail.Revisions[1].RevisionId);
        Assert.True(mangoDetail.Revisions[1].CreatedAt >= mangoDetail.Revisions[0].CreatedAt);

        PlanDetail appleDetail = Assert.IsType<PlanDetail>(await plans.PlanAsync(apple.Plan, CancellationToken.None));
        Assert.Equal("Untitled", appleDetail.Title);
        Assert.Null(appleDetail.SessionId);

        PlanDetail bananaDetail = Assert.IsType<PlanDetail>(await plans.PlanAsync(banana.Plan, CancellationToken.None));
        Assert.Equal("Kept summary", bananaDetail.Title);
        Assert.Equal("session-m", bananaDetail.SessionId);
        Assert.Null(await plans.PlanAsync(new PlanId(Guid.NewGuid().ToString()), CancellationToken.None));

        RevisionDetail bananaRevision = Assert.IsType<RevisionDetail>(
            await plans.RevisionAsync(banana.Revision, CancellationToken.None));
        Assert.Equal(banana.Plan, bananaRevision.PlanId);
        Assert.Equal(1, bananaRevision.Number);
        Assert.Equal("# Ignored heading\n", bananaRevision.Markdown);
        Assert.Equal("Kept summary", bananaRevision.Summary);
        Assert.Equal("https://example.com/story", bananaRevision.StoryUrl);
        Assert.Equal("Ship it", bananaRevision.AcceptanceCriteria);

        await Task.Delay(40);
        await Submit(plans, "# More\n", null, appleFolder, null, apple.Revision.Value, null, null);
        projects = await plans.ProjectsAsync(CancellationToken.None);
        Assert.Equal(
            [apple.Project, mango.Project, banana.Project],
            projects.Select(project => project.ProjectId).ToArray());
    }

    [Fact]
    public async Task RenameTrimsDisplayNameAndKeepsFolder()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"), "Repo");

        SubmitOutcome.Created created = await Submit(plans, "# Plan\n", null, folder, null, null, null, null);
        SubmitOutcome.Created noFolder = await Submit(plans, "# Plan\n", null, null, null, null, null, null);
        ProjectSummary folderProject = Assert.Single(
            await plans.ProjectsAsync(CancellationToken.None),
            project => project.ProjectId == created.Project);
        ProjectSummary loose = Assert.Single(
            await plans.ProjectsAsync(CancellationToken.None),
            project => project.ProjectId == noFolder.Project);
        Assert.Equal("No folder", loose.DisplayName);
        Assert.Null(loose.FolderPath);

        Assert.IsType<RenameOutcome.Renamed>(
            await plans.RenameProjectAsync(created.Project, "  Padded  ", CancellationToken.None));
        Assert.Equal(
            "Display name is empty.",
            Assert.IsType<RenameOutcome.Refused>(
                await plans.RenameProjectAsync(created.Project, " \t", CancellationToken.None)).Error);
        Assert.Equal(
            "Display name exceeds 120 characters.",
            Assert.IsType<RenameOutcome.Refused>(
                await plans.RenameProjectAsync(created.Project, new string('n', 121), CancellationToken.None)).Error);

        ProjectSummary afterRefusal = Assert.Single(
            await plans.ProjectsAsync(CancellationToken.None),
            project => project.ProjectId == created.Project);
        Assert.Equal("Padded", afterRefusal.DisplayName);
        Assert.Equal(folderProject.FolderPath, afterRefusal.FolderPath);

        string kept = new string('k', 120);
        Assert.IsType<RenameOutcome.Renamed>(
            await plans.RenameProjectAsync(created.Project, kept, CancellationToken.None));
        Assert.Equal(
            "Project was not found.",
            Assert.IsType<RenameOutcome.Refused>(
                await plans.RenameProjectAsync(new ProjectId(Guid.NewGuid().ToString()), "Name", CancellationToken.None)).Error);

        ProjectSummary renamed = Assert.Single(
            await plans.ProjectsAsync(CancellationToken.None),
            project => project.ProjectId == created.Project);
        Assert.Equal(kept, renamed.DisplayName);
        Assert.Equal(folderProject.FolderPath, renamed.FolderPath);

        Assert.IsType<RenameOutcome.Renamed>(
            await plans.RenameProjectAsync(noFolder.Project, "  Loose  ", CancellationToken.None));
        ProjectSummary looseAfter = Assert.Single(
            await plans.ProjectsAsync(CancellationToken.None),
            project => project.ProjectId == noFolder.Project);
        Assert.Equal("Loose", looseAfter.DisplayName);
        Assert.Null(looseAfter.FolderPath);

        SubmitOutcome.Created again = await Submit(
            plans,
            "# Again\n",
            null,
            folder + Path.DirectorySeparatorChar,
            null,
            null,
            null,
            null);
        Assert.Equal(created.Project, again.Project);
        SubmitOutcome.Created stillLoose = await Submit(plans, "# Again\n", null, " \t", null, null, null, null);
        Assert.Equal(noFolder.Project, stillLoose.Project);
    }

    [Fact]
    public async Task RevisionActivityListsEveryRevisionWithoutLoadingMarkdown()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));
        SubmitOutcome.Created created = Assert.IsType<SubmitOutcome.Created>(await plans.SubmitAsync(
            new SubmitRevision("# Secret plan\n", "Atlas plan", folder, null, null, null, null, "Cursor", "claude-opus-4", "cursor", "1"),
            CancellationToken.None));
        Assert.IsType<ProjectChange.Done>(await plans.ArchiveProjectAsync(created.Project, CancellationToken.None));

        RevisionActivity row = Assert.Single(await plans.RevisionActivityAsync(CancellationToken.None));
        Assert.Equal(created.Revision, row.RevisionId);
        Assert.Equal(created.Plan, row.PlanId);
        Assert.Equal("Atlas plan", row.PlanTitle);
        Assert.Equal(created.Project, row.ProjectId);
        Assert.True(row.ProjectArchived);
        Assert.Equal(1, row.Number);
        Assert.Equal(new Attribution("Cursor", "claude-opus-4", "cursor", "1"), row.Attribution);
    }

    private static async Task<SubmitOutcome.Created> Submit(
        IPlans plans,
        string markdown,
        string? summary,
        string? folder,
        string? sessionId,
        string? parentRevisionId,
        string? storyUrl,
        string? acceptanceCriteria)
    {
        SubmitOutcome outcome = await plans.SubmitAsync(
            new SubmitRevision(markdown, summary, folder, sessionId, parentRevisionId, storyUrl, acceptanceCriteria, "Cursor", "claude-opus-4"),
            CancellationToken.None);
        return Assert.IsType<SubmitOutcome.Created>(outcome);
    }

    private static string FullPath(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
}