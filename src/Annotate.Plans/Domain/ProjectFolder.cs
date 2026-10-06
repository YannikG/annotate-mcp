namespace Annotate.Plans.Domain;

internal static class ProjectFolder
{
    public static string? Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string unified = path.Trim().Replace('\\', '/');
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(unified));
    }

    public static string DisplayName(string? normalized)
    {
        if (normalized is null)
        {
            return "No folder";
        }

        string segment = Path.GetFileName(normalized);
        return segment.Length == 0 ? normalized : segment;
    }
}