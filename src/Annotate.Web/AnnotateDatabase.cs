namespace Annotate.Web;

internal static class AnnotateDatabase
{
    public static string Path(IConfiguration configuration)
    {
        string? configured = configuration["Annotate:DataDir"];
        string directory = string.IsNullOrWhiteSpace(configured) ? Fallback() : configured;
        Directory.CreateDirectory(directory);
        return System.IO.Path.Combine(directory, "annotate.db");
    }

    private static string Fallback()
    {
        string? xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrWhiteSpace(xdg))
        {
            return System.IO.Path.Combine(xdg, "annotate");
        }

        return System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config",
            "annotate");
    }
}