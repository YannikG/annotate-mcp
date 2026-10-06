using AngleSharp.Dom;

using Annotate.Reviews.Application;
using Annotate.Web;
using Annotate.Web.Components.Browse;
using Annotate.Web.Components.Layout;
using Annotate.Web.Components.Review;

using Bunit;

namespace Annotate.Web.Tests;

public sealed class InterfaceStructureTests
{
    [Fact]
    public void FooterUsesKeyHints()
    {
        using BunitContext context = new();
        context.JSInterop.Setup<bool>("annotateTheme.isDark").SetResult(false);
        IRenderedComponent<MainLayout> layout = context.Render<MainLayout>();

        Assert.Equal("d delete, r replace, s insert, c comment", layout.Find("footer").TextContent.Trim());
        Assert.Equal(["d", "r", "s", "c"], layout.FindAll("footer kbd").Select(key => key.TextContent));
    }

    [Fact]
    public void EmptyListsExplainThemselves()
    {
        using BunitContext context = new();

        DashboardModel empty = DashboardStats.Compute([], [], DateTimeOffset.UnixEpoch, TimeZoneInfo.Utc);
        IRenderedComponent<DashboardView> home = context.Render<DashboardView>(parameters =>
            parameters.Add(component => component.Model, empty));
        Assert.Contains("No projects yet. Submit a plan from an agent to create one.", home.Markup, StringComparison.Ordinal);

        IRenderedComponent<ProjectView> project = context.Render<ProjectView>(parameters =>
            parameters.Add(component => component.Model, new ProjectViewModel("Atlas", "/work/atlas", null, ProjectPlanListing.Empty)));
        Assert.Equal("This project has no plans.", project.Find(".empty-state h2").TextContent);

        IRenderedComponent<PlanView> plan = context.Render<PlanView>(parameters =>
            parameters.Add(component => component.Model, new PlanViewModel("Storage", [])));
        Assert.Equal("This plan has no revisions.", plan.Find(".empty-state h2").TextContent);

        IRenderedComponent<RevisionView> revision = context.Render<RevisionView>(parameters =>
            parameters.Add(component => component.Model, new RevisionViewModel(1, [])));
        Assert.Equal("This revision has no blocks.", revision.Find("[data-revision-changes] .empty-state h2").TextContent);
        Assert.Equal("No annotations yet.", revision.Find("[data-side-panel='annotations'] .empty-state h2").TextContent);
    }

    [Fact]
    public void OutcomeChartShowsTheCountScale()
    {
        WeekBar[] weeks = new WeekBar[12];
        for (int index = 0; index < weeks.Length; index++)
        {
            weeks[index] = new WeekBar($"2026-07-{index:00}", $"{index + 1} Jul", 0, 0, 0);
        }

        weeks[11] = new WeekBar("2026-10-05", "5 Oct", 3, 1, 6);
        using BunitContext context = new();
        IRenderedComponent<DashboardView> view = context.Render<DashboardView>(parameters =>
            parameters.Add(component => component.Model, Chart(weeks)));

        Assert.Equal(["4", "2", "0"], view.FindAll("[data-axis-tick]").Select(tick => tick.TextContent));
        Assert.Equal("75", view.FindAll(".week-approved")[^1].GetAttribute("height"));
        Assert.Empty(view.FindAll(".week-line"));
        Assert.Equal("5 Oct", view.FindAll(".week-label")[^1].TextContent);
    }

    private static DashboardModel Chart(IReadOnlyList<WeekBar> weeks) => new(
        false,
        new Metric("0", "", null),
        new Metric("0", "", null),
        new Metric("0", "", null),
        new Metric("0", "", null),
        new Metric("0", "", null),
        [],
        weeks,
        [],
        [],
        [],
        []);

