using Annotate.Plans.Application;
using Annotate.Web;

namespace Annotate.Web.Tests;

internal sealed class FakeProjectPlanPages : IProjectPlanPages
{
    public List<(string Project, ProjectPlanTab Tab, int Page)> Calls { get; } = [];

    public ProjectPlanListing Listing { get; set; } = ProjectPlanListing.Empty;

    public Func<ProjectPlanTab, int, ProjectPlanListing>? Resolve { get; set; }

    public Task<ProjectPlanListing> PageAsync(
        ProjectId project,
        ProjectPlanTab tab,
        int page,
        CancellationToken cancellationToken)
    {
        Calls.Add((project.Value, tab, page));
        return Task.FromResult(Resolve?.Invoke(tab, page) ?? Listing);
    }
}