using System.Globalization;

namespace UMonsPlanning.Pronote.Internal;

/// <summary>
/// Parses PRONOTE sets of the form <c>"[2..7,9,11..15]"</c>.
/// </summary>
public static class PronoteSet
{
    public static IReadOnlyList<int> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<int>();
        }

        string trimmed = value.Trim();
        if (trimmed.StartsWith('[')) trimmed = trimmed[1..];
        if (trimmed.EndsWith(']')) trimmed = trimmed[..^1];
        if (trimmed.Length == 0)
        {
            return Array.Empty<int>();
        }

        var result = new List<int>();
        foreach (string part in trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int rangeIndex = part.IndexOf("..", StringComparison.Ordinal);
            if (rangeIndex < 0)
            {
                if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int single))
                {
                    result.Add(single);
                }

                continue;
            }

            if (int.TryParse(part[..rangeIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out int from) &&
                int.TryParse(part[(rangeIndex + 2)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int to) &&
                to >= from &&
                to - from < 10_000)
            {
                for (int i = from; i <= to; i++)
                {
                    result.Add(i);
                }
            }
        }

        return result;
    }
}
