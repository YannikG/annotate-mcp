using System.Globalization;

using Annotate.Plans.Application;
using Annotate.Reviews.Application;

namespace Annotate.Web.Components.Browse;

public static class DashboardStats
{
    public static DashboardModel Compute(
        IReadOnlyList<RevisionActivity> revisions,
        IReadOnlyList<ListedReview> reviews,
        DateTimeOffset now,
        TimeZoneInfo zone)
    {
        Dictionary<string, RevisionActivity> byRevision = revisions.ToDictionary(
            revision => revision.RevisionId.Value,
            StringComparer.Ordinal);
        List<Joined> joined = [];
        foreach (ListedReview review in reviews)
        {
            if (byRevision.TryGetValue(review.RevisionId.Value, out RevisionActivity? revision))
            {
                joined.Add(new Joined(revision, review));
            }
        }

        List<RevisionActivity> active = revisions.Where(revision => !revision.ProjectArchived).ToList();
        List<Joined> activeReviews = joined.Where(item => !item.Revision.ProjectArchived).ToList();
        DateTimeOffset cutoff = now.AddDays(-30);
        DateTime[] weeks = Weeks(now, zone);
        return new DashboardModel(
            revisions.Count == 0,
            Approval(activeReviews, cutoff),
            FirstPass(activeReviews),
            new Metric(Duration(Median(Turnaround(activeReviews, cutoff))), "median, last 30 days", null),
            new Metric(Number(Median(Feedback(activeReviews))), "median annotations", null),
            Activity(active, cutoff),
            Pending(active, activeReviews),
            WeekBars(active, activeReviews, weeks, zone),
            Projects(revisions, joined),
            Groups(active, activeReviews, revision => revision.Attribution.Agent, "Unknown"),
            Groups(active, activeReviews, revision => revision.Attribution.Model, "Unknown"),
            Switches(active));
    }

    private static ReviewLink[] Pending(IReadOnlyList<RevisionActivity> revisions, IReadOnlyList<Joined> reviews)
    {
        Dictionary<string, int> newest = revisions
            .GroupBy(item => item.PlanId.Value, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Max(item => item.Number), StringComparer.Ordinal);
        return reviews
            .Where(item => !item.Revision.PlanArchived)
            .Where(item => item.Review.Status == ReviewStatus.Pending)
            .Where(item => newest.TryGetValue(item.Revision.PlanId.Value, out int number) && number == item.Revision.Number)
            .OrderBy(item => item.Review.CreatedAt)
            .ThenBy(item => item.Review.Id.Value, StringComparer.Ordinal)
            .Select(item => new ReviewLink(
                item.Review.Id.Value,
                string.IsNullOrWhiteSpace(item.Revision.PlanTitle) ? "Untitled" : item.Revision.PlanTitle,
                item.Revision.PlanId.Value,
                item.Revision.ProjectId.Value,
                item.Revision.ProjectName,
                item.Revision.Number,
                item.Review.CreatedAt))
            .ToArray();
    }

    private static Metric Approval(IReadOnlyList<Joined> reviews, DateTimeOffset cutoff)
    {
        (int approved, int decided) = Split(reviews);
        List<Joined> recent = reviews.Where(item => item.Review.DecidedAt >= cutoff).ToList();
        (int recentApproved, int recentDecided) = Split(recent);
        return new Metric(Rate(approved, decided), "last 30 days " + Rate(recentApproved, recentDecided), null);
    }

    private static Metric FirstPass(IReadOnlyList<Joined> reviews)
    {
        Dictionary<string, Joined> first = FirstRevisions(reviews);
        int decided = first.Values.Count(item => item.Review.Status != ReviewStatus.Pending);
        int approved = first.Values.Count(item => item.Review.Status == ReviewStatus.Approved);
        return new Metric(Rate(approved, decided), "revision 1", null);
    }

    private static Metric Activity(IReadOnlyList<RevisionActivity> revisions, DateTimeOffset cutoff)
    {
        int created = revisions.Count(revision => revision.CreatedAt >= cutoff);
        int plans = revisions.Count(revision => revision.Number == 1 && revision.CreatedAt >= cutoff);
        return new Metric(
            plans.ToString(CultureInfo.InvariantCulture) + " plans",
            created.ToString(CultureInfo.InvariantCulture) + " revisions",
            null);
    }

