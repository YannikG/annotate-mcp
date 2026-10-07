namespace Annotate.Web;

public interface IToast
{
    string? Message { get; }

    event Action? Changed;

    void Show(string message);
}