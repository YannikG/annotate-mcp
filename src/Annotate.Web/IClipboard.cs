namespace Annotate.Web;

public interface IClipboard
{
    Task<bool> CopyAsync(string text);
}