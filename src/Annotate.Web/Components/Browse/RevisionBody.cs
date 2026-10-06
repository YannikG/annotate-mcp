using Annotate.Plans.Application;
using Annotate.Reviews.Application;

namespace Annotate.Web.Components.Browse;

internal static class RevisionBody
{
    public static RevisionViewModel From(
        RevisionDetail revision, IReadOnlyList<Annotation>? annotations = null, bool approved = false) =>
        new(
            revision.Number,
            revision.Blocks.Select(block => new BlockRow(
                Text(revision.Markdown, block.Start, block.End),
                block.Start,
                block.End,
                Change(block.Change))).ToArray(),
            revision.Markdown, annotations, revision.Summary, revision.StoryUrl, revision.AcceptanceCriteria,
            approved, revision.Attribution);

    private static string Text(string markdown, int start, int end)
    {
        if ((uint)start > (uint)markdown.Length || end < start || end > markdown.Length)
        {
            return "";
        }

        return markdown[start..end];
    }

    private static string Change(BlockChange change) => change switch
    {
        BlockChange.Added => "Added",
        BlockChange.Changed => "Changed",
        BlockChange.Unchanged => "Unchanged",
        _ => throw new InvalidOperationException("Block change was not recognised."),
    };
}