using Annotate.Plans.Application;
using Annotate.Web.Components.Pages;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using PlanRevisionId = Annotate.Plans.Application.RevisionId;

namespace Annotate.Web.Tests;

public sealed class PlanLifecyclePageTests
{
    [Fact]
    public async Task ArchiveAndDeleteWaitForConfirmation()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 1));
        plans.PlansById["plan-1"] = new PlanDetail(
            new PlanId("plan-1"),
            new ProjectId("project-1"),
            "Storage",
            null,
            [new PlanRevision(new PlanRevisionId("rev-1"), 1, DateTimeOffset.UnixEpoch)]);
        plans.DeletePlanResult = new PlanChange.Done(["rev-1"]);
        FakeReviews reviews = new();
        using BunitContext context = BrowseHost.Open(plans, reviews);
        NavigationManager navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/plans/plan-1");
        IRenderedComponent<PlanPage> page = context.Render<PlanPage>(parameters =>
            parameters.Add(component => component.Id, "plan-1"));

        page.WaitForAssertion(() => Assert.Equal("Storage", page.Find("h1").TextContent));
        page.Find("[data-archive]").Click();
        page.Find("[data-cancel]").Click();
        Assert.Empty(plans.ArchivedPlanIds);

        page.Find("[data-archive]").Click();
        await page.Find("[data-confirm]").ClickAsync();
        page.WaitForAssertion(() =>
        {
            Assert.Equal(["plan-1"], plans.ArchivedPlanIds);
            Assert.Equal("Archived", page.Find("[data-archived-plan]").TextContent);
            Assert.Empty(page.FindAll("[data-archive]"));
            Assert.NotNull(page.Find("[data-restore]"));
        });

        page.Find("[data-restore]").Click();
        await page.Find("[data-confirm]").ClickAsync();
        page.WaitForAssertion(() =>
        {
            Assert.Equal(["plan-1"], plans.RestoredPlanIds);
            Assert.NotNull(page.Find("[data-archive]"));
            Assert.Empty(page.FindAll("[data-restore]"));
        });

        page.Find("[data-delete]").Click();
        page.Find("[data-cancel]").Click();
        Assert.Empty(plans.DeletedPlanIds);

        page.Find("[data-delete]").Click();
        await page.Find("[data-confirm]").ClickAsync();
        Assert.Equal(["plan-1"], plans.DeletedPlanIds);
        Assert.Equal(["rev-1"], reviews.RemovedRevisions);
        Assert.Equal("/projects/project-1", new Uri(navigation.Uri).AbsolutePath);
    }
}