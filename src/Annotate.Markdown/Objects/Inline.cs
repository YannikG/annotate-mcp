namespace Annotate.Markdown;

public abstract record Inline
{
    public int Start { get; }

    public int End { get; }

    private protected Inline(int start, int end)
    {
        Start = start;
        End = end;
    }
}

public sealed record TextInline : Inline
{
    public TextInline(int start, int end)
        : base(start, end)
    {
    }
}

public sealed record EmphasisInline : Inline
{
    public int Level { get; }

    public IReadOnlyList<Inline> Children { get; }

    public EmphasisInline(int level, IReadOnlyList<Inline> children, int start, int end)
        : base(start, end)
    {
        Level = level;
        Children = children;
    }
}

public sealed record StrikeInline : Inline
{
    public IReadOnlyList<Inline> Children { get; }

    public StrikeInline(IReadOnlyList<Inline> children, int start, int end)
        : base(start, end)
    {
        Children = children;
    }
}

public sealed record CodeInline : Inline
{
    public string Text { get; }

    public CodeInline(string text, int start, int end)
        : base(start, end)
    {
        Text = text;
    }
}

public sealed record LinkInline : Inline
{
    public IReadOnlyList<Inline> Children { get; }

    public string? Destination { get; }

    public LinkInline(IReadOnlyList<Inline> children, string? destination, int start, int end)
        : base(start, end)
    {
        Children = children;
        Destination = destination;
    }
}

public sealed record ImageInline : Inline
{
    public IReadOnlyList<Inline> Children { get; }

    public string? Destination { get; }

    public ImageInline(IReadOnlyList<Inline> children, string? destination, int start, int end)
        : base(start, end)
    {
        Children = children;
        Destination = destination;
    }
}