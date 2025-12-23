using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Jint;
using Jint.Native;
using Jint.Native.Array;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.Components;
using Telegram.Bot.UI.Components.Attributes;
using Telegram.Bot.UI.Runtime;

namespace Telegram.Bot.UI.Menu;


/// <summary>
/// Auto-card component that renders items from a data source using slot templates.
/// Unlike MenuCard which uses static child elements, auto-card dynamically creates
/// components from a bound items array.
///
/// Usage:
/// <code>
/// &lt;auto-card :items="users" max-items="5"&gt;
///     &lt;template #item&gt;
///         &lt;button :title="item.name" @click="selectUser(item.id)" /&gt;
///     &lt;/template&gt;
/// &lt;/auto-card&gt;
/// </code>
/// </summary>
[Component("auto-card")]
public class MenuAutoCard : AutoComponent {
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
    /// Gets or sets the binding for the items array.
    /// </summary>
    [Bind("items")]
    public string? itemsBinding { get; set; }

    /// <summary>
    /// Gets or sets the variable name for the current item in templates.
    /// </summary>
    [Prop("itemName")]
    public string? itemNameAttr { get; set; }

    /// <summary>
    /// Gets or sets the variable name for the current index in templates.
    /// </summary>
    [Prop("indexName")]
    public string? indexNameAttr { get; set; }

    /// <summary>
    /// Gets the maximum number of items per page.
    /// </summary>
    public int? maxItems => GetPropInt(nameof(maxItemsAttr), 0) > 0 ? GetPropInt(nameof(maxItemsAttr), 0) : null;

    /// <summary>
    /// Gets the maximum number of rows per page.
    /// </summary>
    public int? maxRows => GetPropInt(nameof(maxRowsAttr), 0) > 0 ? GetPropInt(nameof(maxRowsAttr), 0) : null;

    /// <summary>
    /// Gets the variable name for the current item. Defaults to "item".
    /// </summary>
    public string itemName => GetProp(nameof(itemNameAttr), "item");

    /// <summary>
    /// Gets the variable name for the current index. Defaults to "index".
    /// </summary>
    public string indexName => GetProp(nameof(indexNameAttr), "index");

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
    private IElement? itemTemplateHtml = null;
    private bool initialized = false;

    /// <summary>
    /// Applies component definition by extracting item template from XML.
    /// </summary>
    /// <param name="element">The XML element to parse.</param>
    internal override void ApplyDefinition(IElement element) {
        base.ApplyDefinition(element);

        var templateElement = element.QuerySelectorAll(":scope > template")
            .FirstOrDefault(e => e.GetAttribute("slot") == "item"
                              || e.GetAttribute("#item") != null
                              || e.HasAttribute("#item"));

        if (templateElement is IHtmlTemplateElement htmlTemplate) {
            itemTemplateHtml = htmlTemplate.Content.Children.FirstOrDefault();
        } else if (templateElement != null) {
            itemTemplateHtml = templateElement.Children.FirstOrDefault();
        }
    }

    /// <summary>
    /// Initializes the component by building elements from bound items.
    /// </summary>
    public async Task InitializeAsync() {
        if (initialized) {
            return;
        }
        initialized = true;

        await RebuildElements();
    }

    /// <summary>
    /// Rebuilds all elements from the bound items array.
    /// </summary>
    private async Task RebuildElements() {
        foreach (var element in allElements) {
            element.Dispose();
        }
        allElements.Clear();
        pages.Clear();

        if (parent is not ScriptPage scriptPage) {
            return;
        }

        if (scriptContext is null) {
            return;
        }

        var itemsExpr = GetBindingExpression(nameof(itemsBinding));
        if (string.IsNullOrEmpty(itemsExpr)) {
            return;
        }

        var itemsValue = scriptContext.Engine.Evaluate(itemsExpr);

        if (itemsValue.IsNull() || itemsValue.IsUndefined()) {
            BuildPages();
            return;
        }

        IEnumerable<object?> items;
        if (itemsValue is ArrayInstance arr) {
            items = arr.Select(v => v.ToObject()).ToList();
        } else {
            var obj = itemsValue.ToObject();
            if (obj is System.Collections.IEnumerable enumerable) {
                items = enumerable.Cast<object?>().ToList();
            } else {
                BuildPages();
                return;
            }
        }

        int rowIndex = 0;
        int index = 0;

        foreach (var item in items) {
            scriptContext.SetValue(itemName, item);
            scriptContext.SetValue(indexName, index);

            MenuElement? component = null;

            if (itemTemplateHtml != null) {
                var tagName = itemTemplateHtml.TagName.ToLower();
                var registry = GetRegistry(scriptPage);
                if (registry != null) {
                    component = await registry.CreateAsync(tagName, itemTemplateHtml, scriptContext, scriptPage);
                }
            }

            if (component != null) {
                if (component is AutoComponent autoComp) {
                    autoComp.FreezeProps(itemName, item, indexName, index);
                }
                component.rowIndex = rowIndex;
                allElements.Add(component);
                rowIndex++;
            }

            index++;
        }

        scriptContext.SetValue(itemName, JsValue.Undefined);
        scriptContext.SetValue(indexName, JsValue.Undefined);

        BuildPages();
    }

    /// <summary>
    /// Gets the component registry from the parent ScriptPage using reflection.
    /// </summary>
    /// <param name="page">The parent ScriptPage.</param>
    /// <returns>The ComponentRegistry instance or null.</returns>
    private ComponentRegistry? GetRegistry(ScriptPage page) {
        var prop = typeof(ScriptPage).GetProperty("registry",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return prop?.GetValue(page) as ComponentRegistry;
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
                pages.Add(allElements.ToList());
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
        await InitializeAsync();

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
    /// Refreshes items from the bound data source.
    /// </summary>
    public async Task RefreshAsync() {
        initialized = false;
        await InitializeAsync();
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

        foreach (var element in allElements) {
            element.Dispose();
        }
        allElements.Clear();
    }
}