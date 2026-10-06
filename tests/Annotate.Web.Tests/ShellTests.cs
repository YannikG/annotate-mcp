using Annotate.Web.Components.Layout;

using Bunit;

namespace Annotate.Web.Tests;

public sealed class ShellTests
{
    [Fact]
    public void TokensDefineColoursAndFonts()
    {
        string css = ReadStyle("tokens.css");

        Assert.Contains("--paper: #f6f8fa", css, StringComparison.Ordinal);
        Assert.Contains("--ink: #1f2328", css, StringComparison.Ordinal);
        Assert.Contains("--line: #d1d9e0", css, StringComparison.Ordinal);
        Assert.Contains("--surface-sunken: #eaeef2", css, StringComparison.Ordinal);
        Assert.Contains("--sheet: #ffffff", css, StringComparison.Ordinal);
        Assert.Contains("--surface: #ffffff", css, StringComparison.Ordinal);
        Assert.Contains("--accent: #0969da", css, StringComparison.Ordinal);
        Assert.Contains("--paper: #0d1117", css, StringComparison.Ordinal);
        Assert.Contains("--ink: #f0f6fc", css, StringComparison.Ordinal);
        Assert.Contains("--line: #3d444d", css, StringComparison.Ordinal);
        Assert.Contains("--muted: #9198a1", css, StringComparison.Ordinal);
        Assert.Contains("--surface-sunken: #010409", css, StringComparison.Ordinal);
        Assert.Contains("--accent: #1f6feb", css, StringComparison.Ordinal);
        Assert.Contains("--nav: #24292f", css, StringComparison.Ordinal);
        Assert.Contains("--nav: #010409", css, StringComparison.Ordinal);
        Assert.Contains("--approve: #1a7f37", css, StringComparison.Ordinal);
        Assert.Contains("--on-approve: #ffffff", css, StringComparison.Ordinal);
        Assert.Contains("system-ui, \"Segoe UI\", sans-serif", css, StringComparison.Ordinal);
    }

    [Fact]
    public void DashboardMetricsStayOnOneRowOnLargeScreens()
    {
        string css = ReadStyle("dashboard.css");

        Assert.Contains(".frame > .metric-grid", css, StringComparison.Ordinal);
        Assert.Contains("@media (min-width: 800px)", css, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: repeat(5, minmax(0, 1fr));", css, StringComparison.Ordinal);
        Assert.Contains("grid-template-rows: 6rem auto;", css, StringComparison.Ordinal);
    }

    [Fact]
    public void BaseStylesCoverTheBlazorErrorBoundary()
    {
        string css = ReadStyle("base.css");

        Assert.Contains("blazor-error-boundary", css, StringComparison.Ordinal);
    }

    private static string ReadStyle(string name) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "Annotate.Web", "wwwroot", "css", name));

    [Fact]
    public void AppRazorLoadsNoInlineImportMap()
    {
        string app = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Annotate.Web", "Components", "App.razor"));

        Assert.DoesNotContain("<ImportMap", app, StringComparison.Ordinal);
    }

    [Fact]
    public void ShellShowsHeaderMainAndFooter()
    {
        using BunitContext context = new();
        context.JSInterop.Setup<bool>("annotateTheme.isDark").SetResult(false);
        IRenderedComponent<MainLayout> layout = context.Render<MainLayout>();

        Assert.Contains("Annotate", layout.Find("header").TextContent);
        Assert.NotNull(layout.Find("main"));
        Assert.Equal("Dashboard", layout.Find("nav[aria-label='Primary'] a[href='/']").TextContent);
        Assert.Equal("Projects", layout.Find("nav[aria-label='Primary'] a[href='/projects']").TextContent);
        Assert.Equal("d delete, r replace, s insert, c comment", layout.Find("footer").TextContent.Trim());
        Assert.Contains("light", layout.Find(".shell").ClassName);
        context.JSInterop.SetupVoid("annotateTheme.setDark", _ => true);
        layout.Find("header button").Click();
        Assert.Contains("dark", layout.Find(".shell").ClassName);
    }

    [Fact]
    public void ShellAppliesTheSavedTheme()
    {
        using BunitContext context = new();
        context.JSInterop.Setup<bool>("annotateTheme.isDark").SetResult(true);

        IRenderedComponent<MainLayout> layout = context.Render<MainLayout>();

        Assert.Contains("dark", layout.Find(".shell").ClassName);
    }

    [Fact]
    public void TogglingTheThemePersistsIt()
    {
        using BunitContext context = new();
        context.JSInterop.Setup<bool>("annotateTheme.isDark").SetResult(false);
        context.JSInterop.SetupVoid("annotateTheme.setDark", _ => true);
        IRenderedComponent<MainLayout> layout = context.Render<MainLayout>();

        layout.Find("header button").Click();

        Assert.Contains("dark", layout.Find(".shell").ClassName);
        var invocation = context.JSInterop.VerifyInvoke("annotateTheme.setDark");
        Assert.Equal(true, invocation.Arguments[0]);
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