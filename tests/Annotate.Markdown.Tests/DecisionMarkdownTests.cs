using Annotate.Markdown;

namespace Annotate.Markdown.Tests;

public sealed class DecisionMarkdownTests
{
    [Fact]
    public void ChoiceAndTextFencesStayCodeBlocksAndFillDecisions()
    {
        string source =
            "```decision\n"
            + "id: storage\n"
            + "kind: choice\n"
            + "prompt: Which store?\n"
            + "\n"
            + "- SQLite\n"
            + "- Other\n"
            + "- Postgres\n"
            + "```\n"
            + "<details>\n"
            + "<summary>Pick</summary>\n"
            + "```DECISION\n"
            + "id: inside\n"
            + "kind: choice\n"
            + "prompt: Go?\n"
            + "- Yes\n"
            + "- oThEr\n"
            + "```\n"
            + "</details>\n"
            + "```decision\n"
            + "id: note\n"
            + "kind: text\n"
            + "prompt: Anything else?\n"
            + "```\n";

        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));
        CodeBlock storageCode = Assert.IsType<CodeBlock>(ok.Document.Blocks[0]);
        DetailsBlock details = Assert.IsType<DetailsBlock>(ok.Document.Blocks[1]);
        CodeBlock insideCode = Assert.IsType<CodeBlock>(Assert.Single(details.Children));
        CodeBlock noteCode = Assert.IsType<CodeBlock>(ok.Document.Blocks[2]);

        Assert.Equal("decision", storageCode.Language);
        Assert.Equal("DECISION", insideCode.Language);
        Assert.Equal("decision", noteCode.Language);
        Assert.Contains("id: storage\n", storageCode.Body, StringComparison.Ordinal);
        Assert.Contains("- Other\n", storageCode.Body, StringComparison.Ordinal);

        Assert.Equal(3, ok.Document.Decisions.Count);
        DecisionPrompt storage = ok.Document.Decisions[0];
        Assert.Equal("storage", storage.Id);
        Assert.Equal(DecisionKind.Choice, storage.Kind);
        Assert.Equal(["SQLite", "Postgres"], storage.Options);
        Assert.Equal(storageCode.Start, storage.Start);
        Assert.Equal(storageCode.End, storage.End);

        DecisionPrompt inside = ok.Document.Decisions[1];
        Assert.Equal("inside", inside.Id);
        Assert.Equal(DecisionKind.Choice, inside.Kind);
        Assert.Equal(["Yes"], inside.Options);
        Assert.Equal(insideCode.Start, inside.Start);
        Assert.Equal(insideCode.End, inside.End);

        DecisionPrompt note = ok.Document.Decisions[2];
        Assert.Equal("note", note.Id);
        Assert.Equal(DecisionKind.Text, note.Kind);
        Assert.Empty(note.Options);
        Assert.Equal(noteCode.Start, note.Start);
        Assert.Equal(noteCode.End, note.End);
    }

    [Fact]
    public void IdOfEightyCharactersIsAccepted()
    {
        string id = "a" + new string('b', 79);
        string source = "```decision\nid: " + id + "\nkind: text\nprompt: Why?\n```\n";
        ParseOutcome.Ok ok = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(source));

        Assert.Equal(id, Assert.Single(ok.Document.Decisions).Id);
    }

    [Theory]
    [MemberData(nameof(InvalidDecisions))]
    public void InvalidDecisionFenceReturnsTheMatchingError(string source, string error)
    {
        ParseOutcome.Invalid invalid = Assert.IsType<ParseOutcome.Invalid>(PlanMarkdown.Parse(source));

        Assert.Equal(error, invalid.Error);
    }

    public static IEnumerable<object[]> InvalidDecisions()
    {
        yield return Row("hello\n", "Error: decision block 1: unexpected line");
        yield return Row("id: bad id\nid: storage\n", "Error: decision block 1: unexpected line");
        yield return Row("id: storage\nhello\n", "Error: decision \"storage\": unexpected line");
        yield return Row("id: storage\nid: other\n", "Error: decision \"storage\": unexpected line");
        yield return Row(
            "id: storage\n- Yes\nkind: choice\n",
            "Error: decision \"storage\": unexpected line");
        yield return Row("- Yes\nid: storage\n", "Error: decision block 1: unexpected line");
        yield return Row("kind: choice\nprompt: Which?\n", "Error: decision block 1: missing id");
        yield return Row("id:\nkind: choice\nprompt: Which?\n", "Error: decision block 1: missing id");
        yield return Row("id: has space\nkind: choice\nprompt: Which?\n", "Error: decision block 1: unexpected line");
        yield return Row("id: " + new string('a', 81) + "\nkind: text\nprompt: Why?\n", "Error: decision block 1: unexpected line");
        yield return Row("id: storage\nprompt: Which?\n", "Error: decision \"storage\": missing kind");
        yield return Row("id: storage\nkind:\nprompt: Which?\n", "Error: decision \"storage\": missing kind");
        yield return Row("id: storage\nkind: widget\nprompt: Which?\n", "Error: decision \"storage\": unknown kind \"widget\"");
        yield return Row("kind: widget\nid: storage\nprompt: Which?\n", "Error: decision \"storage\": unknown kind \"widget\"");
        yield return Row("id: storage\nkind: Choice\nprompt: Which?\n", "Error: decision \"storage\": unknown kind \"Choice\"");
        yield return Row("id: storage\nkind: foo-bar\nprompt: Which?\n", "Error: decision \"storage\": unknown kind \"foo-bar\"");
        yield return Row(
            "id: storage\nkind: " + new string('b', 40) + "\nprompt: Which?\n",
            "Error: decision \"storage\": unknown kind \"" + new string('b', 40) + "\"");
        yield return Row("id: storage\nkind: " + new string('b', 41) + "\nprompt: Which?\n", "Error: decision \"storage\": unexpected line");
        yield return Row("id: storage\nkind: 123\nprompt: Which?\n", "Error: decision \"storage\": unexpected line");
        yield return Row("id: storage\nkind: choice\n", "Error: decision \"storage\": missing prompt");
        yield return Row("id: storage\nkind: text\nprompt:\n", "Error: decision \"storage\": missing prompt");
        yield return Row("id: storage\nkind: text\nprompt: " + new string('p', 4001) + "\n", "Error: decision \"storage\": unexpected line");
        yield return Row("id: storage\n- Yes\n", "Error: decision \"storage\": missing kind");
        yield return Row("- Yes\n", "Error: decision block 1: missing id");
        yield return Row(
            "id: storage\nkind: text\nprompt: Which?\n- " + new string('x', 4001) + "\n",
            "Error: decision \"storage\": text questions cannot list options");
        yield return Row("id: storage\nkind: text\nprompt: Which?\n- Yes\n", "Error: decision \"storage\": text questions cannot list options");
        yield return Row("id: storage\nkind: choice\nprompt: Which?\n-   \n", "Error: decision \"storage\": unexpected line");
        yield return Row(
            "id: storage\nkind: choice\nprompt: Which?\n- " + new string('x', 4001) + "\n",
            "Error: decision \"storage\": unexpected line");
        yield return Row(
            "id: storage\nkind: choice\nprompt: Which?\n- Other\n- other\n",
            "Error: decision \"storage\": choice has no option left after dropping Other");
        yield return [
            "```csharp\nint x = 1;\n```\n```decision\nnope\n```\n",
            "Error: decision block 1: unexpected line",
        ];
        yield return [
            "```Decision\nnope\n```\n",
            "Error: decision block 1: unexpected line",
        ];
        yield return [
            Fence("id: storage\nkind: text\nprompt: Why?\n")
                + "<details>\n<summary>s</summary>\n```decision\nnope\n```\n</details>\n",
            "Error: decision block 2: unexpected line",
        ];
        yield return [
            Fence("id: storage\nkind: text\nprompt: Why?\n")
                + Fence("id: storage\nkind: choice\nprompt: Which?\n- Yes\n"),
            "Error: decision \"storage\": duplicate id",
        ];
    }

    private static object[] Row(string body, string error) => [Fence(body), error];

    private static string Fence(string body) => "```decision\n" + body + "```\n";
}