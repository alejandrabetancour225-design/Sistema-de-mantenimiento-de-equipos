using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Backend.API.Infrastructure;

// Normaliza texto para comparar frases sin importar mayúsculas, tildes ni signos
// ("Se FUNDIÓ!!" -> "se fundio"). Mismo criterio que python-service/app/texto.py.
public static partial class TextNormalizer
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "el", "la", "los", "las", "un", "una", "de", "del", "y", "o", "a", "en", "que", "se", "me", "mi",
        "es", "esta", "con", "por", "para", "al", "lo", "le", "no", "muy", "mas", "pero", "cuando", "como"
    };

    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.IsLetterOrDigit(c) ? c : ' ');
            }
        }

        return Spaces().Replace(builder.ToString(), " ").Trim();
    }

    // Palabras significativas (sin conectores) para medir parecido entre textos.
    public static HashSet<string> Keywords(string? text)
        => Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && !StopWords.Contains(w))
            .ToHashSet(StringComparer.Ordinal);

    // ¿La frase aparece completa dentro del texto? (" se pega " no coincide con "despega").
    public static bool ContainsPhrase(string normalizedText, string phrase)
    {
        var normalizedPhrase = Normalize(phrase);
        return normalizedPhrase.Length > 0 && $" {normalizedText} ".Contains($" {normalizedPhrase} ", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
