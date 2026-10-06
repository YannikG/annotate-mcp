using Microsoft.JSInterop;

namespace Annotate.Web;

public readonly record struct ReportDownload(bool Saved, string Error);

public interface IReportScript
{
    Task<ReportDownload> DownloadAsync(string html, string fileName);

    Task CloseAsync();
}

internal sealed class JsReportScript(IJSRuntime runtime) : IReportScript
{
    public async Task<ReportDownload> DownloadAsync(string html, string fileName)
    {
        try
        {
            string? error = await runtime.InvokeAsync<string?>("annotateReport.download", html, fileName);
            return error is null
                ? new ReportDownload(true, "")
                : new ReportDownload(false, error);
        }
        catch (Exception exception)
        {
            return new ReportDownload(false, exception.Message);
        }
    }

    public Task CloseAsync() => runtime.InvokeVoidAsync("annotateReport.close").AsTask();
}