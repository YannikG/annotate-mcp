using Annotate.Web;

namespace Annotate.Web.Tests;

internal sealed class FakeSelection : ISelectionReader
{
    public TextSelection? Next { get; set; }

    public int Reads { get; private set; }

    public Task<TextSelection?> ReadAsync()
    {
        Reads++;
        return Task.FromResult(Next);
    }
}