using Annotate.Plans.Application;

namespace Annotate.Web.Components.Browse;

internal static class RevisionLabel
{
    public static string Format(RevisionRow revision)
    {
        List<string> parts = [$"v{revision.Number}", revision.Status];
        if (!string.IsNullOrWhiteSpace(revision.Agent))
        {
            parts.Add(revision.Agent);
        }

        if (!string.IsNullOrWhiteSpace(revision.Model))
        {
            parts.Add(revision.Model);
        }

        return string.Join(" · ", parts);
    }

    public static string? Note(IEnumerable<PlanRevision> revisions)
    {
        int agents = Count(revisions, revision => revision.Attribution?.Agent);
        int models = Count(revisions, revision => revision.Attribution?.Model);
        List<string> parts = [];
        if (agents > 1)
        {
            parts.Add($"{agents} agents");
        }

        if (models > 1)
        {
            parts.Add($"{models} models");
        }

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    public static string? WrittenBy(Attribution? attribution)
    {
        if (attribution is null)
        {
            return null;
        }

        List<string> parts = [];
        if (!string.IsNullOrWhiteSpace(attribution.Agent))
        {
            parts.Add(attribution.Agent);
        }

        if (!string.IsNullOrWhiteSpace(attribution.Model))
        {
            parts.Add(attribution.Model);
        }

        string? client = Client(attribution);
        if (parts.Count == 0 && client is null)
        {
            return null;
        }

        string body = string.Join(" · ", parts);
        return client is null ? body : body.Length == 0 ? client : $"{body} ({client})";
    }

    private static string? Client(Attribution attribution)
    {
        if (string.IsNullOrWhiteSpace(attribution.ClientName))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(attribution.ClientVersion)
            ? "client " + attribution.ClientName
            : $"client {attribution.ClientName} {attribution.ClientVersion}";
    }

    private static int Count(IEnumerable<PlanRevision> revisions, Func<PlanRevision, string?> value) =>
        revisions
            .Select(value)
            .Select(AttributionRules.Trim)
            .Where(item => item is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
}