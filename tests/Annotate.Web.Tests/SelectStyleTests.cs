using System.Text.RegularExpressions;

using Annotate.Web.Components.Browse;

using Bunit;

namespace Annotate.Web.Tests;

public sealed class SelectStyleTests
{
    [Fact]
    public void RevisionPickerRendersTheCustomDropdown()
    {
        using BunitContext context = new();
        PlanViewModel model = new("Plan", [new RevisionRow("rev-1", 1, "2026-01-01", "Pending")], "rev-1");

        var picker = context.Render<RevisionPicker>(p => p.Add(c => c.Model, model));

        Assert.Equal("Revision", picker.Find(".select[data-revision] .select-label").TextContent);
        Assert.Equal("v1 · Pending", picker.Find("[data-select-trigger]").TextContent.Trim());
        Assert.Empty(picker.FindAll("select"));
    }

    [Fact]
    public void DropdownStylingLivesInTheComponentLibrary()
    {
        string css = ReadCss("ui.css");

        Assert.Contains(".select-trigger::after", css, StringComparison.Ordinal);
        Assert.Matches(@"\.select-list\s*\{[^}]*position:\s*absolute", css);
        Assert.Matches(@"\.select-list\s*\{[^}]*z-index:\s*var\(--z-flyout\)", css);
        Assert.DoesNotContain(".select select", css, StringComparison.Ordinal);
    }

    [Fact]
    public void NoNativeSelectSurvivesAnywhere()
    {
        string baseCss = ReadCss("base.css");
        Assert.DoesNotMatch(new Regex(@"(^|,)\s*select\s*[,:]", RegexOptions.Multiline), baseCss);

        string components = Path.Combine(RepoRoot(), "src", "Annotate.Web", "Components");
        foreach (string file in Directory.EnumerateFiles(components, "*.razor", SearchOption.AllDirectories))
        {
            Assert.DoesNotContain("<select", File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

    private static string ReadCss(string name) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "Annotate.Web", "wwwroot", "css", name));

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