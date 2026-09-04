using System.Globalization;
using System.Text;

namespace UMonsPlanning.Pronote.Internal;

/// <summary>
/// Generates a stable, readable identifier from a PRONOTE label.
/// Native identifiers (<c>N</c>) are recomputed every session, so they cannot serve as a public
/// key.
/// </summary>
public static class Slug
{
    public static string From(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return string.Empty;
        }

        string normalized = label.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        bool lastWasSeparator = true;

        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
                lastWasSeparator = false;
            }
            else if (!lastWasSeparator)
            {
                builder.Append('-');
                lastWasSeparator = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}
