namespace Annotate.Reviews.Domain;

internal static class AnswerRules
{
    public static bool Accepts(
        string kind,
        IReadOnlyList<string> options,
        string answer,
        bool isOther,
        out string stored)
    {
        stored = answer;
        if (kind == "choice")
        {
            if (!isOther)
            {
                foreach (string option in options)
                {
                    if (option == answer)
                    {
                        return true;
                    }
                }

                return false;
            }

            return Trimmed(answer, out stored);
        }

        if (kind == "text" && !isOther)
        {
            return Trimmed(answer, out stored);
        }

        return false;
    }

    private static bool Trimmed(string answer, out string stored)
    {
        stored = answer.Trim();
        return stored.Length is >= 1 and <= ReviewLimits.Answer;
    }
}