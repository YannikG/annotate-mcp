using Microsoft.EntityFrameworkCore;

namespace Annotate.Plans.Infrastructure;

internal static class PlansDatabase
{
    public const string HistoryTable = "plans_migration_history";

    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlite(
            connectionString,
            sqlite => sqlite.MigrationsHistoryTable(HistoryTable));
    }
}