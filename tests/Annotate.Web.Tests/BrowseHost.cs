using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Web.Tests;

internal static class BrowseHost
{
    public static BunitContext Open(IPlans plans, IReviews reviews, IProjectPlanPages? pages = null)
    {
        BunitContext context = new();
        context.Services.AddSingleton(plans);
        context.Services.AddSingleton(reviews);
        context.Services.AddSingleton(pages ?? new FakeProjectPlanPages());
        MemoryAutoClose preference = new();
        context.Services.AddSingleton<IAutoClosePreference>(preference);
        context.Services.AddSingleton<IReportScript>(new RecordingReport());
        return context;
    }
}