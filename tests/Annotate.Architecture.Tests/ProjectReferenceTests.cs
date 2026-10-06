namespace Annotate.Architecture.Tests;

public sealed class ProjectReferenceTests
{
    [Fact]
    public void PlansReferencingReviewsFails()
    {
        const string csproj = """
            <Project>
              <ItemGroup>
                <ProjectReference Include="..\Annotate.Reviews\Annotate.Reviews.csproj" />
              </ItemGroup>
            </Project>
            """;

        Dictionary<string, IReadOnlyList<string>> actual = new(StringComparer.Ordinal);
        foreach ((string name, string[] references) in ProjectReferences.Expected)
        {
            actual[name] = references;
        }

        actual["Annotate.Plans"] = ProjectReferences.ReadReferences(csproj);

        IReadOnlyList<string> violations = ProjectReferences.Violations(actual);
        Assert.Contains(
            violations,
            violation => violation.Contains("Annotate.Plans", StringComparison.Ordinal)
                && violation.Contains("Annotate.Reviews", StringComparison.Ordinal));
    }

    [Fact]
    public void RepositoryMatchesExpectedGraph()
    {
        IReadOnlyList<string> violations = ProjectReferences.Violations(
            ProjectReferences.ReadRepository(RepoRoot.Find()));
        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }
}