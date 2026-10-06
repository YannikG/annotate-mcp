namespace Annotate.Reviews.Infrastructure;

internal sealed class ReviewSignals
{
    private readonly object _gate = new();

    private readonly Dictionary<string, List<TaskCompletionSource>> _waiters = new(StringComparer.Ordinal);

    public Task Wait(string reviewId, CancellationToken cancellationToken)
    {
        TaskCompletionSource source = new(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_gate)
        {
            if (!_waiters.TryGetValue(reviewId, out List<TaskCompletionSource>? waiters))
            {
                waiters = [];
                _waiters[reviewId] = waiters;
            }

            waiters.Add(source);
        }

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() =>
            {
                Remove(reviewId, source);
                source.TrySetCanceled(cancellationToken);
            });
        }

        return source.Task;
    }

    public void Pulse(string reviewId)
    {
        List<TaskCompletionSource>? waiters;
        lock (_gate)
        {
            if (!_waiters.Remove(reviewId, out waiters))
            {
                return;
            }
        }

        foreach (TaskCompletionSource source in waiters)
        {
            source.TrySetResult();
        }
    }

    private void Remove(string reviewId, TaskCompletionSource source)
    {
        lock (_gate)
        {
            if (!_waiters.TryGetValue(reviewId, out List<TaskCompletionSource>? waiters))
            {
                return;
            }

            waiters.Remove(source);
            if (waiters.Count == 0)
            {
                _waiters.Remove(reviewId);
            }
        }
    }
}