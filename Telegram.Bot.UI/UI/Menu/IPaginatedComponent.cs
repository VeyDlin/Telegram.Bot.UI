namespace Telegram.Bot.UI.Menu;

/// <summary>
/// Interface for components that support pagination.
/// </summary>
public interface IPaginatedComponent {
    /// <summary>
    /// Gets or sets the current page index.
    /// </summary>
    int currentPage { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    int pageCount { get; }

    /// <summary>
    /// Navigates to a specific page.
    /// </summary>
    /// <param name="page">The page index to navigate to.</param>
    void GoToPage(int page);
}
