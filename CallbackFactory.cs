using System.Collections.Concurrent;
using Telegram.Bot.UI.MenuBuilder;

namespace Telegram.Bot.UI;


public class CallbackFactory {
    private static ulong idCount = 0;

    public delegate Task CallbackHandler(string callbackQueryId, int messageId, long chatId);

    private ConcurrentDictionary<string, CallbackData> callbackCache;





    public CallbackFactory() {
        callbackCache = new();
    }





    public string Subscribe(long userId, CallbackHandler? onCallback) {
        var callbackId = GenerateId();

        var callback = callbackCache.GetOrAdd(callbackId, (key) => new CallbackData() {
            onCallback = onCallback
        });
        callback.lastUpdate = DateTime.UtcNow;

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





    public bool InvokeAsync(string callbackQueryId, string callbackId, int messageId, long chatId) {
        if (callbackCache.TryGetValue(callbackId, out var callback)) {
            if (callback.onCallback is not null) {
                Task.Run(() => callback.onCallback(callbackQueryId, messageId, chatId));
                ;
            }
            return true;
        }

        return false;
    }





    public string GenerateId() {
        return (idCount++).ToString();
    }
}
