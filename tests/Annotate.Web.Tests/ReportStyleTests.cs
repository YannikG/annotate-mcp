using System.Text.RegularExpressions;

using Annotate.Web;

namespace Annotate.Web.Tests;

public sealed class ReportStyleTests
{
    private static readonly string[] SharedTokens =
    [
        "--paper", "--ink", "--line", "--muted", "--surface", "--surface-sunken",
        "--panel-inset", "--accent", "--font-sans", "--font-mono",
        "--radius-xs", "--radius-sm", "--radius-md", "--radius-lg",
    ];

    [Fact]
    public void ReportMirrorsTheLightThemeTokens()
    {
        string tokens = ReadTokens();
        string report = ReportCss();

        foreach (string name in SharedTokens)
        {
            Match declaration = Regex.Match(tokens, $@"{Regex.Escape(name)}:\s*([^;]+);");
            Assert.True(declaration.Success, $"{name} is not defined in tokens.css");
            Assert.Contains($"{name}: {declaration.Groups[1].Value.Trim()}", report, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ReportUsesTokensInsteadOfLiteralsOutsideTheRootBlock()
    {
        string report = ReportCss();
        string withoutRoot = Regex.Replace(report, @":root\s*\{[^}]*\}", "");

        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b", withoutRoot);
        Assert.DoesNotMatch(@"\brgba?\s*\(", withoutRoot);
        Assert.DoesNotMatch(@"\bhsla?\s*\(", withoutRoot);
    }

    private static string ReportCss()
    {
        string html = PlanReport.Html("Storage", null, null, "Body");
        int start = html.IndexOf("<style>", StringComparison.Ordinal) + "<style>".Length;
        int end = html.IndexOf("</style>", StringComparison.Ordinal);
        return html[start..end];
    }

    private static string ReadTokens()
    {
        string css = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Annotate.Web", "wwwroot", "css", "tokens.css"));
        return css[..css.IndexOf("\n.dark", StringComparison.Ordinal)];
    }

    private static string RepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Annotate.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate Annotate.slnx by walking up from the test output.");
    }
}