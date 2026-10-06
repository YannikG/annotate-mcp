using Annotate.Plans;
using Annotate.Plans.Application;
using Annotate.Reviews;
using Annotate.Reviews.Application;
using Annotate.Web;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using ReviewRevisionId = Annotate.Reviews.Application.RevisionId;

namespace Annotate.Web.Tests;

public sealed class ProjectPlanPageQueryTests
{
    [Fact]
    public async Task DatabaseReturnsOnePageAndLeavesUnreviewedPlansOffTheStatusTabs()
    {
        string connectionString = $"Data Source=file:pages-{Guid.NewGuid():N}?mode=memory&cache=shared";
        await using SqliteConnection connection = new(connectionString);
        await connection.OpenAsync();
        Clock clock = new();
        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(clock);
        services.AddPlans(new PlansSettings(connectionString, []));
        services.AddReviews(new ReviewsSettings(connectionString));
        await using ServiceProvider host = services.BuildServiceProvider();
        IPlans plans = host.GetRequiredService<IPlans>();
        IReviews reviews = host.GetRequiredService<IReviews>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-pages-" + Guid.NewGuid().ToString("N"));
        ProjectId project = default;
        for (int index = 0; index < 21; index++)
        {
            project = await Approve(plans, reviews, clock, folder, index.ToString("00"), null);
        }

        SubmitOutcome.Created stale = await Submit(plans, folder, "Stale", null);
        await ApproveRevision(reviews, clock, stale.Revision.Value);
        await Submit(plans, folder, "Stale", stale.Revision.Value);
        await Submit(plans, folder, "Bare", null);
        SubmitOutcome.Created shelved = await Submit(plans, folder, "Shelved", null);
        await ApproveRevision(reviews, clock, shelved.Revision.Value);
        Assert.IsType<PlanChange.Done>(await plans.ArchivePlanAsync(shelved.Plan, CancellationToken.None));
        await Approve(plans, reviews, clock, folder + "-other", "Elsewhere", null);

        SqliteProjectPlanPages pages = new(connectionString);
        ProjectPlanListing first = await pages.PageAsync(project, ProjectPlanTab.Approved, 1, CancellationToken.None);
        ProjectPlanListing second = await pages.PageAsync(project, ProjectPlanTab.Approved, 2, CancellationToken.None);
        ProjectPlanListing third = await pages.PageAsync(project, ProjectPlanTab.Approved, 3, CancellationToken.None);
        ProjectPlanListing repeated = await pages.PageAsync(project, ProjectPlanTab.Approved, 0, CancellationToken.None);
        ProjectPlanListing archived = await pages.PageAsync(project, ProjectPlanTab.Archived, 1, CancellationToken.None);
        ProjectPlanListing pending = await pages.PageAsync(project, ProjectPlanTab.Pending, 1, CancellationToken.None);

        Assert.Equal(21, first.Approved);
        Assert.Equal(23, first.ActivePlans);
        Assert.Equal(1, first.Archived);
        Assert.Equal(0, first.Pending);
        Assert.Equal(0, first.Changes);
        Assert.Equal(20, first.Rows.Count);
        Assert.Equal("20", first.Rows[0].Title);
        Assert.DoesNotContain(first.Rows, row => row.Title is "00" or "Stale" or "Bare" or "Shelved" or "Elsewhere");
        Assert.Equal("00", Assert.Single(second.Rows).Title);
        Assert.Equal(21, second.Approved);
        Assert.Empty(third.Rows);
        Assert.Equal(21, third.Approved);
        Assert.Equal(first.Rows[0].Title, repeated.Rows[0].Title);
        Assert.Equal("Shelved", Assert.Single(archived.Rows).Title);
        Assert.Null(archived.Rows[0].ReviewId);
        Assert.Empty(pending.Rows);
        Assert.DoesNotContain(archived.Rows, row => row.Title == "20");
    }

    private static async Task<ProjectId> Approve(
        IPlans plans, IReviews reviews, Clock clock, string folder, string title, string? parent)
    {
        SubmitOutcome.Created created = await Submit(plans, folder, title, parent);
        await ApproveRevision(reviews, clock, created.Revision.Value);
        return created.Project;
    }

    private static async Task ApproveRevision(IReviews reviews, Clock clock, string revision)
    {
        clock.Now = clock.Now.AddDays(1);
        OpenOutcome.Opened opened = Assert.IsType<OpenOutcome.Opened>(
            await reviews.OpenAsync(new ReviewRevisionId(revision), "# Plan\n", CancellationToken.None));
        Assert.IsType<DecideOutcome.Done>(await reviews.ApproveAsync(opened.Id, CancellationToken.None));
    }

    private static async Task<SubmitOutcome.Created> Submit(IPlans plans, string folder, string title, string? parent)
    {
        SubmitOutcome outcome = await plans.SubmitAsync(
            new SubmitRevision("# Plan\n", title, folder, null, parent, null, null, "Cursor", "Grok 4.7"),
            CancellationToken.None);
        return Assert.IsType<SubmitOutcome.Created>(outcome);
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}