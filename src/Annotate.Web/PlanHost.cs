using System.ComponentModel;

using Annotate.Markdown;
using Annotate.Plans.Application;
using Annotate.Reviews.Application;

using ReviewRevisionId = Annotate.Reviews.Application.RevisionId;

namespace Annotate.Web;

internal sealed class PlanHost(
    IPlans plans,
    IReviews reviews,
    IConfiguration configuration,
    IReviewBrowser browser) : IPlanHost
{
    public async Task<string> SubmitAsync(
        PlanSubmission submission,
        string requestHost,
        CancellationToken cancellationToken)
    {
        if (!LoopbackHost.Accepts(requestHost))
        {
            return "Error: the review host must be loopback.";
        }

        string? parentRevisionId = null;
        if (!string.IsNullOrWhiteSpace(submission.PreviousReviewId))
        {
            ReviewDetail? previous = await reviews.FindAsync(new ReviewId(submission.PreviousReviewId), cancellationToken);
            if (previous is null)
            {
                return Unknown(submission.PreviousReviewId);
            }

            parentRevisionId = previous.RevisionId.Value;
        }

        SubmitOutcome outcome = await plans.SubmitAsync(
            new SubmitRevision(
                submission.Plan,
                submission.Summary,
                submission.FolderPath,
                submission.SessionId,
                parentRevisionId,
                submission.StoryUrl,
                submission.AcceptanceCriteria,
                submission.Agent,
                submission.Model,
                submission.ClientName,
                submission.ClientVersion),
            cancellationToken);
        if (outcome is SubmitOutcome.Archived archived)
        {
            return ArchivedProject(archived.DisplayName);
        }

        if (outcome is SubmitOutcome.PlanArchived planArchived)
        {
            return ArchivedPlanRevision(planArchived.Title);
        }

        if (outcome is SubmitOutcome.Refused refused)
        {
            return Error(refused.Error);
        }

        SubmitOutcome.Created created = (SubmitOutcome.Created)outcome;
        OpenOutcome opened = await reviews.OpenAsync(
            new ReviewRevisionId(created.Revision.Value),
            submission.Plan,
            cancellationToken);
        if (opened is OpenOutcome.Refused reviewRefused)
        {
            return Error(reviewRefused.Error);
        }

        OpenOutcome.Opened review = (OpenOutcome.Opened)opened;
        OpenReview(configuration, browser, requestHost, review.Id.Value);
        return Pending(review.Id.Value, created.Plan.Value, created.Number, requestHost);
    }

    public async Task<string> WaitAsync(string reviewId, int? waitSeconds, CancellationToken cancellationToken)
    {
        int limit = Math.Max(0, configuration.GetValue("Annotate:MaxWaitSeconds", 600));
        int seconds = waitSeconds ?? configuration.GetValue("Annotate:DefaultWaitSeconds", 50);
        if (seconds < 0)
        {
            seconds = 0;
        }
        else if (seconds > limit)
        {
            seconds = limit;
        }

        WaitOutcome outcome = await reviews.WaitAsync(
            new ReviewId(reviewId),
            TimeSpan.FromSeconds(seconds),
            cancellationToken);
        return outcome switch
        {
            WaitOutcome.Missing => Unknown(reviewId),
            WaitOutcome.Pending => StillPending(reviewId),
            WaitOutcome.Decided(ReviewDecision.Approved) => await Approved(reviewId, cancellationToken),
            WaitOutcome.Decided(ReviewDecision.ChangesRequested requested) => Changes(reviewId, requested.Feedback),
            _ => throw new InvalidOperationException("Review wait was not recognised."),
        };
    }

    public async Task<string> ArchiveAsync(string planId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(planId))
        {
            return Error("Plan was not found.");
        }

        PlanChange outcome = await plans.ArchivePlanAsync(new PlanId(planId.Trim()), cancellationToken);
        return outcome switch
        {
            PlanChange.Refused refused => Error(refused.Error),
            PlanChange.AlreadyArchived => ArchiveResult(planId.Trim(), already: true),
            PlanChange.Done => ArchiveResult(planId.Trim(), already: false),
            _ => throw new InvalidOperationException("Plan archive was not recognised."),
        };
    }

    public string Guide() => PlanMarkdown.Guide;

    private static void OpenReview(
        IConfiguration configuration,
        IReviewBrowser browser,
        string requestHost,
        string reviewId)
    {
        if (!configuration.GetValue("Annotate:OpenBrowser", false))
        {
            return;
        }

        try
        {
            browser.TryOpen($"http://{requestHost}/review/{reviewId}");
        }
        catch (InvalidOperationException)
        {
            return;
        }
        catch (Win32Exception)
        {
            return;
        }
        catch (PlatformNotSupportedException)
        {
            return;
        }
    }

    private static string StillPending(string reviewId) =>
        $"""
        Plan review status: plan_status=pending.
        Review ID: {reviewId}

        The review is still pending. Call `await_plan_review` again.
        """;

    private async Task<string> Approved(string reviewId, CancellationToken cancellationToken)
    {
        ReviewDetail? review = await reviews.FindAsync(new ReviewId(reviewId), cancellationToken);
        string planId = "";
        if (review is not null)
        {
            RevisionDetail? revision = await plans.RevisionAsync(
                new Annotate.Plans.Application.RevisionId(review.RevisionId.Value),
                cancellationToken);
            planId = revision?.PlanId.Value ?? "";
        }

        return
            $"""
            Plan review status: plan_status=approved.
            Plan ID: {planId}
            Proceed with the approved plan.
            """;
    }

    private static string Changes(string reviewId, string feedback) =>
        $"""
        Plan review status: plan_status=rejected.
        State transition: next_state=PLAN_DRAFT.

        ## User feedback

        {feedback}

        Revise the plan using this feedback, then submit the revised draft once via `annotate_plan` with previousReviewId="{reviewId}".
        """;

    private static string ArchiveResult(string planId, bool already) =>
        $"""
        {(already ? "This plan is already archived." : "Plan archived.")}
        Plan ID: {planId}

        {ArchivePlanText.Notice}
        """;

    private static string ArchivedPlanRevision(string title) =>
        $"""
        This plan is archived.
        Plan: {title}

        Archive is not a new variant. Ask the user whether to restore this plan or start a new one. Do not submit again until they answer.
        """;

    private static string ArchivedProject(string displayName) =>
        $"""
        This project got archived.
        Project: {displayName}

        Ask the user what to do. Do not submit the plan again until they answer.
        """;

    private static string Unknown(string reviewId) =>
        $"Error: unknown or expired reviewId \"{reviewId}\". Submit the plan again with annotate_plan.";

    private static string Error(string message) =>
        message.StartsWith("Error: ", StringComparison.Ordinal) ? message : "Error: " + message;

    private static string Pending(string reviewId, string planId, int number, string requestHost) =>
        $"""
        Plan review status: plan_status=pending.
        Review ID: {reviewId}
        Plan ID: {planId}
        Revision: {number}
        Review URL: http://{requestHost}/review/{reviewId}

        Send the Review URL to the user now, before you wait. Do not call `await_plan_review` until the user has that URL.
        """;
}