    private static List<double> ApprovalNumbers(IReadOnlyList<RevisionActivity> revisions, IReadOnlyList<Joined> reviews)
    {
        Dictionary<string, List<Joined>> byPlan = reviews
            .Where(item => item.Review.Status == ReviewStatus.Approved)
            .GroupBy(item => item.Revision.PlanId.Value, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        List<double> numbers = [];
        foreach (string plan in revisions.Select(revision => revision.PlanId.Value).Distinct(StringComparer.Ordinal))
        {
            if (byPlan.TryGetValue(plan, out List<Joined>? approved))
            {
                numbers.Add(approved.Min(item => item.Revision.Number));
            }
        }

        return numbers;
    }

    private static List<double> Turnaround(IReadOnlyList<Joined> reviews, DateTimeOffset cutoff) =>
        reviews
            .Where(item => item.Review.DecidedAt >= cutoff)
            .Select(item => (item.Review.DecidedAt!.Value - item.Review.CreatedAt).TotalMinutes)
            .ToList();

    private static List<double> Feedback(IReadOnlyList<Joined> reviews) =>
        reviews
            .Where(item => item.Review.Status == ReviewStatus.ChangesRequested)
            .Select(item => (double)item.Review.AnnotationCount)
            .ToList();

    private static WeekBar[] WeekBars(
        IReadOnlyList<RevisionActivity> revisions,
        IReadOnlyList<Joined> reviews,
        DateTime[] weeks,
        TimeZoneInfo zone)
    {
        Dictionary<DateTime, WeekBar> bars = weeks.ToDictionary(
            week => week,
            week => new WeekBar(week.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), week.ToString("d MMM", CultureInfo.InvariantCulture), 0, 0, 0));
        foreach (Joined item in reviews)
        {
            if (item.Review.DecidedAt is not DateTimeOffset decided)
            {
                continue;
            }

            DateTime week = WeekStart(decided, zone);
            if (!bars.TryGetValue(week, out WeekBar? bar))
            {
                continue;
            }

            bars[week] = item.Review.Status switch
            {
                ReviewStatus.Approved => bar with { Approved = bar.Approved + 1 },
                ReviewStatus.ChangesRequested => bar with { Changes = bar.Changes + 1 },
                _ => bar,
            };
        }

        foreach (RevisionActivity revision in revisions)
        {
            DateTime week = WeekStart(revision.CreatedAt, zone);
            if (bars.TryGetValue(week, out WeekBar? bar))
            {
                bars[week] = bar with { Revisions = bar.Revisions + 1 };
            }
        }

        return weeks.Select(week => bars[week]).ToArray();
    }

