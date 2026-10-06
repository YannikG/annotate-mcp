using Annotate.Markdown;

namespace Annotate.Web.Components.Review;

internal static class ListMarkers
{
    public static bool Ordered(IReadOnlyList<ListItem> items) =>
        items.Count > 0 && items[0].Marker.Length > 0 && char.IsAsciiDigit(items[0].Marker[0]);
}