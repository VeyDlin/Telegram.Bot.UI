using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.UI.MenuBuilder;


public abstract class MenuElement : IDisposable {
    public virtual required MessagePage parrent { get; set; }
    public virtual required BaseBotUser botUser { get; set; }
    public virtual bool hide { get; set; } = false;
    public virtual int columns { get; set; } = 3;
    private bool disposed { get; set; } = false;


    ~MenuElement() {
        Dispose();
    }


    public abstract Task<List<InlineKeyboardButton>> BuildAsync();


    public void Dispose() {
        if (!disposed) {
            OnDispose();
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }


    protected virtual void OnDispose() { }
}