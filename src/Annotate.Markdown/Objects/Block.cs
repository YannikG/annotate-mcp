namespace Annotate.Markdown;

public enum BlockKind
{
    Heading,
    Paragraph,
    List,
    Code,
    Quote,
    Table,
    Rule,
    Details,
}

public abstract record Block
{
    public BlockKind Kind { get; }

    public int Start { get; }

    public int End { get; }

    public string SectionPath { get; }

    private protected Block(BlockKind kind, int start, int end, string sectionPath)
    {
        Kind = kind;
        Start = start;
        End = end;
        SectionPath = sectionPath;
    }
}

public sealed record HeadingBlock : Block
{
    public int Level { get; }

    public string Text { get; }

    public IReadOnlyList<Inline> Inlines { get; }

    public HeadingBlock(
        int level,
        string text,
        int start,
        int end,
        string sectionPath,
        IReadOnlyList<Inline> inlines)
        : base(BlockKind.Heading, start, end, sectionPath)
    {
        Level = level;
        Text = text;
        Inlines = inlines;
    }
}

public sealed record ParagraphBlock : Block
{
    public string Text { get; }

    public IReadOnlyList<Inline> Inlines { get; }

    public ParagraphBlock(string text, int start, int end, string sectionPath, IReadOnlyList<Inline> inlines)
        : base(BlockKind.Paragraph, start, end, sectionPath)
    {
        Text = text;
        Inlines = inlines;
    }
}

public sealed record ListBlock : Block
{
    public IReadOnlyList<ListItem> Items { get; }

    public ListBlock(IReadOnlyList<ListItem> items, int start, int end, string sectionPath)
        : base(BlockKind.List, start, end, sectionPath)
    {
        Items = items;
    }
}

public sealed record CodeBlock : Block
{
    public string Language { get; }

    public string Body { get; }

    public CodeBlock(string language, string body, int start, int end, string sectionPath)
        : base(BlockKind.Code, start, end, sectionPath)
    {
        Language = language;
        Body = body;
    }
}

public sealed record ListItem(
    string Marker,
    bool? Checked,
    string Text,
    IReadOnlyList<ListItem> Items,
    IReadOnlyList<Inline> Inlines);

public sealed record QuoteBlock : Block
{
    public IReadOnlyList<Block> Children { get; }

    public QuoteBlock(IReadOnlyList<Block> children, int start, int end, string sectionPath)
        : base(BlockKind.Quote, start, end, sectionPath)
    {
        Children = children;
    }
}

public enum ColumnAlignment
{
    None,
    Left,
    Center,
    Right,
}

public sealed record TableCell(IReadOnlyList<Inline> Inlines);

public sealed record TableBlock : Block
{
    public IReadOnlyList<TableCell> Header { get; }

    public IReadOnlyList<ColumnAlignment> Alignments { get; }

    public IReadOnlyList<IReadOnlyList<TableCell>> Rows { get; }

    public TableBlock(
        IReadOnlyList<TableCell> header,
        IReadOnlyList<ColumnAlignment> alignments,
        IReadOnlyList<IReadOnlyList<TableCell>> rows,
        int start,
        int end,
        string sectionPath)
        : base(BlockKind.Table, start, end, sectionPath)
    {
        Header = header;
        Alignments = alignments;
        Rows = rows;
    }
}

public sealed record RuleBlock : Block
{
    public RuleBlock(int start, int end, string sectionPath)
        : base(BlockKind.Rule, start, end, sectionPath)
    {
    }
}

public sealed record DetailsBlock : Block
{
    public IReadOnlyList<Inline> Summary { get; }

    public IReadOnlyList<Block> Children { get; }

    public DetailsBlock(
        IReadOnlyList<Inline> summary,
        IReadOnlyList<Block> children,
        int start,
        int end,
        string sectionPath)
        : base(BlockKind.Details, start, end, sectionPath)
    {
        Summary = summary;
        Children = children;
    }
}