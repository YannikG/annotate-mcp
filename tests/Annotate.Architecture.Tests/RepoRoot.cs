namespace Annotate.Architecture.Tests;

internal static class RepoRoot
{
    public static string Find()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string marker = Path.Combine(directory.FullName, "Annotate.slnx");
            if (File.Exists(marker))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate Annotate.slnx by walking up from the test output.");
    }
}