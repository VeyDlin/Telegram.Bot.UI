using Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.BotWorker;
using Telegram.Bot.UI.Runtime;
using Telegram.Bot.UI.Utils;

namespace Telegram.Bot.UI;


/// <summary>
/// Base class for bot user sessions. Each chat has its own user instance.
/// Override virtual methods to handle messages, commands, callbacks, etc.
/// </summary>
public abstract class BaseBotUser : IDisposable {
    private bool disposed = false;
    private readonly ConcurrentLimitedList<MessagePage> activePages = new(maxItems: 10);

    /// <summary>Telegram chat ID.</summary>
    public long chatId { get; private set; }

    /// <summary>Parent bot worker.</summary>
    public IBotWorker worker { get; private set; }

    /// <summary>Telegram bot client.</summary>
    public ITelegramBotClient client { get; private set; }

    /// <summary>Cancellation token for this session.</summary>
    public CancellationToken cancellationToken { get; private set; }

    /// <summary>Factory for creating inline keyboard callbacks.</summary>
    public CallbackFactory callbackFactory { get; private set; } = new();

    /// <summary>Localization manager for this user.</summary>
    public LocalizationManager localization { get; private set; } = new();

    /// <summary>Current script context (set during page rendering).</summary>
    public ScriptContext? scriptContext { get; set; }

    /// <summary>Default parse mode for messages. Default: Markdown.</summary>
    public ParseMode parseMode { get; set; } = ParseMode.Markdown;

    /// <summary>Whether user accepted license. If false, HandleAcceptLicense is called.</summary>
    public bool acceptLicense { get; set; } = true;

    /// <summary>Whether to process /commands. Default: true.</summary>
    public bool enableCommands { get; set; } = true;

    /// <summary>Logger instance.</summary>
    public ILogger logger { get; set; } = NullLogger.Instance;


    /// <summary>
    /// Creates a new user session.
    /// </summary>
    public BaseBotUser(IBotWorker worker, long chatId, ITelegramBotClient client, CancellationToken token) {
        this.worker = worker;
        this.chatId = chatId;
        this.client = client;
        cancellationToken = token;
        localization = new(worker.localizationPack);
    }


    /// <inheritdoc/>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Disposes resources.</summary>
    protected virtual void Dispose(bool disposing) {
        if (!disposed) {
            if (disposing) {
                DisposeResources();
            }
            disposed = true;
        }
    }

    /// <summary>Finalizer.</summary>
    ~BaseBotUser() {
        Dispose(false);
    }


    #region Localization helpers

    /// <summary>Translate text using current locale.</summary>
    public string L(string text) => localization.TryTranslate(text);

    /// <summary>Create lazy-translated string.</summary>
    public LocalizedString LS(string text) => new(localization, text);

    /// <summary>Translate text or return null.</summary>
    public string? LN(string? text) => localization.Translate(text);

    #endregion


    #region Lifecycle hooks

    /// <summary>Called once when user is first created.</summary>
    public virtual Task CreateAsync(Message? message) => Task.CompletedTask;

    /// <summary>Called at start of each request.</summary>
    public virtual void Begin(Message? message) { }

    /// <summary>Called at start of each request (async).</summary>
    public virtual Task BeginAsync(Message? message) => Task.CompletedTask;

    /// <summary>Called at end of each request.</summary>
    public virtual void End() { }

    /// <summary>Called at end of each request (async).</summary>
    public virtual Task EndAsync() => Task.CompletedTask;

    /// <summary>Called during disposal to clean up resources.</summary>
    public virtual void DisposeResources() { }

    #endregion


    #region Message handlers

    /// <summary>Handle text message.</summary>
    public virtual Task HandleMessageAsync(string text, Message message) => Task.CompletedTask;

    /// <summary>Handle photo message.</summary>
    public virtual Task HandlePhotoAsync(PhotoSize[] photo, Message message) => Task.CompletedTask;

