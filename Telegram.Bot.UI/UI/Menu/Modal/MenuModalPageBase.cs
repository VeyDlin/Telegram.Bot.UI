using Telegram.Bot.UI.Menu.Selectors;
using Telegram.Bot.UI.Parsing;
using Telegram.Bot.UI.Runtime;
using Telegram.Bot.UI.TextTags;
using Telegram.Bot.UI.Utils;

namespace Telegram.Bot.UI.Menu.Modal;


/// <summary>
/// Base class for modal pages (radio-modal, checkbox-modal).
/// Provides shared functionality for pagination, message rendering, and navigation.
/// </summary>
public abstract class MenuModalPageBase : MessagePage {
    protected List<MenuSelector> allSelectors;
    protected List<List<MenuSelector>> pages = new();
    protected int currentPageIndex = 0;
    protected MenuNavigatePanel? navigatePanel;
    protected int columnsValue = 1;

    public int columns {
        get => GetColumnsValue();
        set {
            columnsValue = value;
            SetColumnsValue(value);
        }
    }

    public IEnumerable<MenuModalDetails>? details { get; protected set; }
    public MessageDefinition? fallbackMessage { get; set; }
    public Runtime.ScriptContext? scriptContext { get; set; }
    public TextTagRegistry? textTags { get; set; }

    // Pagination settings
    public int? maxItems { get; set; }
    public int? maxRows { get; set; }

    public override string? pageResource => GetCurrentDetails()?.pageResource ?? parent?.pageResource;

    // Modal's parent is the ScriptPage (Base), so use Base.title
    public override string backTitle => "« {{ Base.title }}";

    // Back button rendered via JS template
    protected override MenuOpen CreateBackButton(MessagePage targetPage) =>
        new() {
            targetPage = targetPage,
            title = scriptContext is not null ? scriptContext.RenderAsync(backTitle).GetAwaiter().GetResult() : backTitle,
            botUser = botUser,
            parent = this,
            scriptContext = scriptContext
        };

    protected MenuModalPageBase(
        IEnumerable<MenuSelector> selectors,
        IEnumerable<MenuModalDetails>? details,
        BaseBotUser botUser
    ) : base(botUser) {
        this.allSelectors = selectors.ToList();
        this.details = details;
        backToParent = true;
    }

    /// <summary>
    /// Gets the columns value from the underlying button component.
    /// </summary>
    protected abstract int GetColumnsValue();

    /// <summary>
    /// Sets the columns value on the underlying button component.
    /// </summary>
    protected abstract void SetColumnsValue(int value);

    /// <summary>
    /// Gets the MenuModalDetails for the currently selected/focused option.
    /// </summary>
    protected abstract MenuModalDetails? GetCurrentDetails();

    /// <summary>
    /// Creates the button component for displaying options.
    /// </summary>
    protected abstract MenuElement CreateButtonComponent(IEnumerable<MenuSelector> selectors);

    /// <summary>
    /// Gets the current button component.
    /// </summary>
    protected abstract MenuElement? GetButtonComponent();

    /// <summary>
    /// Disposes the current button component.
    /// </summary>
    protected abstract void DisposeButtonComponent();

    /// <summary>
    /// Restores selection state after page change.
    /// </summary>
    protected abstract void RestoreSelectionState(List<MenuSelector> currentSelectors);


    protected override void OnDispose() {
        DisposeButtonComponent();
        navigatePanel?.Dispose();
    }


    // Don't inherit message resource from parent - modal handles its own messages
    public override string? RequestMessageResource() => null;

