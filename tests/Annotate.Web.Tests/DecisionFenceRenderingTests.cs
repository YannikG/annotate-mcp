using System.Globalization;

using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;
using Annotate.Web.Components.Pages;
using Annotate.Web.Components.Review;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Web.Tests;

public sealed class DecisionFenceRenderingTests
{
    [Fact]
    public void RepeatedDecisionLabelsKeepTheirOwnSourceOffsets()
    {
        const string Markdown = "```decision\nid: repeat\nkind: choice\nprompt: Repeat Repeat?\n- Repeat\n- Repeat\n```\n";
        using BunitContext context = new();
        var page = context.Render<PlanText>(p => p.Add(c => c.Markdown, Markdown));
        var segments = page.FindAll("[data-decision-block] [data-seg-start]");
        Assert.Equal(["Repeat Repeat?", "Repeat", "Repeat"], segments.Select(n => n.TextContent));
        Assert.Equal(3, segments.Select(n => n.GetAttribute("data-seg-start")).Distinct().Count());
        foreach (var segment in segments)
        {
            int start = int.Parse(segment.GetAttribute("data-seg-start")!, CultureInfo.InvariantCulture);
            int end = int.Parse(segment.GetAttribute("data-seg-end")!, CultureInfo.InvariantCulture);
            Assert.Equal(Markdown[start..end], segment.TextContent);
            Assert.Equal("0", segment.GetAttribute("data-block-index"));
        }
    }

    [Theory]
    [InlineData("plan")]
    [InlineData("review")]
    [InlineData("revision")]
    public void DecisionFencesRenderInPlaceOnEverySurface(string surface)
    {
        const string Markdown = "# Decisions\r\n\r\n```DECISION\r\nid: store\r\nkind: choice\r\nprompt: Which <store>?\r\n-  SQLite\r\n- Postgres\r\n- Other\r\n```\r\n\r\n<details>\r\n<summary>\r\nMore\r\n</summary>\r\n\r\n```decision\r\nid: reason\r\nkind: text\r\nprompt: Why?\r\n```\r\n</details>\r\n";
        FakePlans plans = new();
        plans.Revisions["rev-1"] = new RevisionDetail(
            new Annotate.Plans.Application.RevisionId("rev-1"), new PlanId("plan-1"), 1, Markdown, null, null, null, [], null);
        plans.PlansById["plan-1"] = new PlanDetail(new PlanId("plan-1"), new ProjectId("project-1"), "Decisions", null,
            [new PlanRevision(new Annotate.Plans.Application.RevisionId("rev-1"), 1, DateTimeOffset.UnixEpoch)]);
        FakeReviews reviews = new();
        reviews.Review = new ReviewDetail(new ReviewId("review-1"), new Annotate.Reviews.Application.RevisionId("rev-1"),
            ReviewStatus.ChangesRequested, null, [],
            [new DecisionAnswer("store", "Postgres", false)],
            [new ReviewPrompt("store", PromptKind.Choice, "Which <store>?", ["SQLite", "Postgres"]), new ReviewPrompt("reason", PromptKind.Text, "Why?", [])],
            DateTimeOffset.UnixEpoch, null);
        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection());
        switch (surface)
        {
            case "plan": Check(context.Render<PlanPage>(p => p.Add(c => c.Id, "plan-1")), Markdown, false); break;
            case "review": Check(context.Render<ReviewPage>(p => p.Add(c => c.Id, "review-1")), Markdown, true); break;
            default: Check(context.Render<RevisionPage>(p => p.Add(c => c.Id, "rev-1")), Markdown, false); break;
        }
    }

    private static void Check<T>(IRenderedComponent<T> page, string markdown, bool review)
        where T : Microsoft.AspNetCore.Components.IComponent
    {
        page.WaitForAssertion(() =>
        {
            var cards = page.FindAll("[data-plan] [data-decision-block]");
            Assert.Equal(2, cards.Count);
            Assert.Equal("Which <store>?", cards[0].QuerySelector("[data-decision-question]")!.TextContent);
            Assert.Equal(["SQLite", "Postgres", "Other"], cards[0].QuerySelectorAll("[data-decision-option]").Select(n => n.TextContent));
            Assert.Equal("Why?", cards[1].QuerySelector("[data-decision-question]")!.TextContent);
            Assert.Empty(cards[1].QuerySelectorAll("[data-decision-option]"));
            Assert.Empty(page.FindAll("[data-plan] pre, [data-plan] script, [data-plan] store"));
            foreach (var segment in cards.SelectMany(card => card.QuerySelectorAll("[data-seg-start]")))
            {
                int start = int.Parse(segment.GetAttribute("data-seg-start")!, CultureInfo.InvariantCulture);
                int end = int.Parse(segment.GetAttribute("data-seg-end")!, CultureInfo.InvariantCulture);
                Assert.Equal(markdown[start..end], segment.TextContent);
                Assert.True(segment.GetAttribute("data-block-index") is "1" or "2");
            }
            Assert.Empty(page.FindAll("[data-answer-decision]"));
            if (review) Assert.Equal("Postgres", cards[0].QuerySelector("[data-decision-answer]")!.TextContent);
        });
    }
}