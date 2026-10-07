using Microsoft.JSInterop;

namespace Annotate.Web;

internal sealed class JsClipboard(IJSRuntime runtime) : IClipboard
{
    public async Task<bool> CopyAsync(string text)
    {
        try
        {
            string? error = await runtime.InvokeAsync<string?>("annotateClipboard.copy", text);
            return error is null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}