using Annotate.Reviews.Application;

namespace Annotate.Web.Components.Ui;

/// <summary>
/// The one mapping between a review status, its display label, and its badge tone.
/// Badge styles key off these tones via data-state.
/// </summary>
public static class StatusTone
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Changes = "changes";
    public const string None = "none";

    public static string Label(ReviewStatus? status) => status switch
    {
        ReviewStatus.Pending => "Pending",
        ReviewStatus.Approved => "Approved",
        ReviewStatus.ChangesRequested => "Changes requested",
        null => "No review",
        _ => throw new InvalidOperationException("Review status was not recognised."),
    };

    public static string Of(ReviewStatus? status) => status switch
    {
        ReviewStatus.Pending => Pending,
        ReviewStatus.Approved => Approved,
        ReviewStatus.ChangesRequested => Changes,
        null => None,
        _ => throw new InvalidOperationException("Review status was not recognised."),
    };

    public static string OfLabel(string label) => label switch
    {
        "Pending" => Pending,
        "Approved" => Approved,
        "Changes requested" => Changes,
        _ => None,
    };
}