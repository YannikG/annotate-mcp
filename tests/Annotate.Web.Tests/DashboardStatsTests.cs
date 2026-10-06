using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web.Components.Browse;

using PlanRevisionId = Annotate.Plans.Application.RevisionId;
using ReviewRevisionId = Annotate.Reviews.Application.RevisionId;

namespace Annotate.Web.Tests;

public sealed class DashboardStatsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    private static readonly TimeZoneInfo Zone = TimeZoneInfo.Utc;

    [Fact]
    public void HeadlinesIgnoreArchivedProjectsAndDefineEachMetric()
    {
        RevisionActivity waiting = Revision("rev-wait", "plan-wait", "Atlas", 1, Now.AddDays(-2), "Cursor", "opus");
        RevisionActivity approved = Revision("rev-ok", "plan-ok", "Atlas", 1, Now.AddDays(-10), "Cursor", "opus");
        RevisionActivity changed = Revision("rev-no", "plan-no", "Atlas", 2, Now.AddDays(-3), "Claude Code", "opus");
        RevisionActivity first = Revision("rev-first", "plan-no", "Atlas", 1, Now.AddDays(-40), "Claude Code", "opus");
        RevisionActivity archived = Revision("rev-old", "plan-old", "Shelf", 1, Now.AddDays(-1), "Cursor", "opus", archived: true);
        ListedReview[] reviews =
        [
            Review("review-wait", "rev-wait", ReviewStatus.Pending, Now.AddDays(-2)),
            Review("review-ok", "rev-ok", ReviewStatus.Approved, Now.AddDays(-10), Now.AddHours(-2)),
            Review("review-no", "rev-no", ReviewStatus.ChangesRequested, Now.AddDays(-3), Now.AddDays(-1), 4),
            Review("review-first", "rev-first", ReviewStatus.ChangesRequested, Now.AddDays(-40), Now.AddDays(-39), 2),
            Review("review-old", "rev-old", ReviewStatus.Pending, Now.AddDays(-20)),
        ];

        DashboardModel model = DashboardStats.Compute(
            [waiting, approved, changed, first, archived],
            reviews,
            Now,
            Zone);

        Assert.False(model.Empty);
        ReviewLink open = Assert.Single(model.Pending);
        Assert.Equal("review-wait", open.ReviewId);
        Assert.Equal("plan-wait", open.PlanId);
        Assert.Equal("Atlas plan", open.Title);
        Assert.Equal("33% of 3", model.Approval.Value);
        Assert.Equal("last 30 days 50% of 2", model.Approval.Detail);
        Assert.Equal("50% of 2", model.FirstPass.Value);
        Assert.Equal("5 d", model.Turnaround.Value);
        Assert.Equal("3", model.Feedback.Value);
        Assert.Equal("2 plans", model.Activity.Value);
        Assert.Equal("3 revisions", model.Activity.Detail);
    }

    [Fact]
    public void PendingPlansListActiveReviewsOldestFirst()
    {
        RevisionActivity older = Revision("rev-old", "plan-old", "Atlas", 1, Now.AddDays(-4), "Cursor", "opus", title: "Older");
        RevisionActivity newer = Revision("rev-new", "plan-new", "Mango", 2, Now.AddDays(-1), "Cursor", "opus", title: "Newer");
        RevisionActivity archived = Revision("rev-arch", "plan-arch", "Shelf", 1, Now.AddDays(-9), "Cursor", "opus", true, "Archived");

        DashboardModel model = DashboardStats.Compute(
            [newer, older, archived],
            [
                Review("review-new", "rev-new", ReviewStatus.Pending, Now.AddDays(-1)),
                Review("review-old", "rev-old", ReviewStatus.Pending, Now.AddDays(-4)),
                Review("review-arch", "rev-arch", ReviewStatus.Pending, Now.AddDays(-9)),
            ],
            Now,
            Zone);

        Assert.Equal(["review-old", "review-new"], model.Pending.Select(item => item.ReviewId));
        Assert.Equal("Older", model.Pending[0].Title);
        Assert.Equal("Atlas", model.Pending[0].ProjectName);
        Assert.Equal(1, model.Pending[0].RevisionNumber);
    }

    [Fact]
    public void WeeksBucketDecisionsOnMondayAndLeaveArchivedHistoryOut()
    {
        RevisionActivity active = Revision("rev-1", "plan-1", "Atlas", 1, new DateTimeOffset(2026, 10, 5, 1, 0, 0, TimeSpan.Zero), "Cursor", "opus");
        RevisionActivity sunday = Revision("rev-2", "plan-2", "Atlas", 1, new DateTimeOffset(2026, 10, 4, 23, 0, 0, TimeSpan.Zero), "Cursor", "opus");
        RevisionActivity archived = Revision("rev-3", "plan-3", "Shelf", 1, new DateTimeOffset(2026, 10, 5, 2, 0, 0, TimeSpan.Zero), "Cursor", "opus", true);
        ListedReview[] reviews =
        [
            Review("a", "rev-1", ReviewStatus.Approved, Now, new DateTimeOffset(2026, 10, 5, 8, 0, 0, TimeSpan.Zero)),
            Review("b", "rev-2", ReviewStatus.ChangesRequested, Now, new DateTimeOffset(2026, 10, 4, 23, 0, 0, TimeSpan.Zero)),
            Review("c", "rev-3", ReviewStatus.Approved, Now, new DateTimeOffset(2026, 10, 5, 9, 0, 0, TimeSpan.Zero)),
        ];

        DashboardModel model = DashboardStats.Compute([active, sunday, archived], reviews, Now, Zone);

        Assert.Equal(12, model.Weeks.Count);
        Assert.Equal("2026-07-20", model.Weeks[0].Key);
        Assert.Equal("2026-10-05", model.Weeks[^1].Key);
        WeekBar current = Assert.Single(model.Weeks, week => week.Key == "2026-10-05");
        Assert.Equal(1, current.Approved);
        Assert.Equal(0, current.Changes);
        Assert.Equal(1, current.Revisions);
        WeekBar previous = Assert.Single(model.Weeks, week => week.Key == "2026-09-28");
        Assert.Equal(1, previous.Changes);
        Assert.Equal(1, previous.Revisions);
    }

    [Fact]
    public void AgentsModelsAndSwitchesGroupActiveRevisionsAndKeepUnknownLast()
    {
        RevisionActivity cursor = Revision("rev-1", "plan-1", "Atlas", 1, Now.AddDays(-2), "cursor", "opus");
        RevisionActivity claude = Revision("rev-2", "plan-1", "Atlas", 2, Now.AddDays(-1), "Claude Code", "Opus");
        RevisionActivity unknown = Revision("rev-3", "plan-2", "Atlas", 1, Now, null, null);
        ListedReview[] reviews =
        [
            Review("a", "rev-1", ReviewStatus.Approved, Now.AddDays(-2), Now.AddDays(-2)),
            Review("b", "rev-3", ReviewStatus.ChangesRequested, Now, Now),
        ];

        DashboardModel model = DashboardStats.Compute([cursor, claude, unknown], reviews, Now, Zone);

        Assert.Equal(["Claude Code", "cursor", "Unknown"], model.Agents.Select(agent => agent.Name));
        Assert.Equal(1, Assert.Single(model.Agents, agent => agent.Name == "cursor").Plans);
        Assert.Equal(["opus", "Unknown"], model.Models.Select(modelRow => modelRow.Name));
        SwitchStat changed = Assert.Single(model.Switches);
        Assert.Equal("plan-1", changed.PlanId);
        Assert.Equal("cursor → Claude Code", changed.Agents);
        Assert.Equal("opus", changed.Models);
    }

    [Fact]
    public void ProjectTableKeepsArchivedRowsOutOfTheHeadlineOrder()
    {
        RevisionActivity older = Revision("rev-1", "plan-1", "Atlas", 1, Now.AddDays(-5), "Cursor", "opus");
        RevisionActivity newer = Revision("rev-2", "plan-2", "Mango", 1, Now, "Cursor", "opus", title: "Mango plan");
        RevisionActivity archived = Revision("rev-3", "plan-3", "Shelf", 1, Now.AddHours(1), "Cursor", "opus", true);

        DashboardModel model = DashboardStats.Compute([older, newer, archived], [], Now, Zone);

        Assert.Equal(["Mango", "Atlas", "Shelf"], model.Projects.Select(project => project.Name));
        Assert.True(model.Projects[^1].Archived);
        Assert.Empty(model.Pending);
    }

    [Fact]
    public void EmptyHistoryHasNoRows()
    {
        DashboardModel model = DashboardStats.Compute([], [], Now, Zone);

        Assert.True(model.Empty);
        Assert.Equal("—", model.Approval.Value);
        Assert.Empty(model.Projects);
        Assert.Empty(model.Agents);
        Assert.Empty(model.Switches);
    }

    [Fact]
    public void PendingKeepsOnlyTheNewestRevisionOfAnActivePlan()
    {
        RevisionActivity current = Revision("rev-current", "plan-current", "Atlas", 1, Now.AddDays(-1), "Cursor", "opus", title: "Current");
        RevisionActivity older = Revision("rev-older", "plan-done", "Atlas", 1, Now.AddDays(-4), "Cursor", "opus", title: "Done");
        RevisionActivity newer = Revision("rev-newer", "plan-done", "Atlas", 2, Now.AddDays(-2), "Cursor", "opus", title: "Done");
        RevisionActivity shelved = Revision("rev-shelf", "plan-shelf", "Atlas", 1, Now.AddDays(-3), "Cursor", "opus", title: "Shelf", planArchived: true);
        ListedReview[] reviews =
        [
            Review("review-current", "rev-current", ReviewStatus.Pending, Now.AddDays(-1)),
            Review("review-older", "rev-older", ReviewStatus.ChangesRequested, Now.AddDays(-4), Now.AddDays(-3)),
            Review("review-newer", "rev-newer", ReviewStatus.Approved, Now.AddDays(-2), Now.AddHours(-2)),
            Review("review-shelf", "rev-shelf", ReviewStatus.Approved, Now.AddDays(-3), Now.AddDays(-2)),
        ];

        DashboardModel model = DashboardStats.Compute([current, older, newer, shelved], reviews, Now, Zone);

        Assert.Equal(["review-current"], model.Pending.Select(item => item.ReviewId));
        Assert.Equal("67% of 3", model.Approval.Value);
    }

    private static RevisionActivity Revision(
        string id,
        string plan,
        string project,
        int number,
        DateTimeOffset at,
        string? agent,
        string? model,
        bool archived = false,
        string? title = null,
        bool planArchived = false) =>
        new(
            new PlanRevisionId(id),
            new PlanId(plan),
            title ?? project + " plan",
            new ProjectId(project),
            project,
            archived,
            number,
            at,
            new Attribution(agent, model, null, null),
            planArchived);

    private static ListedReview Review(
        string id,
        string revision,
        ReviewStatus status,
        DateTimeOffset created,
        DateTimeOffset? decided = null,
        int annotations = 0) =>
        new(new ReviewId(id), new ReviewRevisionId(revision), status, created, decided, annotations);
}