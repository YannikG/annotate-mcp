using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;
using Annotate.Web.Components.Pages;

using Bunit;

namespace Annotate.Web.Tests;

public sealed class ProjectPlanTabsTests
{
    [Fact]
    public async Task TabsFollowTheQueryAndOmitAllPlans()
    {
        ProjectPlanRow pending = Row("plan-pending", "pending", "review-pending", ReviewStatus.Pending);
        FakeProjectPlanPages pages = new()
        {
            Resolve = (tab, _) => new ProjectPlanListing(
                4, 1, 1, 1, 0, tab == ProjectPlanTab.Pending ? [pending] : []),
        };
        using BunitContext context = BrowseHost.Open(Project(), new FakeReviews(), pages);
        IRenderedComponent<ProjectPage> page = context.Render<ProjectPage>(parameters =>
            parameters.Add(component => component.Id, "project-1"));

        page.WaitForAssertion(() =>
        {
            Assert.Equal(["Pending reviews", "Changes requested", "Recently approved", "Archived"],
                page.FindAll("[role='tab'] .tab-label").Select(node => node.TextContent));
            Assert.Equal(["1", "1", "1", "0"], page.FindAll(".tab-count").Select(node => node.TextContent));
            Assert.Empty(page.FindAll("[data-tab='all']"));
            Assert.Equal("Pending", page.Find("[data-plan-card] .badge").TextContent);
            Assert.NotNull(page.Find("a[href='/plans/plan-pending']"));
            Assert.Empty(page.FindAll("a[href='/review/review-pending']"));
            Assert.Empty(page.FindAll("[data-page-next]"));
        });

        await page.Find("[data-tab='approved']").ClickAsync();
        Assert.Equal("No approved reviews yet.", page.Find(".empty-state h2").TextContent);
        Assert.Equal((ProjectPlanTab.Approved, 1), (pages.Calls[^1].Tab, pages.Calls[^1].Page));
        await page.Find("[data-tab='approved']").KeyDownAsync("Home");
        page.WaitForAssertion(() =>
            Assert.Equal("true", page.Find("[data-tab='pending']").GetAttribute("aria-selected")));
        Assert.Equal(ProjectPlanTab.Pending, pages.Calls[^1].Tab);
        Assert.Equal(1, pages.Calls[^1].Page);
    }

    [Fact]
    public async Task NextPageAsksTheQueryAndRendersOnlyThatPage()
    {
        FakeProjectPlanPages pages = new()
        {
            Resolve = (tab, page) =>
            {
                if (tab != ProjectPlanTab.Approved || page < 2)
                {
                    ProjectPlanRow[] rows = tab == ProjectPlanTab.Approved && page < 2
                        ? Enumerable.Range(1, 20).Select(index => Row("plan-" + index, index.ToString("00"), "review-" + index, ReviewStatus.Approved)).ToArray()
                        : [];
                    return new ProjectPlanListing(21, 0, 0, 21, 1, rows);
                }

                return new ProjectPlanListing(21, 0, 0, 21, 1, [Row("plan-0", "00", "review-0", ReviewStatus.Approved)]);
            },
        };
        using BunitContext context = BrowseHost.Open(Project(), new FakeReviews(), pages);
        IRenderedComponent<ProjectPage> page = context.Render<ProjectPage>(parameters =>
            parameters.Add(component => component.Id, "project-1"));
        page.WaitForAssertion(() => Assert.Equal("Atlas", page.Find("h1").TextContent));

        await page.Find("[data-tab='approved']").ClickAsync();
        page.WaitForAssertion(() => Assert.Equal(20, page.FindAll("[data-plan-card]").Count));
        Assert.Equal("Page 1 of 2", page.Find("[data-page-status]").TextContent);
        await page.Find("[data-page-next]").ClickAsync();
        page.WaitForAssertion(() =>
        {
            Assert.Equal("00", page.Find("[data-plan-card] .card-title").TextContent);
            Assert.Single(page.FindAll("[data-plan-card]"));
            Assert.Equal("Page 2 of 2", page.Find("[data-page-status]").TextContent);
        });
        Assert.Contains(pages.Calls, call => call.Tab == ProjectPlanTab.Approved && call.Page == 2);
    }

    [Fact]
    public async Task ArchivedTabAsksForTheArchivedPage()
    {
        FakeProjectPlanPages pages = new()
        {
            Resolve = (tab, _) => tab == ProjectPlanTab.Archived
                ? new ProjectPlanListing(1, 0, 0, 0, 1, [Row("plan-kept", "Kept", null, ReviewStatus.Approved)])
                : new ProjectPlanListing(1, 0, 0, 0, 1, []),
        };
        using BunitContext context = BrowseHost.Open(Project(), new FakeReviews(), pages);
        IRenderedComponent<ProjectPage> page = context.Render<ProjectPage>(parameters =>
            parameters.Add(component => component.Id, "project-1"));
        page.WaitForAssertion(() => Assert.Equal("1", page.Find("[data-tab='archived'] .tab-count").TextContent));
        Assert.Empty(page.FindAll("a[href='/plans/plan-kept']"));

        await page.Find("[data-tab='archived']").ClickAsync();
        page.WaitForAssertion(() =>
            Assert.NotNull(page.Find("[data-archived-plans] a[href='/plans/plan-kept']")));
        Assert.Empty(page.FindAll("a[href='/review/review-kept']"));
        Assert.Equal(ProjectPlanTab.Archived, pages.Calls[^1].Tab);
        Assert.Equal(1, pages.Calls[^1].Page);
    }

    private static FakePlans Project()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 1));
        return plans;
    }

    private static ProjectPlanRow Row(string planId, string title, string? reviewId, ReviewStatus status) =>
        new(planId, title, 1, DateTimeOffset.UnixEpoch, reviewId, status);
}