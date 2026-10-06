using AngleSharp.Dom;

using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;
using Annotate.Web.Components.Pages;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using PlanRevisionId = Annotate.Plans.Application.RevisionId;
using ReviewRevisionId = Annotate.Reviews.Application.RevisionId;

namespace Annotate.Web.Tests;

public sealed class DecisionQuestionTests
{
    [Fact]
    public async Task AnswersListedChoiceOtherAndText()
    {
        string markdown =
            """
            # Storage

            ```csharp
            int x = 1;
            ```

            ```decision
            id: storage
            kind: choice
            prompt: Which store?
            - SQLite
            - Postgres
            ```

            ```mermaid
            graph TD
              A-->B
            ```

            ```diff
            - old
            + new
            ```
            """;
        FakePlans plans = new();
        plans.Revisions["rev-1"] = Revision(markdown);
        FakeReviews reviews = new();
        reviews.Review = Review(
            ReviewStatus.Pending,
            [
                new ReviewPrompt("storage", PromptKind.Choice, "Which store?", ["SQLite", "Postgres"]),
                new ReviewPrompt("label", PromptKind.Choice, "What label?", ["Disk"]),
                new ReviewPrompt("note", PromptKind.Text, "Why?", []),
            ],
            [
                new DecisionAnswer("storage", "SQLite", false),
                new DecisionAnswer("label", "tape", true),
            ]);
        FakeSelection selection = new() { Next = new TextSelection(0, 2, 9, "Storage") };

        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(selection);
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));

        page.WaitForAssertion(() =>
        {
            IElement plan = page.Find("[data-plan]");
            Assert.Contains("int x = 1;", plan.TextContent, StringComparison.Ordinal);
            IElement decision = plan.QuerySelector("[data-decision-block]")!;
            Assert.NotNull(decision);
            Assert.Equal("Which store?", decision.QuerySelector("[data-decision-question]")!.TextContent);
            Assert.Equal(["SQLite", "Postgres", "Other"], decision.QuerySelectorAll("[data-decision-option]").Select(option => option.TextContent));
            Assert.Empty(decision.QuerySelectorAll("pre, code"));
            Assert.Equal("SQLite", decision.QuerySelector("[data-decision-answer]")!.TextContent);
            Assert.Contains("graph TD", plan.TextContent, StringComparison.Ordinal);
            Assert.Equal(" new", plan.QuerySelector("[data-diff] .diff-add .diff-content")!.TextContent);
            Assert.Equal("+", plan.QuerySelector("[data-diff] .diff-add .diff-marker")!.TextContent);
            Assert.Empty(plan.QuerySelectorAll("svg"));
            Assert.Empty(plan.QuerySelectorAll("[data-option]"));
            Assert.Empty(plan.QuerySelectorAll("textarea"));
            Assert.Empty(plan.QuerySelectorAll("[data-prompts]"));

            IReadOnlyList<IElement> rows = page.FindAll("[data-prompt]");
            Assert.Equal(["storage", "label", "note"], rows.Select(row => row.GetAttribute("data-prompt")));
            Assert.Equal("Which store?", rows[0].QuerySelector("[data-prompt-text]")!.TextContent);
            Assert.Equal("SQLite", rows[0].QuerySelector("[data-answer]")!.TextContent);
            Assert.Equal("What label?", rows[1].QuerySelector("[data-prompt-text]")!.TextContent);
            Assert.Equal("tape", rows[1].QuerySelector("[data-answer]")!.TextContent);
            Assert.Equal("Why?", rows[2].QuerySelector("[data-prompt-text]")!.TextContent);
            Assert.Equal("(unanswered)", rows[2].QuerySelector("[data-answer]")!.TextContent);
            Assert.Empty(page.FindAll("[data-dialog]"));
        });

        await page.Find("[data-answer-decision='storage']").ClickAsync();
        Assert.Equal("1 of 3", page.Find("[data-progress]").TextContent);
        Assert.Equal("Which store?", page.Find("[data-dialog-prompt]").TextContent);
        Assert.Equal(["SQLite", "Postgres"], page.FindAll("[data-option]").Select(option => option.GetAttribute("data-option")));
        Assert.True(page.Find("[data-option='SQLite']").HasAttribute("checked"));
        Assert.False(page.Find("[data-option='Postgres']").HasAttribute("checked"));
        Assert.False(page.Find("[data-other]").HasAttribute("checked"));
        Assert.Equal("Other", page.Find("[data-other]").ParentElement!.TextContent.Trim());
        Assert.True(page.Find("[data-other-text]").HasAttribute("disabled"));
        Assert.Equal("4000", page.Find("[data-other-text]").GetAttribute("maxlength"));
        Assert.True(page.Find("[data-previous]").HasAttribute("disabled"));
        Assert.NotNull(page.Find("[data-next]"));
        Assert.Empty(reviews.SavedAnswers);

        await page.Find("[data-other]").ClickAsync();
        Assert.False(page.Find("[data-other-text]").HasAttribute("disabled"));
        await page.Find("[data-option='Postgres']").ClickAsync();
        Assert.True(page.Find("[data-option='Postgres']").HasAttribute("checked"));
        Assert.False(page.Find("[data-other]").HasAttribute("checked"));
        Assert.True(page.Find("[data-other-text]").HasAttribute("disabled"));
        await page.Find("[data-close]").ClickAsync();
        Assert.Empty(page.FindAll("[data-dialog]"));
        Assert.Empty(reviews.SavedAnswers);
        Assert.Equal("SQLite", page.Find("[data-prompt='storage'] [data-answer]").TextContent);

        await page.Find("[data-prompt='label']").ClickAsync();
        Assert.Equal("2 of 3", page.Find("[data-progress]").TextContent);
        Assert.Equal("What label?", page.Find("[data-dialog-prompt]").TextContent);
        Assert.True(page.Find("[data-other]").HasAttribute("checked"));
        Assert.False(page.Find("[data-other-text]").HasAttribute("disabled"));
        Assert.Equal("tape", page.Find("[data-other-text]").GetAttribute("value"));
        await page.Find("[data-backdrop]").ClickAsync();
        Assert.Empty(page.FindAll("[data-dialog]"));
        Assert.Empty(reviews.SavedAnswers);

        await page.Find("[data-prompt='note']").ClickAsync();
        Assert.Equal("3 of 3", page.Find("[data-progress]").TextContent);
        Assert.Equal("Why?", page.Find("[data-dialog-prompt]").TextContent);
        Assert.Empty(page.FindAll("[data-option]"));
        Assert.Empty(page.FindAll("[data-other]"));
        Assert.Equal("4000", page.Find("[data-answer-text]").GetAttribute("maxlength"));
        Assert.Equal("", page.Find("[data-answer-text]").GetAttribute("value") ?? page.Find("[data-answer-text]").TextContent);
        Assert.Empty(page.FindAll("[data-next]"));
        Assert.False(page.Find("[data-previous]").HasAttribute("disabled"));
        await page.Find("[data-previous]").ClickAsync();
        Assert.Empty(reviews.SavedAnswers);
        Assert.Equal("2 of 3", page.Find("[data-progress]").TextContent);
        Assert.Equal("tape", page.Find("[data-other-text]").GetAttribute("value"));

        await page.Find("[data-prompt='storage']").ClickAsync();
        await page.Find("[data-option='Postgres']").ClickAsync();
        await page.Find("[data-next]").ClickAsync();
        Assert.Equal(new DecisionAnswer("storage", "Postgres", false), Assert.Single(reviews.SavedAnswers).Answer);
        Assert.Equal("review-1", reviews.SavedAnswers[0].ReviewId);
        Assert.Equal("2 of 3", page.Find("[data-progress]").TextContent);
        Assert.Equal("tape", page.Find("[data-other-text]").GetAttribute("value"));
        Assert.Equal("SQLite", page.Find("[data-prompt='storage'] [data-answer]").TextContent);

        await page.Find("[data-previous]").ClickAsync();
        Assert.Single(reviews.SavedAnswers);
        Assert.Equal("1 of 3", page.Find("[data-progress]").TextContent);
        Assert.True(page.Find("[data-option='Postgres']").HasAttribute("checked"));
        Assert.Equal("SQLite", page.Find("[data-prompt='storage'] [data-answer]").TextContent);

        await page.Find("[data-next]").ClickAsync();
        Assert.Equal("2 of 3", page.Find("[data-progress]").TextContent);
        await page.Find("[data-other-text]").InputAsync("disk");
        await page.Find("[data-next]").ClickAsync();
        Assert.Equal(new DecisionAnswer("label", "disk", true), reviews.SavedAnswers[^1].Answer);
        Assert.Equal("3 of 3", page.Find("[data-progress]").TextContent);
        Assert.Equal("tape", page.Find("[data-prompt='label'] [data-answer]").TextContent);
        Assert.Empty(page.FindAll("[data-next]"));

        await page.Find("[data-answer-text]").InputAsync("because");
        await page.Find("[data-done]").ClickAsync();
        Assert.Equal(new DecisionAnswer("note", "because", false), reviews.SavedAnswers[^1].Answer);
        Assert.Empty(page.FindAll("[data-dialog]"));
        Assert.Equal("Postgres", page.Find("[data-prompt='storage'] [data-answer]").TextContent);
        Assert.Equal("Postgres", page.Find("[data-decision-answer]").TextContent);
        Assert.Equal("disk", page.Find("[data-prompt='label'] [data-answer]").TextContent);
        Assert.Equal("because", page.Find("[data-prompt='note'] [data-answer]").TextContent);

        await page.Find("[data-review]").KeyDownAsync("d");
        Assert.Equal("true", page.Find("[data-countdown]").GetAttribute("data-running"));
        int saved = reviews.SavedAnswers.Count;
        await page.Find("[data-prompt='note']").ClickAsync();
        Assert.Equal("because", page.Find("[data-answer-text]").GetAttribute("value") ?? page.Find("[data-answer-text]").TextContent);
        await page.Find("[data-dialog]").KeyDownAsync(Key.Escape);
        Assert.Empty(page.FindAll("[data-dialog]"));
        Assert.Equal(saved, reviews.SavedAnswers.Count);
        Assert.Equal("true", page.Find("[data-countdown]").GetAttribute("data-running"));
        Assert.Equal("5", page.Find("[data-countdown]").TextContent);
    }

    [Fact]
    public async Task RefusedAnswerLeavesTheShownAnswer()
    {
        FakePlans plans = new();
        plans.Revisions["rev-1"] = Revision("# Storage\n");
        FakeReviews reviews = new();
        reviews.SaveOutcome = new SaveAnswerOutcome.Refused("Decision answer was rejected.");
        reviews.Review = Review(
            ReviewStatus.Pending,
            [
                new ReviewPrompt("storage", PromptKind.Choice, "Which store?", ["SQLite", "Postgres"]),
                new ReviewPrompt("note", PromptKind.Text, "Why?", []),
            ],
            [new DecisionAnswer("storage", "SQLite", false)]);

        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection());
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));
        page.WaitForAssertion(() => Assert.Equal("SQLite", page.Find("[data-prompt='storage'] [data-answer]").TextContent));

        await page.Find("[data-prompt='storage']").ClickAsync();
        await page.Find("[data-option='Postgres']").ClickAsync();
        await page.Find("[data-next]").ClickAsync();

        Assert.Equal(new DecisionAnswer("storage", "Postgres", false), Assert.Single(reviews.SavedAnswers).Answer);
        Assert.Equal("1 of 2", page.Find("[data-progress]").TextContent);
        Assert.Equal("Which store?", page.Find("[data-dialog-prompt]").TextContent);
        Assert.Equal("Decision answer was rejected.", page.Find("[data-error]").TextContent);
        Assert.Equal("SQLite", page.Find("[data-prompt='storage'] [data-answer]").TextContent);

        reviews.SaveOutcome = new SaveAnswerOutcome.Done();
        await page.Find("[data-next]").ClickAsync();
        Assert.Equal("2 of 2", page.Find("[data-progress]").TextContent);
        Assert.Empty(page.FindAll("[data-error]"));
        Assert.Equal("SQLite", page.Find("[data-prompt='storage'] [data-answer]").TextContent);
        await page.Find("[data-answer-text]").InputAsync("because");
        await page.Find("[data-done]").ClickAsync();
        Assert.Empty(page.FindAll("[data-dialog]"));
        Assert.Equal("Postgres", page.Find("[data-prompt='storage'] [data-answer]").TextContent);
        Assert.Equal("because", page.Find("[data-prompt='note'] [data-answer]").TextContent);
    }

    [Fact]
    public async Task DecidedReviewDoesNotOpen()
    {
        FakePlans plans = new();
        plans.Revisions["rev-1"] = Revision("# Storage\n");
        FakeReviews reviews = new();
        reviews.Review = Review(
            ReviewStatus.Approved,
            [new ReviewPrompt("storage", PromptKind.Choice, "Which store?", ["SQLite"])],
            [new DecisionAnswer("storage", "SQLite", false)]);

        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection());
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));
        page.WaitForAssertion(() =>
        {
            IElement row = page.Find("[data-prompt='storage']");
            Assert.Equal("Which store?", row.QuerySelector("[data-prompt-text]")!.TextContent);
            Assert.Equal("SQLite", row.QuerySelector("[data-answer]")!.TextContent);
            Assert.True(row.HasAttribute("disabled"));
        });

        await page.Find("[data-prompt='storage']").ClickAsync();
        Assert.Empty(page.FindAll("[data-dialog]"));
        Assert.Empty(reviews.SavedAnswers);
    }

    [Fact]
    public async Task ApproveStaysDisabledWhileAPromptExists()
    {
        FakePlans plans = new();
        plans.Revisions["rev-1"] = Revision("# Storage\n");
        FakeReviews reviews = new();
        reviews.Review = Review(
            ReviewStatus.Pending,
            [new ReviewPrompt("note", PromptKind.Text, "Why?", [])],
            [new DecisionAnswer("note", "because", false)]);

        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection());
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review-1"));
        page.WaitForAssertion(() =>
        {
            Assert.Equal("because", page.Find("[data-answer]").TextContent);
            Assert.True(page.Find("[data-approve]").HasAttribute("disabled"));
            Assert.False(page.Find("[data-request-changes]").HasAttribute("disabled"));
        });

        await page.Find("[data-approve]").ClickAsync();
        Assert.Empty(reviews.Approved);
        Assert.Equal("Pending", page.Find("[data-status]").TextContent);
    }

    private static RevisionDetail Revision(string markdown) =>
        new(new PlanRevisionId("rev-1"), new PlanId("plan-1"), 1, markdown, null, null, null, [], null);

    private static ReviewDetail Review(
        ReviewStatus status,
        IReadOnlyList<ReviewPrompt> prompts,
        IReadOnlyList<DecisionAnswer> answers) =>
        new(
            new ReviewId("review-1"),
            new ReviewRevisionId("rev-1"),
            status,
            null,
            [],
            answers,
            prompts,
            DateTimeOffset.UnixEpoch,
            status == ReviewStatus.Pending ? null : DateTimeOffset.UnixEpoch);
}