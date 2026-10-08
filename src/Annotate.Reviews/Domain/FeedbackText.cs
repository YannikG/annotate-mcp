using System.Text;

namespace Annotate.Reviews.Domain;

internal static class FeedbackText
{
    public static string Write(
        IReadOnlyList<AnnotationDraft> annotations,
        IReadOnlyList<string> fenceIds,
        IReadOnlyDictionary<string, string> answers)
    {
        string body = annotations.Count == 0 ? "Plan changes requested." : Markup(annotations);
        if (fenceIds.Count == 0)
        {
            return body;
        }

        StringBuilder builder = new(body);
        builder.Append("\n\n### Decisions\n");
        foreach (string fenceId in fenceIds)
        {
            string answer = answers.TryGetValue(fenceId, out string? stored) ? stored : "(unanswered)";
            builder.Append('\n').Append("- ").Append(fenceId).Append(": ").Append(answer);
        }

        return builder.ToString();
    }

    private static string Markup(IReadOnlyList<AnnotationDraft> annotations)
    {
        StringBuilder builder = new();
        builder.Append("## Plan Review Feedback\n\n");
        builder.Append("Apply the following anchored review comments before proceeding.\n\n");
        builder.Append("### Suggested Changes\n\n");
        for (int index = 0; index < annotations.Count; index++)
        {
            if (index > 0)
            {
                builder.Append('\n');
            }

            builder.Append(index + 1).Append(". ").Append(Entry(annotations[index]));
        }

        builder.Append("\n\nPlease revise the plan to address this feedback and submit the revised draft again.");
        return builder.ToString();
    }

    private static string Entry(AnnotationDraft annotation)
    {
        StringBuilder builder = new(annotation.BlockKey is null ? Phrase(annotation) : Block(annotation));
        builder.Append('\n').Append("from: ").Append(annotation.Author);
        if (annotation.Replies is { Count: > 0 })
        {
            foreach (string reply in annotation.Replies)
            {
                builder.Append('\n').Append("- ").Append(reply);
            }
        }

        return builder.ToString();
    }

    private static string Block(AnnotationDraft annotation) =>
        "blockId: " + annotation.BlockKey + "\n" + (annotation.Comment ?? "");

    private static string Phrase(AnnotationDraft annotation)
    {
        string tail = "{id=\"" + EscapeToken(annotation.Id) + "\" by=\"user\" at=\"" + EscapeToken(annotation.CreatedAt) + "\"}";
        return annotation.Kind switch
        {
            "Deletion" => "{--" + Escape(annotation.Text, "--") + "--}}" + tail,
            "Replacement" => "{~~"
                + Escape(annotation.Text, "~~", "~>")
                + "~>"
                + Escape(annotation.Replacement ?? "", "~~", "~>")
                + "~~}}"
                + tail,
            "Insertion" => "After {=="
                + Escape(annotation.Text, "==")
                + "==}}, insert {++"
                + Escape(annotation.Replacement ?? "", "++")
                + "++}}"
                + tail,
            "Comment" => "{=="
                + Escape(annotation.Text, "==")
                + "==}}{{>>"
                + Escape(annotation.Comment ?? "", ">>", "<<")
                + "<<}}"
                + tail,
            _ => throw new InvalidOperationException("Annotation kind was not recognised."),
        };
    }

    private static string Escape(string value, params string[] delimiters)
    {
        string[] ordered = delimiters.OrderByDescending(delimiter => delimiter.Length).ToArray();
        StringBuilder builder = new(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            string? match = null;
            foreach (string delimiter in ordered)
            {
                if (value.AsSpan(index).StartsWith(delimiter))
                {
                    match = delimiter;
                    break;
                }
            }

            if (match is null)
            {
                builder.Append(value[index]);
                index++;
                continue;
            }

            builder.Append("[escaped ").Append(match).Append(']');
            index += match.Length;
        }

        return builder.ToString();
    }

    private static string EscapeToken(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}