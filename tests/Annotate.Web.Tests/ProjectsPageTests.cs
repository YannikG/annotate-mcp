using Annotate.Plans.Application;
using Annotate.Web.Components.Pages;

using Bunit;

namespace Annotate.Web.Tests;

public sealed class ProjectsPageTests
{
    [Fact]
    public void DirectoryLinksToEachProjectAndKeepsArchivedProjectsAccessible()
    {
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("z"), "Zulu", "/work/z", 3));
        plans.Projects.Add(new ProjectSummary(new ProjectId("a"), "Atlas", "/work/a", 1));
        plans.ArchivedProjects.Add(new ProjectSummary(new ProjectId("s"), "Shelf", "/work/s", 2, true));
        using BunitContext context = BrowseHost.Open(plans, new FakeReviews());
        IRenderedComponent<ProjectsPage> page = context.Render<ProjectsPage>();
        page.WaitForAssertion(() =>
        {
            Assert.Equal("Projects", page.Find("h1").TextContent);
            Assert.Equal(["/projects/a", "/projects/z", "/projects/s"],
                page.FindAll("[data-project-card]").Select(card => card.GetAttribute("href")));
            Assert.Contains("1 plan", page.Find("a[href='/projects/a']").TextContent, StringComparison.Ordinal);
            Assert.Contains("/work/a", page.Find("a[href='/projects/a']").TextContent, StringComparison.Ordinal);
            Assert.Contains("Archived", page.Find("a[href='/projects/s']").TextContent, StringComparison.Ordinal);
            Assert.Empty(page.FindAll("[data-plan-card]"));
        });
    }

    [Fact]
    public void EmptyDirectoryExplainsHowProjectsAppear()
    {
        using BunitContext context = BrowseHost.Open(new FakePlans(), new FakeReviews());
        IRenderedComponent<ProjectsPage> page = context.Render<ProjectsPage>();
        page.WaitForAssertion(() => Assert.Contains("No projects yet.", page.Markup, StringComparison.Ordinal));
        Assert.NotNull(page.Find("a[href='/']"));
    }
}