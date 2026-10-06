using System.Text.Json;
using System.Text.Json.Serialization;

namespace Annotate.Web;

public interface IAutoClosePreference
{
    Task<bool> ReadAsync(CancellationToken cancellationToken);

    Task<bool> WriteAsync(bool autoCloseOnSubmit, CancellationToken cancellationToken);
}

public sealed class FileAutoClosePreference(string directory) : IAutoClosePreference
{
    private readonly string path = Path.Combine(directory, "preferences.json");

    public async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            string json = await File.ReadAllTextAsync(path, cancellationToken);
            PreferenceFile? stored = JsonSerializer.Deserialize<PreferenceFile>(json);
            return stored?.AutoCloseOnSubmit ?? false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> WriteAsync(bool autoCloseOnSubmit, CancellationToken cancellationToken)
    {
        string temporary = path + ".tmp";
        try
        {
            string json = JsonSerializer.Serialize(new PreferenceFile(autoCloseOnSubmit));
            await File.WriteAllTextAsync(temporary, json, cancellationToken);
            File.Move(temporary, path, overwrite: true);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private sealed record PreferenceFile(
        [property: JsonPropertyName("autoCloseOnSubmit")] bool AutoCloseOnSubmit);
}