using Localization;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.Menu;
using Telegram.Bot.UI.Menu.Selectors;

namespace Telegram.Bot.UI;


/// <summary>
/// Base class for Telegram message pages with inline keyboard navigation.
/// Provides text rendering, media support, navigation chain, and menu building.
/// </summary>
public abstract class MessagePage : IDisposable, IAsyncDisposable {
    #region Properties

    /// <summary>
    /// Unique identifier for this page type. Used for page registration and lookup.
    /// </summary>
    public virtual string? pageId { get; } = null;

    /// <summary>
    /// Resource path for page content template.
    /// </summary>
    public virtual string? pageResource { get; } = null;

    /// <summary>
    /// Display title for this page. Used in back button templates.
    /// </summary>
    public virtual string? title { get; } = null;

    /// <summary>
    /// Template for back button text. Default includes parent title placeholder.
    /// </summary>
    public virtual string backTitle => "« {{ Base.parent.title }}";

    /// <summary>
    /// Gets the rendered text content for this page.
    /// </summary>
    public virtual Task<string?> text => BuildTextTemplate();

    /// <summary>
    /// Currently selected button page index for multi-page keyboards.
    /// </summary>
    public int selectedPage { get; set; } = 0;

    /// <summary>
    /// Parent page in navigation hierarchy. Used for back navigation.
    /// </summary>
    public MessagePage? parent { get; set; }

    /// <summary>
    /// Whether to show back button to parent page.
    /// </summary>
    public bool backToParent { get; set; } = true;

    /// <summary>
    /// Bot user context for this page.
    /// </summary>
    public BaseBotUser botUser { get; private set; }

    /// <summary>
    /// Whether to show link previews in message text.
    /// </summary>
    public bool webPreview { get; set; } = true;

    /// <summary>
    /// Parse mode for message formatting. Null uses botUser default.
    /// </summary>
    public ParseMode? parseMode { get; set; } = null;

    /// <summary>
    /// Media attachment for this page. Null for text-only pages.
    /// </summary>
    public Parsing.MediaDefinition? media { get; set; } = null;

    /// <summary>
    /// Last sent/edited message for this page instance.
    /// </summary>
    public Message? lastMessage { get; set; } = null;

    private bool disposed { get; set; } = false;

    #endregion


    #region Constructor and Disposal

    /// <summary>
    /// Creates a new MessagePage instance.
    /// </summary>
    /// <param name="botUser">Bot user context.</param>
    public MessagePage(BaseBotUser botUser) {
        this.botUser = botUser;
    }


    /// <summary>
    /// Destructor ensures disposal.
    /// </summary>
    ~MessagePage() {
        Dispose();
    }


