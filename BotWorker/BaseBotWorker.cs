using Localization;
using SafeStop;
using System.Collections.Concurrent;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.UI.BotWorker;
using Telegram.Bot.UI.Loader;

namespace Telegram.Bot.UI;


public abstract class BaseBotWorker<T> : IBotWorker where T : BaseBotUser {
    public delegate T UserFactoryDelegate(IBotWorker worker, long chatId, ITelegramBotClient client, CancellationToken token);

    protected UserFactoryDelegate userFactory;
    protected ConcurrentDictionary<long, T> usersCache = new();
    protected SafeStopManager safeStop = new();
    public bool isSafeStopSet => safeStop.isStopSet;

    public DateTime startTime { get; private set; } = DateTime.Now.ToUniversalTime();
    public PageResourceLoader pageResourceLoader { get; protected set; } = new();
    public LocalizationPack? localizationPack { get; set; } = null;

    public CancellationTokenSource cancellationTokenSource { get; set; } = new();
    public Func<Task>? onStopRequest { get; set; } = null;
    public bool skipMessagesBeforeStart { get; set; } = true;
    public string? resourcePath { set => pageResourceLoader = new(value); }


    public BaseBotWorker(UserFactoryDelegate userFactory) {
        this.userFactory = userFactory;
    }



    protected virtual Task StartHandleAsync() => throw new NotImplementedException();
    protected virtual Task StopHandleAsync() => Task.CompletedTask;





    public async Task StartAsync() {
        startTime = DateTime.Now.ToUniversalTime();
        await StartHandleAsync();
    }





    public async Task StopAsync() {
        if (onStopRequest is not null) {
            await onStopRequest.Invoke();
        }

        await StopHandleAsync();

        safeStop.Stop();
        await safeStop.WaitStopAsync();

        cancellationTokenSource.Cancel();
    }





    public Task<DisposeAction> CriticalAsync() => safeStop.CriticalAsync();





    private async Task HandleUpdateCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken) {
        if (callbackQuery.Message is not { } message) {
            return;
        }

        if (callbackQuery.Data is not string clickedNavigation) {
            return;
        }

        if (skipMessagesBeforeStart && message.Date < startTime.ToUniversalTime()) {
            return;
        }

        var user = GetOrCreateChatUser(callbackQuery.From.Id, botClient, cancellationToken);

        if (!await user.HandlePermissiveAsync(message)) {
            return;
        }

        if (safeStop.isStopSet) {
            var stopMessage = await user.HandleStoppingProcess(message); // TODO: сделать проще
            if (stopMessage is not null) {
                await user.ShowAlertAsync(stopMessage, callbackQuery.Id);
            }
            return;
        }

        try {
            user.HandleCallbackAsync(callbackQuery.Id, clickedNavigation, message.MessageId, message.Chat.Id);
            await botClient.AnswerCallbackQuery(callbackQuery.Id);
        } catch (Exception ex) {
            try {
                await user.HandleErrorAsync(ex);
            } catch (Exception ex2) {
                Console.WriteLine(ex2.ToString());
            }
        }
    }





    private async Task HandleUpdateMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken) {

        if (message.Type is MessageType.SuccessfulPayment) {
            await HandleSuccessfulPaymentAsync(botClient, message, cancellationToken);
            return;
        }


        if (skipMessagesBeforeStart && message.Date < startTime.ToUniversalTime()) {
            return;
        }

        var user = GetOrCreateChatUser(message.Chat.Id, botClient, cancellationToken);

        if (!await user.HandlePermissiveAsync(message)) {
            return;
        }

        if (safeStop.isStopSet) {
            var stopMessage = await user.HandleStoppingProcess(message); // TODO: сделать проще
            if (stopMessage is not null) {
                await user.SendTextMessageAsync(stopMessage);
            }
            return;
        }

        if (!user.acceptLicense) {
            await user.HandleAcceptLicense(message);
            return;
        }

        if (message.Photo is PhotoSize[] photo) {
            await user.HandlePhotoAsync(photo, message);
            return;
        }

        if (message.Text is not { } messageText) {
            await user.HandleOtherMessageAsync(message);
            return;
        }

        messageText = messageText.Trim();

        if (messageText == "") {
            return;
        }

        if (messageText.StartsWith("/")) {
            string[] parts = messageText.Split(" ");
            string command = parts[0].Substring(1).Trim().ToLower();
            string[] arguments = parts.Skip(1).Select(x => x.Trim()).ToArray();

            await user.HandleCommandAsync(command, arguments, message);
        } else {
            await user.HandleMessageAsync(messageText, message);
        }
    }





    private async Task HandleSuccessfulPaymentAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken) {
        if (message.SuccessfulPayment is not { } payment) {
            return;
        }

        var user = GetOrCreateChatUser(message.Chat.Id, botClient, cancellationToken);

        // TODO: нужно ли skipMessagesBeforeStart? HandlePermissiveAsync? HandleAcceptLicense?

        await user.HandleSuccessPayment(payment);
    }





    protected async Task UpdateHandlerAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken) {
        long? chatId = null;

        try {
            chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;
            if (chatId is null) {
                return;
            }

            switch (update.Type) {
                case UpdateType.Message when update.Message is { } message:
                    await HandleUpdateMessageAsync(botClient, message, cancellationToken);
                break;

                case UpdateType.CallbackQuery when update.CallbackQuery is { } callbackQuery:
                    await HandleUpdateCallbackQueryAsync(botClient, callbackQuery, cancellationToken);
                break;
            }
        } catch (Exception ex) {
            if (chatId is not null) {
                try {
                    var user = GetOrCreateChatUser((long)chatId, botClient, cancellationToken);
                    await user.HandleErrorAsync(ex);
                } catch (Exception ex2) {
                    Console.WriteLine(ex2.ToString());
                }
            }
        }
    }





    protected Task ErrorHandlerAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken) {
        // TODO: add Handle
        return Task.CompletedTask;
    }





    private BaseBotUser GetOrCreateChatUser(long chatId, ITelegramBotClient cient, CancellationToken cancellationToken) {
        return usersCache.GetOrAdd(chatId, (key) => {
            var user = userFactory(this, chatId, cient, cancellationToken);
            user.Begin();
            return user;
        });
    }
}
