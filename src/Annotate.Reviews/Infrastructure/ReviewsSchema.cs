using Microsoft.EntityFrameworkCore;

namespace Annotate.Reviews.Infrastructure;

internal static class ReviewsSchema
{
    public static async Task<ReviewsDbContext> Open(
        IDbContextFactory<ReviewsDbContext> contexts,
        CancellationToken cancellationToken)
    {
        ReviewsDbContext db = await contexts.CreateDbContextAsync(cancellationToken);
        try
        {
            await db.Database.MigrateAsync(cancellationToken);
            return db;
        }
        catch
        {
            await db.DisposeAsync();
            throw;
        }
    }
}