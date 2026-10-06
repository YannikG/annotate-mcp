namespace Annotate.Markdown;

public static class PlanMarkdown
{
    public static string Guide { get; } =
        """
        Plans are Markdown.

        A heading is one to six # marks, or one title line over === (level 1) or --- (level 2). A line of ---, ***, or ___ (at least three, spaces allowed) ends the paragraph and is a horizontal rule when it is not a setext underline. The same line at the start of the file, or after a blank line, is a horizontal rule. One title line over --- stays a level-2 heading.

        Blank lines separate blocks. The lines between them are a paragraph.

        A list item starts with -, *, +, or a number and a dot. - [ ] and - [x] are tasks. Indent a child item under its parent.

        A blockquote line starts with >. What follows is parsed as blocks, so the quote can hold a list or a nested blockquote written as > >.

        A pipe table has a header row, a separator row of dashes, and body rows. :--- aligns left, :---: centers, ---: aligns right, and --- sets none. Cell text uses the same inline marks as a paragraph.

        <details> folds the blocks that follow. Put <summary> on the next line, the summary text, </summary>, the child blocks, and </details>. Summary text is inline. A <script> tag, or any other HTML tag, stays paragraph text.

        Fences open and close with ``` or ~~~. The word after the opening mark is the language. diff, patch, and mermaid keep a literal body, which is not split into inline tokens. A longer fence can contain a shorter one, including inside details.

        Inline marks are *emphasis*, **strong**, ~~strike~~, `code`, [label](https://example.com/a), and ![alt](https://example.com/a.png).

        A decision fence is a code fence with language decision, in any case. It stays a code block and asks one question. It can sit inside details.

        ```
        id: storage
        kind: choice
        prompt: Which store?
        - SQLite
        - Postgres
        ```

        id is a short token: a letter or digit, then letters, digits, underscores, or hyphens. kind is choice or text. prompt is the question. A choice lists options as lines that start with "- ". A label that is only Other is dropped, because the page adds Other. A text question has no option lines. Each id appears once in the plan.
        """;

    public static ParseOutcome Parse(string source)
    {
        List<SourceLine> lines = LineReader.Read(source);
        DecisionLog decisions = new();
        ReadResult read = BlockReader.Read(source, lines, 0, lines.Count, new SectionPath(), decisions);
        if (read.Error is not null)
        {
            return new ParseOutcome.Invalid(read.Error);
        }

        return new ParseOutcome.Ok(new PlanDocument(read.Blocks, decisions.Prompts));
    }
}