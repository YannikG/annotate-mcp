using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;
using Annotate.Web.Components.Pages;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using PlanRevisionId = Annotate.Plans.Application.RevisionId;
using ReviewRevisionId = Annotate.Reviews.Application.RevisionId;

namespace Annotate.Web.Tests;

public sealed class ReviewReportTests
{
    [Fact(Timeout = 5000)]
    public async Task FailedDownloadDoesNotApprove()
    {
        using ReviewSession session = ReviewSession.Open("Hello", "Keep one file", null, null);
        session.Report.Gate = new TaskCompletionSource<ReportDownload>();

        Task click = session.Page.Find("[data-download]").ClickAsync();
        Assert.Empty(session.Reviews.Approved);
        session.Report.Gate.SetResult(new ReportDownload(false, "disk full"));
        await click;

        Assert.Empty(session.Reviews.Approved);
        Assert.Equal("Pending", session.Page.Find("[data-status]").TextContent);
        Assert.Equal("Report was not downloaded", session.Page.Find("[data-report-failure]").TextContent);
        Assert.Equal("disk full", session.Page.Find("[data-report-error]").TextContent);

        await session.Page.Find("[data-dismiss-report]").ClickAsync();
        Assert.Empty(session.Page.FindAll("[data-report-failure]"));
        Assert.Equal("Pending", session.Page.Find("[data-status]").TextContent);
        Assert.Empty(session.Reviews.Approved);

        session.Report.Gate = new TaskCompletionSource<ReportDownload>();
        Task again = session.Page.Find("[data-download]").ClickAsync();
        session.Report.Gate.SetResult(new ReportDownload(false, "disk full"));
        await again;
        await session.Page.Find("[data-approve-without-download]").ClickAsync();
        Assert.Equal(["review-1"], session.Reviews.Approved);
        Assert.Equal("Approved", session.Page.Find("[data-status]").TextContent);
    }

    [Fact]
    public async Task ApproveWithoutAFile()
    {
        using ReviewSession session = ReviewSession.Open("Hello", "Keep one file", null, null);
        await session.Page.Find("[data-approve]").ClickAsync();

        Assert.Empty(session.Report.Downloads);
        Assert.Equal(["review-1"], session.Reviews.Approved);
        Assert.Equal("Approved", session.Page.Find("[data-status]").TextContent);
    }

    [Fact(Timeout = 5000)]
    public async Task DownloadThenApproves()
    {
        const string markdown = "Hello";
        const string summary = "Keep one file";
        const string story = "https://example.com/story";
        const string criteria = "The file stays on disk";
        using ReviewSession session = ReviewSession.Open(markdown, summary, story, criteria);
        session.Selection.Next = new TextSelection(0, 0, 5, "Hello");
        session.Page.WaitForAssertion(() => Assert.NotNull(session.Page.Find("[data-review]")));
        await session.Page.Find("[data-review]").KeyDownAsync("c");
        await session.Page.Find("[data-note]").InputAsync("secret-note");
        await session.Page.Find("[data-save-note]").ClickAsync();
        Assert.True(session.Page.Find("[data-download]").HasAttribute("disabled"));
        await session.Page.Find("[data-review]").KeyDownAsync("u");
        session.Report.Gate = new TaskCompletionSource<ReportDownload>();

        Task click = session.Page.Find("[data-download]").ClickAsync();
        Assert.NotEmpty(session.Report.Downloads);
        Assert.Empty(session.Reviews.Approved);
        session.Report.Gate.SetResult(new ReportDownload(true, ""));
        await click;

        Assert.Equal(["review-1"], session.Reviews.Approved);
        Assert.Equal("plan.html", session.Report.Downloads[0].FileName);
        Assert.Equal(PlanReport.Html(summary, story, criteria, markdown), session.Report.Downloads[0].Html);
        var document = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(session.Report.Downloads[0].Html);
        Assert.Single(document.QuerySelectorAll("head style"));
        Assert.Empty(document.QuerySelectorAll("link[rel='stylesheet']"));
        Assert.DoesNotContain("secret-note", session.Report.Downloads[0].Html, StringComparison.Ordinal);
        Assert.Equal("Approved", session.Page.Find("[data-status]").TextContent);
    }

    [Fact]
    public async Task AutoCloseOffDoesNotCloseAndOnClosesAfterDone()
    {
        using ReviewSession off = ReviewSession.Open("Hello", null, null, null);
        Assert.Contains("Auto-close", off.Page.Find("label").TextContent, StringComparison.Ordinal);
        Assert.False(off.Page.Find("[data-auto-close]").HasAttribute("checked"));
        await off.Page.Find("[data-approve]").ClickAsync();
        Assert.Equal(["review-1"], off.Reviews.Approved);
        Assert.Equal(0, off.Report.Closes);

        using ReviewSession on = ReviewSession.Open("Hello", null, null, null);
        on.Report.DecisionRecorded = () => on.Reviews.Changes.Count > 0;
        await on.Page.Find("[data-auto-close]").ChangeAsync(true);
        Assert.True(on.Preference.AutoCloseOnSubmit);
        Assert.Equal(1, on.Preference.Writes);
        Assert.True(on.Page.Find("[data-auto-close]").HasAttribute("checked"));
        await on.Page.Find("[data-request-changes]").ClickAsync();
        Assert.Single(on.Reviews.Changes);
        Assert.Equal(1, on.Report.Closes);
        Assert.True(on.Report.DecisionRecordedBeforeClose);
    }

