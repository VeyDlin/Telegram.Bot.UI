using Localization;
using SafeStop;
using Telegram.Bot.UI.Loader;

namespace Telegram.Bot.UI.BotWorker;


public interface IBotWorker {
    public DateTime startTime { get; }
    public PageResourceLoader pageResourceLoader { get; }
    public LocalizationPack? localizationPack { get; set; }
    public Func<Task>? onStopRequest { get; set; }


    public Task StartAsync();
    public Task StopAsync();
    public Task<DisposeAction> CriticalAsync();
}
