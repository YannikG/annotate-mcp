namespace Annotate.Web;

public sealed class SubmitCountdown
{
    public int Count { get; private set; } = 5;

    public bool Running { get; private set; }

    public bool Complete => Running && Count == 0;

    public void Start()
    {
        Count = 5;
        Running = true;
    }

    public void Tick()
    {
        if (Count > 0)
        {
            Count--;
        }
    }

    public void Cancel()
    {
        Running = false;
        Count = 5;
    }
}