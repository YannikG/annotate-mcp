using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;

namespace Annotate.Web.Components.Review;

public sealed record ReviewViewModel(
    string Status,
    bool Pending,
    bool CanApprove,
    string? Summary,
    string? StoryUrl,
    string? AcceptanceCriteria,
    string Markdown,
    IReadOnlyList<Annotation> Annotations,
    int Countdown,
    bool CountdownRunning,
    IReadOnlyList<PromptRow> Prompts,
    DecisionDialogModel? Dialog,
    string? Notice,
    DiffPanel? Diff,
    bool AutoCloseOnSubmit,
    string? ReportFailure,
    string Title,
    NotePromptModel? NotePrompt = null,
    SelectionBox? Toolbar = null,
    string? DraftSaveStatus = null,
    Annotation? ActiveNote = null,
    Attribution? Attribution = null,
    string? ProjectId = null,
    string? ProjectName = null,
    string? PlanId = null,
    string? ReviewId = null,
    IReadOnlyList<string>? BlockKeys = null,
    IReadOnlyDictionary<string, string>? BlockNames = null)
{
    public IReadOnlyList<Annotation> Marks => ActiveNote is null ? Annotations : [.. Annotations, ActiveNote];
}

public sealed record NotePromptModel(string Title, string Quote, SelectionBox? Box = null);

public sealed record DiffPanel(string Heading, IReadOnlyList<DiffRow> Rows);

public sealed record DiffRow(string Kind, string Text);

public sealed record ReviewKey(string Key, string Note);

public sealed record PromptRow(string Id, string Prompt, string Answer);

public sealed record ReviewDecisionContext(
    IReadOnlyList<PromptRow> Prompts, bool Pending, Microsoft.AspNetCore.Components.EventCallback<string> Open);

public sealed record DecisionDialogModel(
    int Number,
    int Count,
    string Id,
    PromptKind Kind,
    string Prompt,
    IReadOnlyList<string> Options,
    string Choice,
    bool Other,
    string OtherText,
    string Text,
    string? Error);

public sealed record DecisionDraft(string Choice, bool Other, string OtherText, string Text);