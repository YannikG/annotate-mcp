using System.Diagnostics;

using Annotate.Reviews.Application;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Reviews.Tests;

public sealed class MigrationOnceTests
{
    [Fact]
    public async Task ASecondReadDoesNotTakeTheMigrationLock()
    {
        await using OpenReviews open = await OpenReviews.Open();
        await using ServiceProvider host = open.Reviews();
        IReviews reviews = host.GetRequiredService<IReviews>();
        using MigrationLockCount locks = new(open.ConnectionString);

        await reviews.PendingAsync(CancellationToken.None);
        int first = locks.Count;
        Assert.True(first > 0);

        await reviews.PendingAsync(CancellationToken.None);
        Assert.Equal(first, locks.Count);
    }

    private sealed class MigrationLockCount : IObserver<DiagnosticListener>, IDisposable
    {
        private readonly string connection;
        private readonly List<IDisposable> subscriptions = [];
        private readonly IDisposable all;

        public MigrationLockCount(string connection)
        {
            this.connection = connection;
            all = DiagnosticListener.AllListeners.Subscribe(this);
        }

        public int Count { get; private set; }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener value)
        {
            if (value.Name == "Microsoft.EntityFrameworkCore")
            {
                subscriptions.Add(value.Subscribe(new Commands(this)));
            }
        }

        public void Dispose()
        {
            foreach (IDisposable subscription in subscriptions)
            {
                subscription.Dispose();
            }

            all.Dispose();
        }

        private sealed class Commands(MigrationLockCount owner) : IObserver<KeyValuePair<string, object?>>
        {
            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(KeyValuePair<string, object?> value)
            {
                if (value.Value is not CommandExecutedEventData executed)
                {
                    return;
                }

                string text = executed.Command.CommandText;
                string source = executed.Command.Connection?.ConnectionString ?? "";
                if (source.Contains(owner.connection, StringComparison.Ordinal)
                    && text.Contains("__EFMigrationsLock", StringComparison.Ordinal))
                {
                    owner.Count++;
                }
            }
        }
    }
}