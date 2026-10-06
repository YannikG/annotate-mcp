using Annotate.Web;

namespace Annotate.Web.Tests;

public sealed class AutoClosePreferenceTests
{
    [Fact]
    public async Task MissingOrUnreadableFileReadsFalseAndWriteRoundTrips()
    {
        string directory = Directory.CreateTempSubdirectory("annotate-preferences-").FullName;
        try
        {
            FileAutoClosePreference preference = new(directory);
            Assert.False(await preference.ReadAsync(CancellationToken.None));

            Assert.True(await preference.WriteAsync(true, CancellationToken.None));
            string path = Path.Combine(directory, "preferences.json");
            string json = await File.ReadAllTextAsync(path);
            Assert.Contains("\"autoCloseOnSubmit\":true", json, StringComparison.Ordinal);
            Assert.True(await preference.ReadAsync(CancellationToken.None));

            await File.WriteAllTextAsync(path, "{");
            Assert.False(await preference.ReadAsync(CancellationToken.None));

            Directory.CreateDirectory(Path.Combine(directory, "blocked"));
            FileAutoClosePreference blocked = new(Path.Combine(directory, "blocked"));
            Directory.CreateDirectory(Path.Combine(directory, "blocked", "preferences.json"));
            Assert.False(await blocked.ReadAsync(CancellationToken.None));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}