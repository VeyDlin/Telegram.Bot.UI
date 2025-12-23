namespace Telegram.Bot.UI.Utils;

/// <summary>
/// Provides extension methods for grammatical declension of numbers with words.
/// </summary>
public static class Declension {
    /// <summary>
    /// Gets the appropriate declension form for a number based on Slavic language rules
    /// (e.g., Russian, Ukrainian).
    /// </summary>
    /// <param name="number">The number to format.</param>
    /// <param name="nominative">The nominative form (e.g., "item" for 1 item).</param>
    /// <param name="genitiveSingular">The genitive singular form (e.g., "items" for 2-4 items).</param>
    /// <param name="genitivePlural">The genitive plural form (e.g., "items" for 5+ items).</param>
    /// <param name="fornat">Optional format string for the number.</param>
    /// <returns>A formatted string with the number and appropriate word form.</returns>
    public static string GetDeclension(
        this int number,
        string nominative,
        string genitiveSingular,
        string genitivePlural,
        string? fornat = null
    ) {
        // Special case for numbers ending in 11-14
        if (number % 100 >= 11 && number % 100 <= 14) {
            return genitivePlural;
        }

        string numberText = fornat is null ? number.ToString() : number.ToString(fornat);
        switch (number % 10) {
            case 1:
            return $"{numberText} {nominative}";
            case 2:
            case 3:
            case 4:
            return $"{numberText} {genitiveSingular}";
            default:
            return $"{numberText} {genitivePlural}";
        }
    }
}