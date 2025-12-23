using System.Collections.Concurrent;
using Telegram.Bot.UI.Utils;

namespace Telegram.Bot.UI;

/// <summary>
/// Factory for managing callback handlers for inline keyboard buttons.
/// </summary>
public class CallbackFactory {
    private readonly string sessionId = Guid.NewGuid().ToString("N")[..8];
    private long idCount = 0;

    /// <summary>
    /// Delegate for callback handlers triggered by inline button interactions.
    /// </summary>
    /// <param name="callbackQueryId">The callback query ID for answering.</param>
    /// <param name="messageId">The message ID containing the button.</param>
    /// <param name="chatId">The chat ID where callback originated.</param>
    public delegate Task CallbackHandler(string callbackQueryId, int messageId, long chatId);

    private ConcurrentDictionary<string, TimeCache<CallbackHandler?>> callbackCache;

    /// <summary>
    /// Gets the time after which cached callbacks are cleared.
    /// </summary>
    public TimeSpan clearCacheTime { get; private set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Initializes a new instance of the CallbackFactory class.
    /// </summary>
    public CallbackFactory() {
        callbackCache = new();
    }

    /// <summary>
    /// Subscribes a callback handler and returns a unique callback ID.
    /// </summary>
    /// <param name="userId">The user ID associated with this callback.</param>
    /// <param name="onCallback">The callback handler to invoke.</param>
    /// <param name="debugInfo">Optional debug information for logging.</param>
    /// <returns>Unique callback ID to use in inline button callback data.</returns>
    public string Subscribe(long userId, CallbackHandler? onCallback, string? debugInfo = null) {
        var callbackId = GenerateId();
        callbackCache.GetOrAdd(callbackId, (key) => new(onCallback));
        return callbackId;
    }

    /// <summary>
    /// Unsubscribes a callback handler by its ID.
    /// </summary>
    /// <param name="callbackId">The callback ID to unsubscribe.</param>
    public void Unsubscribe(string? callbackId) {
        if (callbackId is null) {
            return;
        }
        callbackCache.TryRemove(callbackId, out _);
    }

    /// <summary>
    /// Unsubscribes multiple callback handlers.
    /// </summary>
    /// <param name="callbacksId">List of callback IDs to unsubscribe.</param>
    public void Unsubscribe(List<string> callbacksId) {
        foreach (var callback in callbacksId) {
            Unsubscribe(callback);
        }
    }

    /// <summary>
    /// Invokes a callback handler by its ID.
    /// </summary>
    /// <param name="callbackQueryId">The callback query ID for answering.</param>
    /// <param name="callbackId">The callback ID to invoke.</param>
    /// <param name="messageId">The message ID containing the button.</param>
    /// <param name="chatId">The chat ID where callback originated.</param>
    /// <returns>True if callback was found and invoked, false otherwise.</returns>
    public async Task<bool> InvokeAsync(string callbackQueryId, string callbackId, int messageId, long chatId) {
        if (callbackCache.TryGetValue(callbackId, out var callback)) {
            if (callback.value is not null) {
                await callback.value(callbackQueryId, messageId, chatId);
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Generates a unique callback ID.
    /// </summary>
    /// <returns>Unique callback ID string.</returns>
    public string GenerateId() {
        return $"{sessionId}_{Interlocked.Increment(ref idCount)}";
    }

    /// <summary>
    /// Clears expired callbacks from the cache based on clearCacheTime.
    /// </summary>
    public void ClearCache() {
        var callbacks = callbackCache.Where(c => c.Value.time < DateTime.UtcNow - clearCacheTime);
        foreach (var callback in callbacks) {
            Unsubscribe(callback.Key);
        }
    }
}
