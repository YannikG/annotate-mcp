using AngleSharp.Dom;

using Annotate.Markdown;
using Annotate.Plans.Application;
using Annotate.Web.Components.Pages;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using PlanRevisionId = Annotate.Plans.Application.RevisionId;

namespace Annotate.Web.Tests;

public sealed class RevisionPageTests
{
    [Fact]
    public void RevisionShowsBlockTextPositionsAndChanges()
    {
        const string Markdown = "alpha\nbeta\ngamma\n";
        FakePlans plans = new();
        plans.Revisions["rev-1"] = new RevisionDetail(
            new PlanRevisionId("rev-1"),
            new PlanId("plan-1"),
            3,
            Markdown,
            null,
            null,
            null,
            [
                Block("k1", 0, 6, BlockChange.Added),
                Block("k2", 6, 11, BlockChange.Changed),
                Block("k3", 11, 17, BlockChange.Unchanged),
            ],
            null);

        using BunitContext context = BrowseHost.Open(plans, new FakeReviews());
        IRenderedComponent<RevisionPage> page = context.Render<RevisionPage>(parameters =>
            parameters.Add(component => component.Id, "rev-1"));

        page.WaitForAssertion(() =>
        {
            Assert.Contains("alpha", page.Find("[data-plan]").TextContent, StringComparison.Ordinal);
            Assert.Contains("beta", page.Find("[data-plan]").TextContent, StringComparison.Ordinal);
            Assert.Contains("gamma", page.Find("[data-plan]").TextContent, StringComparison.Ordinal);
            IReadOnlyList<IElement> items = page.FindAll("[data-changes] li");
            Assert.Equal(3, items.Count);
            Assert.Equal("Added", items[0].GetAttribute("data-change"));
            Assert.Contains("alpha", items[0].TextContent, StringComparison.Ordinal);
            Assert.Equal("Changed", items[1].GetAttribute("data-change"));
            Assert.Contains("beta", items[1].TextContent, StringComparison.Ordinal);
            Assert.Equal("Unchanged", items[2].GetAttribute("data-change"));
            Assert.Contains("gamma", items[2].TextContent, StringComparison.Ordinal);
            Assert.Empty(page.FindAll("pre"));
        });
    }

    [Fact]
    public void RevisionListsChaptersOnTheLeft()
    {
        const string Markdown =
            """
            # Storage

            Keep it.

            ## Layout

            One file.
            """;
        FakePlans plans = new();
        plans.Projects.Add(new ProjectSummary(new ProjectId("project-1"), "Atlas", "/work/atlas", 1));
        plans.PlansById["plan-1"] = new PlanDetail(new PlanId("plan-1"), new ProjectId("project-1"), "Storage", null, []);
        plans.Revisions["rev-1"] = new RevisionDetail(
            new PlanRevisionId("rev-1"),
            new PlanId("plan-1"),
            1,
            Markdown,
            null,
            null,
            null,
            [],
            null);

        using BunitContext context = BrowseHost.Open(plans, new FakeReviews());
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/revisions/rev-1");
        IRenderedComponent<RevisionPage> page = context.Render<RevisionPage>(parameters =>
            parameters.Add(component => component.Id, "rev-1"));

        page.WaitForAssertion(() =>
        {
            IReadOnlyList<IElement> links = page.FindAll("[data-contents] a");
            Assert.Equal(["Storage", "Layout"], links.Select(link => link.TextContent));
            Assert.Equal("/revisions/rev-1#b-0", links[0].GetAttribute("href"));
            Assert.Equal("false", links[0].GetAttribute("data-enhance-nav"));
            Assert.Equal("/revisions/rev-1#b-2", links[1].GetAttribute("href"));
            int contents = page.Markup.IndexOf("data-contents", StringComparison.Ordinal);
            int plan = page.Markup.IndexOf("data-plan", StringComparison.Ordinal);
            Assert.True(contents >= 0 && plan > contents);
            Assert.Equal("Projects", page.Find(".breadcrumb a[href='/projects']").TextContent);
            Assert.Equal("Atlas", page.Find(".breadcrumb a[href='/projects/project-1']").TextContent);
        });
    }

    private static RevisionBlock Block(string key, int start, int end, BlockChange change) =>
        new(key, BlockKind.Paragraph, "", start, end, change, key);
}