    /// <summary>Handle document message.</summary>
    public virtual Task HandleDocumentAsync(Document document, Message message) => Task.CompletedTask;

    /// <summary>Handle /command.</summary>
    public virtual Task HandleCommandAsync(string cmd, string[] arguments, Message message) => Task.CompletedTask;

    /// <summary>Handle errors.</summary>
    public virtual Task HandleErrorAsync(Exception exception) => Task.CompletedTask;

    /// <summary>Handle other message types (stickers, voice, etc).</summary>
    public virtual Task HandleOtherMessageAsync(Message message) => Task.CompletedTask;

    /// <summary>Check if user is allowed. Return false to block.</summary>
    public virtual Task<bool> HandlePermissiveAsync(Message message) => Task.FromResult(true);

    /// <summary>Show license agreement if acceptLicense is false.</summary>
    public virtual Task HandleAcceptLicense(Message message) => Task.CompletedTask;

    /// <summary>Handle successful payment.</summary>
    public virtual Task HandleSuccessPaymentAsync(SuccessfulPayment payment) => Task.CompletedTask;

    /// <summary>
    /// Pre-checkout validation. Return null to approve, error message to reject.
    /// </summary>
    public virtual Task<string?> HandlePreCheckoutQueryAsync(PreCheckoutQuery preCheckoutQuery)
        => Task.FromResult<string?>("error");

    #endregion


    #region Script extensions

    /// <summary>
    /// Register custom JavaScript variables and functions for page scripts.
    /// Called when a new ScriptContext is created.
    /// </summary>
    /// <example>
    /// <code>
    /// public override void RegisterScriptExtensions(ScriptContext context) {
    ///     context.SetValue("UserData", new { premium = isPremium });
    ///     context.SetValue("buyPremium", new Action(() => OpenPayment()));
    /// }
    /// </code>
    /// </example>
    public virtual void RegisterScriptExtensions(ScriptContext context) { }

    /// <summary>
    /// Get or create cached page. Override for state preservation.
    /// </summary>
    public virtual ScriptPage? GetOrCreateCachedPage(string pageId, PageManager pageManager) {
        return pageManager.GetPage(pageId, this);
    }

    #endregion


    #region Callback handling

    /// <summary>
    /// Invoke registered callback by ID.
    /// </summary>
    public Task<bool> HandleCallbackAsync(string callbackQueryId, string callbackId, int messageId, long chatId) {
        return callbackFactory.InvokeAsync(callbackQueryId, callbackId, messageId, chatId);
    }

    /// <summary>
    /// Handle rejected callback query. Override to customize behavior.
    /// Default implementation shows an alert with reason message.
    /// </summary>
    /// <param name="reason">Why the callback was rejected.</param>
    /// <param name="callbackQueryId">Telegram callback query ID.</param>
    public virtual Task HandleRejectedCallbackAsync(RejectedCallback reason, string callbackQueryId) {
        var message = reason switch {
            RejectedCallback.Skip => "The message is outdated",
            RejectedCallback.Permission => "You don't have permission",
            RejectedCallback.Unknown => "Unknown action",
            _ => "Error"
        };
        return ShowAlertAsync(message, callbackQueryId, showAlert: false);
    }

    #endregion


    #region Chat actions

    /// <summary>Send chat action (typing, uploading, etc).</summary>
    public Task SendChatActionAsync(ChatAction action, CancellationToken cancellationToken = default) {
        return client.SendChatAction(chatId: chatId, action: action, cancellationToken: cancellationToken);
    }

    /// <summary>Send repeating chat action until cancelled.</summary>
    public void SendChatLongAction(ChatAction action, CancellationTokenSource token, int delay = 4000) {
        _ = Task.Run(async () => {
            while (!token.IsCancellationRequested) {
                await SendChatActionAsync(ChatAction.UploadPhoto, token.Token);
                await Task.Delay(delay);
            }
        });
    }

    #endregion


    #region Send messages

