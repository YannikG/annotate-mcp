using System.ComponentModel;
using System.Diagnostics;

namespace Annotate.Web;

public enum BrowserPlatform
{
    Windows,
    MacOs,
    Linux,
}

public sealed record BrowserCommand(string FileName, IReadOnlyList<string> Arguments, bool UseShellExecute)
{
    public ProcessStartInfo ToStartInfo()
    {
        ProcessStartInfo start = new()
        {
            FileName = FileName,
            UseShellExecute = UseShellExecute,
        };
        foreach (string argument in Arguments)
        {
            start.ArgumentList.Add(argument);
        }

        return start;
    }
}

public interface IBrowserProcess
{
    bool Start(BrowserCommand command);
}

public interface IReviewBrowser
{
    bool TryOpen(string url);
}

public sealed class ReviewBrowser(BrowserPlatform platform, IBrowserProcess process) : IReviewBrowser
{
    public static BrowserPlatform CurrentPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return BrowserPlatform.Windows;
        }

        if (OperatingSystem.IsMacOS())
        {
            return BrowserPlatform.MacOs;
        }

        return BrowserPlatform.Linux;
    }

    public bool TryOpen(string url)
    {
        BrowserCommand? command = CommandFor(url, platform);
        if (command is null)
        {
            return false;
        }

        try
        {
            return process.Start(command);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (Win32Exception)
        {
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
    }

    private static BrowserCommand? CommandFor(string url, BrowserPlatform platform)
    {
        if (!IsLoopbackHttp(url))
        {
            return null;
        }

        return platform switch
        {
            BrowserPlatform.Windows => new BrowserCommand(url, [], true),
            BrowserPlatform.MacOs => new BrowserCommand("open", [url], false),
            BrowserPlatform.Linux => new BrowserCommand("xdg-open", [url], false),
            _ => null,
        };
    }

    private static bool IsLoopbackHttp(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo) || string.IsNullOrEmpty(uri.Host))
        {
            return false;
        }

        string host = uri.IdnHost;
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.Ordinal)
            || host.Equals("::1", StringComparison.Ordinal);
    }
}

internal sealed class ShellBrowserProcess : IBrowserProcess
{
    public bool Start(BrowserCommand command)
    {
        using Process? started = Process.Start(command.ToStartInfo());
        return started is not null;
    }
}