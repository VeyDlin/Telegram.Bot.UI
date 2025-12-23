using Jint;
using Jint.Native;
using Jint.Native.Array;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.UI.Components;
using Telegram.Bot.UI.Menu;
using Telegram.Bot.UI.Parsing;
using Telegram.Bot.UI.TextTags;
using Telegram.Bot.UI.Utils;

namespace Telegram.Bot.UI.Runtime;


/// <summary>
/// Dynamic page with JavaScript execution, template rendering, and component management.
/// Extends MessagePage with scripting, v-for/v-if directives, and auto-pagination.
/// </summary>
public class ScriptPage : MessagePage {
    #region Fields

    private CompiledPage compiled { get; set; }
    private PageDefinition definition => compiled.definition;
    private ScriptContext context { get; set; }
    private ComponentFactory factory { get; set; }
    private PageManager pageManager { get; set; }
    private object? vmodel { get; set; }
    private ComponentRegistry? registry { get; set; }
    private List<MenuElement> components { get; set; } = [];
    private List<List<MenuElement>> menuPages { get; set; } = [];
    private MenuNavigatePanel? navigateComponent { get; set; }
    private bool initialized { get; set; } = false;
    private PageHandle? handleBacking { get; set; }
    private ILogger logger => botUser.worker.logger;

    #endregion


    #region Properties

    /// <summary>
    /// Gets the text tag registry for processing custom tags.
    /// </summary>
    public TextTagRegistry textTags => pageManager.textTags;

    /// <summary>
    /// Current page index for multi-page navigation.
    /// </summary>
    public int currentPageIndex { get; set; } = 0;

    /// <summary>
    /// Handle for this page, used for navigation chain management.
    /// Created lazily on first access.
    /// </summary>
    public PageHandle handle {
        get {
            if (handleBacking is null) {
                var parentHandle = (parent as ScriptPage)?.handle;
                handleBacking = new PageHandle(this, parentHandle);
            }
            return handleBacking;
        }
        internal set => handleBacking = value;
    }

    /// <summary>
    /// Directory containing the page file. Used for relative resource paths.
    /// </summary>
    public string pageDirectory => compiled.directory;

    /// <inheritdoc />
    public override string? pageResource => compiled.directory;

    /// <inheritdoc />
    public override string? pageId => definition.id;

    /// <inheritdoc />
    public override string? title {
        get {
            if (definition.title is null) {
                return null;
            }
            var t = definition.title.content;
            return definition.title.lang ? L(t) : RenderTemplateAsync(t).GetAwaiter().GetResult();
        }
    }

    /// <inheritdoc />
    public override string backTitle => definition.backTitle is not null
        ? RenderTemplateAsync(definition.backTitle).GetAwaiter().GetResult()
        : RenderTemplateAsync(base.backTitle).GetAwaiter().GetResult();

    /// <summary>
    /// Returns true if this page has a photo handler registered.
    /// </summary>
    public bool hasPhotoHandler => context.HasPhotoHandler;

    /// <summary>
    /// Returns true if this page has a document handler registered.
    /// </summary>
    public bool hasDocumentHandler => context.HasDocumentHandler;

    #endregion


    #region Constructor

    /// <summary>
    /// Creates a new ScriptPage instance.
    /// </summary>
    /// <param name="botUser">Bot user context.</param>
    /// <param name="compiled">Compiled page definition.</param>
    /// <param name="pageManager">Page manager for navigation.</param>
    /// <param name="vmodel">Optional ViewModel instance.</param>
    /// <param name="registry">Component registry for creating menu elements.</param>
    /// <param name="props">Optional properties passed to the page.</param>
    public ScriptPage(
        BaseBotUser botUser,
        CompiledPage compiled,
        PageManager pageManager,
        object? vmodel = null,
        ComponentRegistry? registry = null,
        Dictionary<string, object?>? props = null
    ) : base(botUser) {
        this.compiled = compiled;
        this.pageManager = pageManager;
        this.vmodel = vmodel;
        this.registry = registry;

        webPreview = definition.webPreview;
        backToParent = definition.backToParent;
        parseMode = definition.message?.md == true
            ? Telegram.Bot.Types.Enums.ParseMode.Markdown
            : Telegram.Bot.Types.Enums.ParseMode.Html;
        media = definition.media;

        context = new ScriptContext(botUser, vmodel);
        context.SetPage(this);

        if (props is not null) {
            context.SetProps(props);
        }

        factory = new ComponentFactory(this, context, definition, registry);
    }

