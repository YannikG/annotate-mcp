using System.Globalization;

using Annotate.Plans.Application;
using Annotate.Reviews.Application;

using Microsoft.Data.Sqlite;

namespace Annotate.Web;

public enum ProjectPlanTab
{
    Pending,
    Changes,
    Approved,
    Archived,
}

public sealed record ProjectPlanRow(
    string PlanId,
    string Title,
    int RevisionNumber,
    DateTimeOffset UpdatedAt,
    string? ReviewId,
    ReviewStatus? Status);

public sealed record ProjectPlanListing(
    int ActivePlans,
    int Pending,
    int Changes,
    int Approved,
    int Archived,
    IReadOnlyList<ProjectPlanRow> Rows)
{
    public static ProjectPlanListing Empty { get; } = new(0, 0, 0, 0, 0, []);
}

public interface IProjectPlanPages
{
    public const int PageSize = 20;

    Task<ProjectPlanListing> PageAsync(
        ProjectId project,
        ProjectPlanTab tab,
        int page,
        CancellationToken cancellationToken);
}

public sealed class SqliteProjectPlanPages(string connectionString) : IProjectPlanPages
{
    private const string Newest = """
        WITH newest AS (
            SELECT revision.plan_id, revision.id AS revision_id, revision.number
            FROM revisions AS revision
            WHERE revision.number = (
                SELECT MAX(later.number)
                FROM revisions AS later
                WHERE later.plan_id = revision.plan_id))
        """;

    public async Task<ProjectPlanListing> PageAsync(
        ProjectId project,
        ProjectPlanTab tab,
        int page,
        CancellationToken cancellationToken)
    {
        int requested = page < 1 ? 1 : page;
        int offset = (requested - 1) * IProjectPlanPages.PageSize;
        await using SqliteConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        (int pending, int changes, int approved, int archived, int active) =
            await Counts(connection, project.Value, cancellationToken);
        List<ProjectPlanRow> rows = await Rows(connection, project.Value, tab, offset, cancellationToken);
        return new ProjectPlanListing(active, pending, changes, approved, archived, rows);
    }

    private static async Task<(int Pending, int Changes, int Approved, int Archived, int Active)> Counts(
        SqliteConnection connection,
        string project,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = Newest + """
            SELECT
                (SELECT COUNT(*)
                 FROM plans AS plan
                 JOIN newest ON newest.plan_id = plan.id
                 JOIN reviews AS review ON review.revision_id = newest.revision_id
                 WHERE plan.project_id = $project AND plan.archived_at IS NULL AND review.status = 'pending'),
                (SELECT COUNT(*)
                 FROM plans AS plan
                 JOIN newest ON newest.plan_id = plan.id
                 JOIN reviews AS review ON review.revision_id = newest.revision_id
                 WHERE plan.project_id = $project AND plan.archived_at IS NULL AND review.status = 'changes_requested'),
                (SELECT COUNT(*)
                 FROM plans AS plan
                 JOIN newest ON newest.plan_id = plan.id
                 JOIN reviews AS review ON review.revision_id = newest.revision_id
                 WHERE plan.project_id = $project AND plan.archived_at IS NULL AND review.status = 'approved'),
                (SELECT COUNT(*) FROM plans WHERE project_id = $project AND archived_at IS NOT NULL),
                (SELECT COUNT(*) FROM plans WHERE project_id = $project AND archived_at IS NULL)
            """;
        command.Parameters.AddWithValue("$project", project);
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return (ReadCount(reader, 0), ReadCount(reader, 1), ReadCount(reader, 2), ReadCount(reader, 3), ReadCount(reader, 4));
    }

    private static async Task<List<ProjectPlanRow>> Rows(
        SqliteConnection connection,
        string project,
        ProjectPlanTab tab,
        int offset,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = tab == ProjectPlanTab.Archived ? ArchivedSql() : StatusSql();
        command.Parameters.AddWithValue("$project", project);
        command.Parameters.AddWithValue("$limit", IProjectPlanPages.PageSize);
        command.Parameters.AddWithValue("$offset", offset);
        if (tab != ProjectPlanTab.Archived)
        {
            command.Parameters.AddWithValue("$status", StatusText(tab));
            command.Parameters.AddWithValue("$by_created", tab == ProjectPlanTab.Pending ? 1 : 0);
        }

        List<ProjectPlanRow> rows = [];
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new ProjectPlanRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(3),
                ReadTime(reader, 2),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                ParseStatus(reader.IsDBNull(5) ? null : reader.GetString(5))));
        }

        return rows;
    }

    private static string StatusSql() => Newest + """
        SELECT plan.id, plan.title, plan.updated_at, newest.number, review.id, review.status
        FROM plans AS plan
        JOIN newest ON newest.plan_id = plan.id
        JOIN reviews AS review ON review.revision_id = newest.revision_id
        WHERE plan.project_id = $project
          AND plan.archived_at IS NULL
          AND review.status = $status
        ORDER BY CASE WHEN $by_created = 1 THEN review.created_at ELSE COALESCE(review.decided_at, review.created_at) END DESC,
                 plan.id ASC
        LIMIT $limit OFFSET $offset
        """;

    private static string ArchivedSql() => Newest + """
        SELECT plan.id, plan.title, plan.updated_at, newest.number, NULL, review.status
        FROM plans AS plan
        JOIN newest ON newest.plan_id = plan.id
        LEFT JOIN reviews AS review ON review.revision_id = newest.revision_id
        WHERE plan.project_id = $project AND plan.archived_at IS NOT NULL
        ORDER BY plan.updated_at DESC, plan.id ASC
        LIMIT $limit OFFSET $offset
        """;

    private static string StatusText(ProjectPlanTab tab) => tab switch
    {
        ProjectPlanTab.Pending => "pending",
        ProjectPlanTab.Changes => "changes_requested",
        ProjectPlanTab.Approved => "approved",
        _ => throw new ArgumentOutOfRangeException(nameof(tab)),
    };

    private static ReviewStatus? ParseStatus(string? status) => status switch
    {
        "pending" => ReviewStatus.Pending,
        "approved" => ReviewStatus.Approved,
        "changes_requested" => ReviewStatus.ChangesRequested,
        null or "" => null,
        _ => throw new InvalidOperationException("Review status was not recognised."),
    };

    private static int ReadCount(SqliteDataReader reader, int ordinal) => (int)reader.GetInt64(ordinal);

    private static DateTimeOffset ReadTime(SqliteDataReader reader, int ordinal)
    {
        object value = reader.GetValue(ordinal);
        return value switch
        {
            DateTimeOffset date => date,
            DateTime date => new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc)),
            string text => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException("Plan time was not recognised."),
        };
    }
}