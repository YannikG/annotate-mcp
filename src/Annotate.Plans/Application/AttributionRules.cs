namespace Annotate.Plans.Application;

public static class AttributionRules
{
    public const int MaxLength = 100;

    public static string? Reject(string? agent, string? model, string? clientName, string? clientVersion)
    {
        string? agentValue = Trim(agent);
        string? modelValue = Trim(model);
        if (agentValue is null)
        {
            return "Agent is required.";
        }

        if (agentValue.Length > MaxLength)
        {
            return "Agent exceeds 100 characters.";
        }

        if (modelValue is null)
        {
            return "Model is required.";
        }

        if (modelValue.Length > MaxLength)
        {
            return "Model exceeds 100 characters.";
        }

        if (Trim(clientName) is { Length: > MaxLength })
        {
            return "Client name exceeds 100 characters.";
        }

        if (Trim(clientVersion) is { Length: > MaxLength })
        {
            return "Client version exceeds 100 characters.";
        }

        return null;
    }

    public static Attribution Normalize(string? agent, string? model, string? clientName, string? clientVersion) =>
        new(Trim(agent), Trim(model), Trim(clientName), Trim(clientVersion));

    public static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}