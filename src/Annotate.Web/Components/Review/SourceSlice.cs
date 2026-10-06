namespace Annotate.Web.Components.Review;

internal static class SourceSlice
{
    public static string Read(string source, int start, int end)
    {
        if ((uint)start > (uint)source.Length || end < start || end > source.Length)
        {
            return "";
        }

        return source[start..end];
    }
}