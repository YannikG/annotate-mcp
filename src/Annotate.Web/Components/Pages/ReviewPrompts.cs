using Annotate.Reviews.Application;
using Annotate.Web.Components.Review;

namespace Annotate.Web.Components.Pages;

internal sealed class ReviewPrompts
{
    private readonly List<ReviewPrompt> prompts = [];

    private readonly Dictionary<string, DecisionAnswer> saved = new(StringComparer.Ordinal);

    private readonly Dictionary<string, DecisionAnswer> shown = new(StringComparer.Ordinal);

    private int? openIndex;

    private string? answerError;

    public bool Any => prompts.Count > 0;

    public void Load(IReadOnlyList<ReviewPrompt> loaded, IReadOnlyList<DecisionAnswer> answers)
    {
        prompts.Clear();
        saved.Clear();
        shown.Clear();
        openIndex = null;
        answerError = null;
        prompts.AddRange(loaded);
        foreach (DecisionAnswer answer in answers)
        {
            saved[answer.FenceId] = answer;
            shown[answer.FenceId] = answer;
        }
    }

    public bool Open(string id, bool pending)
    {
        if (!pending)
        {
            return false;
        }

        int index = prompts.FindIndex(prompt => prompt.Id == id);
        if (index < 0)
        {
            return false;
        }

        openIndex = index;
        answerError = null;
        return true;
    }

    public void Close()
    {
        openIndex = null;
        answerError = null;
    }

    public bool Previous()
    {
        if (openIndex is not int index || index == 0)
        {
            return false;
        }

        openIndex = index - 1;
        answerError = null;
        return true;
    }

    public async Task<bool> Next(DecisionDraft draft, IReviews reviews, string reviewId)
    {
        if (openIndex is not int index || index >= prompts.Count - 1)
        {
            return false;
        }

        if (!await Store(index, draft, reviews, reviewId))
        {
            return true;
        }

        openIndex = index + 1;
        answerError = null;
        return true;
    }

    public async Task<bool> Finish(DecisionDraft draft, IReviews reviews, string reviewId)
    {
        if (openIndex is not int index)
        {
            return false;
        }

        if (!await Store(index, draft, reviews, reviewId))
        {
            return true;
        }

        shown.Clear();
        foreach ((string id, DecisionAnswer answer) in saved)
        {
            shown[id] = answer;
        }

        openIndex = null;
        answerError = null;
        return true;
    }

    public PromptRow[] Rows() =>
        prompts
            .Select(prompt => new PromptRow(
                prompt.Id,
                prompt.Prompt,
                shown.TryGetValue(prompt.Id, out DecisionAnswer? answer) ? answer.Answer : "(unanswered)"))
            .ToArray();

    public DecisionDialogModel? Dialog()
    {
        if (openIndex is not int index)
        {
            return null;
        }

        ReviewPrompt prompt = prompts[index];
        saved.TryGetValue(prompt.Id, out DecisionAnswer? answer);
        bool other = answer is { IsOther: true } && prompt.Kind == PromptKind.Choice;
        string choice = answer is { IsOther: false } && prompt.Kind == PromptKind.Choice ? answer.Answer : "";
        string otherText = other ? answer!.Answer : "";
        string text = prompt.Kind == PromptKind.Text ? answer?.Answer ?? "" : "";
        return new DecisionDialogModel(
            index + 1,
            prompts.Count,
            prompt.Id,
            prompt.Kind,
            prompt.Prompt,
            prompt.Options,
            choice,
            other,
            otherText,
            text,
            answerError);
    }

    private async Task<bool> Store(int index, DecisionDraft draft, IReviews reviews, string reviewId)
    {
        ReviewPrompt prompt = prompts[index];
        DecisionAnswer answer = ToAnswer(prompt, draft);
        SaveAnswerOutcome outcome = await reviews.SaveAnswerAsync(new ReviewId(reviewId), answer, CancellationToken.None);
        if (outcome is SaveAnswerOutcome.Refused refused)
        {
            answerError = refused.Error;
            return false;
        }

        saved[prompt.Id] = answer;
        answerError = null;
        return true;
    }

    private static DecisionAnswer ToAnswer(ReviewPrompt prompt, DecisionDraft draft)
    {
        if (prompt.Kind == PromptKind.Text)
        {
            return new DecisionAnswer(prompt.Id, draft.Text, false);
        }

        if (draft.Other)
        {
            return new DecisionAnswer(prompt.Id, draft.OtherText, true);
        }

        return new DecisionAnswer(prompt.Id, draft.Choice, false);
    }
}