    /// <summary>Send message with URL button.</summary>
    public Task<Message> SendUrlButtonsAsync(string message, string text, string url) {
        return client.SendMessage(
            chatId: chatId,
            text: message,
            cancellationToken: cancellationToken,
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl(text, url))
        );
    }

    /// <summary>Delete message by ID.</summary>
    public Task DeleteMessageAsync(int messageId) {
        return client.DeleteMessage(chatId: chatId, messageId: messageId);
    }

    /// <summary>Send text message.</summary>
    public Task<Message> SendTextMessageAsync(
        string? text,
        ReplyMarkup? markup = null,
        ParseMode? mode = null,
        bool webPreview = true
    ) {
        return client.SendMessage(
            chatId: chatId,
            text: string.IsNullOrEmpty(text) ? "..." : text,
            parseMode: mode ?? parseMode,
            cancellationToken: cancellationToken,
            replyMarkup: markup,
            linkPreviewOptions: new() { IsDisabled = !webPreview }
        );
    }

    /// <summary>Send document.</summary>
    public Task<Message> SendDocumentAsync(
        InputFile file,
        string? text = null,
        ReplyMarkup? markup = null,
        ParseMode? mode = null
    ) {
        return client.SendDocument(
            chatId: chatId,
            document: file,
            caption: text,
            parseMode: mode ?? parseMode,
            cancellationToken: cancellationToken,
            replyMarkup: markup
        );
    }

    /// <summary>Send document from bytes.</summary>
    public Task<Message> SendDocumentAsync(byte[] file, string? text = null, ReplyMarkup? markup = null, ParseMode? mode = null) {
        return SendDocumentAsync(InputFile.FromStream(new MemoryStream(file)), text, markup, mode);
    }

    /// <summary>Send document by file ID.</summary>
    public Task<Message> SendDocumentAsync(string fileId, string? text = null, ReplyMarkup? markup = null, ParseMode? mode = null) {
        return SendDocumentAsync(InputFile.FromString(fileId), text, markup, mode);
    }

    /// <summary>Send photo.</summary>
    public Task<Message> SendPhotoAsync(
        InputFile image,
        string? text = null,
        ReplyMarkup? markup = null,
        ParseMode? mode = null
    ) {
        return client.SendPhoto(
            chatId: chatId,
            photo: image,
            caption: text,
            parseMode: mode ?? parseMode,
            cancellationToken: cancellationToken,
            replyMarkup: markup
        );
    }

    /// <summary>Send photo from bytes.</summary>
    public Task<Message> SendPhotoAsync(byte[] image, string? text = null, ReplyMarkup? markup = null, ParseMode? mode = null) {
        return SendPhotoAsync(InputFile.FromStream(new MemoryStream(image)), text, markup, mode);
    }

    /// <summary>Send photo by file ID.</summary>
    public Task<Message> SendPhotoAsync(string imageId, string? text = null, ReplyMarkup? markup = null, ParseMode? mode = null) {
        return SendPhotoAsync(InputFile.FromString(imageId), text, markup, mode);
    }

    #endregion


    #region Edit messages

    /// <summary>Edit message text.</summary>
    public Task<Message> EditMessageTextAsync(
        int messageId,
        long chatId,
        string? text = null,
        InlineKeyboardMarkup? markup = null,
        ParseMode? mode = null,
        bool webPreview = true
    ) {
        return client.EditMessageText(
           chatId: chatId,
           messageId: messageId,
           text: string.IsNullOrEmpty(text) ? "..." : text,
           parseMode: mode ?? parseMode,
           replyMarkup: markup,
           linkPreviewOptions: new() { IsDisabled = !webPreview }
        );
    }

    /// <summary>Edit message media.</summary>
    public Task<Message> EditMessageMediaAsync(
        int messageId,
        long chatId,
        InputMedia media,
        InlineKeyboardMarkup? markup = null
    ) {
        return client.EditMessageMedia(chatId: chatId, messageId: messageId, media: media, replyMarkup: markup);
    }

    /// <summary>Edit message with new image from bytes.</summary>
    public Task<Message> EditMessageImageAsync(
        int messageId,
        long chatId,
        byte[] image,
        string? text = null,
        InlineKeyboardMarkup? markup = null
    ) {
        return EditMessageMediaAsync(
            messageId, chatId,
            new InputMediaPhoto(InputFile.FromStream(new MemoryStream(image))) {
                Caption = text,
                ParseMode = ParseMode.Markdown
            },
            markup
        );
    }

    /// <summary>Edit message with new image by file ID.</summary>
    public Task<Message> EditMessageImageAsync(int messageId, long chatId, string imageId, InlineKeyboardMarkup? markup = null) {
        return EditMessageMediaAsync(messageId, chatId, new InputMediaPhoto(InputFile.FromString(imageId)), markup);
    }

    /// <summary>Edit message with new document from bytes.</summary>
    public Task<Message> EditMessageDocumentAsync(int messageId, long chatId, byte[] image, InlineKeyboardMarkup? markup = null) {
        return EditMessageMediaAsync(messageId, chatId, new InputMediaDocument(InputFile.FromStream(new MemoryStream(image))), markup);
    }

    /// <summary>Edit message with new document by file ID.</summary>
    public Task<Message> EditMessageDocumentAsync(int messageId, long chatId, string imageId, InlineKeyboardMarkup? markup = null) {
        return EditMessageMediaAsync(messageId, chatId, new InputMediaDocument(InputFile.FromString(imageId)), markup);
    }

    #endregion


    #region Alerts

    /// <summary>Show callback alert or toast.</summary>
    public Task ShowAlertAsync(string? text, string callbackQueryId, bool showAlert = false) {
        return client.AnswerCallbackQuery(
            callbackQueryId: callbackQueryId,
            text: string.IsNullOrEmpty(text) ? "..." : text,
            showAlert: showAlert,
            cancellationToken: cancellationToken
        );
    }

    #endregion


    #region Page management

    /// <summary>Register active page. Old pages are disposed when limit reached.</summary>
    public void RegisterPage(MessagePage page) {
        var removed = activePages.Add(page);
        foreach (var oldPage in removed) {
            oldPage.Dispose();
        }
    }

    /// <summary>Get most recently active page.</summary>
    public MessagePage? GetActivePage() {
        return activePages.LastOrDefault();
    }

    /// <summary>Forward photo to active page's onPhoto handler.</summary>
    public async Task<bool> ForwardPhotoToActivePageAsync(PhotoSize[] photos, Message message) {
        var activePage = GetActivePage();
        if (activePage is ScriptPage scriptPage && scriptPage.hasPhotoHandler) {
            await scriptPage.HandlePhotoAsync(photos, message);
            return true;
        }
        return false;
    }

    /// <summary>Forward document to active page's onDocument handler.</summary>
    public async Task<bool> ForwardDocumentToActivePageAsync(Document document, Message message) {
        var activePage = GetActivePage();
        if (activePage is ScriptPage scriptPage && scriptPage.hasDocumentHandler) {
            await scriptPage.HandleDocumentAsync(document, message);
            return true;
        }
        return false;
    }

    #endregion


    #region Utilities

    /// <summary>Escape special characters for parse mode.</summary>
    public string EscapeText(string? text, ParseMode? mode = null) {
        if (string.IsNullOrEmpty(text)) {
            return text ?? "";
        }

        switch (mode ?? parseMode) {
            case ParseMode.Markdown: {
                char[] chars = { '_', '*', '`', '[' };
                foreach (var c in chars) {
                    text = text.Replace(c.ToString(), "\\" + c);
                }
                return text;
            }

            case ParseMode.MarkdownV2: {
                text = text.Replace("\\", "\\\\");
                char[] chars = { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
                foreach (var c in chars) {
                    text = text.Replace(c.ToString(), "\\" + c);
                }
                return text;
            }

            case ParseMode.Html: {
                return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
            }
        }

        return text;
    }

    #endregion
}
