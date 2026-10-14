using Annotate.Plans.Application;

namespace Annotate.Web.Tests;

internal sealed class FakeDocuments : IDocuments
{
    public Dictionary<string, DocumentDetail?> Documents { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, DocumentRevisionDetail?> Revisions { get; } = new(StringComparer.Ordinal);

    public Func<DocumentId, string, bool, DocumentChange>? SetBlockLock { get; set; }

    public List<string> Archived { get; } = [];

    public List<string> Restored { get; } = [];

    public List<string> Deleted { get; } = [];

    public Task<DocumentSubmitOutcome> SubmitAsync(SubmitDocument request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<DocumentSummary>> DocumentsAsync(ProjectId id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<DocumentSummary>> ArchivedDocumentsAsync(ProjectId id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<DocumentChange> ArchiveAsync(DocumentId id, CancellationToken cancellationToken)
    {
        Archived.Add(id.Value);
        return Task.FromResult<DocumentChange>(new DocumentChange.Done([]));
    }

    public Task<DocumentChange> RestoreAsync(DocumentId id, CancellationToken cancellationToken)
    {
        Restored.Add(id.Value);
        return Task.FromResult<DocumentChange>(new DocumentChange.Done([]));
    }

    public Task<DocumentChange> DeleteAsync(DocumentId id, CancellationToken cancellationToken)
    {
        Deleted.Add(id.Value);
        return Task.FromResult<DocumentChange>(new DocumentChange.Done([]));
    }

    public Task<DocumentChange> SetReadOnlyAsync(DocumentId id, bool makeReadOnly, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<DocumentChange> SetBlockLockAsync(DocumentId id, string blockKey, bool locked, CancellationToken cancellationToken) =>
        Task.FromResult(SetBlockLock?.Invoke(id, blockKey, locked) ?? new DocumentChange.Done([]));

    public Task<DocumentDetail?> DocumentAsync(DocumentId id, CancellationToken cancellationToken) =>
        Task.FromResult(Documents.TryGetValue(id.Value, out DocumentDetail? document) ? document : null);

    public Task<DocumentRevisionDetail?> RevisionAsync(RevisionId id, CancellationToken cancellationToken) =>
        Task.FromResult(Revisions.TryGetValue(id.Value, out DocumentRevisionDetail? revision) ? revision : null);
}