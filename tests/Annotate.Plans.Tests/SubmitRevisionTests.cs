using Annotate.Plans.Application;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Plans.Tests;

public sealed class SubmitRevisionTests
{
    [Fact]
    public async Task FirstSubmitCreatesRevisionOneAndContinuationSeesIt()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider firstHost = open.Plans();
        await using ServiceProvider secondHost = open.Plans();
        IPlans first = firstHost.GetRequiredService<IPlans>();
        IPlans second = secondHost.GetRequiredService<IPlans>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));

        SubmitOutcome created = await first.SubmitAsync(
            new SubmitRevision("# Storage\n", "Keep the plan", folder, "session-1", null, null, null, "Cursor", "claude-opus-4"),
            CancellationToken.None);
        SubmitOutcome.Created revision = Assert.IsType<SubmitOutcome.Created>(created);
        Assert.Equal(1, revision.Number);

        SubmitOutcome continued = await second.SubmitAsync(
            new SubmitRevision("# Storage\n\nNext\n", null, folder, "session-2", revision.Revision.Value, null, null, "Cursor", "claude-opus-4"),
            CancellationToken.None);
        SubmitOutcome.Created child = Assert.IsType<SubmitOutcome.Created>(continued);

        Assert.Equal(2, child.Number);
        Assert.Equal(revision.Project, child.Project);
        Assert.Equal(revision.Plan, child.Plan);
        Assert.NotEqual(revision.Revision, child.Revision);
    }

    [Fact]
    public async Task NormalisedFolderReusesProjectAndNoFolderIsShared()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string relative = Path.Combine("annotate-folder", Guid.NewGuid().ToString("N"), "Repo");
        string absolute = Path.GetFullPath(relative);
        char otherSeparator = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
        string mixed = absolute.Replace(Path.DirectorySeparatorChar, otherSeparator);
        string otherCase = Path.Combine(Path.GetDirectoryName(absolute)!, "repo");
        string otherFolder = Path.Combine(Path.GetDirectoryName(absolute)!, "Other");

        ProjectId relativeProject = await SubmitProject(plans, relative);
        Assert.Equal(relativeProject, await SubmitProject(plans, absolute));
        Assert.Equal(relativeProject, await SubmitProject(plans, absolute + Path.DirectorySeparatorChar));
        Assert.Equal(relativeProject, await SubmitProject(plans, mixed));
        Assert.NotEqual(relativeProject, await SubmitProject(plans, otherCase));
        Assert.NotEqual(relativeProject, await SubmitProject(plans, otherFolder));

        ProjectId noFolder = await SubmitProject(plans, null);
        Assert.Equal(noFolder, await SubmitProject(plans, " \t"));
        Assert.NotEqual(relativeProject, noFolder);
    }

    [Fact]
    public async Task RefusalsInsertNothing()
    {
        await using OpenPlans open = await OpenPlans.Open();
        string[] trusted = ["example.com"];
        string plan = "# Plan\n";
        const string Credentials = "Story URL must be an absolute http or https URL without credentials.";
        const string Host = "Story URL host is not allowed.";

        await AssertRefused(open, trusted, "   ", null, null, null, "Plan is empty.");
        await AssertRefused(open, trusted, "", null, null, null, "Plan is empty.");
        await AssertRefused(open, trusted, new string('a', 256001), null, null, null, "Plan exceeds 256000 characters.");
        await AssertRefused(open, trusted, plan, new string('s', 301), null, null, "Summary exceeds 300 characters.");
        await AssertRefused(open, trusted, plan, null, new string('c', 8001), null, "Acceptance criteria exceed 8000 characters.");
        await AssertRefused(open, trusted, plan, null, null, new string('u', 2001), "Story URL exceeds 2000 characters.");
        await AssertRefused(open, trusted, plan, null, null, "example.com/plan", Credentials);
        await AssertRefused(open, trusted, plan, null, null, "ftp://example.com/plan", Credentials);
        await AssertRefused(open, trusted, plan, null, null, "https://user:secret@example.com/plan", Credentials);
        await AssertRefused(open, trusted, plan, null, null, "https://user@example.com/plan", Credentials);
        await AssertRefused(open, trusted, plan, null, null, "https://evil.test/plan", Host);
        await AssertRefused(open, trusted, plan, null, null, "https://notexample.com/plan", Host);
        await AssertRefused(open, trusted, plan, null, null, "https://example.com.evil.test/plan", Host);
        await AssertRefused(open, trusted, plan, null, null, "https://docs.example.com/plan", Host);
        await AssertRefused(open, ["*"], plan, null, null, "https://example.com/plan", Host);
        await AssertRefused(open, ["*.example.com"], plan, null, null, "https://docs.example.com/plan", Host);
        await AssertRefused(open, [], plan, null, null, "https://example.com/plan", Host);
        await AssertRefused(
            open,
            trusted,
            "```decision\nnope\n```\n",
            null,
            null,
            null,
            "Error: decision block 1: unexpected line");

        string story = "https://example.com/" + new string('a', 2000 - "https://example.com/".Length);
        await AssertAccepted(open, trusted, new string('m', 256000), new string('s', 300), new string('c', 8000), story);
        await AssertAccepted(open, ["docs.example.com"], plan, null, null, "https://docs.example.com/plan");
        await AssertAccepted(open, trusted, plan, "  ", null, "  ");
        await AssertAccepted(open, [], plan, null, null, " \n");
    }

    [Fact]
    public async Task SessionIdOverTwoHundredInsertsNothing()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));

        SubmitOutcome refused = await plans.SubmitAsync(
            new SubmitRevision("# Plan\n", null, folder, new string('s', 201), null, null, null),
            CancellationToken.None);
        Assert.Equal(
            "Session id exceeds 200 characters.",
            Assert.IsType<SubmitOutcome.Refused>(refused).Error);

        SubmitOutcome created = await plans.SubmitAsync(
            new SubmitRevision("# Plan\n", null, folder, null, null, null, null, "Cursor", "claude-opus-4"),
            CancellationToken.None);
        Assert.Equal(1, Assert.IsType<SubmitOutcome.Created>(created).Number);
    }

    [Fact]
    public async Task AttributionIsStoredPerRevisionAndMissingValuesInsertNothing()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));

        SubmitOutcome.Created first = Assert.IsType<SubmitOutcome.Created>(await plans.SubmitAsync(
            new SubmitRevision("# Storage\n", "Keep the plan", folder, "session-1", null, null, null, " Cursor ", " claude-opus-4 ", " cursor-vscode ", " 1.7.2 "),
            CancellationToken.None));
        RevisionDetail stored = Assert.IsType<RevisionDetail>(await plans.RevisionAsync(first.Revision, CancellationToken.None));
        Assert.Equal(new Attribution("Cursor", "claude-opus-4", "cursor-vscode", "1.7.2"), stored.Attribution);
        PlanDetail plan = Assert.IsType<PlanDetail>(await plans.PlanAsync(first.Plan, CancellationToken.None));
        Assert.Equal(stored.Attribution, Assert.Single(plan.Revisions).Attribution);

        SubmitOutcome.Created second = Assert.IsType<SubmitOutcome.Created>(await plans.SubmitAsync(
            new SubmitRevision("# Storage\n\nNext\n", null, folder, "session-2", first.Revision.Value, null, null, "Claude Code", "gpt-5-codex"),
            CancellationToken.None));
        RevisionDetail continued = Assert.IsType<RevisionDetail>(await plans.RevisionAsync(second.Revision, CancellationToken.None));
        Assert.Equal(new Attribution("Claude Code", "gpt-5-codex", null, null), continued.Attribution);
        Assert.Equal(stored.Attribution, (await plans.RevisionAsync(first.Revision, CancellationToken.None))!.Attribution);

        await AssertRefusedAttribution(plans, folder, null, "claude-opus-4", "Agent is required.");
        await AssertRefusedAttribution(plans, folder, "   ", "claude-opus-4", "Agent is required.");
        await AssertRefusedAttribution(plans, folder, "Cursor", null, "Model is required.");
        await AssertRefusedAttribution(plans, folder, "Cursor", " ", "Model is required.");
        await AssertRefusedAttribution(plans, folder, new string('a', 101), "claude-opus-4", "Agent exceeds 100 characters.");
        await AssertRefusedAttribution(plans, folder, "Cursor", new string('m', 101), "Model exceeds 100 characters.");
        Assert.Equal(2, (await plans.PlanAsync(first.Plan, CancellationToken.None))!.Revisions.Count);
    }

    [Fact]
    public async Task UnknownParentInsertsNothing()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));

        SubmitOutcome missing = await plans.SubmitAsync(
            new SubmitRevision("# Plan\n", null, folder, null, "missing-revision", null, null),
            CancellationToken.None);
        Assert.Equal(
            "Previous revision was not found.",
            Assert.IsType<SubmitOutcome.Refused>(missing).Error);

        SubmitOutcome created = await plans.SubmitAsync(
            new SubmitRevision("# Plan\n", null, folder, null, null, null, null, "Cursor", "claude-opus-4"),
            CancellationToken.None);
        Assert.Equal(1, Assert.IsType<SubmitOutcome.Created>(created).Number);
    }

    [Fact]
    public async Task ContinuationMustStayInThePlansProject()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"), "Repo");
        string other = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"), "Other");
        string same = folder + Path.DirectorySeparatorChar;

        SubmitOutcome.Created first = await Submit(plans, "# Plan\n", folder, null);
        SubmitOutcome mismatched = await plans.SubmitAsync(
            new SubmitRevision("# Plan\n", null, other, null, first.Revision.Value, null, null),
            CancellationToken.None);
        Assert.Equal(
            "Folder does not match the plan's project.",
            Assert.IsType<SubmitOutcome.Refused>(mismatched).Error);

        SubmitOutcome.Created sameFolder = await Submit(plans, "# Next\n", same, first.Revision.Value);
        Assert.Equal(2, sameFolder.Number);
        Assert.Equal(first.Plan, sameFolder.Plan);
        Assert.Equal(first.Project, sameFolder.Project);

        SubmitOutcome.Created second = await Submit(plans, "# Other\n", other, null);
        SubmitOutcome.Created omitted = await Submit(plans, "# Next\n", null, second.Revision.Value);
        Assert.Equal(2, omitted.Number);
        Assert.Equal(second.Plan, omitted.Plan);

        SubmitOutcome.Created third = await Submit(plans, "# Third\n", folder, null);
        SubmitOutcome.Created blank = await Submit(plans, "# Next\n", " \t", third.Revision.Value);
        Assert.Equal(2, blank.Number);
        Assert.Equal(third.Plan, blank.Plan);
    }

    private static async Task AssertRefusedAttribution(
        IPlans plans,
        string folder,
        string? agent,
        string? model,
        string error)
    {
        SubmitOutcome refused = await plans.SubmitAsync(
            new SubmitRevision("# Plan\n", null, folder, null, null, null, null, agent, model),
            CancellationToken.None);
        Assert.Equal(error, Assert.IsType<SubmitOutcome.Refused>(refused).Error);
    }

    private static async Task<SubmitOutcome.Created> Submit(
        IPlans plans,
        string markdown,
        string? folder,
        string? parentRevisionId)
    {
        SubmitOutcome outcome = await plans.SubmitAsync(
            new SubmitRevision(markdown, null, folder, null, parentRevisionId, null, null, "Cursor", "claude-opus-4"),
            CancellationToken.None);
        return Assert.IsType<SubmitOutcome.Created>(outcome);
    }

    private static async Task<ProjectId> SubmitProject(IPlans plans, string? folder)
    {
        SubmitOutcome outcome = await plans.SubmitAsync(
            new SubmitRevision("# Plan\n", null, folder, null, null, null, null, "Cursor", "claude-opus-4"),
            CancellationToken.None);
        return Assert.IsType<SubmitOutcome.Created>(outcome).Project;
    }

    private static async Task AssertRefused(
        OpenPlans open,
        IReadOnlyList<string> trusted,
        string markdown,
        string? summary,
        string? acceptanceCriteria,
        string? storyUrl,
        string error)
    {
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));
        await using ServiceProvider host = open.Plans(trusted);
        IPlans plans = host.GetRequiredService<IPlans>();

        SubmitOutcome refused = await plans.SubmitAsync(
            new SubmitRevision(markdown, summary, folder, null, null, storyUrl, acceptanceCriteria, "Cursor", "claude-opus-4"),
            CancellationToken.None);
        Assert.Equal(error, Assert.IsType<SubmitOutcome.Refused>(refused).Error);

        SubmitOutcome created = await plans.SubmitAsync(
            new SubmitRevision("# Plan\n", null, folder, null, null, null, null, "Cursor", "claude-opus-4"),
            CancellationToken.None);
        SubmitOutcome.Created revision = Assert.IsType<SubmitOutcome.Created>(created);
        Assert.Equal(1, revision.Number);
    }

    private static async Task AssertAccepted(
        OpenPlans open,
        IReadOnlyList<string> trusted,
        string markdown,
        string? summary,
        string? acceptanceCriteria,
        string? storyUrl)
    {
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));
        await using ServiceProvider host = open.Plans(trusted);
        IPlans plans = host.GetRequiredService<IPlans>();
        SubmitOutcome created = await plans.SubmitAsync(
            new SubmitRevision(markdown, summary, folder, null, null, storyUrl, acceptanceCriteria, "Cursor", "claude-opus-4"),
            CancellationToken.None);
        Assert.Equal(1, Assert.IsType<SubmitOutcome.Created>(created).Number);
    }
}