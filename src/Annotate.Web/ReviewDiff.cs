using Annotate.Plans.Application;
using Annotate.Web.Components.Review;

namespace Annotate.Web;

internal static class ReviewDiff
{
    public static async Task<ReviewDiffAttempt> Open(
        IPlans plans,
        RevisionDetail revision,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(revision.ParentRevisionId))
        {
            return ReviewDiffAttempt.Skip;
        }

        RevisionDetail? parent;
        try
        {
            parent = await plans.RevisionAsync(new RevisionId(revision.ParentRevisionId), cancellationToken);
        }
        catch (Exception)
        {
            return ReviewDiffAttempt.Fail("Could not load this review");
        }

        if (parent is null)
        {
            return ReviewDiffAttempt.Fail("Revision was not found.");
        }

        DiffOutcome outcome;
        try
        {
            outcome = await plans.DiffAsync(parent.RevisionId, revision.RevisionId, cancellationToken);
        }
        catch (Exception)
        {
            return ReviewDiffAttempt.Fail("Could not load this review");
        }

        if (outcome is DiffOutcome.Refused refused)
        {
            return ReviewDiffAttempt.Fail(refused.Error);
        }

        DiffOutcome.Lines lines = (DiffOutcome.Lines)outcome;
        return ReviewDiffAttempt.Show(
            new DiffPanel(
                $"Changes from v{parent.Number} to v{revision.Number}",
                lines.Rows.Select(ToRow).ToArray()));
    }

    private static DiffRow ToRow(DiffLine line) => line switch
    {
        DiffLine.Equal equal => new("equal", equal.Text),
        DiffLine.Added added => new("added", added.Text),
        DiffLine.Removed removed => new("removed", removed.Text),
        _ => throw new InvalidOperationException("Diff line was not recognised."),
    };
}

internal readonly record struct ReviewDiffAttempt(bool Changed, DiffPanel? Panel, string? Notice)
{
    public static ReviewDiffAttempt Skip => new(false, null, null);

    public static ReviewDiffAttempt Fail(string notice) => new(true, null, notice);

    public static ReviewDiffAttempt Show(DiffPanel panel) => new(true, panel, null);
}