using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web.Components.Review;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Web.Components.Pages;

public partial class ReviewPage
{
    private Task OnAnnotate(string key) => OnKey(new ReviewKey(key, ""));

    private async Task OnSelect()
    {
        if (!pending || notePrompt is not null || diff is not null)
        {
            return;
        }

        TextSelection? selection = await Selection.ReadAsync();
        if (selection is not null && Locked(selection.BlockOrdinal))
        {
            RefuseLocked();
            return;
        }

        toolbar = Usable(selection) && selection!.Box is not null ? selection : null;
        Publish();
    }

    private async Task ToggleLock(string blockKey)
    {
        if (documentId is null || Services.GetService<IDocuments>() is not IDocuments documents)
        {
            return;
        }

        bool locked = lockedBlocks.Contains(blockKey);
        DocumentChange outcome = await documents.SetBlockLockAsync(
            new DocumentId(documentId), blockKey, !locked, CancellationToken.None);
        if (outcome is DocumentChange.Refused refused)
        {
            notice = refused.Error;
            Publish();
            return;
        }

        DocumentRevisionDetail? refreshed = revision is null
            ? null
            : await documents.RevisionAsync(
                new Annotate.Plans.Application.RevisionId(revision.RevisionId.Value),
                CancellationToken.None);
        if (refreshed is null)
        {
            notice = "Revision was not found.";
            Publish();
            return;
        }

        lockedBlocks.Clear();
        foreach (RevisionBlock block in refreshed.Blocks.Where(block => block.Locked))
        {
            lockedBlocks.Add(block.Key);
        }
        notice = null;
        Publish();
    }

    private void RefuseLocked()
    {
        notice = null;
        toolbar = null;
        Services.GetService<IToast>()?.Show("This block is locked.");
        Publish();
    }

    private async Task<bool> DocumentReadOnly()
    {
        if (documentId is null || Services.GetService<IDocuments>() is not IDocuments documents)
        {
            return false;
        }

        DocumentDetail? document = await documents.DocumentAsync(
            new DocumentId(documentId),
            CancellationToken.None);
        return document?.ReadOnly == true;
    }

    private bool Locked(int ordinal) =>
        revision is not null
        && (uint)ordinal < (uint)revision.Blocks.Count
        && lockedBlocks.Contains(revision.Blocks[ordinal].Key);

    private static bool Usable(TextSelection? selection) =>
        selection is not null && selection.Text.Length > 0 && selection.End > selection.Start;

    private static string NoteTitle(AnnotationKind kind) => kind switch
    {
        AnnotationKind.Replacement => "Replace",
        AnnotationKind.Insertion => "Insert",
        AnnotationKind.Comment => "Comment",
        _ => "Note",
    };

    private sealed record NotePrompt(AnnotationKind Kind, TextSelection Selection)
    {
        public string Id { get; } = Guid.NewGuid().ToString();

        public string CreatedAt { get; } = DateTimeOffset.UtcNow.ToString("u");
    }

    private void OpenPrompt(string id)
    {
        if (prompts.Open(id, pending))
        {
            Publish();
        }
    }

    private void CloseDialog()
    {
        prompts.Close();
        Publish();
    }

    private void PreviousPrompt()
    {
        if (prompts.Previous())
        {
            Publish();
        }
    }

    private async Task NextPrompt(DecisionDraft draft)
    {
        if (await prompts.Next(draft, Reviews, Id))
        {
            Publish();
        }
    }

    private async Task FinishPrompt(DecisionDraft draft)
    {
        if (await prompts.Finish(draft, Reviews, Id))
        {
            Publish();
        }
    }
}