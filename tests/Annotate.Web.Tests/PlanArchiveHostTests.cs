using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Web.Tests;

public sealed class PlanArchiveHostTests
{
    private const string Notice =
        "This only archives a finished plan so it leaves the active lists. It does not submit a new revision or a new variant. A new variant is `annotate_plan` with `previousReviewId`. Do not call `archive_plan` to replace a draft.";

    [Fact]
    public async Task ArchiveToolSaysItIsNotANewVariant()
    {
        await using AnnotateApp app = new();
        IPlanHost host = app.Services.GetRequiredService<IPlanHost>();
        string submitted = await host.SubmitAsync(
            new PlanSubmission("# Storage\n", null, null, null, null, null, null, "Cursor", "claude-opus-4"),
            "localhost",
            CancellationToken.None);
        string planId = Value(submitted, "Plan ID: ");

        string archived = await host.ArchiveAsync(planId, CancellationToken.None);
        Assert.Equal(
            $"""
            Plan archived.
            Plan ID: {planId}

            {Notice}
            """,
            archived);
        Assert.Equal(
            $"""
            This plan is already archived.
            Plan ID: {planId}

            {Notice}
            """,
            await host.ArchiveAsync(planId, CancellationToken.None));
        Assert.Equal(
            "Error: Plan was not found.",
            await host.ArchiveAsync(Guid.NewGuid().ToString(), CancellationToken.None));

        IPlans plans = app.Services.GetRequiredService<IPlans>();
        PlanDetail stored = (await plans.PlanAsync(new PlanId(planId), CancellationToken.None))!;
        Assert.True(stored.Archived);
        Assert.Single(stored.Revisions);
    }

    [Fact]
    public async Task ContinuingAnArchivedPlanAsksInsteadOfSubmitting()
    {
        await using AnnotateApp app = new();
        IPlanHost host = app.Services.GetRequiredService<IPlanHost>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-folder-" + Guid.NewGuid().ToString("N"));
        string first = await host.SubmitAsync(
            new PlanSubmission("# Storage\n", null, null, null, folder, null, null, "Cursor", "claude-opus-4"),
            "localhost",
            CancellationToken.None);
        string reviewId = Value(first, "Review ID: ");
        string planId = Value(first, "Plan ID: ");
        Assert.StartsWith("Plan archived.", await host.ArchiveAsync(planId, CancellationToken.None), StringComparison.Ordinal);

        string ask =
            """
            This plan is archived.
            Plan: Storage

            Archive is not a new variant. Ask the user whether to restore this plan or start a new one. Do not submit again until they answer.
            """;
        Assert.Equal(
            ask,
            await host.SubmitAsync(
                new PlanSubmission("# Storage\n\nNext\n", null, reviewId, null, folder, null, null, "Cursor", "claude-opus-4"),
                "localhost",
                CancellationToken.None));

        IPlans plans = app.Services.GetRequiredService<IPlans>();
        IReviews reviews = app.Services.GetRequiredService<IReviews>();
        Assert.Single((await plans.PlanAsync(new PlanId(planId), CancellationToken.None))!.Revisions);
        Assert.Single(await reviews.PendingAsync(CancellationToken.None));

        string fresh = await host.SubmitAsync(
            new PlanSubmission("# Fresh\n", null, null, null, folder, null, null, "Cursor", "claude-opus-4"),
            "localhost",
            CancellationToken.None);
        Assert.NotEqual(planId, Value(fresh, "Plan ID: "));
    }

    private static string Value(string text, string label)
    {
        foreach (string line in text.Split('\n'))
        {
            if (line.StartsWith(label, StringComparison.Ordinal))
            {
                return line[label.Length..];
            }
        }

        throw new InvalidOperationException($"Missing {label}");
    }
}