    [Fact]
    public async Task RefusedDecisionDoesNotClose()
    {
        using ReviewSession session = ReviewSession.Open("Hello", null, null, null, autoClose: true);
        session.Reviews.ApproveOutcome = new DecideOutcome.Refused("no");
        Assert.True(session.Page.Find("[data-auto-close]").HasAttribute("checked"));
        await session.Page.Find("[data-approve]").ClickAsync();

        Assert.Equal("Pending", session.Page.Find("[data-status]").TextContent);
        Assert.Equal(0, session.Report.Closes);
    }

    [Fact]
    public async Task FailedPreferenceSaveKeepsPreviousValue()
    {
        using ReviewSession session = ReviewSession.Open("Hello", null, null, null);
        session.Preference.FailWrite = true;
        await session.Page.Find("[data-auto-close]").ChangeAsync(true);

        Assert.False(session.Preference.AutoCloseOnSubmit);
        Assert.Equal(1, session.Preference.Writes);
        Assert.False(session.Page.Find("[data-auto-close]").HasAttribute("checked"));
    }

    [Fact]
    public async Task DecidedReviewAndPromptsDisableApproveActions()
    {
        using ReviewSession decided = ReviewSession.Open(
            "Hello",
            null,
            null,
            null,
            status: ReviewStatus.Approved);
        Assert.True(decided.Page.Find("[data-approve]").HasAttribute("disabled"));
        Assert.True(decided.Page.Find("[data-download]").HasAttribute("disabled"));
        await decided.Page.Find("[data-approve]").ClickAsync();
        await decided.Page.Find("[data-download]").ClickAsync();
        Assert.Empty(decided.Reviews.Approved);
        Assert.Empty(decided.Report.Downloads);

        using ReviewSession prompted = ReviewSession.Open(
            "Hello",
            null,
            null,
            null,
            prompts: [new ReviewPrompt("storage", PromptKind.Choice, "Which store?", ["SQLite"])]);
        Assert.True(prompted.Page.Find("[data-approve]").HasAttribute("disabled"));
        Assert.True(prompted.Page.Find("[data-download]").HasAttribute("disabled"));
        await prompted.Page.Find("[data-approve]").ClickAsync();
        await prompted.Page.Find("[data-download]").ClickAsync();
        Assert.Empty(prompted.Reviews.Approved);
        Assert.Empty(prompted.Report.Downloads);
    }

    private sealed class ReviewSession : IDisposable
    {
        private ReviewSession(
            BunitContext context,
            FakeReviews reviews,
            MemoryAutoClose preference,
            RecordingReport report,
            FakeSelection selection,
            IRenderedComponent<ReviewPage> page)
        {
            Context = context;
            Reviews = reviews;
            Preference = preference;
            Report = report;
            Selection = selection;
            Page = page;
        }

        public BunitContext Context { get; }

        public FakeReviews Reviews { get; }

        public MemoryAutoClose Preference { get; }

        public RecordingReport Report { get; }

        public FakeSelection Selection { get; }

        public IRenderedComponent<ReviewPage> Page { get; }

        public static ReviewSession Open(
            string markdown,
            string? summary,
            string? story,
            string? criteria,
            ReviewStatus status = ReviewStatus.Pending,
            IReadOnlyList<ReviewPrompt>? prompts = null,
            bool autoClose = false)
        {
            FakePlans plans = new();
            plans.Revisions["rev-1"] = new RevisionDetail(
                new PlanRevisionId("rev-1"),
                new PlanId("plan-1"),
                1,
                markdown,
                summary,
                story,
                criteria,
                [],
                null);
            FakeReviews reviews = new()
            {
                Review = new ReviewDetail(
                    new ReviewId("review-1"),
                    new ReviewRevisionId("rev-1"),
                    status,
                    null,
                    [],
                    [],
                    prompts ?? [],
                    DateTimeOffset.UnixEpoch,
                    status == ReviewStatus.Pending ? null : DateTimeOffset.UnixEpoch),
            };
            BunitContext context = BrowseHost.Open(plans, reviews);
            FakeSelection selection = new();
            context.Services.AddSingleton<ISelectionReader>(selection);
            MemoryAutoClose preference = new() { AutoCloseOnSubmit = autoClose };
            context.Services.AddSingleton<IAutoClosePreference>(preference);
            RecordingReport report = new();
            context.Services.AddSingleton<IReportScript>(report);
            IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
                parameters.Add(component => component.Id, "review-1"));
            page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-review]")));
            return new ReviewSession(context, reviews, preference, report, selection, page);
        }

        public void Dispose() => Context.Dispose();
    }
}