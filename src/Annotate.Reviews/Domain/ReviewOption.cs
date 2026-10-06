namespace Annotate.Reviews.Domain;

internal sealed class ReviewOption
{
    public string ReviewId { get; private set; }

    public string FenceId { get; private set; }

    public int Ordinal { get; private set; }

    public string Label { get; private set; }

    public ReviewOption(string reviewId, string fenceId, int ordinal, string label)
    {
        ReviewId = reviewId;
        FenceId = fenceId;
        Ordinal = ordinal;
        Label = label;
    }

    private ReviewOption()
    {
        ReviewId = "";
        FenceId = "";
        Label = "";
    }
}