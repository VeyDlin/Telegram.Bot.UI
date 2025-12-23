namespace Telegram.Bot.UI.Menu.Selectors;

/// <summary>
/// Represents a selectable menu option with an ID and title.
/// Used by menu components like MenuRadio, MenuCheckboxList, and MenuSwitch.
/// </summary>
public class MenuSelector {
    /// <summary>
    /// Gets or sets the display title of the menu option.
    /// </summary>
    public required string title { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the menu option.
    /// </summary>
    public required string id { get; set; }

    /// <summary>
    /// Creates a list of MenuSelector objects from an enumerable of title-id tuples.
    /// </summary>
    /// <param name="source">The source enumerable containing title and id pairs.</param>
    /// <returns>A list of MenuSelector objects.</returns>
    public static List<MenuSelector> FromArray(IEnumerable<(string title, string id)> source) {
        return source.Select(x => new MenuSelector {
            title = x.title,
            id = x.id
        }).ToList();
    }

    /// <summary>
    /// Enumerates MenuSelector items with their corresponding index.
    /// </summary>
    /// <param name="source">The source enumerable of MenuSelector items.</param>
    /// <returns>An enumerable of tuples containing each item and its index.</returns>
    public static IEnumerable<(MenuSelector item, int index)> WithIndex(IEnumerable<MenuSelector> source) {
        return source.Select((item, index) => (item, index));
    }
}