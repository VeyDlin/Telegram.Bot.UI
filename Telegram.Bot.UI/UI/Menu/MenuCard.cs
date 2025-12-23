using AngleSharp.Dom;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.Components;
using Telegram.Bot.UI.Components.Attributes;
using Telegram.Bot.UI.Runtime;

namespace Telegram.Bot.UI.Menu;

/// <summary>
/// Represents a card component that displays menu elements with optional pagination support.
/// </summary>
[Component("card")]
public class MenuCard : AutoComponent {
    /// <summary>
    /// Gets or sets the maximum number of items per page attribute.
    /// </summary>
    [Prop("max-items")]
    public string? maxItemsAttr { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of rows per page attribute.
    /// </summary>
    [Prop("max-rows")]
    public string? maxRowsAttr { get; set; }

    /// <summary>
    /// Gets the maximum number of items per page.
    /// </summary>
    public int? maxItems => GetPropInt(nameof(maxItemsAttr), 0) > 0 ? GetPropInt(nameof(maxItemsAttr), 0) : null;

    /// <summary>
    /// Gets the maximum number of rows per page.
    /// </summary>
    public int? maxRows => GetPropInt(nameof(maxRowsAttr), 0) > 0 ? GetPropInt(nameof(maxRowsAttr), 0) : null;

    /// <summary>
    /// Gets or sets the index of the currently displayed page.
    /// </summary>
    public int currentPage { get; set; } = 0;

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int pageCount => pages.Count > 0 ? pages.Count : 1;

    /// <summary>
    /// Gets the list of pages, where each page contains a list of menu elements.
    /// </summary>
    public List<List<MenuElement>> pages { get; private set; } = new();

    private List<MenuElement> allElements = new();
    private List<IElement>? manualPageElements = null;
    private List<IElement>? childElements = null;
    private bool initialized = false;

    /// <summary>
    /// Applies component definition by parsing child elements and page structure from XML.
    /// </summary>
    /// <param name="element">The XML element to parse.</param>
    internal override void ApplyDefinition(IElement element) {
        base.ApplyDefinition(element);

        var pageElements = element.QuerySelectorAll(":scope > page").ToList();
        if (pageElements.Any()) {
            manualPageElements = pageElements;
        } else {
            childElements = element.Children
                .Where(e => e.TagName.ToLower() != "option"
                         && e.TagName.ToLower() != "title"
                         && e.TagName.ToLower() != "message")
                .ToList();
        }
    }

    /// <summary>
    /// Initializes the component by creating child components and building pages.
    /// </summary>
    public async Task InitializeAsync() {
        if (initialized) {
            return;
        }
        initialized = true;

        if (parent is not ScriptPage scriptPage) {
            return;
        }

        if (manualPageElements != null) {
            foreach (var pageElement in manualPageElements) {
                var pageComponents = new List<MenuElement>();
                int rowIndex = 0;

                foreach (var child in pageElement.Children) {
                    if (child.TagName.ToLower() == "row") {
                        foreach (var rowChild in child.Children) {
                            var component = await scriptPage.CreateChildComponentAsync(rowChild);
                            if (component != null) {
                                component.rowIndex = rowIndex;
                                pageComponents.Add(component);
                            }
                        }
                        rowIndex++;
                    } else {
                        var component = await scriptPage.CreateChildComponentAsync(child);
                        if (component != null) {
                            component.rowIndex = rowIndex;
                            pageComponents.Add(component);
                            rowIndex++;
                        }
                    }
                }
                pages.Add(pageComponents);
            }
        } else if (childElements != null && childElements.Count > 0) {
            int rowIndex = 0;
            foreach (var child in childElements) {
                if (child.TagName.ToLower() == "row") {
                    foreach (var rowChild in child.Children) {
                        var components = await scriptPage.CreateChildComponentsExpandedAsync(rowChild);
                        foreach (var component in components) {
                            component.rowIndex = rowIndex;
                            allElements.Add(component);
                        }
                    }
                    rowIndex++;
                } else {
                    var components = await scriptPage.CreateChildComponentsExpandedAsync(child);
                    foreach (var component in components) {
                        component.rowIndex = rowIndex;
                        allElements.Add(component);
                        rowIndex++;
                    }
                }
            }
            BuildPages();
        }
    }

    /// <summary>
    /// Sets the elements and rebuilds pages.
    /// </summary>
    /// <param name="elements">The list of elements to display.</param>
    public void SetElements(List<MenuElement> elements) {
        allElements = elements ?? new List<MenuElement>();
        BuildPages();
    }

    /// <summary>
    /// Navigates to a specific page.
    /// </summary>
    /// <param name="page">The page index to navigate to.</param>
    public void GoToPage(int page) {
        if (page >= 0 && page < pageCount) {
            currentPage = page;
        }
    }

    /// <summary>
    /// Builds the pages based on maxItems or maxRows settings.
    /// </summary>
    private void BuildPages() {
        pages.Clear();

        if (!maxItems.HasValue && !maxRows.HasValue) {
            if (allElements.Count > 0) {
                pages.Add(allElements);
            }
            return;
        }

        var currentPageList = new List<MenuElement>();
        int itemCount = 0;
        int limit = maxItems ?? maxRows ?? int.MaxValue;

        foreach (var element in allElements) {
            if (itemCount >= limit && currentPageList.Count > 0) {
                pages.Add(currentPageList);
                currentPageList = new List<MenuElement>();
                itemCount = 0;
            }
            currentPageList.Add(element);
            itemCount++;
        }

        if (currentPageList.Count > 0) {
            pages.Add(currentPageList);
        }
    }

    /// <summary>
    /// Builds the inline keyboard buttons for the current page.
    /// </summary>
    /// <returns>A list of inline keyboard buttons.</returns>
    public override async Task<List<InlineKeyboardButton>> BuildAsync() {
        if (hide || pages.Count == 0) {
            return [];
        }

        if (currentPage >= pages.Count) {
            currentPage = pages.Count - 1;
        }
        if (currentPage < 0) {
            currentPage = 0;
        }

        var buttons = new List<InlineKeyboardButton>();
        var currentElements = pages[currentPage];

        foreach (var element in currentElements) {
            var elementButtons = await element.BuildAsync();
            buttons.AddRange(elementButtons);
        }

        return buttons;
    }

    /// <summary>
    /// Disposes all child components when component is disposed.
    /// </summary>
    protected override void OnDispose() {
        foreach (var page in pages) {
            foreach (var element in page) {
                element.Dispose();
            }
        }
        pages.Clear();
        allElements.Clear();
    }
}