    #endregion


    #region Back Button

    /// <inheritdoc />
    protected override MenuOpen CreateBackButton(MessagePage targetPage) =>
        new() {
            targetPage = targetPage,
            title = backTitle,
            botUser = botUser,
            parent = this,
            scriptContext = context
        };

    #endregion


    #region Resource Resolution

    /// <summary>
    /// Resolves a resource path relative to page location.
    /// </summary>
    /// <remarks>
    /// Path formats:
    /// - @/path - absolute from resources root
    /// - ./path or ../path - relative to page location
    /// - other - treated as relative to page location
    /// </remarks>
    protected override string? ResolveResourcePath(string? path) {
        if (string.IsNullOrEmpty(path)) {
            return null;
        }

        if (path.StartsWith("@/")) {
            var resourcesRoot = botUser.worker.resourceLoader.BasePath;
            if (resourcesRoot is not null) {
                return Path.Combine(resourcesRoot, path.Substring(2));
            }
            return path.Substring(2);
        }

        if (path.StartsWith("./") || path.StartsWith("../")) {
            var basePath = pageDirectory;
            if (!string.IsNullOrEmpty(basePath)) {
                return Path.GetFullPath(Path.Combine(basePath, path));
            }
        }

        if (!string.IsNullOrEmpty(pageDirectory)) {
            return Path.GetFullPath(Path.Combine(pageDirectory, path));
        }

        return null;
    }

    #endregion


    #region Initialization

    /// <summary>
    /// Initializes the page: executes script, creates components, sets up pagination.
    /// Called lazily before first render.
    /// </summary>
    private async Task InitializeAsync() {
        if (initialized) {
            return;
        }
        initialized = true;

        try {
            logger.LogDebug("[ScriptPage] InitializeAsync: {PageId}", definition.id);

            // Execute script block FIRST (defines variables for v-for)
            if (definition.script is not null) {
                logger.LogDebug("[ScriptPage] Running script block (before components)");
                context.Execute(definition.script.code);
            }

            // Build pages (v-for can now access variables)
            if (definition.menuPages is not null && definition.menuPages.Count > 0) {
                foreach (var pageDef in definition.menuPages) {
                    var pageComponents = await factory.ExpandAndCreateAsync(pageDef.components);
                    menuPages.Add(pageComponents);
                }
            } else {
                var allComponents = await factory.ExpandAndCreateAsync(definition.components);

                if (definition.maxItems.HasValue || definition.maxRows.HasValue) {
                    var navComp = allComponents.OfType<MenuNavigatePanel>().FirstOrDefault();
                    var componentsWithoutNav = allComponents.Where(c => !(c is MenuNavigatePanel)).ToList();

                    menuPages = AutoPaginateComponents(componentsWithoutNav, definition.maxItems, definition.maxRows);

                    if (navComp is not null) {
                        navigateComponent = navComp;
                    }
                } else {
                    components = allComponents;
                }
            }

            // Configure navigate component
            if (navigateComponent is null) {
                var allCreatedComponents = menuPages.Count > 0
                    ? menuPages.SelectMany(p => p).ToList()
                    : components;
                navigateComponent = allCreatedComponents.OfType<MenuNavigatePanel>().FirstOrDefault();
            }
            if (navigateComponent is not null) {
                navigateComponent.totalPages = menuPages.Count > 0 ? menuPages.Count : 1;
                navigateComponent.currentPage = currentPageIndex;
                navigateComponent.onPageChangeAsync = async (newPage) => {
                    currentPageIndex = newPage;
                    navigateComponent.currentPage = newPage;
                    if (lastMessage is not null) {
                        await UpdatePageAsync(lastMessage.MessageId, lastMessage.Chat.Id);
                    }
                };
            } else if (definition.navigate is not null) {
                navigateComponent = await factory.CreateAsync(definition.navigate) as MenuNavigatePanel;
                if (navigateComponent is not null) {
                    navigateComponent.totalPages = menuPages.Count > 0 ? menuPages.Count : 1;
                    navigateComponent.currentPage = currentPageIndex;
                    navigateComponent.onPageChangeAsync = async (newPage) => {
                        currentPageIndex = newPage;
                        navigateComponent.currentPage = newPage;
                        if (lastMessage is not null) {
                            await UpdatePageAsync(lastMessage.MessageId, lastMessage.Chat.Id);
                        }
                    };
                }
            }

            // Execute script again for component() calls
            if (definition.script is not null) {
                context.ClearLifecycleCallbacks();
                logger.LogDebug("[ScriptPage] Running script block (after components)");
                context.Execute(definition.script.code);
            }

            // Initialize auto-cards
            var allCreated = menuPages.Count > 0
                ? menuPages.SelectMany(p => p).ToList()
                : components;
            foreach (var comp in allCreated.OfType<MenuAutoCard>()) {
                await comp.InitializeAsync();
            }

            // Call onMounted callbacks
            logger.LogDebug("[ScriptPage] Invoking onMounted callbacks");
            await context.InvokeMounted();

            logger.LogDebug("[ScriptPage] InitializeAsync done, pages: {MenuPagesCount}, components: {ComponentsCount}", menuPages.Count, components.Count);
        } catch (Exception ex) {
            logger.LogError(ex, "[ScriptPage] InitializeAsync failed for {PageId}", definition.id);
            throw;
        }
    }

