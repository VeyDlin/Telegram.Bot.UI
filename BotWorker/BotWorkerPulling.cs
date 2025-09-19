using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.UI.BotWorker;


class BotWorkerPullingUpdateHandler : IUpdateHandler {
    public required Func<Update, CancellationToken, Task> update { init; get; }
    public required Func<Exception, CancellationToken, Task> error { init; get; }
    public Task HandleErrorAsync(ITelegramBotClient c, Exception e, HandleErrorSource s, CancellationToken t) => this.error(e, t);  
    public Task HandleUpdateAsync(ITelegramBotClient c, Update u, CancellationToken t) => this.update(u, t);
}


public class BotWorkerPulling<T> : BaseBotWorker<T> where T : BaseBotUser {
    protected TelegramBotClient? botClient;
    public override ITelegramBotClient client => botClient!;
    public required string botToken { init => botClient = new(value); }


    public BotWorkerPulling(UserFactoryDelegate userFactory) : base(userFactory) { }





    protected override Task StartHandleAsync() {
        var receiverOptions = new ReceiverOptions() {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        var updateHandler = new BotWorkerPullingUpdateHandler() {
            update = UpdateHandlerAsync,
            error = ErrorHandlerAsync
        };

        botClient!.StartReceiving(
            updateHandler: updateHandler,
            receiverOptions: receiverOptions,
            cancellationToken: cancellationTokenSource.Token
        );

        return Task.CompletedTask;
    }





    protected override Task StopHandleAsync() {
        cancellationTokenSource.Cancel();
        return Task.CompletedTask;
    }
}
