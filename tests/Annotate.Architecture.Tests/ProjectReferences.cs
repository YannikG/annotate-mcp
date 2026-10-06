using System.Xml.Linq;

namespace Annotate.Architecture.Tests;

internal static class ProjectReferences
{
    public static IReadOnlyDictionary<string, string[]> Expected { get; } =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Annotate.Markdown"] = [],
            ["Annotate.Plans"] = ["Annotate.Markdown"],
            ["Annotate.Reviews"] = ["Annotate.Markdown"],
            ["Annotate.Web"] = ["Annotate.Markdown", "Annotate.Plans", "Annotate.Reviews"],
            ["Annotate.Markdown.Tests"] = ["Annotate.Markdown"],
            ["Annotate.Plans.Tests"] = ["Annotate.Plans"],
            ["Annotate.Reviews.Tests"] = ["Annotate.Reviews"],
            ["Annotate.Web.Tests"] = ["Annotate.Web"],
            ["Annotate.Architecture.Tests"] =
            [
                "Annotate.Markdown",
                "Annotate.Plans",
                "Annotate.Reviews",
                "Annotate.Web",
            ],
        };

    public static IReadOnlyList<string> ReadReferences(string csprojXml)
    {
        string xml = csprojXml.TrimStart('\uFEFF');
        XDocument document = XDocument.Parse(xml);
        return document
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => ProjectName(include!))
            .ToList();
    }

    public static Dictionary<string, IReadOnlyList<string>> ReadRepository(string repoRoot)
    {
        Dictionary<string, IReadOnlyList<string>> actual = new(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(repoRoot, "*.csproj", SearchOption.AllDirectories))
        {
            if (Skip(file))
            {
                continue;
            }

            string name = Path.GetFileNameWithoutExtension(file);
            actual[name] = ReadReferences(File.ReadAllText(file));
        }

        return actual;
    }

    public static IReadOnlyList<string> Violations(IReadOnlyDictionary<string, IReadOnlyList<string>> actual)
    {
        List<string> violations = [];
        foreach (string project in Expected.Keys.Order(StringComparer.Ordinal))
        {
            if (!actual.TryGetValue(project, out IReadOnlyList<string>? references))
            {
                violations.Add($"Missing project {project}.");
                continue;
            }

            string[] expected = Expected[project];
            if (!Same(references, expected))
            {
                violations.Add(
                    $"{project} references [{string.Join(", ", references)}] but expected [{string.Join(", ", expected)}].");
            }
        }

        foreach (string project in actual.Keys.Except(Expected.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            violations.Add($"Unexpected project {project}.");
        }

        return violations;
    }

    private static bool Same(IReadOnlyList<string> actual, string[] expected)
    {
        HashSet<string> left = new(actual, StringComparer.Ordinal);
        HashSet<string> right = new(expected, StringComparer.Ordinal);
        return actual.Count == expected.Length && left.SetEquals(right);
    }

    private static string ProjectName(string include)
    {
        string normalized = include.Replace('\\', '/');
        return Path.GetFileNameWithoutExtension(normalized);
    }

    private static bool Skip(string path)
    {
        foreach (string segment in path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (segment is "bin" or "obj")
            {
                return true;
            }
        }

        return false;
    }
}