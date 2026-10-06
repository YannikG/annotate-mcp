using System.Diagnostics;

using Annotate.Web;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Web.Tests;

public sealed class BrowserLaunchTests
{
    [Theory]
    [InlineData("https://127.0.0.1:24173/review/abc")]
    [InlineData("http:///review/abc")]
    [InlineData("http://user@localhost/review/abc")]
    [InlineData("http://example.com/review/abc")]
    [InlineData("/review/abc")]
    public void RefusedUrlStartsNothing(string url)
    {
        RecordingProcess process = new();
        ReviewBrowser browser = new(BrowserPlatform.Linux, process);

        Assert.False(browser.TryOpen(url));
        Assert.Empty(process.Commands);
    }

    [Fact]
    public void WindowsPassesTheUrlAsTheProcessFileName()
    {
        const string Url = "http://127.0.0.1:24173/review/abc";
        BrowserCommand command = Record(BrowserPlatform.Windows, Url);
        ProcessStartInfo start = command.ToStartInfo();

        Assert.Equal(Url, start.FileName);
        Assert.Empty(start.ArgumentList);
        Assert.True(start.UseShellExecute);
    }

    [Fact]
    public void MacOsPassesTheUrlToOpen()
    {
        const string Url = "http://localhost:24173/review/abc";
        BrowserCommand command = Record(BrowserPlatform.MacOs, Url);
        ProcessStartInfo start = command.ToStartInfo();

        Assert.Equal("open", start.FileName);
        Assert.Equal(Url, Assert.Single(start.ArgumentList));
        Assert.False(start.UseShellExecute);
    }

    [Fact]
    public void LinuxPassesTheUrlToXdgOpen()
    {
        const string Url = "http://[::1]:24173/review/abc";
        BrowserCommand command = Record(BrowserPlatform.Linux, Url);
        ProcessStartInfo start = command.ToStartInfo();

        Assert.Equal("xdg-open", start.FileName);
        Assert.Equal(Url, Assert.Single(start.ArgumentList));
        Assert.False(start.UseShellExecute);
    }

    [Fact]
    public async Task OpenBrowserFalseRecordsNothing()
    {
        RecordingBrowser browser = new();
        await using AnnotateApp app = new() { OpenBrowser = false, Browser = browser };
        IPlanHost host = app.Services.GetRequiredService<IPlanHost>();

        string text = await host.SubmitAsync(Plan(), "127.0.0.1:24173", CancellationToken.None);

        Assert.Contains("plan_status=pending", text, StringComparison.Ordinal);
        Assert.Empty(browser.Urls);
    }

    [Fact]
    public async Task ErrorResultOpensNothing()
    {
        RecordingBrowser browser = new();
        await using AnnotateApp app = new() { OpenBrowser = true, Browser = browser };
        IPlanHost host = app.Services.GetRequiredService<IPlanHost>();

        string text = await host.SubmitAsync(
            new PlanSubmission("   ", null, null, null, null, null, null),
            "localhost",
            CancellationToken.None);

        Assert.Equal("Error: Plan is empty.", text);
        Assert.Empty(browser.Urls);
    }

    [Fact]
    public async Task SuccessfulSubmitRecordsTheReviewUrl()
    {
        RecordingBrowser browser = new();
        await using AnnotateApp app = new() { OpenBrowser = true, Browser = browser };
        IPlanHost host = app.Services.GetRequiredService<IPlanHost>();
        const string RequestHost = "127.0.0.1:24173";

        string text = await host.SubmitAsync(Plan(), RequestHost, CancellationToken.None);

        string url = $"http://{RequestHost}/review/{Value(text, "Review ID: ")}";
        Assert.Contains($"Review URL: {url}", text, StringComparison.Ordinal);
        Assert.Equal(url, Assert.Single(browser.Urls));
    }

    [Fact]
    public async Task FailedStartStillReturnsPendingText()
    {
        RecordingBrowser browser = new() { Fail = true };
        await using AnnotateApp app = new() { OpenBrowser = true, Browser = browser };
        IPlanHost host = app.Services.GetRequiredService<IPlanHost>();
        const string RequestHost = "localhost:24173";

        string text = await host.SubmitAsync(Plan(), RequestHost, CancellationToken.None);

        string url = $"http://{RequestHost}/review/{Value(text, "Review ID: ")}";
        Assert.Contains("plan_status=pending", text, StringComparison.Ordinal);
        Assert.Contains($"Review URL: {url}", text, StringComparison.Ordinal);
        Assert.Equal(url, Assert.Single(browser.Urls));
    }

    private static BrowserCommand Record(BrowserPlatform platform, string url)
    {
        RecordingProcess process = new();
        ReviewBrowser browser = new(platform, process);
        Assert.True(browser.TryOpen(url));
        return Assert.Single(process.Commands);
    }

    private static PlanSubmission Plan() =>
        new("# Storage\n", null, null, null, null, null, null, "Cursor", "claude-opus-4");

    private static string Value(string text, string label)
    {
        foreach (string line in text.Split('\n'))
        {
            if (line.StartsWith(label, StringComparison.Ordinal))
            {
                return line[label.Length..];
            }
        }

        throw new InvalidOperationException($"Missing {label}");
    }

    private sealed class RecordingBrowser : IReviewBrowser
    {
        public List<string> Urls { get; } = [];

        public bool Fail { get; init; }

        public bool TryOpen(string url)
        {
            Urls.Add(url);
            if (Fail)
            {
                throw new InvalidOperationException("Browser start failed.");
            }

            return true;
        }
    }

    private sealed class RecordingProcess : IBrowserProcess
    {
        public List<BrowserCommand> Commands { get; } = [];

        public bool Start(BrowserCommand command)
        {
            Commands.Add(command);
            return true;
        }
    }
}