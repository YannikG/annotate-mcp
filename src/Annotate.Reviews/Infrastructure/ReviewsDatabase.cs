using System.Collections.Concurrent;

using Microsoft.EntityFrameworkCore;

namespace Annotate.Reviews.Infrastructure;

internal static class ReviewsDatabase
{
    public const string HistoryTable = "reviews_migration_history";

    private static readonly ConcurrentDictionary<string, Lazy<Task>> Migrated = new(StringComparer.Ordinal);

    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlite(
            connectionString,
            sqlite => sqlite.MigrationsHistoryTable(HistoryTable));
    }

    public static async Task EnsureMigratedAsync(DbContext db, CancellationToken cancellationToken)
    {
        string key = db.GetType().FullName + "\n" + db.Database.GetDbConnection().ConnectionString;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Lazy<Task> lazy = Migrated.GetOrAdd(
                key,
                _ => new Lazy<Task>(() => db.Database.MigrateAsync(cancellationToken)));
            try
            {
                await lazy.Value.WaitAsync(cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                Migrated.TryRemove(new KeyValuePair<string, Lazy<Task>>(key, lazy));
                throw;
            }
        }
    }
}