using Annotate.Web.Components.Browse;

using Bunit;

namespace Annotate.Web.Tests;

public sealed class PlanCardTests
{
    [Theory]
    [InlineData(null, "/plans/plan")]
    [InlineData("review", "/review/review")]
    public void CardHasOnePrimaryDestinationAndKeepsTheProjectLink(string? reviewId, string destination)
    {
        using BunitContext context = new();
        var item = context.Render<PlanListItem>(p => p
            .Add(c => c.PlanId, "plan")
            .Add(c => c.Title, "Storage")
            .Add(c => c.ReviewId, reviewId)
            .Add(c => c.ProjectId, "project")
            .Add(c => c.ProjectName, "Atlas"));

        Assert.Equal(destination, item.Find(".card-title").GetAttribute("href"));
        Assert.Equal("Storage", item.Find(".card-title").TextContent);
        Assert.Equal("/projects/project", item.Find(".project-link").GetAttribute("href"));
        Assert.Equal(2, item.FindAll("a").Count);
        Assert.Empty(item.FindAll(".plan-link"));
        Assert.DoesNotContain("View plan", item.Markup, StringComparison.Ordinal);
    }
}