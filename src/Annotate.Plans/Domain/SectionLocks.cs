namespace Annotate.Plans.Domain;

internal static class SectionLocks
{
    public static bool Broken(
        IReadOnlyList<string> locks,
        IReadOnlyList<StoredBlock> parent,
        IReadOnlyList<StoredBlock> next)
    {
        foreach (string key in locks)
        {
            StoredBlock? previous = parent.FirstOrDefault(block => block.BlockKey == key);
            StoredBlock? current = next.FirstOrDefault(block => block.BlockKey == key);
            if (previous is null
                || current is null
                || previous.ContentHash != current.ContentHash
                || previous.SectionPath != current.SectionPath)
            {
                return true;
            }
        }

        foreach (string path in HeadingPaths(locks, parent))
        {
            StoredBlock[] before = parent.Where(block => block.SectionPath == path).ToArray();
            StoredBlock[] after = next.Where(block => block.SectionPath == path).ToArray();
            if (before.Length != after.Length)
            {
                return true;
            }

            for (int index = 0; index < before.Length; index++)
            {
                if (before[index].BlockKey != after[index].BlockKey
                    || before[index].ContentHash != after[index].ContentHash)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static HashSet<string> Covered(IReadOnlyList<StoredBlock> blocks, IReadOnlySet<string> locks)
    {
        HashSet<string> paths = HeadingPaths(locks, blocks);
        HashSet<string> covered = new(locks, StringComparer.Ordinal);
        foreach (StoredBlock block in blocks)
        {
            if (paths.Contains(block.SectionPath))
            {
                covered.Add(block.BlockKey);
            }
        }

        return covered;
    }

    private static HashSet<string> HeadingPaths(IReadOnlyCollection<string> locks, IReadOnlyList<StoredBlock> blocks)
    {
        HashSet<string> paths = new(StringComparer.Ordinal);
        foreach (StoredBlock block in blocks)
        {
            if (block.Kind == "Heading" && locks.Contains(block.BlockKey))
            {
                paths.Add(block.SectionPath);
            }
        }

        return paths;
    }
}