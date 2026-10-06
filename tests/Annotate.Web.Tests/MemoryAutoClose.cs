using Annotate.Web;

namespace Annotate.Web.Tests;

internal sealed class MemoryAutoClose : IAutoClosePreference
{
    public bool AutoCloseOnSubmit { get; set; }

    public bool FailWrite { get; set; }

    public int Writes { get; private set; }

    public Task<bool> ReadAsync(CancellationToken cancellationToken) =>
        Task.FromResult(AutoCloseOnSubmit);

    public Task<bool> WriteAsync(bool autoCloseOnSubmit, CancellationToken cancellationToken)
    {
        Writes++;
        if (FailWrite)
        {
            return Task.FromResult(false);
        }

        AutoCloseOnSubmit = autoCloseOnSubmit;
        return Task.FromResult(true);
    }
}