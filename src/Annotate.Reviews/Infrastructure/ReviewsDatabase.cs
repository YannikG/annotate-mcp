using Microsoft.EntityFrameworkCore;

namespace Annotate.Reviews.Infrastructure;

internal static class ReviewsDatabase
{
    public const string HistoryTable = "reviews_migration_history";

    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlite(
            connectionString,
            sqlite => sqlite.MigrationsHistoryTable(HistoryTable));
    }
}