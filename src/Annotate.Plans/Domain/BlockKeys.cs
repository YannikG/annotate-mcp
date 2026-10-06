using System.Security.Cryptography;
using System.Text;

using Annotate.Markdown;

namespace Annotate.Plans.Domain;

internal static class BlockKeys
{
    public const int SectionPathLength = 2000;

    public const int KindLength = 16;

    public const int HashLength = 64;

    public const int KeyLength = 36;

    public static IReadOnlyList<StoredBlock> Assign(
        string revisionId,
        string markdown,
        IReadOnlyList<Block> blocks,
        IReadOnlyList<StoredBlock> parentBlocks)
    {
        Candidate[] candidates = new Candidate[blocks.Count];
        Dictionary<string, int> positions = new(StringComparer.Ordinal);
        for (int ordinal = 0; ordinal < blocks.Count; ordinal++)
        {
            Block block = blocks[ordinal];
            string path = Cut(block.SectionPath);
            int position = positions.GetValueOrDefault(path);
            positions[path] = position + 1;
            candidates[ordinal] = new Candidate(
                ordinal,
                block.Kind.ToString(),
                path,
                block.Start,
                block.End,
                ContentHash(markdown, block.Start, block.End),
                position);
        }

        List<ParentSlot> parents = ParentSlots(parentBlocks);
        string?[] keys = new string?[candidates.Length];
        for (int index = 0; index < candidates.Length; index++)
        {
            Candidate candidate = candidates[index];
            ParentSlot? match = Earliest(
                parents,
                slot => slot.Hash == candidate.Hash && slot.Path == candidate.Path);
            if (match is null)
            {
                continue;
            }

            match.Used = true;
            keys[index] = match.Key;
        }

        for (int index = 0; index < candidates.Length; index++)
        {
            if (keys[index] is not null)
            {
                continue;
            }

            Candidate candidate = candidates[index];
            ParentSlot? match = Earliest(
                parents,
                slot => slot.Path == candidate.Path && slot.Position == candidate.Position);
            if (match is not null)
            {
                match.Used = true;
            }

            keys[index] = match is null ? Guid.NewGuid().ToString() : match.Key;
        }

        List<StoredBlock> stored = new(candidates.Length);
        foreach (Candidate candidate in candidates)
        {
            stored.Add(new StoredBlock(
                revisionId,
                candidate.Ordinal,
                keys[candidate.Ordinal]!,
                candidate.Kind,
                candidate.Path,
                candidate.Start,
                candidate.End,
                candidate.Hash));
        }

        return stored;
    }

    private static List<ParentSlot> ParentSlots(IReadOnlyList<StoredBlock> parentBlocks)
    {
        List<ParentSlot> parents = new(parentBlocks.Count);
        Dictionary<string, int> positions = new(StringComparer.Ordinal);
        foreach (StoredBlock block in parentBlocks.OrderBy(block => block.Ordinal))
        {
            int position = positions.GetValueOrDefault(block.SectionPath);
            positions[block.SectionPath] = position + 1;
            parents.Add(new ParentSlot(block.BlockKey, block.SectionPath, block.ContentHash, position));
        }

        return parents;
    }

    private static ParentSlot? Earliest(List<ParentSlot> parents, Func<ParentSlot, bool> match)
    {
        foreach (ParentSlot parent in parents)
        {
            if (!parent.Used && match(parent))
            {
                return parent;
            }
        }

        return null;
    }

    private readonly record struct Candidate(
        int Ordinal,
        string Kind,
        string Path,
        int Start,
        int End,
        string Hash,
        int Position);

    private sealed class ParentSlot(string key, string path, string hash, int position)
    {
        public string Key { get; } = key;

        public string Path { get; } = path;

        public string Hash { get; } = hash;

        public int Position { get; } = position;

        public bool Used { get; set; }
    }

    public static string Cut(string sectionPath) =>
        sectionPath.Length <= SectionPathLength ? sectionPath : sectionPath[..SectionPathLength];

    public static string ContentHash(string source, int start, int end)
    {
        int hashEnd = end;
        while (hashEnd > start && source[hashEnd - 1] is '\r' or '\n')
        {
            hashEnd--;
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(source[start..hashEnd]));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}