using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;
using Annotate.Web.Components.Pages;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Web.Tests;

public sealed class ProjectPageTests
{
    [Fact]
    public async Task ProjectShowsPlansAndRename()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 2));
        FakeProjectPlanPages pages = new()
        {
            Listing = new ProjectPlanListing(2, 2, 0, 0, 0,
            [
                new ProjectPlanRow("plan-new", "Newest", 2, DateTimeOffset.UnixEpoch, "review-new", ReviewStatus.Pending),
                new ProjectPlanRow("plan-old", "Older", 1, DateTimeOffset.UnixEpoch, "review-old", ReviewStatus.Pending),
            ]),
        };
        plans.Rename = (id, displayName) =>
        {
            string trimmed = displayName.Trim();
            ProjectSummary current = plans.Projects.Single(project => project.ProjectId == id);
            plans.Projects.Clear();
            plans.Projects.Add(current with { DisplayName = trimmed });
            return new RenameOutcome.Renamed();
        };

        using BunitContext context = BrowseHost.Open(plans, new FakeReviews(), pages);
        IRenderedComponent<ProjectPage> page = context.Render<ProjectPage>(parameters =>
            parameters.Add(component => component.Id, "project-1"));

        page.WaitForAssertion(() =>
        {
            Assert.Equal("Atlas", page.Find("h1").TextContent);
            Assert.Contains("/work/atlas", page.Markup);
            int newest = page.Markup.IndexOf("Newest", StringComparison.Ordinal);
            int older = page.Markup.IndexOf("Older", StringComparison.Ordinal);
            Assert.True(newest >= 0 && older > newest);
            Assert.Empty(page.FindAll("#display-name"));
            Assert.Equal("Projects", page.Find(".breadcrumb a[href='/projects']").TextContent);
            Assert.Contains("v2", page.Find("a[href='/plans/plan-new']").Closest("li")!.TextContent, StringComparison.Ordinal);
        });

        page.Find("[data-edit]").Click();
        page.Find("#display-name").Input("  Renamed  ");
        await page.Find("[data-edit-dialog]").SubmitAsync();

        page.WaitForAssertion(() =>
        {
            Assert.Equal("  Renamed  ", plans.RenamedName);
            Assert.Equal("Renamed", page.Find("h1").TextContent);
            Assert.Contains("/work/atlas", page.Markup);
            Assert.Empty(page.FindAll("#display-name"));
        });

        plans.Rename = (_, _) => new RenameOutcome.Refused("Display name is empty.");
        page.Find("[data-edit]").Click();
        page.Find("#display-name").Input("   ");
        await page.Find("[data-edit-dialog]").SubmitAsync();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("Display name is empty.", page.Markup);
            Assert.Equal("Renamed", page.Find("h1").TextContent);
            Assert.Contains("/work/atlas", page.Markup);
        });
    }

    [Fact]
    public async Task ArchiveAndDeleteWaitForConfirmation()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 1));
        plans.DeleteResult = new ProjectChange.Done(["revision-1"]);
        FakeReviews reviews = new();
        using BunitContext context = BrowseHost.Open(plans, reviews);
        NavigationManager navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/projects/project-1");
        IRenderedComponent<ProjectPage> page = context.Render<ProjectPage>(parameters =>
            parameters.Add(component => component.Id, "project-1"));

        page.WaitForAssertion(() => Assert.Equal("Atlas", page.Find("h1").TextContent));
        page.Find("[data-archive]").Click();
        page.Find("[data-cancel]").Click();
        Assert.Empty(plans.Archived);
        Assert.EndsWith("/projects/project-1", navigation.Uri, StringComparison.Ordinal);

        page.Find("[data-archive]").Click();
        await page.Find("[data-confirm]").ClickAsync();
        Assert.Equal(["project-1"], plans.Archived);
        Assert.Equal("/projects", new Uri(navigation.Uri).AbsolutePath);

        navigation.NavigateTo("/projects/project-1");
        page.Find("[data-delete]").Click();
        page.Find("[data-backdrop]").Click();
        Assert.Empty(plans.Deleted);

        page.Find("[data-delete]").Click();
        await page.Find("[data-confirm]").ClickAsync();
        Assert.Equal(["project-1"], plans.Deleted);
        Assert.Equal(["revision-1"], reviews.RemovedRevisions);
        Assert.Equal("/projects", new Uri(navigation.Uri).AbsolutePath);
    }

    [Fact]
    public async Task RestoreWaitsForConfirmation()
    {
        FakePlans plans = new();
        plans.ArchivedProjects.Add(new ProjectSummary(
            new ProjectId("project-1"),
            "Atlas",
            "/work/atlas",
            1,
            true));

        using BunitContext context = BrowseHost.Open(plans, new FakeReviews());
        IRenderedComponent<ProjectPage> page = context.Render<ProjectPage>(parameters =>
            parameters.Add(component => component.Id, "project-1"));

        page.WaitForAssertion(() =>
        {
            Assert.Equal("Atlas", page.Find("h1").TextContent);
            Assert.Empty(page.FindAll("[data-archive]"));
        });

        page.Find("[data-restore]").Click();
        page.Find("[data-cancel]").Click();
        Assert.Empty(plans.Restored);

        page.Find("[data-restore]").Click();
        await page.Find("[data-confirm]").ClickAsync();
        page.WaitForAssertion(() =>
        {
            Assert.Equal(["project-1"], plans.Restored);
            Assert.NotNull(page.Find("[data-archive]"));
            Assert.Empty(page.FindAll("[data-restore]"));
        });
    }
}