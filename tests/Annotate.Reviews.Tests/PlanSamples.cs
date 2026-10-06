namespace Annotate.Reviews.Tests;

internal static class PlanSamples
{
    public const string Plain = "# Plan\n\nShip the change.\n";

    public const string InvalidDecision = "```decision\nkind: choice\n```\n";

    public const string Decisions =
        """
        # Storage

        ```decision
        id: storage
        kind: choice
        prompt: Which store?
        - SQLite
        - Other
        - Postgres
        ```

        ```decision
        id: note
        kind: text
        prompt: Anything else?
        ```
        """;

    public const string Choice =
        """
        # Storage

        ```decision
        id: storage
        kind: choice
        prompt: Which store?
        - SQLite
        - Postgres
        ```
        """;
}