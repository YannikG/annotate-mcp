using System.ComponentModel;

using ModelContextProtocol.Server;

namespace Annotate.Web;

[McpServerToolType]
internal sealed class PlanTools
{
    [McpServerTool(Name = "annotate_plan"), Description("Returns immediately with plan_status=pending and a review URL. Send that URL to the user before await_plan_review. Do not implement the plan before plan_status=approved. If the project got archived, ask the user what to do and do not submit again until they answer. agent and model are required on every call, because either may change between revisions.")]
    public static Task<string> AnnotatePlan(
        IPlanHost host,
        IHttpContextAccessor http,
        McpServer server,
        string plan,
        string? summary = null,
        string? previousReviewId = null,
        string? sessionId = null,
        string? cwd = null,
        string? storyUrl = null,
        string? acceptanceCriteria = null,
        [Description("Required. Agent product making this call, for example Cursor, Claude Code, or Codex CLI.")] string? agent = null,
        [Description("Required. Exact model id currently generating this plan. Send it on every call.")] string? model = null,
        CancellationToken cancellationToken = default)
    {
        string requestHost = http.HttpContext?.Request.Host.Value ?? "";
        return host.SubmitAsync(
            new PlanSubmission(
                plan,
                summary,
                previousReviewId,
                sessionId,
                cwd,
                storyUrl,
                acceptanceCriteria,
                agent,
                model,
                server.ClientInfo?.Name,
                server.ClientInfo?.Version),
            requestHost,
            cancellationToken);
    }

    [McpServerTool(Name = "await_plan_review"), Description("Call only after the user has the review URL. A pending result is normal. Call again with the same review id. A stored decision is returned after a restart.")]
    public static Task<string> AwaitPlanReview(
        IPlanHost host,
        string reviewId,
        int? waitSeconds = null,
        CancellationToken cancellationToken = default) =>
        host.WaitAsync(reviewId, waitSeconds, cancellationToken);

    [McpServerTool(Name = "archive_plan"), Description(ArchivePlanText.Notice)]
    public static Task<string> ArchivePlan(
        IPlanHost host,
        [Description("Plan ID from annotate_plan. This does not submit a revision.")] string planId,
        CancellationToken cancellationToken = default) =>
        host.ArchiveAsync(planId, cancellationToken);

    [McpServerTool(Name = "get_plan_markdown_guide"), Description("Returns the Markdown the page can render. Call before a plan that uses diagrams, tables, or decision fences.")]
    public static string GetPlanMarkdownGuide(IPlanHost host) => host.Guide();
}