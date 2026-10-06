namespace Annotate.Reviews.Domain;

internal sealed class ReviewFence
{
    public string ReviewId { get; private set; }

    public int Ordinal { get; private set; }

    public string FenceId { get; private set; }

    public string Kind { get; private set; }

    public string Prompt { get; private set; }

    public ReviewFence(string reviewId, int ordinal, string fenceId, string kind, string prompt)
    {
        ReviewId = reviewId;
        Ordinal = ordinal;
        FenceId = fenceId;
        Kind = kind;
        Prompt = prompt;
    }

    private ReviewFence()
    {
        ReviewId = "";
        FenceId = "";
        Kind = "";
        Prompt = "";
    }
}