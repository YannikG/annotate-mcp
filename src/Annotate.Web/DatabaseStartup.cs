using Annotate.Plans.Application;
using Annotate.Reviews.Application;

namespace Annotate.Web;

internal sealed class DatabaseStartup(IPlans plans, IReviews reviews) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await plans.ProjectsAsync(cancellationToken);
        await reviews.PendingAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}