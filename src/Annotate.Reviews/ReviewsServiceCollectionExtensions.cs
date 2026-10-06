using Annotate.Reviews.Application;
using Annotate.Reviews.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Annotate.Reviews;

public sealed record ReviewsSettings(string ConnectionString);

public static class ReviewsServiceCollectionExtensions
{
    public static IServiceCollection AddReviews(this IServiceCollection services, ReviewsSettings settings)
    {
        services.AddSingleton(settings);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ReviewSignals>();
        services.AddDbContextFactory<ReviewsDbContext>(options =>
            ReviewsDatabase.Configure(options, settings.ConnectionString));
        services.AddSingleton<IReviews, ReviewService>();
        return services;
    }
}