using Annotate.Plans.Application;
using Annotate.Plans.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Plans;

public sealed record PlansSettings(string ConnectionString, IReadOnlyList<string> TrustedStoryDomains);

public static class PlansServiceCollectionExtensions
{
    public static IServiceCollection AddPlans(this IServiceCollection services, PlansSettings settings)
    {
        services.AddSingleton(settings);
        services.AddDbContextFactory<PlansDbContext>(options =>
            PlansDatabase.Configure(options, settings.ConnectionString));
        services.AddSingleton<IPlans>(provider => new PlanSubmitter(
            provider.GetRequiredService<IDbContextFactory<PlansDbContext>>(),
            provider.GetRequiredService<PlansSettings>()));
        return services;
    }
}