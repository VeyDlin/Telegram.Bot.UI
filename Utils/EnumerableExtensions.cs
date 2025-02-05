
namespace Telegram.Bot.UI.Utils;


public static class EnumerableExtensions {
    public static T? FirstOrNull<T>(this IEnumerable<T> source) where T : class {
        return source.Any() ? source.First() : null;
    }

    public static T? FirstOrNull<T>(this IEnumerable<T?> source) where T : struct {
        return source.Any() ? source.First() : null;
    }
}