using Annotate.Reviews.Application;

namespace Annotate.Web.Components.Pages;

public partial class ReviewPage
{
    private static readonly TimeSpan NotePause = TimeSpan.FromMilliseconds(400);

    private Task draftSave = Task.CompletedTask;
    private Task notePause = Task.CompletedTask;
    private int draftVersion;
    private int noteEdit;
    private bool noteDirty;
    private bool disposing;
    private string? draftSaveStatus;
    private readonly List<Annotation> threads = [];

    public async ValueTask DisposeAsync()
    {
        disposing = true;
        noteEdit++;
        if (noteDirty)
        {
            await PersistDrafts(loadedId ?? Id);
        }

        GC.SuppressFinalize(this);
    }

    private async Task RefreshAnnotations()
    {
        if (model is null)
        {
            await FlushDrafts(loadedId);
            loadedId = null;
            await OnParametersSetAsync();
            return;
        }

        ReviewDetail? review;
        while (true)
        {
            int version = draftVersion;
            await draftSave;
            if (version != draftVersion)
            {
                continue;
            }

            try
            {
                review = await Reviews.FindAsync(new ReviewId(Id), CancellationToken.None);
            }
            catch (Exception)
            {
                return;
            }

            if (review is null)
            {
                return;
            }

            if (version != draftVersion)
            {
                continue;
            }

            break;
        }

        drafts.Clear();
        threads.Clear();
        foreach (Annotation annotation in review.Annotations)
        {
            (annotation.BlockKey is null ? drafts : threads).Add(annotation);
        }

        status = review.Status;
        pending = review.Status == ReviewStatus.Pending;
        Publish();
    }

    private async Task RemovePhrase(string annotationId)
    {
        if (drafts.RemoveAll(item => item.Id == annotationId) == 0)
        {
            return;
        }

        await PersistDrafts(Id);
    }

    private Task<bool> RetryDraft() => PersistDrafts(Id);

    private async Task FlushDrafts(string? reviewId)
    {
        noteEdit++;
        if (reviewId is null || !noteDirty)
        {
            return;
        }

        await PersistDrafts(reviewId);
    }

    private Task<bool> PersistDrafts() => PersistDrafts(Id);

    private Task<bool> PersistDrafts(string reviewId)
    {
        int edit = noteEdit;
        Annotation[] annotations = drafts.ToArray();
        bool show = !disposing && reviewId == Id;
        if (show)
        {
            draftSaveStatus = "Saving draft…";
            Publish();
        }

        Task<bool> save = StoreDrafts(draftSave, reviewId, annotations, ++draftVersion, show, edit);
        draftSave = save;
        return save;
    }

    private async Task<bool> StoreDrafts(
        Task previous,
        string reviewId,
        Annotation[] annotations,
        int version,
        bool show,
        int edit)
    {
        await previous;
        string? error;
        if (await DocumentReadOnly())
        {
            error = "Document is read-only.";
        }
        else
        {
            try
            {
                SaveAnnotationsOutcome result = await Reviews.SaveAnnotationsAsync(
                    new ReviewId(reviewId), annotations, CancellationToken.None);
                error = result is SaveAnnotationsOutcome.Refused refused ? refused.Error : null;
            }
            catch (Exception)
            {
                error = "Please try again.";
            }
        }

        if (error is null && edit == noteEdit)
        {
            noteDirty = false;
        }

        if (show && reviewId == Id && version == draftVersion)
        {
            draftSaveStatus = error is null ? "Draft saved" : "Could not save draft. " + error;
            Publish();
        }

        return error is null;
    }

    private Task EditNote(string text)
    {
        if (!RememberNote(text))
        {
            return Task.CompletedTask;
        }

        int edit = ++noteEdit;
        notePause = PauseThenSave(edit);
        _ = notePause.IsCompleted;
        return Task.CompletedTask;
    }

    private async Task PauseThenSave(int edit)
    {
        try
        {
            await Task.Delay(NotePause);
            if (edit != noteEdit || disposing)
            {
                return;
            }

            await InvokeAsync(async () =>
            {
                if (edit != noteEdit || disposing || !noteDirty)
                {
                    return;
                }

                await PersistDrafts(Id);
            });
        }
        catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
        {
        }
    }

    private bool RememberNote(string text)
    {
        if (!pending || notePrompt is null)
        {
            return false;
        }

        NotePrompt prompt = notePrompt;
        int index = drafts.FindIndex(annotation => annotation.Id == prompt.Id);
        Annotation? current = index >= 0 ? drafts[index] : null;
        Annotation? next = null;
        if (text.Trim().Length > 0)
        {
            bool comment = prompt.Kind == AnnotationKind.Comment;
            TextSelection selection = prompt.Selection;
            next = new Annotation(
                prompt.Id,
                prompt.Kind,
                selection.Text,
                comment ? text.Trim() : null,
                comment ? null : text.Trim(),
                selection.BlockOrdinal,
                selection.Start,
                selection.End,
                prompt.CreatedAt);
        }

        if (current == next)
        {
            return false;
        }

        if (index >= 0)
        {
            drafts.RemoveAt(index);
        }

        if (next is not null)
        {
            if (index >= 0)
            {
                drafts.Insert(index, next);
            }
            else
            {
                drafts.Add(next);
            }
        }

        noteDirty = true;
        return true;
    }

    private async Task SaveNote(string text)
    {
        if (notePrompt is null || text.Trim().Length == 0)
        {
            return;
        }

        NotePrompt prompt = notePrompt;
        noteEdit++;
        RememberNote(text);
        await PersistDrafts(Id);
        if (!ReferenceEquals(notePrompt, prompt) || !pending)
        {
            return;
        }

        if (draftSaveStatus?.StartsWith("Could not save", StringComparison.Ordinal) == true)
        {
            return;
        }

        notePrompt = null;
        countdown.Start();
        Publish();
    }

    private async Task CancelNote()
    {
        if (notePrompt is not null)
        {
            noteEdit++;
            if (drafts.RemoveAll(annotation => annotation.Id == notePrompt.Id) > 0)
            {
                noteDirty = true;
            }

            notePrompt = null;
            await PersistDrafts(Id);
        }

        Publish();
    }
}