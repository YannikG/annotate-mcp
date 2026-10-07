namespace Annotate.Web;

internal static class IdCopy
{
    public const string PlanCopied = "Plan ID copied";

    public const string ReviewCopied = "Review ID copied";

    public const string BlockCopied = "Block ID copied";

    public const string Failed = "Could not copy";

    public static string Plan(string id) => $"planId: {id}";

    public static string Review(string id) => $"reviewId: {id}";

    public static string Block(string reviewId, string blockKey) => $"reviewId: {reviewId}, blockId: {blockKey}";
}