using Annotate.Web;

namespace Annotate.Web.Tests;

internal sealed class RecordingReport : IReportScript
{
    public List<DownloadedReport> Downloads { get; } = [];

    public string? Failure { get; set; }

    public TaskCompletionSource<ReportDownload>? Gate { get; set; }

    public int Closes { get; private set; }

    public bool DecisionRecordedBeforeClose { get; private set; }

    public Func<bool>? DecisionRecorded { get; set; }

    public Task<ReportDownload> DownloadAsync(string html, string fileName)
    {
        Downloads.Add(new DownloadedReport(html, fileName));
        if (Gate is not null)
        {
            return Gate.Task;
        }

        return Task.FromResult(
            Failure is null ? new ReportDownload(true, "") : new ReportDownload(false, Failure));
    }

    public Task CloseAsync()
    {
        DecisionRecordedBeforeClose = DecisionRecorded?.Invoke() ?? false;
        Closes++;
        return Task.CompletedTask;
    }
}

internal sealed record DownloadedReport(string Html, string FileName);