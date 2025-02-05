using static Telegram.Bot.UI.CallbackFactory;

namespace Telegram.Bot.UI.MenuBuilder;


public class CallbackData {
    public CallbackHandler? onCallback { get; set; }
    public DateTime lastUpdate { get; set; }
}
