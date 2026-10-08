using Annotate.Reviews.Application;

namespace Annotate.Web.Components.Pages;

public partial class ReviewPage
{
    private Task draftSave = Task.CompletedTask;
    private int draftVersion;
    private string? draftSaveStatus;
    private readonly List<Annotation> threads = [];

    private async Task RefreshAnnotations()
    {
        if (model is null)
        {
            loadedId = null;
            await OnParametersSetAsync();
            return;
        }

        ReviewDetail? review;
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

        drafts.Clear();
        threads.Clear();
        foreach (Annotation annotation in review.Annotations)
        {
            (annotation.BlockKey is null ? drafts : threads).Add(annotation);
        }

        status = review.Status;
        pending = review.Status == ReviewStatus.Pending;
        prompts.Load(review.Prompts, review.Answers);
        Publish();
    }

    private Task<bool> RetryDraft() => PersistDrafts();

    private Task<bool> PersistDrafts()
    {
        draftSaveStatus = "Saving draft…";
        Publish();
        Task<bool> save = StoreDrafts(draftSave, Id, drafts.ToArray(), ++draftVersion);
        draftSave = save;
        return save;
    }

    private async Task<bool> StoreDrafts(Task previous, string reviewId, Annotation[] annotations, int version)
    {
        await previous;
        string? error;
        try
        {
            SaveAnnotationsOutcome result = await Reviews.SaveAnnotationsAsync(new ReviewId(reviewId), annotations, CancellationToken.None);
            error = result is SaveAnnotationsOutcome.Refused refused ? refused.Error : null;
        }
        catch (Exception)
        {
            error = "Please try again.";
        }

        if (reviewId == Id && version == draftVersion)
        {
            draftSaveStatus = error is null ? "Draft saved" : "Could not save draft. " + error;
            Publish();
        }
        return error is null;
    }

    private async Task EditNote(string text)
    {
        if (!pending || notePrompt is null) return;
        NotePrompt prompt = notePrompt;
        int index = drafts.FindIndex(annotation => annotation.Id == prompt.Id);
        if (index >= 0) drafts.RemoveAt(index);
        if (text.Trim().Length > 0)
        {
            bool comment = prompt.Kind == AnnotationKind.Comment;
            TextSelection selection = prompt.Selection;
            Annotation annotation = new(prompt.Id, prompt.Kind, selection.Text,
                comment ? text.Trim() : null, comment ? null : text.Trim(),
                selection.BlockOrdinal, selection.Start, selection.End, prompt.CreatedAt);
            if (index >= 0) drafts.Insert(index, annotation);
            else drafts.Add(annotation);
        }
        await PersistDrafts();
    }

    private async Task SaveNote(string text)
    {
        if (notePrompt is null || text.Trim().Length == 0) return;
        NotePrompt prompt = notePrompt;
        await EditNote(text);
        if (!ReferenceEquals(notePrompt, prompt) || !pending) return;
        if (draftSaveStatus?.StartsWith("Could not save", StringComparison.Ordinal) == true) return;
        notePrompt = null;
        countdown.Start();
        Publish();
    }

    private async Task CancelNote()
    {
        if (notePrompt is not null)
        {
            drafts.RemoveAll(annotation => annotation.Id == notePrompt.Id);
            notePrompt = null;
            await PersistDrafts();
        }
        Publish();
    }
}