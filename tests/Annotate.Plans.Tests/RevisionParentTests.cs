using Annotate.Plans.Application;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Plans.Tests;

public sealed class RevisionParentTests
{
    [Fact]
    public async Task RevisionAsyncReturnsNullParentForTheFirstRevisionAndStoredParentWhenContinued()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));

        SubmitOutcome.Created first = await Submit(plans, "# One\n", folder, null);
        SubmitOutcome.Created second = await Submit(plans, "# Two\n", folder, first.Revision.Value);

        RevisionDetail opened = Assert.IsType<RevisionDetail>(
            await plans.RevisionAsync(first.Revision, CancellationToken.None));
        RevisionDetail continued = Assert.IsType<RevisionDetail>(
            await plans.RevisionAsync(second.Revision, CancellationToken.None));

        Assert.Null(opened.ParentRevisionId);
        Assert.Equal(first.Revision.Value, continued.ParentRevisionId);
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