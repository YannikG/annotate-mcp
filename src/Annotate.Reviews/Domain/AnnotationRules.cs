namespace Annotate.Reviews.Domain;

internal static class AnnotationRules
{
    public static string? Reject(IReadOnlyList<AnnotationDraft> annotations)
    {
        if (annotations.Count > ReviewLimits.MaxAnnotations)
        {
            return "Too many annotations.";
        }

        foreach (AnnotationDraft annotation in annotations)
        {
            if (!Accepts(annotation))
            {
                return "Annotation was rejected.";
            }
        }

        return null;
    }

    private static bool Accepts(AnnotationDraft annotation)
    {
        if (annotation.Id.Length is < 1 or > ReviewLimits.AnnotationId)
        {
            return false;
        }

        if (annotation.Text.Length is < 1 or > ReviewLimits.Text)
        {
            return false;
        }

        if (annotation.CreatedAt.Length is < 1 or > ReviewLimits.CreatedAt)
        {
            return false;
        }

        if (annotation.BlockOrdinal < 0 || annotation.StartOffset < 0 || annotation.EndOffset < 0)
        {
            return false;
        }

        if (annotation.Kind is not ("Deletion" or "Replacement" or "Insertion" or "Comment"))
        {
            return false;
        }

        if (TooLong(annotation.Comment) || TooLong(annotation.Replacement))
        {
            return false;
        }

        if (annotation.Kind is "Replacement" or "Insertion" && string.IsNullOrEmpty(annotation.Replacement))
        {
            return false;
        }

        return annotation.Kind != "Comment" || !string.IsNullOrEmpty(annotation.Comment);
    }

    private static bool TooLong(string? value) => value is not null && value.Length > ReviewLimits.Note;
}