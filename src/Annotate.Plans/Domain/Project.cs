namespace Annotate.Plans.Domain;

internal sealed class Project
{
    public const int MaxDisplayName = 120;

    public string Id { get; private set; }

    public string? FolderPath { get; private set; }

    public string DisplayName { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public Project(string id, string? folderPath, string displayName, DateTimeOffset createdAt)
    {
        Id = id;
        FolderPath = folderPath;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }

    public static string? Rejection(string displayName)
    {
        string trimmed = displayName.Trim();
        if (trimmed.Length == 0)
        {
            return "Display name is empty.";
        }

        if (trimmed.Length > MaxDisplayName)
        {
            return "Display name exceeds 120 characters.";
        }

        return null;
    }

    public void Rename(string displayName) => DisplayName = displayName.Trim();

    public void Archive(DateTimeOffset at) => ArchivedAt = at;

    public void Restore() => ArchivedAt = null;

    private Project()
    {
        Id = "";
        DisplayName = "";
    }
}