namespace Annotate.Markdown;

internal readonly record struct ReadResult(List<Block> Blocks, string? Error);

internal static class BlockReader
{
    public static ReadResult Read(
        string source,
        IReadOnlyList<SourceLine> lines,
        int index,
        int limit,
        SectionPath sections,
        DecisionLog decisions)
    {
        List<Block> blocks = [];
        while (index < limit)
        {
            SourceLine line = lines[index];
            if (line.IsBlank(source))
            {
                index++;
                continue;
            }

            if (FenceScanner.TryRead(source, lines, index, limit, sections.Current, out CodeBlock? code, out int afterFence)
                && code is not null)
            {
                string? decisionError = decisions.Accept(code);
                if (decisionError is not null)
                {
                    return new ReadResult([], decisionError);
                }

                blocks.Add(code);
                index = afterFence;
                continue;
            }

            if (HeadingScanner.TryAtx(source, line, out int level, out string text, out int contentStart))
            {
                blocks.Add(new HeadingBlock(
                    level,
                    text,
                    line.Start,
                    line.End,
                    sections.Push(level, text),
                    InlineReader.Read(source, contentStart, line.ContentEnd)));
                index++;
                continue;
            }

            if (QuoteScanner.TryRead(
                    source,
                    lines,
                    index,
                    limit,
                    sections,
                    decisions,
                    out QuoteBlock? quote,
                    out int afterQuote,
                    out string? quoteError))
            {
                if (quoteError is not null)
                {
                    return new ReadResult([], quoteError);
                }

                blocks.Add(quote!);
                index = afterQuote;
                continue;
            }

            if (DetailsScanner.TryRead(
                    source,
                    lines,
                    index,
                    limit,
                    sections,
                    decisions,
                    out DetailsBlock? details,
                    out int afterDetails,
                    out string? detailsError))
            {
                if (detailsError is not null)
                {
                    return new ReadResult([], detailsError);
                }

                blocks.Add(details!);
                index = afterDetails;
                continue;
            }

            if (RuleScanner.TryRead(source, line, sections.Current, out RuleBlock? rule) && rule is not null)
            {
                blocks.Add(rule);
                index++;
                continue;
            }

            if (TableScanner.TryRead(source, lines, index, limit, sections.Current, out TableBlock? table, out int afterTable)
                && table is not null)
            {
                blocks.Add(table);
                index = afterTable;
                continue;
            }

            if (ListScanner.TryRead(source, lines, index, limit, sections.Current, out ListBlock? list, out int next)
                && list is not null)
            {
                blocks.Add(list);
                index = next;
                continue;
            }

            index = ReadParagraph(source, lines, index, limit, sections, blocks);
        }

        return new ReadResult(blocks, null);
    }

    private static int ReadParagraph(
        string source,
        IReadOnlyList<SourceLine> lines,
        int index,
        int limit,
        SectionPath sections,
        List<Block> blocks)
    {
        int start = index;
        index++;
        while (index < limit && !lines[index].IsBlank(source))
        {
            if (index == start + 1
                && HeadingScanner.TrySetextLevel(source, lines[index], out int setextLevel))
            {
                string headingText = source[lines[start].Start..lines[start].ContentEnd].Trim();
                blocks.Add(new HeadingBlock(
                    setextLevel,
                    headingText,
                    lines[start].Start,
                    lines[index].End,
                    sections.Push(setextLevel, headingText),
                    InlineReader.Read(source, lines[start].Start, lines[start].ContentEnd)));
                return index + 1;
            }

            if (Interrupts(source, lines, index, limit))
            {
                break;
            }

            index++;
        }

        int end = lines[index - 1].End;
        int contentEnd = lines[index - 1].ContentEnd;
        blocks.Add(new ParagraphBlock(
            source[lines[start].Start..end],
            lines[start].Start,
            end,
            sections.Current,
            InlineReader.Read(source, lines[start].Start, contentEnd)));
        return index;
    }

    private static bool Interrupts(string source, IReadOnlyList<SourceLine> lines, int index, int limit)
    {
        SourceLine line = lines[index];
        return HeadingScanner.TryAtx(source, line, out _, out _, out _)
            || ListScanner.StartsItem(line.Content(source))
            || FenceScanner.StartsFence(line.Content(source))
            || QuoteScanner.Starts(line.Content(source))
            || DetailsScanner.Starts(line.Content(source))
            || TableScanner.Starts(source, lines, index, limit)
            || RuleScanner.IsRule(line.Content(source));
    }
}