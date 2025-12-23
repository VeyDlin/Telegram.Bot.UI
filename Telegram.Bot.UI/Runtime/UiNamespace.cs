using Jint;
using Jint.Native;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.UI.Runtime;


/// <summary>
/// UI namespace object exposed to JavaScript as 'UI'.
/// Contains all page control functions to avoid polluting global namespace.
/// Usage: UI.refresh(), UI.navigate('page'), UI.toast('message'), etc.
/// </summary>
public class UiNamespace {
    private readonly BaseBotUser botUser;
    private readonly ScriptContext context;
    private ScriptPage? page;


    public UiNamespace(BaseBotUser botUser, ScriptContext context) {
        this.botUser = botUser;
        this.context = context;
    }


    internal void SetPage(ScriptPage page) {
        this.page = page;
    }


    #region Refresh

    /// <summary>
    /// Refresh current page (re-render and update message).
    /// </summary>
    public void refresh() {
        if (page?.lastMessage is not null) {
            context.InvokeRefresh().GetAwaiter().GetResult();
            page.UpdatePageAsync(page.lastMessage.MessageId, page.lastMessage.Chat.Id)
                .GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Refresh current page asynchronously.
    /// </summary>
    public async Task refreshAsync() {
        if (page?.lastMessage is not null) {
            await context.InvokeRefresh();
            await page.UpdatePageAsync(page.lastMessage.MessageId, page.lastMessage.Chat.Id);
        }
    }

    #endregion


    #region Navigation

    /// <summary>
    /// Navigate to another page (uses cached instance if available).
    /// </summary>
    /// <param name="pageId">Target page ID</param>
    /// <param name="subPage">If true, preserves back navigation</param>
    public void navigate(string pageId, bool subPage = true) {
        if (page is null)
            return;
        context.navigated = true;
        page.NavigateToAsync(pageId, subPage, null).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Navigate to another page with props.
    /// </summary>
    public void navigate(string pageId, bool subPage, JsValue props) {
        if (page is null)
            return;
        context.navigated = true;
        var propsDict = context.ConvertJsObjectToDict(props);
        page.NavigateToAsync(pageId, subPage, propsDict).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Navigate asynchronously.
    /// </summary>
    public async Task<PageHandle?> navigateAsync(string pageId, bool subPage = true) {
        if (page is null)
            return null;
        context.navigated = true;
        return await page.NavigateToAsync(pageId, subPage, null);
    }

    /// <summary>
    /// Navigate asynchronously with props.
    /// </summary>
    public async Task<PageHandle?> navigateAsync(string pageId, bool subPage, JsValue props) {
        if (page is null)
            return null;
        context.navigated = true;
        var propsDict = context.ConvertJsObjectToDict(props);
        return await page.NavigateToAsync(pageId, subPage, propsDict);
    }

    /// <summary>
    /// Navigate to a fresh page instance (ignores cache).
    /// </summary>
    public void navigateFresh(string pageId, bool subPage = true) {
        if (page is null)
            return;
        context.navigated = true;
        page.NavigateToFreshAsync(pageId, subPage, null).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Navigate to a fresh page instance with props.
    /// </summary>
    public void navigateFresh(string pageId, bool subPage, JsValue props) {
        if (page is null)
            return;
        context.navigated = true;
        var propsDict = context.ConvertJsObjectToDict(props);
        page.NavigateToFreshAsync(pageId, subPage, propsDict).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Navigate to fresh page asynchronously.
    /// </summary>
    public async Task<PageHandle?> navigateFreshAsync(string pageId, bool subPage = true) {
        if (page is null)
            return null;
        context.navigated = true;
        return await page.NavigateToFreshAsync(pageId, subPage, null);
    }

    /// <summary>
    /// Navigate to fresh page asynchronously with props.
    /// </summary>
    public async Task<PageHandle?> navigateFreshAsync(string pageId, bool subPage, JsValue props) {
        if (page is null)
            return null;
        context.navigated = true;
        var propsDict = context.ConvertJsObjectToDict(props);
        return await page.NavigateToFreshAsync(pageId, subPage, propsDict);
    }

    /// <summary>
    /// Go back to parent page.
    /// </summary>
    public void back() {
        if (page?.parent is not null && page.lastMessage is not null) {
            context.navigated = true;
            page.parent.UpdatePageAsync(page.lastMessage.MessageId, page.lastMessage.Chat.Id)
                .GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Go back to parent page asynchronously.
    /// </summary>
    public async Task backAsync() {
        if (page?.parent is not null && page.lastMessage is not null) {
            context.navigated = true;
            await page.parent.UpdatePageAsync(page.lastMessage.MessageId, page.lastMessage.Chat.Id);
        }
    }

    /// <summary>
    /// Send new message with specified page.
    /// </summary>
    public void sendPage(string pageId) {
        page?.SendPageByIdAsync(pageId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Send new message with specified page asynchronously.
    /// </summary>
    public async Task sendPageAsync(string pageId) {
        if (page is not null) {
            await page.SendPageByIdAsync(pageId);
        }
    }

    #endregion


    #region Page Lifecycle

    /// <summary>
    /// Delete current message.
    /// </summary>
    public void close() {
        context.navigated = true;
        page?.handle?.Close();
    }

    /// <summary>
    /// Delete current message asynchronously.
    /// </summary>
    public async Task closeAsync() {
        context.navigated = true;
        if (page?.handle is not null) {
            await page.handle.CloseAsync();
        }
    }

    /// <summary>
    /// Dispose page (clear keyboard, free memory, but don't delete message).
    /// </summary>
    public void dispose() {
        context.navigated = true;
        page?.handle?.Dispose();
    }

    /// <summary>
    /// Dispose page asynchronously.
    /// </summary>
    public async Task disposeAsync() {
        context.navigated = true;
        if (page?.handle is not null) {
            await page.handle.DisposeAsync();
        }
    }

    /// <summary>
    /// Remove inline keyboard from current message.
    /// </summary>
    public void clearKeyboard() {
        if (page?.lastMessage is not null) {
            context.navigated = true;
            try {
                botUser.client.EditMessageReplyMarkup(
                    chatId: page.lastMessage.Chat.Id,
                    messageId: page.lastMessage.MessageId,
                    replyMarkup: null
                ).GetAwaiter().GetResult();
            } catch (Exception ex) {
                botUser.worker.logger.LogError(ex, "[UI] Failed to clear keyboard");
            }
        }
    }

    /// <summary>
    /// Remove inline keyboard asynchronously.
    /// </summary>
    public async Task clearKeyboardAsync() {
        if (page?.lastMessage is not null) {
            context.navigated = true;
            try {
                await botUser.client.EditMessageReplyMarkup(
                    chatId: page.lastMessage.Chat.Id,
                    messageId: page.lastMessage.MessageId,
                    replyMarkup: null
                );
            } catch (Exception ex) {
                botUser.worker.logger.LogError(ex, "[UI] Failed to clear keyboard");
            }
        }
    }

    #endregion


    #region Notifications

    /// <summary>
    /// Show toast notification (brief popup).
    /// Only works during button click callbacks.
    /// </summary>
    public void toast(string text) {
        var qid = GetCallbackQueryId();
        if (!string.IsNullOrEmpty(qid)) {
            botUser.ShowAlertAsync(text, qid, showAlert: false).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Show toast notification asynchronously.
    /// </summary>
    public async Task toastAsync(string text) {
        var qid = GetCallbackQueryId();
        if (!string.IsNullOrEmpty(qid)) {
            await botUser.ShowAlertAsync(text, qid, showAlert: false);
        }
    }

    /// <summary>
    /// Show alert popup (requires user to dismiss).
    /// Only works during button click callbacks.
    /// </summary>
    public void alert(string text) {
        var qid = GetCallbackQueryId();
        if (!string.IsNullOrEmpty(qid)) {
            botUser.ShowAlertAsync(text, qid, showAlert: true).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Show alert popup asynchronously.
    /// </summary>
    public async Task alertAsync(string text) {
        var qid = GetCallbackQueryId();
        if (!string.IsNullOrEmpty(qid)) {
            await botUser.ShowAlertAsync(text, qid, showAlert: true);
        }
    }

    /// <summary>
    /// Show chat action status (typing, uploading, etc.).
    /// </summary>
    public void status(string type) {
        try {
            var action = ParseChatAction(type);
            botUser.client.SendChatAction(botUser.chatId, action).GetAwaiter().GetResult();
        } catch (Exception ex) {
            botUser.worker.logger.LogError(ex, "[UI] Failed to send chat action");
        }
    }

    /// <summary>
    /// Show chat action status asynchronously.
    /// </summary>
    public async Task statusAsync(string type) {
        try {
            var action = ParseChatAction(type);
            await botUser.client.SendChatAction(botUser.chatId, action);
        } catch (Exception ex) {
            botUser.worker.logger.LogError(ex, "[UI] Failed to send chat action");
        }
    }

    #endregion


    #region Card Pagination

    /// <summary>
    /// Go to next page in multi-page view.
    /// </summary>
    public void nextPage() {
        if (page is null)
            return;
        var totalPages = page.GetPageCount();
        if (totalPages > 1) {
            page.currentPageIndex = (page.currentPageIndex + 1) % totalPages;
            refresh();
        }
    }

    /// <summary>
    /// Go to next page asynchronously.
    /// </summary>
    public async Task nextPageAsync() {
        if (page is null)
            return;
        var totalPages = page.GetPageCount();
        if (totalPages > 1) {
            page.currentPageIndex = (page.currentPageIndex + 1) % totalPages;
            await refreshAsync();
        }
    }

    /// <summary>
    /// Go to previous page in multi-page view.
    /// </summary>
    public void prevPage() {
        if (page is null)
            return;
        var totalPages = page.GetPageCount();
        if (totalPages > 1) {
            page.currentPageIndex = page.currentPageIndex - 1;
            if (page.currentPageIndex < 0) {
                page.currentPageIndex = totalPages - 1;
            }
            refresh();
        }
    }

    /// <summary>
    /// Go to previous page asynchronously.
    /// </summary>
    public async Task prevPageAsync() {
        if (page is null)
            return;
        var totalPages = page.GetPageCount();
        if (totalPages > 1) {
            page.currentPageIndex = page.currentPageIndex - 1;
            if (page.currentPageIndex < 0) {
                page.currentPageIndex = totalPages - 1;
            }
            await refreshAsync();
        }
    }

    /// <summary>
    /// Go to specific page index.
    /// </summary>
    public void goToPage(int index) {
        if (page is null)
            return;
        var totalPages = page.GetPageCount();
        if (index >= 0 && index < totalPages) {
            page.currentPageIndex = index;
            refresh();
        }
    }

    /// <summary>
    /// Go to specific page index asynchronously.
    /// </summary>
    public async Task goToPageAsync(int index) {
        if (page is null)
            return;
        var totalPages = page.GetPageCount();
        if (index >= 0 && index < totalPages) {
            page.currentPageIndex = index;
            await refreshAsync();
        }
    }

    /// <summary>
    /// Get total page count.
    /// </summary>
    public int getPageCount() => page?.GetPageCount() ?? 1;

    /// <summary>
    /// Get current page index.
    /// </summary>
    public int getCurrentPage() => page?.currentPageIndex ?? 0;

    #endregion


    #region Helpers

    private string? GetCallbackQueryId() {
        var qidValue = context.Engine.GetValue("callbackQueryId");
        return qidValue.IsString() ? qidValue.AsString() : null;
    }

    private static ChatAction ParseChatAction(string type) {
        return type.ToLower() switch {
            "typing" => ChatAction.Typing,
            "upload_photo" => ChatAction.UploadPhoto,
            "upload_video" => ChatAction.UploadVideo,
            "upload_document" => ChatAction.UploadDocument,
            "upload_audio" => ChatAction.UploadVoice,
            "record_video" => ChatAction.RecordVideo,
            "record_audio" => ChatAction.RecordVoice,
            "find_location" => ChatAction.FindLocation,
            _ => ChatAction.Typing
        };
    }

    #endregion
}
