namespace Annotate.Web.Components.Review;

public sealed record AnnotationConfirm(string Title, string Body, Func<Task> Action);