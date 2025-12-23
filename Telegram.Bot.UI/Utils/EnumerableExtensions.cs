namespace Telegram.Bot.UI.Utils;

/// <summary>
/// Provides extension methods for IEnumerable collections.
/// </summary>
public static class EnumerableExtensions {
    /// <summary>
    /// Returns the first element of a sequence, or null if the sequence contains no elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements (must be a reference type).</typeparam>
    /// <param name="source">The sequence to return the first element from.</param>
    /// <returns>The first element in the sequence, or null if the sequence is empty.</returns>
    public static T? FirstOrNull<T>(this IEnumerable<T> source) where T : class {
        return source.Any() ? source.First() : null;
    }

    /// <summary>
    /// Returns the first element of a sequence, or null if the sequence contains no elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements (must be a value type).</typeparam>
    /// <param name="source">The sequence to return the first element from.</param>
    /// <returns>The first element in the sequence, or null if the sequence is empty.</returns>
    public static T? FirstOrNull<T>(this IEnumerable<T?> source) where T : struct {
        return source.Any() ? source.First() : null;
    }
}