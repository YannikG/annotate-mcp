namespace Annotate.Markdown;

internal sealed class SectionPath
{
    private readonly List<Segment> _segments = [];

    public string Current => string.Join('/', _segments.Select(segment => segment.Text));

    public string Push(int level, string text)
    {
        while (_segments.Count > 0 && _segments[^1].Level >= level)
        {
            _segments.RemoveAt(_segments.Count - 1);
        }

        _segments.Add(new Segment(level, text));
        return Current;
    }

    private readonly record struct Segment(int Level, string Text);
}