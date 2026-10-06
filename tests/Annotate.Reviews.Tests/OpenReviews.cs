using Annotate.Reviews;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Reviews.Tests;

internal sealed class OpenReviews : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private OpenReviews(SqliteConnection connection, string connectionString)
    {
        _connection = connection;
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public static async Task<OpenReviews> Open()
    {
        string connectionString = $"Data Source=file:reviews-{Guid.NewGuid():N}?mode=memory&cache=shared";
        SqliteConnection connection = new(connectionString);
        await connection.OpenAsync();
        return new OpenReviews(connection, connectionString);
    }

    public ServiceProvider Reviews(TimeProvider? time = null)
    {
        ServiceCollection services = new();
        if (time is not null)
        {
            services.AddSingleton(time);
        }

        return services.AddReviews(new ReviewsSettings(ConnectionString)).BuildServiceProvider();
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}