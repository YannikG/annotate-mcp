using Microsoft.JSInterop;

namespace Annotate.Web;

internal sealed class JsSelectionReader(IJSRuntime runtime) : ISelectionReader
{
    public async Task<TextSelection?> ReadAsync() =>
        await runtime.InvokeAsync<TextSelection?>("annotateSelection.read");
}