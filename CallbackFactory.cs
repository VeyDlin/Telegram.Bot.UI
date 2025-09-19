using System.Collections.Concurrent;
using System.Linq.Expressions;
using Telegram.Bot.Types;
using Telegram.Bot.UI.MenuBuilder;
using Telegram.Bot.UI.Utils;

namespace Telegram.Bot.UI;


public class CallbackFactory {
    private static ulong IdCount = 0;

    public delegate Task CallbackHandler(string callbackQueryId, int messageId, long chatId);

    private ConcurrentDictionary<string, TimeCache<CallbackHandler?>> callbackCache;
    public TimeSpan clearCacheTime { get; private set; } = TimeSpan.FromDays(1);




    public CallbackFactory() {
        callbackCache = new();
    }





    public string Subscribe(long userId, CallbackHandler? onCallback) {
        var callbackId = GenerateId();
        var callback = callbackCache.GetOrAdd(callbackId, (key) => new(onCallback));
        return callbackId;
    }





    public void Unsubscribe(string? callbackId) {
        if (callbackId is null) {
            return;
        }
        callbackCache.TryRemove(callbackId, out _);
    }





    public void Unsubscribe(List<string> callbacksId) {
        foreach (var callback in callbacksId) {
            Unsubscribe(callback);
        }
    }





    public async Task<bool> InvokeAsync(string callbackQueryId, string callbackId, int messageId, long chatId) {
        if (callbackCache.TryGetValue(callbackId, out var callback)) {
            if (callback.value is not null) {
                await callback.value(callbackQueryId, messageId, chatId);
            }
            return true;
        }

        return false;
    }





    public string GenerateId() {
        return (IdCount++).ToString();
    }




    public void ClearCache() {
        var callbacks = callbackCache.Where(c => c.Value.time < DateTime.UtcNow - clearCacheTime);
        foreach (var callback in callbacks) {
            Unsubscribe(callback.Key);
        }
    }
}