    #endregion


    #region Template Rendering

    /// <inheritdoc />
    public override string? RequestMessageResource() {
        var resource = definition.message?.loadResource;
        return resource is not null ? context.RenderAsync(resource).GetAwaiter().GetResult() : null;
    }

    /// <inheritdoc />
    protected override async Task<string?> BuildTextTemplate() {
        await InitializeAsync();
        await context.InvokeBeforeRender();

        botUser.scriptContext = context;

        string? result = null;

        if (!string.IsNullOrEmpty(definition.message?.inlineContent)) {
            result = await context.RenderAsync(definition.message.inlineContent);
        } else if (definition.message?.conditions is not null) {
            foreach (var cond in definition.message.conditions) {
                if (cond.condition == "true" || context.Evaluate<bool>(cond.condition)) {
                    result = await context.RenderAsync(cond.content);
                    break;
                }
            }
        } else {
            result = await base.BuildTextTemplate();
        }

        // Normalize whitespace for HTML mode
        if (result != null && definition.message?.md != true && definition.message?.pre != true) {
            result = NormalizeHtmlWhitespace(result);
        }

        // Process text tags
        if (result != null) {
            var mode = parseMode ?? ParseMode.Html;
            result = await pageManager.textTags.ProcessContentAsync(result, mode, context);
            result = string.Join("\n", result.Split('\n').Select(line => line.Trim()));
            result = result.Trim();
        }

        // Add wallpaper link if present (legacy support)
        if (result != null && !string.IsNullOrEmpty(definition.message?.wallpaperUrl)) {
            var wallpaperUrlExpr = definition.message.wallpaperUrl;
            string? wallpaperUrl = wallpaperUrlExpr.Contains("{{")
                ? await context.RenderAsync(wallpaperUrlExpr)
                : wallpaperUrlExpr;

            if (!string.IsNullOrEmpty(wallpaperUrl)) {
                var wallpaperLink = TextNormalizer.FormatWallpaperLink(wallpaperUrl, parseMode ?? ParseMode.Html);
                result = wallpaperLink + result;
            }
        }

        await context.InvokeAfterRender();

        return result;
    }

