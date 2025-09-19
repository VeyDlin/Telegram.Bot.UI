namespace Telegram.Bot.UI.Utils;


public class TimeCache<T> {
    public T value { get; set; }
    public DateTime time { get; set; } = DateTime.UtcNow;
    public void Update() => time = DateTime.UtcNow;

    public TimeCache(T value) {
        this.value = value;
    }
}
