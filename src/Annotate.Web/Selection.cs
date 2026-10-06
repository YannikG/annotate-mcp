namespace Annotate.Web;

public interface ISelectionReader
{
    Task<TextSelection?> ReadAsync();
}

public sealed record SelectionBox(double Top, double Left, double Bottom, double Width);

public sealed record TextSelection(
    int BlockOrdinal,
    int Start,
    int End,
    string Text,
    SelectionBox? Box = null);