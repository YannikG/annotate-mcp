using AngleSharp.Dom;

using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web.Components.Pages;

using Bunit;

using PlanRevisionId = Annotate.Plans.Application.RevisionId;
using ReviewRevisionId = Annotate.Reviews.Application.RevisionId;

namespace Annotate.Web.Tests;

public sealed class HomePageTests
{
    [Fact]
    public void DashboardShowsActiveMetricsAndListsArchivedProjectsSeparately()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        FakePlans plans = new();
        plans.Activity.Add(Activity("rev-1", "plan-1", "Store plans", "project-1", "Atlas", false, now, "Cursor", "opus"));
        plans.Activity.Add(Activity("rev-old", "plan-old", "Old", "project-old", "Shelf", true, now, "Cursor", "opus"));
        FakeReviews reviews = new();
        reviews.Listed.Add(new ListedReview(
            new ReviewId("review-1"), new ReviewRevisionId("rev-1"), ReviewStatus.Pending, now.AddDays(-2)));
        reviews.Listed.Add(new ListedReview(
            new ReviewId("review-old"), new ReviewRevisionId("rev-old"), ReviewStatus.Pending, now.AddDays(-9)));

        using BunitContext context = BrowseHost.Open(plans, reviews);
        IRenderedComponent<Home> page = context.Render<Home>();

        page.WaitForAssertion(() =>
        {
            Assert.Equal("Dashboard", page.Find("h1").TextContent);
            Assert.Empty(page.FindAll("[data-metric='waiting'], [data-metric='revisions-to-approval']"));
            IElement pending = page.Find("[data-pending]");
            Assert.Equal("Store plans", pending.QuerySelector(".card-title")!.TextContent);
            Assert.Equal("/review/review-1", pending.QuerySelector(".card-title")!.GetAttribute("href"));
            Assert.Equal("/projects/project-1", pending.QuerySelector(".project-link")!.GetAttribute("href"));
            Assert.DoesNotContain("Old", pending.TextContent, StringComparison.Ordinal);
            Assert.Equal(12, page.FindAll("[data-week]").Count);
            Assert.Equal("Atlas", page.Find("[data-project-row] a[href='/projects/project-1']").TextContent);
            Assert.Equal("Shelf", page.Find("[data-project-row][data-archived='true'] a").TextContent);
            Assert.Contains("Cursor", page.Find("[data-agent-row]").TextContent, StringComparison.Ordinal);
            Assert.Contains("opus", page.Find("[data-model-row]").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void EmptyDashboardExplainsHowProjectsAppear()
    {
        using BunitContext context = BrowseHost.Open(new FakePlans(), new FakeReviews());
        IRenderedComponent<Home> page = context.Render<Home>();

        page.WaitForAssertion(() =>
            Assert.Contains("No projects yet. Submit a plan from an agent to create one.", page.Markup, StringComparison.Ordinal));
        Assert.Empty(page.FindAll("[data-metric]"));
    }

    private static RevisionActivity Activity(
        string revision,
        string plan,
        string title,
        string project,
        string name,
        bool archived,
        DateTimeOffset at,
        string agent,
        string model) =>
        new(
            new PlanRevisionId(revision),
            new PlanId(plan),
            title,
            new ProjectId(project),
            name,
            archived,
            1,
            at,
            new Attribution(agent, model, null, null));
}