    /// <summary>
    /// Normalizes whitespace for HTML mode: collapses spaces and trims.
    /// </summary>
    private static string NormalizeHtmlWhitespace(string text) {
        text = text.Replace("\r\n", " ").Replace("\n", " ");

        while (text.Contains("  ")) {
            text = text.Replace("  ", " ");
        }

        return text.Trim();
    }

    /// <summary>
    /// Renders a template string using the script context.
    /// </summary>
    private async Task<string> RenderTemplateAsync(string template) {
        return await context.RenderAsync(template);
    }

    /// <inheritdoc />
    public override Task<object?> RequestModelAsync() {
        return Task.FromResult<object?>(null);
    }

    #endregion


    #region Component Building

    /// <inheritdoc />
    public override async Task<List<ButtonsPage>?> RequestPageComponentsAsync() {
        try {
            await InitializeAsync();
            EnsurePageInBounds();

            List<MenuElement[][]> allPages = new();

            if (menuPages.Count > 0) {
                var currentPageComponents = menuPages[currentPageIndex];
                var visibleComponents = currentPageComponents.Where(c => !c.hide).ToList();
                var rows = visibleComponents
                    .GroupBy(c => c.rowIndex)
                    .OrderBy(g => g.Key)
                    .Select(g => g.ToArray())
                    .ToList();

                var navigateAlreadyIncluded = visibleComponents.OfType<MenuNavigatePanel>().Any();
                if (navigateComponent is not null && !navigateAlreadyIncluded) {
                    navigateComponent.currentPage = currentPageIndex;
                    navigateComponent.totalPages = menuPages.Count;
                    rows.Add([navigateComponent]);
                } else if (navigateComponent is not null) {
                    navigateComponent.currentPage = currentPageIndex;
                    navigateComponent.totalPages = menuPages.Count;
                }

                allPages.Add(rows.ToArray());
            } else {
                var expandedComponents = ExpandComponents(components);
                var visibleComponents = expandedComponents.Where(c => !c.hide).ToList();
                if (visibleComponents.Count == 0 && navigateComponent is null) {
                    return null;
                }

                var rows = visibleComponents
                    .GroupBy(c => c.rowIndex)
                    .OrderBy(g => g.Key)
                    .Select(g => g.ToArray())
                    .ToList();

                allPages.Add(rows.ToArray());
            }

            return ButtonsPage.Page(allPages.ToArray());
        } catch (Exception ex) {
            logger.LogError(ex, "[ScriptPage] RequestPageComponentsAsync failed");
            throw;
        }
    }

    /// <summary>
    /// Expands MenuCard components into their child elements for the current page.
    /// Recalculates rowIndex to maintain proper ordering after expansion.
    /// </summary>
    private List<MenuElement> ExpandComponents(List<MenuElement> components) {
        var result = new List<MenuElement>();
        var expandedWithOriginalIndex = new List<(MenuElement element, int originalIndex, bool fromCard)>();

        foreach (var component in components) {
            if (component is MenuCard card && card.pages.Count > 0) {
                var currentPageElements = card.pages[card.currentPage];
                foreach (var element in currentPageElements) {
                    expandedWithOriginalIndex.Add((element, element.rowIndex, true));
                }
            } else if (component is MenuAutoCard autoCard && autoCard.pages.Count > 0) {
                var currentPageElements = autoCard.pages[autoCard.currentPage];
                foreach (var element in currentPageElements) {
                    expandedWithOriginalIndex.Add((element, element.rowIndex, true));
                }
            } else {
                expandedWithOriginalIndex.Add((component, component.rowIndex, false));
            }
        }

        int currentRowIndex = 0;
        int? lastCardRowIndex = null;
        int? lastNonCardRowIndex = null;
        bool lastWasCard = false;

        foreach (var (element, originalIndex, fromCard) in expandedWithOriginalIndex) {
            if (fromCard) {
                if (lastWasCard && lastCardRowIndex == originalIndex) {
                    element.rowIndex = currentRowIndex;
                } else {
                    if (lastWasCard && lastCardRowIndex != originalIndex) {
                        currentRowIndex++;
                    } else if (!lastWasCard) {
                        if (result.Count > 0) {
                            currentRowIndex++;
                        }
                    }
                    element.rowIndex = currentRowIndex;
                    lastCardRowIndex = originalIndex;
                }
                lastWasCard = true;
            } else {
                if (!lastWasCard && lastNonCardRowIndex == originalIndex) {
                    element.rowIndex = currentRowIndex;
                } else {
                    if (result.Count > 0) {
                        currentRowIndex++;
                    }
                    element.rowIndex = currentRowIndex;
                    lastNonCardRowIndex = originalIndex;
                }
                lastWasCard = false;
            }
            result.Add(element);
        }

        return result;
    }

