namespace Annotate.Reviews.Domain;

internal static class ReviewLimits
{
    public const int Id = 36;

    public const int Status = 18;

    public const int Feedback = 1_500_000;

    public const int FenceId = 80;

    public const int DecisionKind = 8;

    public const int Prompt = 4000;

    public const int Label = 4000;

    public const int Answer = 4000;

    public const int AnnotationId = 200;

    public const int AnnotationKind = 16;

    public const int Text = 4000;

    public const int Note = 4000;

    public const int CreatedAt = 40;

    public const int MaxAnnotations = 100;
}