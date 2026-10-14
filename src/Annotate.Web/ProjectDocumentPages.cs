using System.Globalization;

using Annotate.Plans.Application;
using Annotate.Reviews.Application;

using Microsoft.Data.Sqlite;

namespace Annotate.Web;

public enum ProjectDocumentTab
{
    Active,
    Archived,
}

public sealed record ProjectDocumentRow(
    string DocumentId,
    string Title,
    int RevisionNumber,
    DateTimeOffset UpdatedAt,
    bool ReadOnly,
    ReviewStatus? Status);

public sealed record ProjectDocumentListing(
    int Active,
    int Archived,
    IReadOnlyList<ProjectDocumentRow> Rows)
{
    public static ProjectDocumentListing Empty { get; } = new(0, 0, []);
}

public interface IProjectDocumentPages
{
    public const int PageSize = 20;

    Task<ProjectDocumentListing> PageAsync(
        ProjectId project,
        ProjectDocumentTab tab,
        int page,
        CancellationToken cancellationToken);
}

public sealed class SqliteProjectDocumentPages(string connectionString) : IProjectDocumentPages
{
    private const string Newest = """
        WITH newest AS (
            SELECT revision.document_id, revision.id AS revision_id, revision.number
            FROM revisions AS revision
            WHERE revision.document_id IS NOT NULL
              AND revision.number = (
                SELECT MAX(later.number)
                FROM revisions AS later
                WHERE later.document_id = revision.document_id))
        """;

    public async Task<ProjectDocumentListing> PageAsync(
        ProjectId project,
        ProjectDocumentTab tab,
        int page,
        CancellationToken cancellationToken)
    {
        int requested = page < 1 ? 1 : page;
        int offset = (requested - 1) * IProjectDocumentPages.PageSize;
        await using SqliteConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        (int active, int archived) = await Counts(connection, project.Value, cancellationToken);
        List<ProjectDocumentRow> rows = await Rows(connection, project.Value, tab, offset, cancellationToken);
        return new ProjectDocumentListing(active, archived, rows);
    }

    private static async Task<(int Active, int Archived)> Counts(
        SqliteConnection connection,
        string project,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT COUNT(*) FROM documents WHERE project_id = $project AND archived_at IS NULL),
                (SELECT COUNT(*) FROM documents WHERE project_id = $project AND archived_at IS NOT NULL)
            """;
        command.Parameters.AddWithValue("$project", project);
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return ((int)reader.GetInt64(0), (int)reader.GetInt64(1));
    }

    private static async Task<List<ProjectDocumentRow>> Rows(
        SqliteConnection connection,
        string project,
        ProjectDocumentTab tab,
        int offset,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = Newest + """
            SELECT document.id, document.title, document.updated_at, newest.number, review.status,
                   document.read_only_at IS NOT NULL
            FROM documents AS document
            JOIN newest ON newest.document_id = document.id
            LEFT JOIN reviews AS review ON review.revision_id = newest.revision_id
            WHERE document.project_id = $project
              AND ((@archived = 0 AND document.archived_at IS NULL) OR (@archived = 1 AND document.archived_at IS NOT NULL))
            ORDER BY document.updated_at DESC, document.id ASC
            LIMIT $limit OFFSET $offset
            """;
        command.Parameters.AddWithValue("$project", project);
        command.Parameters.AddWithValue("$limit", IProjectDocumentPages.PageSize);
        command.Parameters.AddWithValue("$offset", offset);
        command.Parameters.AddWithValue("@archived", tab == ProjectDocumentTab.Archived ? 1 : 0);
        List<ProjectDocumentRow> rows = [];
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new ProjectDocumentRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(3),
                ReadTime(reader, 2),
                reader.GetInt64(5) != 0,
                ParseStatus(reader.IsDBNull(4) ? null : reader.GetString(4))));
        }

        return rows;
    }

    private static ReviewStatus? ParseStatus(string? status) => status switch
    {
        "pending" => ReviewStatus.Pending,
        "approved" => ReviewStatus.Approved,
        "changes_requested" => ReviewStatus.ChangesRequested,
        null or "" => null,
        _ => throw new InvalidOperationException("Review status was not recognised."),
    };

    private static DateTimeOffset ReadTime(SqliteDataReader reader, int ordinal)
    {
        object value = reader.GetValue(ordinal);
        return value switch
        {
            DateTimeOffset date => date,
            DateTime date => new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc)),
            string text => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException("Document time was not recognised."),
        };
    }
}