    #endregion


    #region Pagination

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int GetPageCount() {
        return menuPages.Count > 0 ? menuPages.Count : 1;
    }

    /// <summary>
    /// Ensures currentPageIndex is within valid bounds.
    /// </summary>
    private void EnsurePageInBounds() {
        var pageCount = GetPageCount();
        if (pageCount == 0) {
            currentPageIndex = 0;
        } else if (currentPageIndex >= pageCount) {
            currentPageIndex = pageCount - 1;
        } else if (currentPageIndex < 0) {
            currentPageIndex = 0;
        }
    }

    /// <summary>
    /// Splits components into pages based on maxItems/maxRows limits.
    /// </summary>
    private List<List<MenuElement>> AutoPaginateComponents(List<MenuElement> allComponents, int? maxItems, int? maxRows) {
        if (maxItems == null && maxRows == null) {
            return [allComponents];
        }

        var pages = new List<List<MenuElement>>();
        var currentPage = new List<MenuElement>();
        int currentItemCount = 0;
        int currentRowCount = 0;
        int lastRowIndex = -1;

        foreach (var component in allComponents) {
            bool newRow = component.rowIndex != lastRowIndex;
            int newRowCount = currentRowCount + (newRow ? 1 : 0);
            int newItemCount = currentItemCount + 1;

            bool exceedsItems = maxItems.HasValue && newItemCount > maxItems.Value;
            bool exceedsRows = maxRows.HasValue && newRowCount > maxRows.Value;

            if ((exceedsItems || exceedsRows) && currentPage.Count > 0) {
                pages.Add(currentPage);
                currentPage = new List<MenuElement>();
                currentItemCount = 0;
                currentRowCount = 0;
                lastRowIndex = -1;
                newRow = true;
            }

            currentPage.Add(component);
            currentItemCount++;
            if (newRow) {
                currentRowCount++;
                lastRowIndex = component.rowIndex;
            }
        }

        if (currentPage.Count > 0) {
            pages.Add(currentPage);
        }

        return pages.Count > 0 ? pages : [new List<MenuElement>()];
    }

    #endregion


    #region Navigation

    /// <summary>
    /// Navigates to another page by ID.
    /// </summary>
    /// <param name="pageId">Target page ID.</param>
    /// <param name="subPage">If true, opens as sub-page with back button.</param>
    /// <param name="props">Optional properties to pass to target page.</param>
    /// <returns>PageHandle for the target page.</returns>
    public async Task<PageHandle?> NavigateToAsync(string pageId, bool subPage, Dictionary<string, object?>? props = null) {
        MessagePage? targetPage;

        if (props is not null) {
            targetPage = pageManager.GetPage(pageId, botUser, null, props);
            if (targetPage is null) {
                throw new InvalidOperationException($"Page '{pageId}' not found. Available pages: {string.Join(", ", pageManager.GetPageIds())}");
            }

            if (targetPage is ScriptPage scriptTargetPage) {
                scriptTargetPage.context.SetProps(props);
            }
        } else {
            targetPage = botUser.GetOrCreateCachedPage(pageId, pageManager);
            if (targetPage is null) {
                throw new InvalidOperationException($"Page '{pageId}' not found. Available pages: {string.Join(", ", pageManager.GetPageIds())}");
            }
        }

        if (subPage) {
            await OpenSubPageAsync(targetPage);
        } else {
            await OpenPageAsync(targetPage);
        }

        if (targetPage is ScriptPage scriptPage) {
            scriptPage.handle = new PageHandle(scriptPage, this.handle);
            return scriptPage.handle;
        }
        return null;
    }