    [Fact]
    public void ReviewGroupsActionsAndHidesAnnotationMetadata()
    {
        using BunitContext context = new();
        IRenderedComponent<ReviewView> view = context.Render<ReviewView>(parameters =>
            parameters.Add(component => component.Model, Sample(reportFailure: null, countdownRunning: false)));

        IElement toolbar = Assert.Single(view.FindAll("[data-editor-toolbar]"));
        IElement actions = toolbar.QuerySelector("[data-actions]")!;
        Assert.NotNull(actions);
        Assert.Empty(view.FindAll(".actions, .panel-tools"));
        Assert.Equal("Storage", view.Find(".editor-heading h1").TextContent);
        Assert.Empty(toolbar.QuerySelectorAll("[data-side-panel]"));
        Assert.NotNull(actions.QuerySelector("[data-approve]"));
        Assert.NotNull(actions.QuerySelector("[data-download]"));
        Assert.NotNull(actions.QuerySelector("[data-request-changes]"));
        Assert.NotNull(actions.QuerySelector("[data-auto-close]"));
        Assert.Empty(actions.QuerySelectorAll("a[href='/'], a[href='/projects']"));
        Assert.Empty(actions.QuerySelectorAll("button[aria-label='Dark'], button[aria-label='Light']"));
        Assert.Empty(view.FindAll("[data-note]"));
        Assert.Contains("Annotations", view.Markup, StringComparison.Ordinal);
        IElement annotationsPanel = view.Find("[data-side-panel='annotations']");
        Assert.Equal("auto", annotationsPanel.GetAttribute("popover"));
        Assert.Equal("dialog", annotationsPanel.GetAttribute("role"));
        Assert.Equal("Annotations", annotationsPanel.QuerySelector("h2")!.TextContent.Trim());
        Assert.Equal(annotationsPanel.Id, view.Find("[data-panel-trigger='annotations']").GetAttribute("popovertarget"));
        Assert.NotNull(annotationsPanel.QuerySelector("[data-annotations]"));
        Assert.NotNull(view.Find(".editor-toolbar-leading [data-panel-trigger='contents']"));
        Assert.NotNull(view.Find(".editor-toolbar-trailing [data-panel-trigger='annotations']"));

        Assert.True(view.Find(".countdown").HasAttribute("hidden"));
        Assert.Equal("5", view.Find("[data-countdown]").TextContent);

        IElement annotation = view.Find("[data-annotations] li");
        Assert.Equal("Deletion", annotation.QuerySelector(".kind")!.TextContent);
        Assert.Contains("visually-hidden", annotation.QuerySelector("[data-block]")!.ClassName, StringComparison.Ordinal);
        Assert.Contains("visually-hidden", annotation.QuerySelector("[data-start]")!.ClassName, StringComparison.Ordinal);
        Assert.Contains("visually-hidden", annotation.QuerySelector("[data-end]")!.ClassName, StringComparison.Ordinal);
        Assert.Contains("visually-hidden", annotation.QuerySelector("[data-id]")!.ClassName, StringComparison.Ordinal);
        Assert.Contains("visually-hidden", annotation.QuerySelector("[data-created]")!.ClassName, StringComparison.Ordinal);
        Assert.Equal("pending", view.Find("[data-status]").GetAttribute("data-state"));
    }

    [Fact]
    public void ContentsAndAnnotationsUseSidePanelFlyouts()
    {
        using BunitContext context = new();
        IRenderedComponent<RevisionView> revision = context.Render<RevisionView>(parameters =>
            parameters.Add(component => component.Model,
                new RevisionViewModel(1, [new BlockRow("Keep this text", 0, 14, "New")], "# Storage\n")));

        IElement contents = revision.Find("[data-side-panel='contents']");
        Assert.Equal("auto", contents.GetAttribute("popover"));
        Assert.Contains("side-panel-left", contents.ClassName, StringComparison.Ordinal);
        Assert.Equal("Contents", contents.QuerySelector("h2")!.TextContent.Trim());
        Assert.Equal(contents.Id, revision.Find("[data-panel-trigger='contents']").GetAttribute("popovertarget"));
        Assert.Equal("Storage", contents.QuerySelector("a")!.TextContent);
        Assert.EndsWith("#b-0", contents.QuerySelector("a")!.GetAttribute("href"), StringComparison.Ordinal);

        IElement annotations = revision.Find("[data-side-panel='annotations']");
        Assert.Equal("auto", annotations.GetAttribute("popover"));
        Assert.Contains("side-panel-right", annotations.ClassName, StringComparison.Ordinal);
        Assert.Equal("Annotations", annotations.QuerySelector("h2")!.TextContent.Trim());
        Assert.Equal(annotations.Id, revision.Find("[data-panel-trigger='annotations']").GetAttribute("popovertarget"));
        IElement changes = revision.Find("details[data-revision-changes]");
        Assert.False(changes.HasAttribute("open"));
        Assert.Equal("Keep this text", changes.QuerySelector("[data-changes] p")!.TextContent);
        Assert.Empty(revision.FindAll("[data-side-panel='changes']"));
    }

