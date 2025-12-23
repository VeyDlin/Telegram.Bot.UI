using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.UI.BotWorker;

/// <summary>
/// Internal update handler for polling-based bot workers.
/// </summary>
class BotWorkerPullingUpdateHandler : IUpdateHandler {
    /// <summary>
    /// Gets the update handler function.
    /// </summary>
    public required Func<Update, CancellationToken, Task> update { init; get; }

    /// <summary>
    /// Gets the error handler function.
    /// </summary>
    public required Func<Exception, CancellationToken, Task> error { init; get; }

    /// <summary>
    /// Handles errors from the Telegram API.
    /// </summary>
    public Task HandleErrorAsync(
        ITelegramBotClient c,
        Exception e,
        HandleErrorSource s,
        CancellationToken t
    ) => this.error(e, t);

    /// <summary>
    /// Handles updates from the Telegram API.
    /// </summary>
    public Task HandleUpdateAsync(ITelegramBotClient c, Update u, CancellationToken t) => this.update(u, t);
}

/// <summary>
/// Bot worker implementation that uses long polling to receive updates from Telegram.
/// </summary>
/// <typeparam name="T">The type of bot user, must derive from BaseBotUser.</typeparam>
public class BotWorkerPulling<T> : BaseBotWorker<T> where T : BaseBotUser {
    /// <summary>
    /// The Telegram bot client instance.
    /// </summary>
    protected TelegramBotClient? botClient;

    /// <summary>
    /// Gets the Telegram bot client.
    /// </summary>
    public override ITelegramBotClient client => botClient!;

    /// <summary>
    /// Gets or sets the bot token used to authenticate with Telegram.
    /// </summary>
    public required string botToken { init => botClient = new(value); }

    /// <summary>
    /// Initializes a new instance of the BotWorkerPulling class.
    /// </summary>
    /// <param name="userFactory">The factory delegate for creating bot user instances.</param>
    public BotWorkerPulling(UserFactoryDelegate userFactory) : base(userFactory) { }
    /// <summary>
    /// Starts receiving updates from Telegram using long polling.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Stops receiving updates from Telegram.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override Task StopHandleAsync() {
        cancellationTokenSource.Cancel();
        return Task.CompletedTask;
    }
}