    /// <summary>
    /// Navigates to a fresh (non-cached) page instance.
    /// Use when you need independent page state for each navigation.
    /// </summary>
    public async Task<PageHandle?> NavigateToFreshAsync(string pageId, bool subPage, Dictionary<string, object?>? props = null) {
        var targetPage = pageManager.GetPage(pageId, botUser, null, props);
        if (targetPage is null) {
            throw new InvalidOperationException($"Page '{pageId}' not found. Available pages: {string.Join(", ", pageManager.GetPageIds())}");
        }

        if (props is not null && targetPage is ScriptPage scriptTargetPage) {
            scriptTargetPage.context.SetProps(props);
        }

        if (subPage) {
            await OpenSubPageAsync(targetPage);
        } else {
            await OpenPageAsync(targetPage);
        }

        if (targetPage is ScriptPage scriptPage) {
            scriptPage.handle = new PageHandle(scriptPage, this.handle);
            return scriptPage.handle;
        }
        return null;
    }

    /// <summary>
    /// Sends a page as a new message (no back button).
    /// </summary>
    public async Task SendPageByIdAsync(string pageId) {
        var targetPage = pageManager.GetPage(pageId, botUser);
        if (targetPage is null) {
            throw new InvalidOperationException($"Page '{pageId}' not found. Available pages: {string.Join(", ", pageManager.GetPageIds())}");
        }
        targetPage.parent = this;
        targetPage.backToParent = false;
        botUser.RegisterPage(targetPage);
        await targetPage.SendPageAsync();
    }

    #endregion


    #region Child Component Creation

    /// <summary>
    /// Creates a child component from an IElement. Used by container components like MenuCard.
    /// </summary>
    public async Task<MenuElement?> CreateChildComponentAsync(AngleSharp.Dom.IElement element) {
        if (registry is null) {
            return null;
        }

        var tagName = element.TagName.ToLower();
        var component = await registry.CreateAsync(tagName, element, context, this);
        if (component is not null) {
            component.scriptContext = context;
            var id = element.GetAttribute("id") ?? Guid.NewGuid().ToString();
            context.RegisterComponent(id, component);
        }
        return component;
    }

    /// <summary>
    /// Creates child components from an IElement, expanding v-for if present.
    /// Used by container components like MenuCard.
    /// </summary>
    public async Task<List<MenuElement>> CreateChildComponentsExpandedAsync(AngleSharp.Dom.IElement element) {
        if (registry is null) {
            return new();
        }

        var vForAttr = element.GetAttribute("v-for");
        if (!string.IsNullOrEmpty(vForAttr)) {
            return await ExpandVForChildAsync(element, vForAttr);
        }

        var component = await CreateChildComponentAsync(element);
        if (component is not null) {
            return new List<MenuElement> { component };
        }
        return new();
    }

