using Annotate.Web.Components.Pages;

using Bunit;

namespace Annotate.Web.Tests;

public sealed class MissingPageTests
{
    [Fact]
    public void UnknownIdsRenderNotFound()
    {
        using BunitContext context = BrowseHost.Open(new FakePlans(), new FakeReviews());

        IRenderedComponent<ProjectPage> project = context.Render<ProjectPage>(parameters =>
            parameters.Add(component => component.Id, "missing-project"));
        project.WaitForAssertion(() => Assert.Equal("Not found", project.Find("h1").TextContent));

        IRenderedComponent<PlanPage> plan = context.Render<PlanPage>(parameters =>
            parameters.Add(component => component.Id, "missing-plan"));
        plan.WaitForAssertion(() => Assert.Equal("Not found", plan.Find("h1").TextContent));

        IRenderedComponent<RevisionPage> revision = context.Render<RevisionPage>(parameters =>
            parameters.Add(component => component.Id, "missing-revision"));
        revision.WaitForAssertion(() => Assert.Equal("Not found", revision.Find("h1").TextContent));
    }
}