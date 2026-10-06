using System.Globalization;

using AngleSharp.Dom;
using AngleSharp.Html.Parser;

using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;
using Annotate.Web.Components.Pages;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Web.Tests;

public sealed class CodeFenceRenderingTests
{
    [Theory]
    [InlineData("plan", "\n")]
    [InlineData("review", "\n")]
    [InlineData("revision", "\n")]
    [InlineData("report", "\n")]
    [InlineData("plan", "\r\n")]
    [InlineData("review", "\r\n")]
    [InlineData("revision", "\r\n")]
    [InlineData("report", "\r\n")]
    public void IndentedBashFenceShowsItsBodyAndLanguage(string surface, string newline)
    {
        string markdown = string.Join(newline, "# Commands", "", "  ```bash", "  npx ng g @angular/core:standalone --defaults", "    echo '<script>safe</script>'", "  ```", "");
        if (surface == "report")
        {
            IDocument document = new HtmlParser().ParseDocument(PlanReport.Html("Commands", null, null, markdown));
            Check(document.QuerySelector("[data-code-fence]")!, markdown, false);
            Assert.Empty(document.QuerySelectorAll("script"));
            return;
        }
        FakePlans plans = new();
        plans.Revisions["rev-1"] = new(new Annotate.Plans.Application.RevisionId("rev-1"), new PlanId("plan-1"), 1,
            markdown, null, null, null, [], null);
        plans.PlansById["plan-1"] = new(new PlanId("plan-1"), new ProjectId("project"), "Commands", null,
            [new(new Annotate.Plans.Application.RevisionId("rev-1"), 1, DateTimeOffset.UnixEpoch)]);
        FakeReviews reviews = new()
        {
            Review = new(new ReviewId("review-1"), new Annotate.Reviews.Application.RevisionId("rev-1"),
                ReviewStatus.Pending, null, [], [], [], DateTimeOffset.UnixEpoch, null),
        };
        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection());
        switch (surface)
        {
            case "plan": CheckPage(context.Render<PlanPage>(p => p.Add(c => c.Id, "plan-1")), markdown); break;
            case "review": CheckPage(context.Render<ReviewPage>(p => p.Add(c => c.Id, "review-1")), markdown); break;
            default: CheckPage(context.Render<RevisionPage>(p => p.Add(c => c.Id, "rev-1")), markdown); break;
        }
    }

    private static void CheckPage<T>(IRenderedComponent<T> page, string source) where T : Microsoft.AspNetCore.Components.IComponent =>
        page.WaitForAssertion(() => Check(page.Find("[data-code-fence]"), source, true));

    private static void Check(IElement block, string source, bool offsets)
    {
        Assert.NotNull(block);
        Assert.Equal("bash", block.QuerySelector(".code-language")!.TextContent);
        IElement code = block.QuerySelector("pre > code.language-bash")!;
        Assert.NotNull(code);
        Assert.Equal("npx ng g @angular/core:standalone --defaults\n  echo '<script>safe</script>'\n", code.TextContent);
        Assert.DoesNotContain("```", code.TextContent, StringComparison.Ordinal);
        Assert.Empty(block.QuerySelectorAll("script"));
        if (!offsets) return;
        Assert.Equal(2, code.QuerySelectorAll("[data-seg-start]").Length);
        foreach (IElement segment in code.QuerySelectorAll("[data-seg-start]"))
        {
            int start = int.Parse(segment.GetAttribute("data-seg-start")!, CultureInfo.InvariantCulture);
            int end = int.Parse(segment.GetAttribute("data-seg-end")!, CultureInfo.InvariantCulture);
            Assert.Equal(source[start..end], segment.TextContent);
            Assert.Equal("1", segment.GetAttribute("data-block-index"));
        }
    }
}