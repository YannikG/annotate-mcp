namespace Annotate.Reviews.Domain;

internal sealed class StoredAnswer
{
    public string ReviewId { get; private set; }

    public string FenceId { get; private set; }

    public string Answer { get; private set; }

    public bool IsOther { get; private set; }

    public StoredAnswer(string reviewId, string fenceId, string answer, bool isOther)
    {
        ReviewId = reviewId;
        FenceId = fenceId;
        Answer = answer;
        IsOther = isOther;
    }

    public void Replace(string answer, bool isOther)
    {
        Answer = answer;
        IsOther = isOther;
    }

    private StoredAnswer()
    {
        ReviewId = "";
        FenceId = "";
        Answer = "";
    }
}