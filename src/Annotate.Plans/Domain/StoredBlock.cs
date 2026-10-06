namespace Annotate.Plans.Domain;

internal sealed class StoredBlock
{
    public string RevisionId { get; private set; }

    public int Ordinal { get; private set; }

    public string BlockKey { get; private set; }

    public string Kind { get; private set; }

    public string SectionPath { get; private set; }

    public int SourceStart { get; private set; }

    public int SourceEnd { get; private set; }

    public string ContentHash { get; private set; }

    public StoredBlock(
        string revisionId,
        int ordinal,
        string blockKey,
        string kind,
        string sectionPath,
        int sourceStart,
        int sourceEnd,
        string contentHash)
    {
        RevisionId = revisionId;
        Ordinal = ordinal;
        BlockKey = blockKey;
        Kind = kind;
        SectionPath = sectionPath;
        SourceStart = sourceStart;
        SourceEnd = sourceEnd;
        ContentHash = contentHash;
    }

    private StoredBlock()
    {
        RevisionId = "";
        BlockKey = "";
        Kind = "";
        SectionPath = "";
        ContentHash = "";
    }
}