namespace Annotate.Plans.Domain;

internal static class StoryLink
{
    public const int MaxLength = 2000;

    public static string? Reject(string? storyUrl, IReadOnlyList<string> trustedDomains)
    {
        if (string.IsNullOrWhiteSpace(storyUrl))
        {
            return null;
        }

        if (storyUrl.Length > MaxLength)
        {
            return "Story URL exceeds 2000 characters.";
        }

        string trimmed = storyUrl.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri)
            || !IsHttp(uri)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            return "Story URL must be an absolute http or https URL without credentials.";
        }

        if (!HostAllowed(uri.Host, trustedDomains))
        {
            return "Story URL host is not allowed.";
        }

        return null;
    }

    private static bool IsHttp(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

    private static bool HostAllowed(string host, IReadOnlyList<string> trustedDomains)
    {
        foreach (string domain in trustedDomains)
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                continue;
            }

            string allowed = domain.Trim().Trim('.');
            if (host.Equals(allowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}