    private static ProjectStat[] Projects(
        IReadOnlyList<RevisionActivity> revisions,
        IReadOnlyList<Joined> reviews)
    {
        return revisions
            .GroupBy(revision => revision.ProjectId.Value, StringComparer.Ordinal)
            .Select(group =>
            {
                RevisionActivity sample = group.OrderByDescending(revision => revision.CreatedAt).First();
                List<Joined> projectReviews = reviews
                    .Where(item => item.Revision.ProjectId.Value == group.Key)
                    .ToList();
                (int approved, int decided) = Split(projectReviews);
                return new ProjectStat(
                    group.Key,
                    sample.ProjectName,
                    sample.ProjectArchived,
                    group.Select(revision => revision.PlanId.Value).Distinct(StringComparer.Ordinal).Count(),
                    projectReviews.Count(item => item.Review.Status == ReviewStatus.Pending),
                    Rate(approved, decided),
                    Number(Median(ApprovalNumbers(group.ToArray(), projectReviews))),
                    sample.CreatedAt.ToString("d MMM yyyy", CultureInfo.InvariantCulture),
                    sample.CreatedAt);
            })
            .OrderBy(project => project.Archived)
            .ThenByDescending(project => project.ActivityAt)
            .ThenBy(project => project.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static List<GroupStat> Groups(
        IReadOnlyList<RevisionActivity> revisions,
        IReadOnlyList<Joined> reviews,
        Func<RevisionActivity, string?> value,
        string unknown)
    {
        List<GroupStat> groups = revisions
            .GroupBy(revision => Key(value(revision)), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                HashSet<string> ids = group.Select(revision => revision.RevisionId.Value).ToHashSet(StringComparer.Ordinal);
                List<Joined> matched = reviews.Where(item => ids.Contains(item.Revision.RevisionId.Value)).ToList();
                (int approved, int decided) = Split(matched);
                string name = group.Key.Length == 0
                    ? unknown
                    : group.Select(value).Select(AttributionRules.Trim).First(item => item is not null)!;
                return new GroupStat(
                    name,
                    group.Count(),
                    group.Select(revision => revision.PlanId.Value).Distinct(StringComparer.Ordinal).Count(),
                    Rate(approved, decided),
                    FirstPass(matched).Value,
                    Number(Median(matched
                        .Where(item => item.Review.Status == ReviewStatus.Approved)
                        .Select(item => (double)item.Revision.Number)
                        .ToList())));
            })
            .OrderBy(group => group.Name == unknown)
            .ThenByDescending(group => group.Revisions)
            .ThenBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return groups;
    }

    private static SwitchStat[] Switches(IReadOnlyList<RevisionActivity> revisions) =>
        revisions
            .GroupBy(revision => revision.PlanId.Value, StringComparer.Ordinal)
            .Select(group =>
            {
                List<RevisionActivity> ordered = group.OrderBy(revision => revision.Number).ToList();
                string agents = Path(ordered, revision => revision.Attribution.Agent);
                string models = Path(ordered, revision => revision.Attribution.Model);
                return new SwitchStat(
                    group.Key,
                    ordered[^1].PlanTitle,
                    agents,
                    models,
                    ordered.Max(revision => revision.CreatedAt),
                    agents.Contains('→', StringComparison.Ordinal) || models.Contains('→', StringComparison.Ordinal));
            })
            .Where(item => item.Changed)
            .OrderByDescending(item => item.At)
            .ThenBy(item => item.Title, StringComparer.Ordinal)
            .Take(10)
            .Select(item => new SwitchStat(item.PlanId, item.Title, item.Agents, item.Models, item.At, item.Changed))
            .ToArray();

    private static string Path(IReadOnlyList<RevisionActivity> revisions, Func<RevisionActivity, string?> value)
    {
        List<string> names = [];
        foreach (RevisionActivity revision in revisions)
        {
            string name = AttributionRules.Trim(value(revision)) ?? "Unknown";
            if (names.Count == 0 || !names[^1].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                names.Add(name);
            }
        }

        return string.Join(" → ", names);
    }

    private static Dictionary<string, Joined> FirstRevisions(IReadOnlyList<Joined> reviews)
    {
        Dictionary<string, Joined> first = new(StringComparer.Ordinal);
        foreach (Joined item in reviews)
        {
            if (item.Revision.Number != 1 || item.Review.Status == ReviewStatus.Pending)
            {
                continue;
            }

            first[item.Revision.PlanId.Value] = item;
        }

        return first;
    }

    private static (int Approved, int Decided) Split(IReadOnlyList<Joined> reviews)
    {
        int approved = reviews.Count(item => item.Review.Status == ReviewStatus.Approved);
        int changes = reviews.Count(item => item.Review.Status == ReviewStatus.ChangesRequested);
        return (approved, approved + changes);
    }

    private static string Rate(int approved, int decided) =>
        decided == 0
            ? "—"
            : $"{(int)Math.Round(100.0 * approved / decided)}% of {decided.ToString(CultureInfo.InvariantCulture)}";

    private static double? Median(List<double> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        double[] ordered = values.OrderBy(value => value).ToArray();
        int mid = ordered.Length / 2;
        return ordered.Length % 2 == 1 ? ordered[mid] : (ordered[mid - 1] + ordered[mid]) / 2;
    }

    private static string Number(double? value) =>
        value is null
            ? "—"
            : value.Value % 1 == 0
                ? ((int)value.Value).ToString(CultureInfo.InvariantCulture)
                : value.Value.ToString("0.0", CultureInfo.InvariantCulture);

    private static string Duration(double? minutes)
    {
        if (minutes is null)
        {
            return "—";
        }

        TimeSpan span = TimeSpan.FromMinutes(Math.Max(0, minutes.Value));
        if (span.TotalHours < 1)
        {
            return $"{(int)span.TotalMinutes} min";
        }

        if (span.TotalDays < 1)
        {
            return $"{(int)span.TotalHours} h";
        }

        return $"{(int)span.TotalDays} d";
    }

    private static DateTime[] Weeks(DateTimeOffset now, TimeZoneInfo zone)
    {
        DateTime current = WeekStart(now, zone);
        return Enumerable.Range(0, 12).Select(index => current.AddDays(-7 * (11 - index))).ToArray();
    }

    private static DateTime WeekStart(DateTimeOffset instant, TimeZoneInfo zone)
    {
        DateTime local = TimeZoneInfo.ConvertTime(instant, zone).Date;
        return local.AddDays(-(((int)local.DayOfWeek + 6) % 7));
    }

    private static string Key(string? value) => AttributionRules.Trim(value) ?? "";

    private sealed record Joined(RevisionActivity Revision, ListedReview Review);
}

public sealed record DashboardModel(
    bool Empty,
    Metric Approval,
    Metric FirstPass,
    Metric Turnaround,
    Metric Feedback,
    Metric Activity,
    IReadOnlyList<ReviewLink> Pending,
    IReadOnlyList<WeekBar> Weeks,
    IReadOnlyList<ProjectStat> Projects,
    IReadOnlyList<GroupStat> Agents,
    IReadOnlyList<GroupStat> Models,
    IReadOnlyList<SwitchStat> Switches);

public sealed record Metric(string Value, string Detail, string? Href);

public sealed record WeekBar(string Key, string Label, int Approved, int Changes, int Revisions);

public sealed record ProjectStat(
    string ProjectId,
    string Name,
    bool Archived,
    int Plans,
    int Pending,
    string Approval,
    string Rounds,
    string LastActivity,
    DateTimeOffset ActivityAt);

public sealed record GroupStat(
    string Name,
    int Revisions,
    int Plans,
    string Approval,
    string FirstPass,
    string Rounds);

public sealed record SwitchStat(
    string PlanId,
    string Title,
    string Agents,
    string Models,
    DateTimeOffset At,
    bool Changed);