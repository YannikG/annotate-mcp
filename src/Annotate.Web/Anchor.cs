using System.Globalization;

namespace Annotate.Web;

internal readonly record struct AnchorPoint(double Top, double Left);

internal static class Anchor
{
    public static AnchorPoint Above(SelectionBox box)
    {
        double top = box.Top - 44;
        if (top < 60)
        {
            top = box.Bottom + 8;
        }

        return new AnchorPoint(top, box.Left);
    }

    public static AnchorPoint Below(SelectionBox box) => new(box.Bottom + 8, box.Left);

    public static Dictionary<string, object> Attributes(AnchorPoint? point)
    {
        if (point is null)
        {
            return [];
        }

        return new Dictionary<string, object>
        {
            ["data-top"] = point.Value.Top.ToString(CultureInfo.InvariantCulture),
            ["data-left"] = point.Value.Left.ToString(CultureInfo.InvariantCulture),
        };
    }
}