    /// <summary>
    /// Resolves resource paths by delegating to parent page (if ScriptPage).
    /// </summary>
    protected override string? ResolveResourcePath(string? path) {
        if (string.IsNullOrEmpty(path)) {
            return null;
        }

        // Delegate to parent if it can resolve paths
        if (parent is Runtime.ScriptPage scriptParent) {
            // Use reflection to access parent's ResolveResourcePath
            var method = typeof(Runtime.ScriptPage).GetMethod("ResolveResourcePath",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (method is not null) {
                return method.Invoke(scriptParent, new object?[] { path }) as string;
            }
        }

        // Fallback: try using parent's pageResource as base
        var basePath = parent?.pageResource;
        if (!string.IsNullOrEmpty(basePath)) {
            return Path.Combine(basePath, path);
        }

        return null;
    }


    protected override async Task<string?> BuildTextTemplate() {
        var currentDetails = GetCurrentDetails();

        // Get message definition: option → fallback → null
        var msgDef = currentDetails?.message ?? fallbackMessage;
        if (msgDef is null) {
            return null;
        }

        // Apply markdown mode from message definition (before processing)
        if (msgDef.md) {
            parseMode = Telegram.Bot.Types.Enums.ParseMode.Markdown;
        }

        // Load text from resource or use inline content
        string? text;
        if (!string.IsNullOrEmpty(msgDef.loadResource)) {
            text = RenderTemplateText(msgDef.loadResource, []);
        } else {
            text = msgDef.inlineContent;
        }

        if (string.IsNullOrEmpty(text)) {
            return null;
        }

        // Set self context with selected option details
        string? wallpaperUrl = null;
        var mode = parseMode ?? Telegram.Bot.Types.Enums.ParseMode.Html;
        var wallpaperUrlExpr = msgDef.wallpaperUrl;

        if (scriptContext is not null && currentDetails is not null) {
            var ctx = new ComponentContext(scriptContext);
            ctx.SetSelf(new {
                id = currentDetails.id,
                title = currentDetails.title,
                index = currentDetails.index
            });
            text = await ctx.RenderAsync(text);

            // Render wallpaper URL expression if present
            if (!string.IsNullOrEmpty(wallpaperUrlExpr)) {
                wallpaperUrl = wallpaperUrlExpr.Contains("{{")
                    ? await ctx.RenderAsync(wallpaperUrlExpr)
                    : wallpaperUrlExpr;
            }
        } else if (scriptContext is not null) {
            text = await scriptContext.RenderAsync(text);

            if (!string.IsNullOrEmpty(wallpaperUrlExpr)) {
                wallpaperUrl = wallpaperUrlExpr.Contains("{{")
                    ? await scriptContext.RenderAsync(wallpaperUrlExpr)
                    : wallpaperUrlExpr;
            }
        } else if (!string.IsNullOrEmpty(wallpaperUrlExpr) && !wallpaperUrlExpr.Contains("{{")) {
            // No script context, but plain URL
            wallpaperUrl = wallpaperUrlExpr;
        }

        // Process text tags (wallpaper, br, space, tab, etc.)
        if (textTags is not null) {
            text = await textTags.ProcessContentAsync(text, mode, scriptContext);
        }

        // Add wallpaper link at the beginning (for link preview) - legacy support
        if (!string.IsNullOrEmpty(wallpaperUrl)) {
            var wallpaperLink = TextNormalizer.FormatWallpaperLink(wallpaperUrl, mode);
            text = wallpaperLink + text;
        }

        return text;
    }

    public override async Task<object?> RequestModelAsync() {
        var currentDetails = GetCurrentDetails();
        if (currentDetails?.model is not null) {
            return currentDetails.model;
        }
        if (parent is not null) {
            return await parent.RequestModelAsync();
        }
        return null;
    }


    public override Task<List<ButtonsPage>?> RequestPageComponentsAsync() {
        var currentDetails = GetCurrentDetails();
        webPreview = currentDetails?.webPreview ?? parent?.webPreview ?? true;

        // Build pages if pagination is enabled
        if ((maxItems.HasValue || maxRows.HasValue) && pages.Count == 0) {
            BuildPages();
        }

        // No pagination - return all buttons
        if (pages.Count == 0) {
            EnsureButtonComponentCreated();
            var buttonComponent = GetButtonComponent();
            if (buttonComponent is null) {
                return Task.FromResult<List<ButtonsPage>?>(null);
            }
            return ButtonsPage.PageTask([
                [buttonComponent]
            ]);
        }

        // Pagination mode - rebuild buttons for current page
        EnsurePageInBounds();
        var currentSelectors = pages[currentPageIndex];
        DisposeButtonComponent();

        var newComponent = CreateButtonComponent(currentSelectors);
        if (newComponent is MenuElement menuElement) {
            menuElement.botUser = botUser;
            menuElement.scriptContext = scriptContext;
        }

        // Restore selection if selected items are on current page
        RestoreSelectionState(currentSelectors);

        // Create navigate panel if needed
        if (pages.Count > 1 && navigatePanel is null) {
            navigatePanel = new MenuNavigatePanel {
                botUser = botUser,
                parent = this,
                scriptContext = scriptContext,
                totalPages = pages.Count,
                currentPage = currentPageIndex,
                onPageChange = async (newPage) => {
                    currentPageIndex = newPage;
                    if (lastMessage is not null) {
                        await UpdatePageAsync(lastMessage.MessageId, lastMessage.Chat.Id);
                    }
                }
            };
        }

        if (navigatePanel is not null) {
            navigatePanel.currentPage = currentPageIndex;
            navigatePanel.totalPages = pages.Count;
        }

        // Return buttons + navigate (if > 1 page)
        var component = GetButtonComponent();
        if (component is null) {
            return Task.FromResult<List<ButtonsPage>?>(null);
        }

        if (pages.Count > 1 && navigatePanel is not null) {
            return ButtonsPage.PageTask([
                [component],
                [navigatePanel]
            ]);
        }

        return ButtonsPage.PageTask([
            [component]
        ]);
    }

    /// <summary>
    /// Ensures the button component is created.
    /// </summary>
    protected abstract void EnsureButtonComponentCreated();


    protected void BuildPages() {
        if (!maxItems.HasValue && !maxRows.HasValue) {
            return;
        }

        pages.Clear();
        var currentPage = new List<MenuSelector>();
        int itemCount = 0;

        foreach (var selector in allSelectors) {
            // Check if adding this item exceeds limits
            bool exceedsItems = maxItems.HasValue && itemCount >= maxItems.Value;

            // For buttons, each button is one row (columns=1 by default)
            bool exceedsRows = maxRows.HasValue && itemCount >= maxRows.Value;

            if ((exceedsItems || exceedsRows) && currentPage.Count > 0) {
                pages.Add(currentPage);
                currentPage = new List<MenuSelector>();
                itemCount = 0;
            }

            currentPage.Add(selector);
            itemCount++;
        }

        if (currentPage.Count > 0) {
            pages.Add(currentPage);
        }
    }


    protected void EnsurePageInBounds() {
        if (pages.Count == 0) {
            currentPageIndex = 0;
        } else if (currentPageIndex >= pages.Count) {
            currentPageIndex = pages.Count - 1;
        } else if (currentPageIndex < 0) {
            currentPageIndex = 0;
        }
    }
}
