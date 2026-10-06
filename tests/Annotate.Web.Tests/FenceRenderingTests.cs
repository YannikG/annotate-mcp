using System.Globalization;

using AngleSharp.Dom;

using Annotate.Markdown;
using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;
using Annotate.Web.Components.Pages;
using Annotate.Web.Components.Review;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Web.Tests;

public sealed class FenceRenderingTests
{
    [Fact]
    public void LargeHunkCoordinatesDoNotCrashThePlan()
    {
        const string Markdown = "```diff\n@@ -999999999999999999999,1 +2147483648,1 @@\n-old\n+new\n```\n";
        Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(Markdown));
        using BunitContext context = new();
        IRenderedComponent<PlanText> page = context.Render<PlanText>(p => p.Add(c => c.Markdown, Markdown));
        Assert.Equal("999999999999999999999", page.Find(".diff-del .diff-number").TextContent);
        Assert.Equal("2147483648", page.FindAll(".diff-add .diff-number")[1].TextContent);
    }

    [Theory]
    [InlineData("plan", "diff")]
    [InlineData("review", "diff")]
    [InlineData("revision", "diff")]
    [InlineData("plan", "patch")]
    [InlineData("review", "PATCH")]
    [InlineData("revision", "patch")]
    public void FencesRenderOnEverySurfaceWithSourceOffsets(string surface, string language)
    {
        string markdown = $"# Changes\r\n\r\n  ~~~{language}\r\ndiff --git a/a b/a\r\nindex 123..456\r\n--- a/a\r\n+++ b/a\r\n@@ -7,2 +11,2 @@\r\n same\r\n-old\r\n+new\r\n  ~~~\r\n\r\n```mermaid\r\ngraph TD\r\nA-->B\r\n```\r\n";
        FakePlans plans = new();
        plans.Revisions["rev-1"] = new RevisionDetail(
            new Annotate.Plans.Application.RevisionId("rev-1"), new PlanId("plan-1"), 1, markdown, null, null, null, [], null);
        plans.PlansById["plan-1"] = new PlanDetail(
            new PlanId("plan-1"), new ProjectId("project-1"), "Changes", null,
            [new PlanRevision(new Annotate.Plans.Application.RevisionId("rev-1"), 1, DateTimeOffset.UnixEpoch)]);
        FakeReviews reviews = new();
        reviews.Review = new ReviewDetail(
            new ReviewId("review-1"), new Annotate.Reviews.Application.RevisionId("rev-1"),
            ReviewStatus.Pending, null, [], [], [], DateTimeOffset.UnixEpoch, null);
        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection());
        switch (surface)
        {
            case "plan":
                AssertPage(context.Render<PlanPage>(p => p.Add(c => c.Id, "plan-1")), markdown);
                break;
            case "review":
                AssertPage(context.Render<ReviewPage>(p => p.Add(c => c.Id, "review-1")), markdown);
                break;
            default:
                AssertPage(context.Render<RevisionPage>(p => p.Add(c => c.Id, "rev-1")), markdown);
                break;
        }
    }

    private static void AssertPage<T>(IRenderedComponent<T> page, string markdown)
        where T : Microsoft.AspNetCore.Components.IComponent
    {
        page.WaitForAssertion(() =>
        {
            IElement diff = page.Find("[data-diff]");
            Assert.Equal("Unified diff", diff.QuerySelector(".diff-title")!.TextContent.Trim());
            Assert.Empty(diff.QuerySelectorAll("pre, code"));
            Assert.Equal(4, diff.QuerySelectorAll(".diff-meta").Length);
            Assert.Equal("@@ -7,2 +11,2 @@", diff.QuerySelector(".diff-hunk .diff-content")!.TextContent);
            Row(diff, "same", "same", "7", "11", " ");
            Row(diff, "del", "old", "8", "", "−");
            Row(diff, "add", "new", "", "12", "+");
            foreach (IElement segment in diff.QuerySelectorAll("[data-seg-start]"))
            {
                int start = int.Parse(segment.GetAttribute("data-seg-start")!, CultureInfo.InvariantCulture);
                int end = int.Parse(segment.GetAttribute("data-seg-end")!, CultureInfo.InvariantCulture);
                Assert.Equal(markdown[start..end], segment.TextContent);
                Assert.Equal("1", segment.GetAttribute("data-block-index"));
                Assert.DoesNotContain("\r", segment.TextContent, StringComparison.Ordinal);
            }

            IElement diagram = page.Find("[data-mermaid]");
            Assert.NotNull(diagram.QuerySelector("[data-mermaid-canvas]"));
            Assert.Contains("A-->B", diagram.QuerySelector("[data-mermaid-body]")!.TextContent, StringComparison.Ordinal);
            Assert.Contains("```mermaid", diagram.QuerySelector("code[data-block-index]")!.TextContent, StringComparison.Ordinal);
            Assert.Empty(page.Find("[data-plan]").QuerySelectorAll("script, svg"));
        });
    }

    private static void Row(IElement diff, string kind, string text, string old, string next, string marker)
    {
        IElement row = diff.QuerySelector($".diff-{kind}")!;
        Assert.Equal(text, row.QuerySelector(".diff-content")!.TextContent);
        Assert.Equal(old, row.QuerySelectorAll(".diff-number")[0].TextContent);
        Assert.Equal(next, row.QuerySelectorAll(".diff-number")[1].TextContent);
        Assert.Equal(marker, row.QuerySelector(".diff-marker")!.TextContent);
    }
}