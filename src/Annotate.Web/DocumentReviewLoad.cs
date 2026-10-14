using Annotate.Plans.Application;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Web;

internal sealed record DocumentReviewLoad(
    bool Missing,
    bool Redirected,
    RevisionDetail? Revision,
    string Title,
    string? ProjectId,
    string? ProjectName,
    IReadOnlyList<string> LockedKeys)
{
    public static async Task<DocumentReviewLoad> Load(
        IServiceProvider services,
        NavigationManager navigation,
        string revisionId,
        CancellationToken cancellationToken)
    {
        IDocuments? documents = services.GetService<IDocuments>();
        if (documents is null)
        {
            return MissingLoad();
        }

        DocumentRevisionDetail? revision = await documents.RevisionAsync(new RevisionId(revisionId), cancellationToken);
        if (revision is null)
        {
            return MissingLoad();
        }

        if (revision.ReadOnly)
        {
            navigation.NavigateTo("/documents/" + revision.DocumentId.Value);
            return new DocumentReviewLoad(false, true, null, "", null, null, []);
        }

        DocumentDetail? document = await documents.DocumentAsync(revision.DocumentId, cancellationToken);
        string? projectId = null;
        string? projectName = null;
        if (document is not null)
        {
            ProjectSummary? project = await services.GetRequiredService<IPlans>()
                .ProjectAsync(document.ProjectId, cancellationToken);
            projectId = project?.ProjectId.Value;
            projectName = project?.DisplayName;
        }

        return new DocumentReviewLoad(
            false,
            false,
            new RevisionDetail(
                revision.RevisionId,
                new PlanId(revision.DocumentId.Value),
                revision.Number,
                revision.Markdown,
                revision.Summary,
                null,
                null,
                revision.Blocks,
                revision.ParentRevisionId,
                revision.Attribution),
            document?.Title ?? "",
            projectId,
            projectName,
            revision.Blocks.Where(block => block.Locked).Select(block => block.Key).ToArray());
    }

    private static DocumentReviewLoad MissingLoad() => new(true, false, null, "", null, null, []);
}