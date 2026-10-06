namespace Annotate.Web;

internal static class LoopbackHost
{
    public static bool Accepts(string? requestHost)
    {
        if (string.IsNullOrWhiteSpace(requestHost))
        {
            return false;
        }

        string host = Name(requestHost.Trim());
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || host.Equals("::1", StringComparison.OrdinalIgnoreCase);
    }

    private static string Name(string requestHost)
    {
        if (requestHost.StartsWith('['))
        {
            int end = requestHost.IndexOf(']', StringComparison.Ordinal);
            return end > 1 ? requestHost[1..end] : requestHost;
        }

        int colon = requestHost.LastIndexOf(':');
        if (colon > 0 && requestHost.IndexOf(':', StringComparison.Ordinal) == colon)
        {
            return requestHost[..colon];
        }

        return requestHost;
    }
}