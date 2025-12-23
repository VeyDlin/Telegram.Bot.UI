namespace Telegram.Bot.UI.Runtime;

/// <summary>
/// Handle returned from navigation that allows controlling the target page.
/// Supports navigation chain disposal - disposing parent disposes all children.
/// </summary>
/// <remarks>
/// PageHandle manages page lifecycle in a tree structure:
/// - Each handle can have a parent and multiple children
/// - Dispose/Close operations cascade to all children
/// - Provides Back, Update, Close operations for JavaScript access
/// </remarks>
public class PageHandle : IDisposable, IAsyncDisposable {
    #region Fields

    private ScriptPage? page;
    private PageHandle? parent;
    private List<PageHandle> children = new();

    #endregion


    #region Properties

    /// <summary>
    /// Whether this handle has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; } = false;

    #endregion


    #region Constructor

    /// <summary>
    /// Creates a new PageHandle for the specified page.
    /// </summary>
    /// <param name="page">The ScriptPage this handle controls.</param>
    /// <param name="parent">Optional parent handle. Child is automatically registered with parent.</param>
    public PageHandle(ScriptPage page, PageHandle? parent = null) {
        this.page = page;
        this.parent = parent;
        parent?.children.Add(this);
    }

    #endregion


    #region Dispose

    /// <summary>
    /// Dispose the page (free memory).
    /// Disposes all child pages in the navigation chain.
    /// Does NOT delete the message.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) {
            return;
        }
        IsDisposed = true;

        // Dispose children first (reverse order)
        for (int i = children.Count - 1; i >= 0; i--) {
            children[i].Dispose();
        }
        children.Clear();

        // Dispose the page (invokes onUnmounted, disposes components)
        page?.Dispose();
        page = null;

        // Remove from parent's children list
        parent?.children.Remove(this);
        parent = null;

        GC.SuppressFinalize(this);
    }


    /// <summary>
    /// Async version of Dispose.
    /// </summary>
    public async ValueTask DisposeAsync() {
        if (IsDisposed) {
            return;
        }
        IsDisposed = true;

        // Dispose children first (reverse order)
        for (int i = children.Count - 1; i >= 0; i--) {
            await children[i].DisposeAsync();
        }
        children.Clear();

        // Dispose the page async if supported
        if (page is IAsyncDisposable asyncDisposable) {
            await asyncDisposable.DisposeAsync();
        } else {
            page?.Dispose();
        }
        page = null;

        // Remove from parent's children list
        parent?.children.Remove(this);
        parent = null;

        GC.SuppressFinalize(this);
    }

    #endregion


    #region Close

    /// <summary>
    /// Close the page (delete message + dispose).
    /// Closes all child pages in the navigation chain.
    /// </summary>
    public void Close() {
        if (IsDisposed) {
            return;
        }

        // Close children first (reverse order)
        for (int i = children.Count - 1; i >= 0; i--) {
            children[i].Close();
        }
        children.Clear();

        // Delete the message
        page?.DeletePageAsync().GetAwaiter().GetResult();

        // Dispose (sets IsDisposed = true)
        Dispose();
    }


    /// <summary>
    /// Async version of Close.
    /// </summary>
    public async Task CloseAsync() {
        if (IsDisposed) {
            return;
        }

        // Close children first (reverse order)
        for (int i = children.Count - 1; i >= 0; i--) {
            await children[i].CloseAsync();
        }
        children.Clear();

        // Delete the message
        if (page is not null) {
            await page.DeletePageAsync();
        }

        // Dispose async
        await DisposeAsync();
    }

    #endregion


    #region Navigation

    /// <summary>
    /// Navigate back to parent page.
    /// </summary>
    public void Back() {
        if (IsDisposed || page?.parent is null || page.lastMessage is null) {
            return;
        }

        page.OpenSubPageAsync(page.parent).GetAwaiter().GetResult();
    }


    /// <summary>
    /// Async version of Back.
    /// </summary>
    public async Task BackAsync() {
        if (IsDisposed || page?.parent is null || page.lastMessage is null) {
            return;
        }

        await page.OpenSubPageAsync(page.parent);
    }

    #endregion


    #region Update

    /// <summary>
    /// Update/refresh the page message with current content.
    /// </summary>
    public void Update() {
        if (IsDisposed || page?.lastMessage is null) {
            return;
        }

        page.UpdatePageAsync(page.lastMessage.MessageId, page.lastMessage.Chat.Id)
            .GetAwaiter().GetResult();
    }


    /// <summary>
    /// Async version of Update.
    /// </summary>
    public async Task UpdateAsync() {
        if (IsDisposed || page?.lastMessage is null) {
            return;
        }

        await page.UpdatePageAsync(page.lastMessage.MessageId, page.lastMessage.Chat.Id);
    }

    #endregion
}