    /// <summary>
    /// Disposes page resources synchronously.
    /// </summary>
    public void Dispose() {
        if (!disposed) {
            OnDispose();
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }


    /// <summary>
    /// Disposes page resources asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync() {
        if (!disposed) {
            await OnDisposeAsync();
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }


    /// <summary>
    /// Override to add custom synchronous cleanup logic.
    /// </summary>
    protected virtual void OnDispose() { }


    /// <summary>
    /// Override to add custom async cleanup logic.
    /// </summary>
    protected virtual Task OnDisposeAsync() {
        OnDispose();
        return Task.CompletedTask;
    }

    #endregion


    #region Localization

    /// <summary>
    /// Localizes a string using current user's language.
    /// </summary>
    public string L(string text) => botUser.L(text);

    /// <summary>
    /// Gets a LocalizedString for deferred localization.
    /// </summary>
    public LocalizedString LS(string text) => botUser.LS(text);

    /// <summary>
    /// Localizes a nullable string. Returns null if input is null.
    /// </summary>
    public string? LN(string? text) => botUser.LN(text);

    /// <summary>
    /// Gets the localization manager.
    /// </summary>
    public LocalizationManager localization => botUser.localization;

    #endregion


    #region Template and Model

    /// <summary>
    /// Override to provide page button components.
    /// </summary>
    /// <returns>List of button pages, or null for no buttons.</returns>
    public virtual Task<List<ButtonsPage>?> RequestPageComponentsAsync() => Task.FromResult<List<ButtonsPage>?>(null);

    /// <summary>
    /// Override to provide message resource path for text template.
    /// </summary>
    public virtual string? RequestMessageResource() => null;

    /// <summary>
    /// Override to provide model data for template rendering.
    /// </summary>
    public virtual Task<object?> RequestModelAsync() => Task.FromResult<object?>(null);


    /// <summary>
    /// Collects model data from this page and all ancestors.
    /// </summary>
    /// <returns>List of models from root to current page.</returns>
    public async Task<List<object>> InheritedRequestModelAsync() {
        var model = new List<object>();
        var currentPage = this;

        while (currentPage is not null) {
            if (await currentPage.RequestModelAsync() is object currentModel) {
                model.Add(currentModel);
            }

            currentPage = currentPage.parent;
        }

        model.Reverse();
        return model;
    }


    /// <summary>
    /// Builds the text content by rendering the message resource template.
    /// </summary>
    protected virtual async Task<string?> BuildTextTemplate() {
        var text = RenderTemplateText(RequestMessageResource(), await InheritedRequestModelAsync());
        return string.IsNullOrEmpty(text) ? null : text;
    }


    /// <summary>
    /// Renders a text resource template with provided models.
    /// </summary>
    /// <param name="textResource">Resource path or direct text content.</param>
    /// <param name="models">Model objects for template binding.</param>
    /// <returns>Rendered text content.</returns>
    /// <exception cref="FileNotFoundException">Thrown when resource cannot be found.</exception>
    public string? RenderTemplateText(string? textResource, IEnumerable<object> models) {
        if (textResource is null) {
            return null;
        }

        string? text = null;

        // Try path-based loading
        var resolvedPath = ResolveResourcePath(textResource);
        if (!string.IsNullOrEmpty(resolvedPath)) {
            text = botUser.worker.resourceLoader.GetText(resolvedPath);
        }

        // Fallback: try loading directly from textResource
        if (text is null) {
            text = botUser.worker.resourceLoader.GetText(textResource);
        }

        if (text is null) {
            var triedPaths = new List<string>();
            if (!string.IsNullOrEmpty(resolvedPath)) {
                triedPaths.Add(resolvedPath);
            }
            triedPaths.Add(textResource);
            throw new FileNotFoundException(
                $"Resource not found: '{textResource}'. Tried paths: {string.Join(", ", triedPaths)}",
                textResource
            );
        }

        if (botUser.scriptContext is not null) {
            return botUser.scriptContext.RenderAsync(text).GetAwaiter().GetResult();
        }

        return text;
    }

    /// <summary>
    /// Resolves a resource path. Override in ScriptPage for directory-relative resolution.
    /// </summary>
    protected virtual string? ResolveResourcePath(string? path) {
        return null;
    }

    #endregion


    #region Navigation

    /// <summary>
    /// Opens a sub-page by editing the current message. Maintains navigation chain.
    /// </summary>
    /// <param name="page">Page to navigate to.</param>
    /// <returns>Updated message.</returns>
    /// <exception cref="InvalidOperationException">Thrown when current page has no message or formats are incompatible.</exception>
    public async Task<Message> OpenSubPageAsync(MessagePage page) {
        if (lastMessage is null) {
            throw new InvalidOperationException(
                $"Cannot navigate: current page has no lastMessage. " +
                $"The page must be sent first before navigating to sub-pages."
            );
        }

        // Handle parent assignment carefully to avoid circular references
        if (page != this) {
            // Check if navigating to same page ID (fresh instance of same page)
            if (page.pageId is not null && page.pageId == this.pageId) {
                // Same page ID, different instance - inherit our parent
                // so back button returns to original navigation point
                page.parent = this.parent;
            } else {
                // Different page - we become the parent
                page.parent = this;
            }
        }
        // If page == this (self-navigation), don't change parent - it's already correct

        page.backToParent = true;
        botUser.RegisterPage(page);

        bool currentHasMedia = this.media is not null;
        bool targetHasMedia = page.media is not null;

        // Sub-pages must have compatible formats - can't edit text to media or vice versa
        if (currentHasMedia != targetHasMedia) {
            var currentType = currentHasMedia ? "media" : "text";
            var targetType = targetHasMedia ? "media" : "text";
            throw new InvalidOperationException(
                $"Cannot open sub-page '{page.pageId}': incompatible format. " +
                $"Current page is {currentType}, target is {targetType}. "
            );
        }

        if (targetHasMedia) {
            // Both have media - use EditMessageCaption
            lastMessage = await botUser.client.EditMessageCaption(
                chatId: lastMessage.Chat.Id,
                messageId: lastMessage.MessageId,
                caption: await page.text,
                parseMode: page.parseMode ?? botUser.parseMode,
                replyMarkup: await page.BuildSelectedPageAsync(),
                cancellationToken: botUser.cancellationToken
            );
        } else {
            // Both are text - use EditMessageText
            lastMessage = await botUser.EditMessageTextAsync(
                lastMessage.MessageId,
                lastMessage.Chat.Id,
                await page.text,
                await page.BuildSelectedPageAsync(),
                mode: page.parseMode,
                webPreview: page.webPreview
            );
        }

        page.lastMessage = lastMessage;
        return lastMessage;
    }


    /// <summary>
    /// Opens a page, replacing current content. Does not add back button.
    /// </summary>
    /// <param name="page">Page to navigate to.</param>
    /// <returns>Updated or new message.</returns>
    public async Task<Message> OpenPageAsync(MessagePage page) {
        page.parent = this;
        page.backToParent = false;
        botUser.RegisterPage(page);

        // If lastMessage is null (e.g., after close()), send as new message
        if (lastMessage is null) {
            return await page.SendPageAsync();
        }

        bool currentHasMedia = this.media is not null;
        bool targetHasMedia = page.media is not null;

        // Check if formats are compatible (both text or both media)
        if (currentHasMedia != targetHasMedia) {
            // Incompatible formats - send as new message instead of editing
            return await page.SendPageAsync();
        }

        if (targetHasMedia) {
            // Both have media - use EditMessageCaption
            lastMessage = await botUser.client.EditMessageCaption(
                chatId: lastMessage!.Chat.Id,
                messageId: lastMessage.MessageId,
                caption: await page.text,
                parseMode: page.parseMode ?? botUser.parseMode,
                replyMarkup: await page.BuildSelectedPageAsync(),
                cancellationToken: botUser.cancellationToken
            );
        } else {
            // Both are text - use EditMessageText
            lastMessage = await botUser.EditMessageTextAsync(
                lastMessage!.MessageId,
                lastMessage!.Chat.Id,
                await page.text,
                await page.BuildSelectedPageAsync(),
                mode: page.parseMode,
                webPreview: page.webPreview
            );
        }

        page.lastMessage = lastMessage;
        return lastMessage;
    }

    #endregion


    #region Send Page

    /// <summary>
    /// Sends this page as a new message.
    /// </summary>
    /// <returns>Sent message.</returns>
    public async Task<Message> SendPageAsync() {
        botUser.RegisterPage(this);
        if (media is not null) {
            return await SendPageWithMediaAsync();
        }
        var textContent = await text;
        var markup = await BuildSelectedPageAsync();
        lastMessage = await botUser.SendTextMessageAsync(textContent, markup, mode: parseMode, webPreview: webPreview);
        return lastMessage;
    }

    /// <summary>
    /// Sends page with media attachment.
    /// </summary>
    protected virtual async Task<Message> SendPageWithMediaAsync() {
        var caption = await text;
        var markup = await BuildSelectedPageAsync();
        var src = ResolveResourcePath(media!.src) ?? media.src;

        // Check if it's a file_id (no path separators) or a file path
        var isFileId = !src.Contains('/') && !src.Contains('\\') && !File.Exists(src);

        if (isFileId) {
            lastMessage = media.type switch {
                Parsing.MediaType.Photo => await botUser.SendPhotoAsync(src, caption, markup, parseMode),
                Parsing.MediaType.Document => await botUser.SendDocumentAsync(src, caption, markup, parseMode),
                Parsing.MediaType.Audio => await botUser.client.SendAudio(botUser.chatId, InputFile.FromFileId(src), caption: caption, replyMarkup: markup, parseMode: parseMode ?? botUser.parseMode),
                Parsing.MediaType.Video => await botUser.client.SendVideo(botUser.chatId, InputFile.FromFileId(src), caption: caption, replyMarkup: markup, parseMode: parseMode ?? botUser.parseMode),
                _ => await botUser.SendTextMessageAsync(caption, markup, parseMode, webPreview)
            };
        } else {
            // Use using to ensure file streams are properly disposed
            await using var stream = File.OpenRead(src);
            var fileName = Path.GetFileName(src);
            lastMessage = media.type switch {
                Parsing.MediaType.Photo => await botUser.SendPhotoAsync(InputFile.FromStream(stream, fileName), caption, markup, parseMode),
                Parsing.MediaType.Document => await botUser.SendDocumentAsync(InputFile.FromStream(stream, fileName), caption, markup, parseMode),
                Parsing.MediaType.Audio => await botUser.client.SendAudio(botUser.chatId, InputFile.FromStream(stream, fileName), caption: caption, replyMarkup: markup, parseMode: parseMode ?? botUser.parseMode),
                Parsing.MediaType.Video => await botUser.client.SendVideo(botUser.chatId, InputFile.FromStream(stream, fileName), caption: caption, replyMarkup: markup, parseMode: parseMode ?? botUser.parseMode),
                _ => await botUser.SendTextMessageAsync(caption, markup, parseMode, webPreview)
            };
        }

        return lastMessage;
    }

    #endregion


    #region Send Media

    /// <summary>
    /// Sends page with a document attachment.
    /// </summary>
    public async Task<Message> SendDocumentAsync(InputFile file) {
        lastMessage = await botUser.SendDocumentAsync(file, await text, await BuildSelectedPageAsync());
        return lastMessage;
    }

    /// <summary>
    /// Sends page with a document from byte array.
    /// </summary>
    public async Task<Message> SendDocumentAsync(byte[] file) {
        lastMessage = await botUser.SendDocumentAsync(file, await text, await BuildSelectedPageAsync());
        return lastMessage;
    }

    /// <summary>
    /// Sends page with a document by file ID.
    /// </summary>
    public async Task<Message> SendDocumentAsync(string fileId) {
        lastMessage = await botUser.SendDocumentAsync(fileId, await text, await BuildSelectedPageAsync());
        return lastMessage;
    }

    /// <summary>
    /// Sends page with a photo attachment.
    /// </summary>
    public async Task<Message> SendPhotoAsync(InputFile image) {
        lastMessage = await botUser.SendPhotoAsync(image, await text, await BuildSelectedPageAsync());
        return lastMessage;
    }

    /// <summary>
    /// Sends page with a photo from byte array.
    /// </summary>
    public async Task<Message> SendPhotoAsync(byte[] image) {
        lastMessage = await botUser.SendPhotoAsync(image, await text, await BuildSelectedPageAsync());
        return lastMessage;
    }

    /// <summary>
    /// Sends page with a photo by file ID.
    /// </summary>
    public async Task<Message> SendPhotoAsync(string imageId) {
        lastMessage = await botUser.SendPhotoAsync(imageId, await text, await BuildSelectedPageAsync());
        return lastMessage;
    }

    #endregion


    #region Update and Delete

    /// <summary>
    /// Updates an existing message with current page content.
    /// </summary>
    /// <param name="messageId">Message ID to update.</param>
    /// <param name="chatId">Chat ID containing the message.</param>
    /// <returns>Updated message.</returns>
    public async Task<Message> UpdatePageAsync(int messageId, long chatId) {
        var content = await text;
        var markup = await BuildSelectedPageAsync();

        if (media is not null) {
            // Media page - use EditMessageCaption
            lastMessage = await botUser.client.EditMessageCaption(
                chatId: chatId,
                messageId: messageId,
                caption: content,
                parseMode: parseMode ?? botUser.parseMode,
                replyMarkup: markup,
                cancellationToken: botUser.cancellationToken
            );
        } else {
            // Text page - use EditMessageText
            lastMessage = await botUser.EditMessageTextAsync(messageId, chatId, content, markup, mode: parseMode, webPreview: webPreview);
        }

        return lastMessage;
    }


    /// <summary>
    /// Deletes the page message from chat.
    /// </summary>
    public async Task DeletePageAsync() {
        if (lastMessage?.MessageId is int messageId) {
            await botUser.DeleteMessageAsync(messageId);
            lastMessage = null;
        }
    }

    #endregion


    #region Keyboard Building

    /// <summary>
    /// Builds inline keyboard markup for the selected button page.
    /// </summary>
    private async Task<InlineKeyboardMarkup?> BuildSelectedPageAsync() {
        var buttonsPages = await RequestPageComponentsAsync();
        if (buttonsPages is null && (!backToParent || parent is null)) {
            return null;
        }

        if (buttonsPages is not null && (selectedPage > buttonsPages.Count() || selectedPage < 0)) {
            throw new Exception($"{nameof(selectedPage)} out of range. buttonsPages.Count(): {buttonsPages.Count()}; selectedPage: {selectedPage}");
        }

        var buttonsPage = buttonsPages is not null ? buttonsPages[selectedPage] : new() {
            page = new MenuElement[][] { }
        };

        if (backToParent && parent is not null) {
            buttonsPage.page = buttonsPage.page.Concat(new MenuElement[][] {
                [ CreateBackButton(parent) ]
            });
        }

        return new InlineKeyboardMarkup(await BuildButtonsPageAsync(buttonsPage));
    }


    /// <summary>
    /// Converts ButtonsPage to Telegram InlineKeyboardButton rows.
    /// </summary>
    private async Task<List<List<InlineKeyboardButton>>> BuildButtonsPageAsync(ButtonsPage buttonsPage) {
        List<List<InlineKeyboardButton>> menu = new();

        foreach (var row in buttonsPage.page) {
            List<InlineKeyboardButton> rowInline = new();

            foreach (var button in row) {
                if (button is MenuSplit) {
                    menu.Add(rowInline);
                    rowInline = new();
                } else {
                    var buildList = await button.BuildAsync();

                    int count = 0;
                    foreach (var build in buildList) {
                        rowInline.Add(build);

                        if (++count >= button.columns) {
                            menu.Add(rowInline);
                            rowInline = new();
                            count = 0;
                        }
                    }
                }
            }

            if (rowInline.Count > 0) {
                menu.Add(rowInline);
            }
        }

        return menu;
    }

    #endregion


    #region Menu Element Factories

    /// <summary>
    /// Creates a checkbox menu element.
    /// </summary>
    protected MenuCheckbox MenuCheckbox(string title) =>
        new() { title = title, botUser = botUser, parent = this };

    /// <summary>
    /// Creates a radio button group.
    /// </summary>
    protected MenuRadio MenuRadio(IEnumerable<MenuSelector> buttons) =>
        new() { buttons = buttons.ToList(), botUser = botUser, parent = this };

    /// <summary>
    /// Creates a switch button group.
    /// </summary>
    protected MenuSwitch MenuSwitch(IEnumerable<MenuSelector> buttons) =>
        new() { buttons = buttons.ToList(), botUser = botUser, parent = this };

    /// <summary>
    /// Creates a command button.
    /// </summary>
    protected MenuCommand MenuCommand(string title) =>
        new() { title = title, botUser = botUser, parent = this };

    /// <summary>
    /// Creates a back navigation button.
    /// </summary>
    protected virtual MenuOpen CreateBackButton(MessagePage targetPage) =>
        new() {
            targetPage = targetPage,
            title = backTitle,
            botUser = botUser,
            parent = this
        };

    /// <summary>
    /// Creates an external link button.
    /// </summary>
    protected MenuOpen MenuLink(string url, string title) =>
        new() { typeAttr = "link", target = url, botUser = botUser, title = title, parent = this };

    /// <summary>
    /// Creates a web app button.
    /// </summary>
    protected MenuOpen MenuWebApp(string url, string title) =>
        new() { typeAttr = "app", target = url, botUser = botUser, title = title, parent = this };

    /// <summary>
    /// Creates a row split marker.
    /// </summary>
    protected MenuSplit MenuSplit() =>
        new() { botUser = botUser, parent = this };

    /// <summary>
    /// Creates a navigation panel with pagination controls.
    /// </summary>
    protected MenuNavigatePanel MenuNavigatePanel() =>
        new() { botUser = botUser, parent = this };

    #endregion
}
