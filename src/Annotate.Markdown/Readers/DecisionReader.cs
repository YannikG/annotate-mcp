namespace Annotate.Markdown;

internal static class DecisionReader
{
    public static bool TryRead(
        CodeBlock code,
        int number,
        IReadOnlySet<string> usedIds,
        out DecisionPrompt? prompt,
        out string? error)
    {
        prompt = null;
        Dictionary<string, string> fields = [];
        List<string> options = [];
        bool seenOption = false;
        string? acceptedId = null;

        foreach (string line in Lines(code.Body))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (TryField(line, out string name, out string value))
            {
                if (seenOption || fields.ContainsKey(name))
                {
                    error = Format(number, acceptedId, "unexpected line");
                    return false;
                }

                fields[name] = value;
                if (name == "id" && IsAcceptedId(value.Trim()))
                {
                    acceptedId = value.Trim();
                }

                continue;
            }

            if (TryOption(line, out string label))
            {
                seenOption = true;
                options.Add(label);
                continue;
            }

            error = Format(number, acceptedId, "unexpected line");
            return false;
        }

        string id = fields.TryGetValue("id", out string? idText) ? idText.Trim() : "";
        if (id.Length == 0)
        {
            error = Format(number, null, "missing id");
            return false;
        }

        if (!IsAcceptedId(id))
        {
            error = Format(number, null, "unexpected line");
            return false;
        }

        acceptedId = id;
        string kind = fields.TryGetValue("kind", out string? kindText) ? kindText.Trim() : "";
        if (kind.Length == 0)
        {
            error = Format(number, acceptedId, "missing kind");
            return false;
        }

        DecisionKind kindValue;
        if (kind == "choice")
        {
            kindValue = DecisionKind.Choice;
        }
        else if (kind == "text")
        {
            kindValue = DecisionKind.Text;
        }
        else if (IsKindToken(kind))
        {
            error = Format(number, acceptedId, $"unknown kind \"{kind}\"");
            return false;
        }
        else
        {
            error = Format(number, acceptedId, "unexpected line");
            return false;
        }

        string question = fields.TryGetValue("prompt", out string? promptText) ? promptText.Trim() : "";
        if (question.Length == 0)
        {
            error = Format(number, acceptedId, "missing prompt");
            return false;
        }

        if (question.Length > 4000)
        {
            error = Format(number, acceptedId, "unexpected line");
            return false;
        }

        if (kindValue == DecisionKind.Text && options.Count > 0)
        {
            error = Format(number, acceptedId, "text questions cannot list options");
            return false;
        }

        List<string> kept = [];
        foreach (string label in options)
        {
            string trimmed = label.Trim();
            if (trimmed.Length == 0 || trimmed.Length > 4000)
            {
                error = Format(number, acceptedId, "unexpected line");
                return false;
            }

            if (!trimmed.Equals("Other", StringComparison.OrdinalIgnoreCase))
            {
                kept.Add(trimmed);
            }
        }

        if (kindValue == DecisionKind.Choice && options.Count > 0 && kept.Count == 0)
        {
            error = Format(number, acceptedId, "choice has no option left after dropping Other");
            return false;
        }

        if (usedIds.Contains(acceptedId))
        {
            error = Format(number, acceptedId, "duplicate id");
            return false;
        }

        prompt = new DecisionPrompt(acceptedId, kindValue, kept, code.Start, code.End);
        error = null;
        return true;
    }

    private static string Format(int number, string? id, string rule) =>
        id is null
            ? $"Error: decision block {number}: {rule}"
            : $"Error: decision \"{id}\": {rule}";

    private static IEnumerable<string> Lines(string body)
    {
        int start = 0;
        for (int index = 0; index < body.Length; index++)
        {
            if (body[index] != '\n')
            {
                continue;
            }

            yield return StripCr(body[start..index]);
            start = index + 1;
        }

        if (start < body.Length)
        {
            yield return StripCr(body[start..]);
        }
    }

    private static string StripCr(string line) =>
        line.Length > 0 && line[^1] == '\r' ? line[..^1] : line;

    private static bool TryField(string line, out string name, out string value)
    {
        name = "";
        value = "";
        int colon = line.IndexOf(':');
        if (colon <= 0)
        {
            return false;
        }

        name = line[..colon];
        if (name is not ("id" or "kind" or "prompt"))
        {
            return false;
        }

        int index = colon + 1;
        while (index < line.Length && line[index] is ' ' or '\t')
        {
            index++;
        }

        value = line[index..];
        return true;
    }

    private static bool TryOption(string line, out string label)
    {
        label = "";
        if (!line.StartsWith("- ", StringComparison.Ordinal))
        {
            return false;
        }

        label = line[2..];
        return label.Length > 0;
    }

    private static bool IsAcceptedId(string id)
    {
        if (id.Length is < 1 or > 80 || !IsIdChar(id[0], first: true))
        {
            return false;
        }

        for (int index = 1; index < id.Length; index++)
        {
            if (!IsIdChar(id[index], first: false))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsIdChar(char character, bool first) =>
        character is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9')
        || (!first && character is '_' or '-');

    private static bool IsKindToken(string value)
    {
        if (value.Length is < 1 or > 40 || !char.IsAsciiLetter(value[0]))
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character is not ((>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or '-'))
            {
                return false;
            }
        }

        return true;
    }
}