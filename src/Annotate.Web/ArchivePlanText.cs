namespace Annotate.Web;

internal static class ArchivePlanText
{
    public const string Notice =
        "This only archives a finished plan so it leaves the active lists. It does not submit a new revision or a new variant. A new variant is `annotate_plan` with `previousReviewId`. Do not call `archive_plan` to replace a draft.";
}