    /// <summary>
    /// Expands a v-for directive on a child element.
    /// </summary>
    private async Task<List<MenuElement>> ExpandVForChildAsync(AngleSharp.Dom.IElement element, string vForExpr) {
        var result = new List<MenuElement>();

        var match = System.Text.RegularExpressions.Regex.Match(
            vForExpr,
            @"^\s*(?:\(?\s*(\w+)\s*(?:,\s*(\w+))?\s*\)?)\s+in\s+(.+)$"
        );

        if (!match.Success) {
            return result;
        }

        var itemName = match.Groups[1].Value;
        var indexName = string.IsNullOrEmpty(match.Groups[2].Value) ? null : match.Groups[2].Value;
        var expression = match.Groups[3].Value.Trim();

        var collectionValue = context.Engine.Evaluate(expression);

        if (collectionValue.IsNull() || collectionValue.IsUndefined()) {
            return result;
        }

        IEnumerable<object?> items;
        if (collectionValue is ArrayInstance arr) {
            items = arr.Select(v => v.ToObject()).ToList();
        } else {
            var obj = collectionValue.ToObject();
            if (obj is System.Collections.IEnumerable enumerable) {
                items = enumerable.Cast<object?>().ToList();
            } else {
                return result;
            }
        }

        int index = 0;
        var tagName = element.TagName.ToLower();
        var baseId = element.GetAttribute("id") ?? tagName;

        foreach (var item in items) {
            context.SetValue(itemName, item);
            if (indexName is not null) {
                context.SetValue(indexName, index);
            }

            var component = await registry!.CreateAsync(tagName, element, context, this);
            if (component is not null) {
                component.scriptContext = context;

                if (component is Components.AutoComponent autoComp) {
                    var idxName = indexName ?? "index";
                    autoComp.FreezeProps(itemName, item, idxName, index);
                }

                var renderedId = await context.RenderAsync(baseId);
                context.RegisterComponent(renderedId, component);
                result.Add(component);
            }

            index++;
        }

        context.SetValue(itemName, JsValue.Undefined);
        if (indexName is not null) {
            context.SetValue(indexName, JsValue.Undefined);
        }

        return result;
    }

    #endregion


    #region Media Handlers

    /// <summary>
    /// Handles a photo sent by the user.
    /// Calls onPhoto callbacks registered in JavaScript with photo metadata.
    /// </summary>
    public async Task HandlePhotoAsync(Telegram.Bot.Types.PhotoSize[] photos, Telegram.Bot.Types.Message message) {
        if (!hasPhotoHandler) {
            return;
        }

        var photo = photos.LastOrDefault();
        if (photo is null) {
            return;
        }

        var photoData = new Dictionary<string, object?> {
            ["fileId"] = photo.FileId,
            ["fileUniqueId"] = photo.FileUniqueId,
            ["width"] = photo.Width,
            ["height"] = photo.Height,
            ["fileSize"] = photo.FileSize,
            ["messageId"] = message.MessageId,
            ["caption"] = message.Caption
        };

        await context.InvokePhoto(photoData);

        if (!context.navigated && lastMessage is not null) {
            await UpdatePageAsync(lastMessage.MessageId, lastMessage.Chat.Id);
        }
        context.navigated = false;
    }

    /// <summary>
    /// Handles a document sent by the user.
    /// Calls onDocument callbacks registered in JavaScript with document metadata.
    /// </summary>
    public async Task HandleDocumentAsync(Telegram.Bot.Types.Document document, Telegram.Bot.Types.Message message) {
        if (!hasDocumentHandler) {
            return;
        }

        var documentData = new Dictionary<string, object?> {
            ["fileId"] = document.FileId,
            ["fileUniqueId"] = document.FileUniqueId,
            ["fileName"] = document.FileName,
            ["mimeType"] = document.MimeType,
            ["fileSize"] = document.FileSize,
            ["messageId"] = message.MessageId,
            ["caption"] = message.Caption
        };

        await context.InvokeDocument(documentData);

        if (!context.navigated && lastMessage is not null) {
            await UpdatePageAsync(lastMessage.MessageId, lastMessage.Chat.Id);
        }
        context.navigated = false;
    }

    #endregion


    #region Disposal

    /// <inheritdoc />
    protected override void OnDispose() {
        OnDisposeAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    protected override async Task OnDisposeAsync() {
        await context.InvokeUnmounted();

        foreach (var component in components) {
            component.Dispose();
        }
        components.Clear();

        foreach (var page in menuPages) {
            foreach (var component in page) {
                component.Dispose();
            }
        }
        menuPages.Clear();

        navigateComponent?.Dispose();
        context.Dispose();

        if (vmodel is IAsyncDisposable asyncDisposable) {
            await asyncDisposable.DisposeAsync();
        } else if (vmodel is IDisposable disposable) {
            disposable.Dispose();
        }
    }

    #endregion
}
