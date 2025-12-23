namespace Telegram.Bot.UI.Menu.Selectors;

/// <summary>
/// Represents a selectable menu option for modal dialogs with an ID and title.
/// Similar to MenuSelector but intended for modal selection scenarios.
/// </summary>
public class MenuModalSelector {
    /// <summary>
    /// Gets or sets the display title of the menu option.
    /// </summary>
    public required string title { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the menu option.
    /// </summary>
    public required string id { get; set; }

    /// <summary>
    /// Creates a list of MenuModalSelector objects from an enumerable of title-id tuples.
    /// </summary>
    /// <param name="source">The source enumerable containing title and id pairs.</param>
    /// <returns>A list of MenuModalSelector objects.</returns>
    public static List<MenuModalSelector> FromArray(IEnumerable<(string title, string id)> source) {
        return source.Select(x => new MenuModalSelector {
            title = x.title,
            id = x.id
        }).ToList();
    }

    /// <summary>
    /// Enumerates MenuModalSelector items with their corresponding index.
    /// </summary>
    /// <param name="source">The source enumerable of MenuModalSelector items.</param>
    /// <returns>An enumerable of tuples containing each item and its index.</returns>
    public static IEnumerable<(MenuModalSelector item, int index)> WithIndex(IEnumerable<MenuModalSelector> source) {
        return source.Select((item, index) => (item, index));
    }
}