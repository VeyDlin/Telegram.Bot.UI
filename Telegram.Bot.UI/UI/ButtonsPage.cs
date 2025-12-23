using Telegram.Bot.UI.Menu;

namespace Telegram.Bot.UI;

/// <summary>
/// Represents a page of button layouts for inline keyboards.
/// </summary>
public class ButtonsPage {
    /// <summary>
    /// Gets or sets the button layout as rows of menu elements.
    /// </summary>
    public required IEnumerable<IEnumerable<MenuElement>> page { get; set; }

    /// <summary>
    /// Creates a list of button pages from multiple page definitions.
    /// </summary>
    /// <param name="pagePram">Variable number of page definitions.</param>
    /// <returns>List of ButtonsPage instances.</returns>
    public static List<ButtonsPage> Page(params IEnumerable<IEnumerable<MenuElement>>[] pagePram) {
        var pages = new List<ButtonsPage>();
        return pages.Concat(pagePram.Select(x => new ButtonsPage() { page = x })).ToList();
    }

    /// <summary>
    /// Creates a task that returns a list of button pages.
    /// </summary>
    /// <param name="pagePram">Variable number of page definitions.</param>
    /// <returns>Task containing list of ButtonsPage instances.</returns>
    public static Task<List<ButtonsPage>?> PageTask(params IEnumerable<IEnumerable<MenuElement>>[] pagePram) {
        return Task.FromResult<List<ButtonsPage>?>(Page(pagePram));
    }
}