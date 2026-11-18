using System.Text;

namespace Annotate.Web;

internal static class PlanReportScripts
{
    private static readonly Lock Gate = new();
    private static readonly Dictionary<string, string> Cache = new(StringComparer.Ordinal);

    public static void Append(StringBuilder html)
    {
        AppendScript(html, Read("diagram-viewer.js"));
    }

    private static void AppendScript(StringBuilder html, string source)
    {
        html.Append("<script>");
        // A literal end tag inside the bundle would close the element early.
        html.Append(source.Replace("</script", "<\\/script", StringComparison.OrdinalIgnoreCase));
        html.Append("</script>");
    }

    private static string Read(string name)
    {
        lock (Gate)
        {
            if (Cache.TryGetValue(name, out string? cached))
            {
                return cached;
            }

            string path = Locate(name);
            string text = File.ReadAllText(path);
            Cache[name] = text;
            return text;
        }
    }

    private static string Locate(string name)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string direct = Path.Combine(directory.FullName, "wwwroot", "js", name);
            if (File.Exists(direct))
            {
                return direct;
            }

            string repo = Path.Combine(directory.FullName, "src", "Annotate.Web", "wwwroot", "js", name);
            if (File.Exists(repo))
            {
                return repo;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not locate {name}.");
    }
}