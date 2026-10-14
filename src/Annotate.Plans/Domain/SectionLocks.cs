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
            int previousIndex = IndexOf(parent, key);
            if (previousIndex < 0)
            {
                continue;
            }

            int currentIndex = IndexOf(next, key);
            if (currentIndex < 0)
            {
                return true;
            }

            StoredBlock previous = parent[previousIndex];
            StoredBlock current = next[currentIndex];
            if (previous.ContentHash != current.ContentHash || previous.SectionPath != current.SectionPath)
            {
                return true;
            }

            if (previous.Kind != "Heading")
            {
                continue;
            }

            int previousEnd = EndOfSection(parent, previousIndex);
            int currentEnd = EndOfSection(next, currentIndex);
            if (previousEnd - previousIndex != currentEnd - currentIndex)
            {
                return true;
            }

            for (int offset = 0; previousIndex + offset < previousEnd; offset++)
            {
                StoredBlock before = parent[previousIndex + offset];
                StoredBlock after = next[currentIndex + offset];
                if (before.BlockKey != after.BlockKey || before.ContentHash != after.ContentHash)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static HashSet<string> Covered(IReadOnlyList<StoredBlock> blocks, IReadOnlySet<string> locks)
    {
        HashSet<string> covered = new(locks, StringComparer.Ordinal);
        foreach (string key in locks)
        {
            int start = IndexOf(blocks, key);
            if (start < 0 || blocks[start].Kind != "Heading")
            {
                continue;
            }

            for (int index = start; index < EndOfSection(blocks, start); index++)
            {
                covered.Add(blocks[index].BlockKey);
            }
        }

        return covered;
    }

    private static int EndOfSection(IReadOnlyList<StoredBlock> blocks, int heading)
    {
        int index = heading + 1;
        while (index < blocks.Count && blocks[index].Kind != "Heading")
        {
            index++;
        }

        return index;
    }

    private static int IndexOf(IReadOnlyList<StoredBlock> blocks, string key)
    {
        for (int index = 0; index < blocks.Count; index++)
        {
            if (blocks[index].BlockKey == key)
            {
                return index;
            }
        }

        return -1;
    }
}