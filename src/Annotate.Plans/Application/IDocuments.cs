namespace Annotate.Plans.Application;

public interface IDocuments
{
    Task<DocumentSubmitOutcome> SubmitAsync(SubmitDocument request, CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentSummary>> DocumentsAsync(ProjectId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentSummary>> ArchivedDocumentsAsync(ProjectId id, CancellationToken cancellationToken);

    Task<DocumentChange> ArchiveAsync(DocumentId id, CancellationToken cancellationToken);

    Task<DocumentChange> RestoreAsync(DocumentId id, CancellationToken cancellationToken);

    Task<DocumentChange> DeleteAsync(DocumentId id, CancellationToken cancellationToken);

    Task<DocumentChange> SetReadOnlyAsync(DocumentId id, bool makeReadOnly, CancellationToken cancellationToken);

    Task<DocumentChange> SetBlockLockAsync(DocumentId id, string blockKey, bool locked, CancellationToken cancellationToken);

    Task<DocumentDetail?> DocumentAsync(DocumentId id, CancellationToken cancellationToken);

    Task<DocumentRevisionDetail?> RevisionAsync(RevisionId id, CancellationToken cancellationToken);
}