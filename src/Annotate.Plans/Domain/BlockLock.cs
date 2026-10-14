namespace Annotate.Plans.Domain;

internal sealed class BlockLock
{
    public string DocumentId { get; private set; }

    public string BlockKey { get; private set; }

    public BlockLock(string documentId, string blockKey)
    {
        DocumentId = documentId;
        BlockKey = blockKey;
    }

    private BlockLock()
    {
        DocumentId = "";
        BlockKey = "";
    }
}