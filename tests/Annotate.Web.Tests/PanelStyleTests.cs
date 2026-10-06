using System.Text.RegularExpressions;

namespace Annotate.Web.Tests;

public sealed class PanelStyleTests
{
    [Fact]
    public void LeftFlyoutMirrorsTheRightFlyoutShadow()
    {
        string panels = ReadCss("panels.css");
        string tokens = ReadCss("tokens.css");

        Match left = Regex.Match(panels, @"\.side-panel-left\s*\{([^}]*)\}");
        Assert.True(left.Success, ".side-panel-left is not defined in panels.css");
        Assert.Contains("box-shadow: var(--shadow-flyout-left)", left.Groups[1].Value, StringComparison.Ordinal);

        Assert.Matches(@":root\s*\{[^}]*--shadow-flyout-left:\s*12px", tokens);
        Assert.Matches(@"\.dark\s*\{[^}]*--shadow-flyout-left:\s*12px", tokens);
    }

    [Fact]
    public void RightPanelStackDoesNotClipCardShadows()
    {
        string panels = ReadCss("panels.css");

        Match stack = Regex.Match(panels, @"\.panel-tools-right\s*\{([^}]*)\}");
        Assert.True(stack.Success, ".panel-tools-right is not defined in panels.css");
        Assert.DoesNotContain("overflow", stack.Groups[1].Value, StringComparison.Ordinal);

        Assert.Matches(@"\.panel-tools-right\s+\.side-panel\s*\{[^}]*min-height:\s*0", panels);
    }

    [Fact]
    public void IndividualAnnotationsHaveNoShadow()
    {
        string review = ReadCss("review.css");

        Match card = Regex.Match(review, @"\.side-panel\s+\[data-annotations\]\s*>\s*li\s*\{([^}]*)\}");
        Assert.True(card.Success, ".side-panel [data-annotations] > li is not defined in review.css");
        Assert.DoesNotContain("box-shadow", card.Groups[1].Value, StringComparison.Ordinal);
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