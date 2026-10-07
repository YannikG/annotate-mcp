namespace Annotate.Web;

public sealed class ToastHub : IToast
{
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromSeconds(4);

    private readonly TimeSpan lifetime;

    private int generation;

    public ToastHub()
        : this(DefaultLifetime)
    {
    }

    public ToastHub(TimeSpan lifetime) => this.lifetime = lifetime;

    public string? Message { get; private set; }

    public event Action? Changed;

    public void Show(string message)
    {
        int current = ++generation;
        Message = message;
        Changed?.Invoke();
        _ = Expire(current);
    }

    private async Task Expire(int current)
    {
        await Task.Delay(lifetime);
        if (current != generation)
        {
            return;
        }

        Message = null;
        Changed?.Invoke();
    }
}