using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

using Annotate.Markdown;
using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Web.Tests;

public sealed class PlanHostTests
{
    [Fact]
    public async Task SubmitReturnsPendingReviewOnLoopback()
    {
        await using AnnotateApp app = new();
        IPlanHost host = app.Services.GetRequiredService<IPlanHost>();
        const string RequestHost = "127.0.0.1:24173";

        Stopwatch watch = Stopwatch.StartNew();
        string text = await host.SubmitAsync(
            new PlanSubmission("# Storage\n", "Keep the plan", null, null, null, null, null, "Cursor", "claude-opus-4", "cursor-vscode", "1.7.2"),
            RequestHost,
            CancellationToken.None);
        watch.Stop();

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(15));
        string reviewId = Value(text, "Review ID: ");
        string planId = Value(text, "Plan ID: ");
        Assert.Equal(Pending(reviewId, planId, 1, $"http://{RequestHost}/review/{reviewId}"), text);

        IReviews reviews = app.Services.GetRequiredService<IReviews>();
        ReviewDetail? detail = await reviews.FindAsync(new ReviewId(reviewId), CancellationToken.None);
        Assert.NotNull(detail);
        Assert.Equal(ReviewStatus.Pending, detail.Status);
        PlanDetail stored = Assert.IsType<PlanDetail>(
            await app.Services.GetRequiredService<IPlans>().PlanAsync(new PlanId(planId), CancellationToken.None));
        Assert.Equal(
            new Attribution("Cursor", "claude-opus-4", "cursor-vscode", "1.7.2"),
            Assert.Single(stored.Revisions).Attribution);
    }

    [Fact]
    public async Task MissingAgentOrModelInsertsNothing()
    {
        await using AnnotateApp app = new();
        IPlanHost host = app.Services.GetRequiredService<IPlanHost>();
        Assert.Equal(
            "Error: Agent is required.",
            await host.SubmitAsync(new PlanSubmission("# Plan\n", null, null, null, null, null, null), "localhost", CancellationToken.None));
        Assert.Equal(
            "Error: Model is required.",
            await host.SubmitAsync(
                new PlanSubmission("# Plan\n", null, null, null, null, null, null, "Cursor", " "),
                "localhost",
                CancellationToken.None));

        IPlans plans = app.Services.GetRequiredService<IPlans>();
        IReviews reviews = app.Services.GetRequiredService<IReviews>();
        Assert.Empty(await plans.ProjectsAsync(CancellationToken.None));
        Assert.Empty(await reviews.PendingAsync(CancellationToken.None));
    }

    [Fact]
    public async Task RefusalsWriteNoPlanAndNoReview()
    {
        await using AnnotateApp app = new();
        IPlanHost host = app.Services.GetRequiredService<IPlanHost>();
        const string Fence = "# Plan\n\n```decision\nhello\n```\n";
        ParseOutcome.Invalid invalid = Assert.IsType<ParseOutcome.Invalid>(PlanMarkdown.Parse(Fence));

        Assert.Equal(
            "Error: Plan is empty.",
            await host.SubmitAsync(new PlanSubmission("   ", null, null, null, null, null, null), "localhost", CancellationToken.None));
        Assert.Equal(
            "Error: Story URL host is not allowed.",
            await host.SubmitAsync(
                new PlanSubmission("# Plan\n", null, null, null, null, "https://evil.test/plan", null),
                "localhost",
                CancellationToken.None));
        Assert.Equal(
            invalid.Error,
            await host.SubmitAsync(new PlanSubmission(Fence, null, null, null, null, null, null), "localhost", CancellationToken.None));

        IPlans plans = app.Services.GetRequiredService<IPlans>();
        IReviews reviews = app.Services.GetRequiredService<IReviews>();
        Assert.Empty(await plans.ProjectsAsync(CancellationToken.None));
        Assert.Empty(await reviews.PendingAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ArchivedProjectTellsTheAgentToAsk()
    {
        await using AnnotateApp app = new();
        IPlanHost host = app.Services.GetRequiredService<IPlanHost>();
        IPlans plans = app.Services.GetRequiredService<IPlans>();
        IReviews reviews = app.Services.GetRequiredService<IReviews>();
        const string RequestHost = "localhost";
        string folder = Path.Combine(Path.GetTempPath(), "annotate-folder-" + Guid.NewGuid().ToString("N"));
        string first = await host.SubmitAsync(
            new PlanSubmission("# Storage\n", null, null, null, folder, null, null, "Cursor", "claude-opus-4"),
            RequestHost,
            CancellationToken.None);
        string reviewId = Value(first, "Review ID: ");
        ProjectId projectId = Assert.Single(await plans.ProjectsAsync(CancellationToken.None)).ProjectId;
        Assert.IsType<ProjectChange.Done>(await plans.ArchiveProjectAsync(projectId, CancellationToken.None));

        string name = Path.GetFileName(folder);
        string ask =
            $"""
            This project got archived.
            Project: {name}

            Ask the user what to do. Do not submit the plan again until they answer.
            """;
        Assert.Equal(
            ask,
            await host.SubmitAsync(
                new PlanSubmission("# Storage\n\nNext\n", null, null, null, folder, null, null),
                RequestHost,
                CancellationToken.None));
        Assert.Equal(
            ask,
            await host.SubmitAsync(
                new PlanSubmission("# Storage\n\nNext\n", null, reviewId, null, folder, null, null),
                RequestHost,
                CancellationToken.None));
        Assert.Empty(await plans.ProjectsAsync(CancellationToken.None));
        Assert.Single(await reviews.PendingAsync(CancellationToken.None));
    }

    [Fact]
    public async Task PreviousReviewContinuesTheSamePlan()
    {
        await using AnnotateApp app = new();
        IPlanHost host = app.Services.GetRequiredService<IPlanHost>();
        const string RequestHost = "localhost";
        string folder = Path.Combine(Path.GetTempPath(), "annotate-folder-" + Guid.NewGuid().ToString("N"));

        string first = await host.SubmitAsync(
            new PlanSubmission("# Storage\n", null, null, "session-1", folder, null, null, "Cursor", "claude-opus-4"),
            RequestHost,
            CancellationToken.None);
        string firstReview = Value(first, "Review ID: ");
        string planId = Value(first, "Plan ID: ");

        string second = await host.SubmitAsync(
            new PlanSubmission("# Storage\n\nNext\n", null, firstReview, "session-2", folder, null, null, "Claude Code", "gpt-5-codex"),
            RequestHost,
            CancellationToken.None);
        string secondReview = Value(second, "Review ID: ");
        Assert.NotEqual(firstReview, secondReview);
        Assert.Equal(Pending(secondReview, planId, 2, $"http://{RequestHost}/review/{secondReview}"), second);

        IPlans plans = app.Services.GetRequiredService<IPlans>();
        IReviews reviews = app.Services.GetRequiredService<IReviews>();
        PlanDetail? continued = await plans.PlanAsync(new PlanId(planId), CancellationToken.None);
        Assert.NotNull(continued);
        Assert.Equal([1, 2], continued.Revisions.Select(revision => revision.Number).OrderBy(number => number).ToArray());
        Assert.Equal(
            ["Cursor", "Claude Code"],
            continued.Revisions.OrderBy(revision => revision.Number).Select(revision => revision.Attribution!.Agent));
        Assert.Equal(
            ["claude-opus-4", "gpt-5-codex"],
            continued.Revisions.OrderBy(revision => revision.Number).Select(revision => revision.Attribution!.Model));
        int pending = (await reviews.PendingAsync(CancellationToken.None)).Count;
        int planCount = Assert.Single(await plans.ProjectsAsync(CancellationToken.None)).PlanCount;

        const string Unknown = "missing-review";
        Assert.Equal(
            $"Error: unknown or expired reviewId \"{Unknown}\". Submit the plan again with annotate_plan.",
            await host.SubmitAsync(
                new PlanSubmission("# Storage\n\nNope\n", null, Unknown, null, folder, null, null),
                RequestHost,
                CancellationToken.None));
        Assert.Equal(planCount, Assert.Single(await plans.ProjectsAsync(CancellationToken.None)).PlanCount);
        Assert.Equal(pending, (await reviews.PendingAsync(CancellationToken.None)).Count);
        PlanDetail? unchanged = await plans.PlanAsync(new PlanId(planId), CancellationToken.None);
        Assert.NotNull(unchanged);
        Assert.Equal(2, unchanged.Revisions.Count);
    }

    [Fact]
    public async Task WaitReportsUnknownApprovedAndChanges()
    {
        await using AnnotateApp app = new();
        IPlanHost host = app.Services.GetRequiredService<IPlanHost>();
        IReviews reviews = app.Services.GetRequiredService<IReviews>();
        const string Unknown = "missing-review";
        Assert.Equal(UnknownReview(Unknown), await host.WaitAsync(Unknown, 0, CancellationToken.None));

        string approvedId = await SubmitReview(host, "# Approve\n");
        Assert.Equal(StillPending(approvedId), await host.WaitAsync(approvedId, 0, CancellationToken.None));
        Assert.IsType<DecideOutcome.Done>(await reviews.ApproveAsync(new ReviewId(approvedId), CancellationToken.None));
        IPlans plans = app.Services.GetRequiredService<IPlans>();
        string planId = Assert.Single(
            await plans.PlansAsync(
                Assert.Single(await plans.ProjectsAsync(CancellationToken.None)).ProjectId,
                CancellationToken.None)).PlanId.Value;
        Assert.Equal(
            $"""
            Plan review status: plan_status=approved.
            Plan ID: {planId}
            Proceed with the approved plan.
            """,
            await host.WaitAsync(approvedId, null, CancellationToken.None));

        string rejectedId = await SubmitReview(host, "# Change\n");
        Annotation note = new(
            "note-1",
            AnnotationKind.Comment,
            "Keep SQLite",
            "Say why",
            null,
            0,
            0,
            4,
            "2026-10-02T12:00:00Z");
        Assert.IsType<DecideOutcome.Done>(
            await reviews.RequestChangesAsync(new ReviewId(rejectedId), [note], CancellationToken.None));
        string rejected = await host.WaitAsync(rejectedId, 1, CancellationToken.None);
        Assert.StartsWith(
            """
            Plan review status: plan_status=rejected.
            State transition: next_state=PLAN_DRAFT.

            ## User feedback

            """,
            rejected);
        Assert.Contains("Say why", rejected);
        Assert.EndsWith(
            $"\n\nRevise the plan using this feedback, then submit the revised draft once via `annotate_plan` with previousReviewId=\"{rejectedId}\".",
            rejected);
    }

    [Fact]
    public async Task NonLoopbackHostIsRefused()
    {
        await using AnnotateApp app = new();
        IPlanHost host = app.Services.GetRequiredService<IPlanHost>();

        Assert.Equal(
            "Error: the review host must be loopback.",
            await host.SubmitAsync(
                new PlanSubmission("# Plan\n", null, null, null, null, null, null),
                "Example.COM:443",
                CancellationToken.None));

        IPlans plans = app.Services.GetRequiredService<IPlans>();
        IReviews reviews = app.Services.GetRequiredService<IReviews>();
        Assert.Empty(await plans.ProjectsAsync(CancellationToken.None));
        Assert.Empty(await reviews.PendingAsync(CancellationToken.None));

        using HttpClient client = app.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/");
        request.Headers.Host = "example.com";
        HttpResponseMessage response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GuideHealthAndMcpStayOnLoopback()
    {
        await using AnnotateApp app = new();
        Assert.Equal(PlanMarkdown.Guide, app.Services.GetRequiredService<IPlanHost>().Guide());

        using HttpClient client = app.CreateClient();
        HttpResponseMessage health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal("Healthy", (await health.Content.ReadAsStringAsync()).Trim());
        Assert.False(HasServerHeader(health));
        Assert.Equal("nosniff", Header(health, "X-Content-Type-Options"));
        Assert.Equal("default-src 'self'", Header(health, "Content-Security-Policy"));
        Assert.Equal("no-referrer", Header(health, "Referrer-Policy"));

        using HttpRequestMessage mcp = new(HttpMethod.Post, "/mcp");
        mcp.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        mcp.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        mcp.Content = new StringContent(
            """{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}""",
            Encoding.UTF8,
            "application/json");
        HttpResponseMessage posted = await client.SendAsync(mcp);
        string postedBody = await posted.Content.ReadAsStringAsync();
        Assert.NotEqual(HttpStatusCode.NotFound, posted.StatusCode);
        Assert.DoesNotContain("does not exist", postedBody, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"annotate_plan\"", postedBody, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"await_plan_review\"", postedBody, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"get_plan_markdown_guide\"", postedBody, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"archive_plan\"", postedBody, StringComparison.Ordinal);
        Assert.Contains("This only archives a finished plan", postedBody, StringComparison.Ordinal);
        Assert.Contains("\\u0060archive_plan\\u0060 to replace a draft.", postedBody, StringComparison.Ordinal);
        Assert.Contains("\"previousReviewId\"", postedBody, StringComparison.Ordinal);
        Assert.Contains("\"cwd\"", postedBody, StringComparison.Ordinal);
        Assert.Contains("\"agent\"", postedBody, StringComparison.Ordinal);
        Assert.Contains("\"model\"", postedBody, StringComparison.Ordinal);
        Assert.Contains("\"waitSeconds\"", postedBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProjectDirectoryHasItsOwnRoute()
    {
        await using AnnotateApp app = new();
        using HttpClient client = app.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/projects");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h1>Projects</h1>", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task MermaidBundleIsServedUnderTheExistingCsp()
    {
        await using AnnotateApp app = new();
        using HttpClient client = app.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/js/mermaid.min.js");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("default-src 'self'", Header(response, "Content-Security-Policy"));
        Assert.Contains("mermaid", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartupMigratesBothHistoriesBeforeAToolCall()
    {
        await using AnnotateApp app = new();
        app.CreateClient().Dispose();

        Assert.True(File.Exists(app.DatabasePath));
        HashSet<string> tables = await Tables(app.DatabasePath);
        Assert.Contains("plans_migration_history", tables);
        Assert.Contains("reviews_migration_history", tables);
    }

    private static async Task<HashSet<string>> Tables(string path)
    {
        await using SqliteConnection connection = new($"Data Source={path};Mode=ReadOnly");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
        HashSet<string> names = new(StringComparer.Ordinal);
        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private static bool HasServerHeader(HttpResponseMessage response) =>
        response.Headers.Contains("Server");

    private static string Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out IEnumerable<string>? values)
            ? Assert.Single(values)
            : Assert.Single(response.Content.Headers.GetValues(name));

    private static async Task<string> SubmitReview(IPlanHost host, string plan)
    {
        string text = await host.SubmitAsync(
            new PlanSubmission(plan, null, null, null, null, null, null, "Cursor", "claude-opus-4"),
            "localhost",
            CancellationToken.None);
        return Value(text, "Review ID: ");
    }

    private static string UnknownReview(string reviewId) =>
        $"Error: unknown or expired reviewId \"{reviewId}\". Submit the plan again with annotate_plan.";

    private static string StillPending(string reviewId) =>
        $"""
        Plan review status: plan_status=pending.
        Review ID: {reviewId}

        The review is still pending. Call `await_plan_review` again.
        """;

    private static string Pending(string reviewId, string planId, int number, string url) =>
        $"""
        Plan review status: plan_status=pending.
        Review ID: {reviewId}
        Plan ID: {planId}
        Revision: {number}
        Review URL: {url}

        Send the Review URL to the user now, before you wait. Do not call `await_plan_review` until the user has that URL.
        """;

    private static string Value(string text, string label)
    {
        foreach (string line in text.Split('\n'))
        {
            if (line.StartsWith(label, StringComparison.Ordinal))
            {
                return line[label.Length..];
            }
        }

        throw new InvalidOperationException($"Missing {label}");
    }
}