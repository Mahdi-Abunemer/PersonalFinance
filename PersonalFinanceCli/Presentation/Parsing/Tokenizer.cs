using System.Text;

namespace PersonalFinanceCli.Presentation.Parsing;

public static class Tokenizer
{
    public static IReadOnlyList<string> Tokenize(string commandLine)
    {
        var result = new List<string>();

        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return result;
        }

        var stringBuilder = new StringBuilder();
        var isInQuotes = false;

        foreach (var character in commandLine)
        {
            if (character == '"')
            {
                isInQuotes = !isInQuotes;
                continue;
            }

            if (char.IsWhiteSpace(character) && !isInQuotes)
            {
                if (stringBuilder.Length > 0)
                {
                    result.Add(stringBuilder.ToString());
                    stringBuilder.Clear();
                }
            }
            else
            {
                stringBuilder.Append(character);
            }
        }

        if (stringBuilder.Length > 0)
        {
            result.Add(stringBuilder.ToString());
        }

        return result;
    }
}