    [Fact]
    public async Task PlanToolbarGroupsRevisionWithAnnotationsAndKeepsContentsOnTheLeft()
    {
        using BunitContext context = new();
        string? selected = null;
        PlanViewModel model = new("Storage",
            [new RevisionRow("v1", 1, "", "Approved"), new RevisionRow("v2", 2, "", "Pending")],
            "v2", new RevisionViewModel(2, [], "# Storage\n"), new ProjectRow("project", "Atlas", 1));
        IRenderedComponent<PlanView> plan = context.Render<PlanView>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.RevisionChanged, value => selected = value));

        IElement toolbar = Assert.Single(plan.FindAll("[data-editor-toolbar]"));
        Assert.NotNull(toolbar.QuerySelector(".editor-toolbar-leading [data-panel-trigger='contents']"));
        Assert.NotNull(toolbar.QuerySelector(".editor-toolbar-trailing [data-panel-trigger='annotations']"));
        Assert.NotNull(toolbar.QuerySelector(".editor-toolbar-leading [data-revision]"));
        Assert.Empty(plan.FindAll(".page-title [data-revision]"));
        Assert.Equal("Revision", toolbar.QuerySelector(".select-label")!.TextContent.Trim());
        Assert.Empty(toolbar.QuerySelectorAll("[data-side-panel]"));
        Assert.Empty(plan.FindAll(".panel-tools, .actions"));
        await plan.Find("[data-revision] [data-select-trigger]").ClickAsync();
        await plan.FindAll("[role='option']").Single(option => option.TextContent.Trim() == "v1 · Approved").ClickAsync();
        Assert.Equal("v1", selected);
    }

    [Fact]
    public void RunningCountdownIsLabelled()
    {
        using BunitContext context = new();
        IRenderedComponent<ReviewView> view = context.Render<ReviewView>(parameters =>
            parameters.Add(component => component.Model, Sample(reportFailure: null, countdownRunning: true)));

        Assert.False(view.Find(".countdown").HasAttribute("hidden"));
        Assert.Contains("Submitting in", view.Find(".countdown").TextContent, StringComparison.Ordinal);
        Assert.Equal("5", view.Find("[data-countdown]").TextContent);
        Assert.Equal("true", view.Find("[data-countdown]").GetAttribute("data-running"));
    }

    [Fact]
    public void ReportFailureIsAnAlert()
    {
        using BunitContext context = new();
        IRenderedComponent<ReviewView> view = context.Render<ReviewView>(parameters =>
            parameters.Add(component => component.Model, Sample(reportFailure: "disk full", countdownRunning: false)));

        IElement alert = view.Find("[role='alert']");
        Assert.Equal("Report was not downloaded", alert.QuerySelector("[data-report-failure]")!.TextContent);
        Assert.Equal("disk full", alert.QuerySelector("[data-report-error]")!.TextContent);
        Assert.NotNull(alert.QuerySelector("[data-approve-without-download]"));
        Assert.NotNull(alert.QuerySelector("[data-dismiss-report]"));
    }

    [Fact]
    public void DecisionDialogGroupsActions()
    {
        using BunitContext context = new();
        DecisionDialogModel model = new(
            1,
            3,
            "storage",
            PromptKind.Choice,
            "Which store?",
            ["SQLite"],
            "",
            false,
            "",
            "",
            null);
        IRenderedComponent<DecisionDialog> dialog = context.Render<DecisionDialog>(parameters =>
            parameters.Add(component => component.Model, model));

        IElement dismiss = dialog.Find("[data-dialog-dismiss]");
        Assert.NotNull(dismiss.QuerySelector("[data-close]"));
        Assert.NotNull(dismiss.QuerySelector("[data-previous]"));
        Assert.Null(dismiss.QuerySelector("[data-next]"));
        Assert.Null(dismiss.QuerySelector("[data-done]"));

        IElement forward = dialog.Find("[data-dialog-forward]");
        Assert.Contains("primary", forward.QuerySelector("[data-next]")!.ClassName, StringComparison.Ordinal);
        Assert.NotNull(forward.QuerySelector("[data-done]"));
        Assert.Null(forward.QuerySelector("[data-close]"));

        IElement other = dialog.Find(".other-answer");
        Assert.NotNull(other.QuerySelector("[data-other]"));
        Assert.NotNull(other.QuerySelector("[data-other-text]"));
        Assert.Equal("Other", dialog.Find("[data-other]").ParentElement!.TextContent.Trim());
    }

    [Fact]
    public async Task CompactReviewActionsOpenDismissAndStillInvokeReviewActions()
    {
        using BunitContext context = new();
        int requests = 0;
        var view = context.Render<ReviewView>(parameters => parameters
            .Add(component => component.Model, Sample(null, false))
            .Add(component => component.RequestChanges, () => requests++));
        Assert.Equal("false", view.Find("[data-review-actions-trigger]").GetAttribute("aria-expanded"));
        await view.Find("[data-review-actions-trigger]").ClickAsync();
        Assert.Equal("true", view.Find("[data-review-actions-trigger]").GetAttribute("aria-expanded"));
        Assert.Contains("is-open", view.Find(".nav-right").ClassName, StringComparison.Ordinal);
        await view.Find("[data-request-changes]").ClickAsync();
        Assert.Equal(1, requests);
        await view.Find("[data-actions-dismiss]").ClickAsync();
        Assert.Equal("false", view.Find("[data-review-actions-trigger]").GetAttribute("aria-expanded"));
        await view.Find("[data-review-actions-trigger]").ClickAsync();
        await view.Find("[data-review]").KeyDownAsync("Escape");
        Assert.Equal("false", view.Find("[data-review-actions-trigger]").GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task AnnotationCommandsLiveOnlyInTheSelectionPopup()
    {
        using BunitContext context = new();
        string? command = null;
        ReviewViewModel model = Sample(null, false);
        var view = context.Render<ReviewView>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Annotate, key => command = key));
        Assert.Empty(view.FindAll("[data-editor-annotate]"));
        Assert.Empty(view.FindAll("[data-annotate-toolbar]"));
        view.Render(p => p.Add(c => c.Model, model with { Toolbar = new Annotate.Web.SelectionBox(10, 10, 30, 100) }));
        Assert.Empty(view.FindAll("[data-editor-annotate]"));
        AngleSharp.Dom.IElement popup = view.Find("[data-annotate-toolbar]");
        Assert.Equal(4, popup.QuerySelectorAll("[data-annotate]").Length);
        await popup.QuerySelector("[data-annotate='c']")!.ClickAsync();
        Assert.Equal("c", command);
    }

    private static ReviewViewModel Sample(string? reportFailure, bool countdownRunning) =>
        new(
            "Pending",
            true,
            true,
            "Keep one file",
            "https://example.com/story",
            "The file stays on disk",
            "# Storage\n",
            [
                new Annotation(
                    "11111111-1111-1111-1111-111111111111",
                    AnnotationKind.Deletion,
                    "Storage",
                    null,
                    null,
                    0,
                    2,
                    9,
                    "2026-10-01 08:00:00Z"),
            ],
            5,
            countdownRunning,
            [new PromptRow("storage", "Which store?", "(unanswered)")],
            null,
            null,
            null,
            false,
            reportFailure,
            "Storage");
}