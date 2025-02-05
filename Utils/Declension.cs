namespace Telegram.Bot.UI.Utils;


public static class Declension {
    public static string GetDeclension(this int number, string nominative, string genitiveSingular, string genitivePlural, string? fornat = null) {
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
