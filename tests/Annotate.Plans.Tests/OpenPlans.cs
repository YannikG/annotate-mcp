using Annotate.Plans;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Plans.Tests;

internal sealed class OpenPlans : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private OpenPlans(SqliteConnection connection, string connectionString)
    {
        _connection = connection;
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public static async Task<OpenPlans> Open()
    {
        string connectionString = $"Data Source=file:plans-{Guid.NewGuid():N}?mode=memory&cache=shared";
        SqliteConnection connection = new(connectionString);
        await connection.OpenAsync();
        return new OpenPlans(connection, connectionString);
    }

    public ServiceProvider Plans(IReadOnlyList<string>? trustedStoryDomains = null)
    {
        PlansSettings settings = new(ConnectionString, trustedStoryDomains ?? []);
        return new ServiceCollection().AddPlans(settings).BuildServiceProvider();
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}