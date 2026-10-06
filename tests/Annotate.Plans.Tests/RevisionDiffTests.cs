using Annotate.Plans.Application;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Plans.Tests;

public sealed class RevisionDiffTests
{
    [Fact]
    public async Task DiffComparesLinesOfOnePlan()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));
        string olderText = "alpha\r\nbeta\r\ngamma\r\n";
        string newerText = "alpha\r\nBETA\r\ngamma\r\ndelta\r\n";

        SubmitOutcome.Created older = await Submit(plans, olderText, folder, null);
        SubmitOutcome.Created newer = await Submit(plans, newerText, folder, older.Revision.Value);
        SubmitOutcome.Created other = await Submit(plans, "# Other\n", null, null);

        DiffOutcome.Lines rows = Assert.IsType<DiffOutcome.Lines>(
            await plans.DiffAsync(older.Revision, newer.Revision, CancellationToken.None));
        Assert.Equal(
            [
                new DiffLine.Equal("alpha"),
                new DiffLine.Removed("beta"),
                new DiffLine.Added("BETA"),
                new DiffLine.Equal("gamma"),
                new DiffLine.Added("delta"),
            ],
            rows.Rows);

        Assert.Equal(
            "Revisions belong to different plans.",
            Assert.IsType<DiffOutcome.Refused>(
                await plans.DiffAsync(older.Revision, other.Revision, CancellationToken.None)).Error);
        Assert.Equal(
            "Revision was not found.",
            Assert.IsType<DiffOutcome.Refused>(
                await plans.DiffAsync(
                    older.Revision,
                    new RevisionId(Guid.NewGuid().ToString()),
                    CancellationToken.None)).Error);
        Assert.Equal(
            "Revision was not found.",
            Assert.IsType<DiffOutcome.Refused>(
                await plans.DiffAsync(
                    new RevisionId(Guid.NewGuid().ToString()),
                    newer.Revision,
                    CancellationToken.None)).Error);

        DiffOutcome.Lines same = Assert.IsType<DiffOutcome.Lines>(
            await plans.DiffAsync(older.Revision, older.Revision, CancellationToken.None));
        Assert.Empty(same.Rows);
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
}