using Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SafeStop;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.UI.BotWorker;
using Telegram.Bot.UI.Loader;
using Telegram.Bot.UI.Utils;

namespace Telegram.Bot.UI;


public abstract class BaseBotWorker<T> : IBotWorker where T : BaseBotUser {
    public delegate T UserFactoryDelegate(IBotWorker worker, long chatId, ITelegramBotClient client, CancellationToken token);

    public abstract ITelegramBotClient client { get; }

    protected UserFactoryDelegate userFactory;

    protected SafeStopManager safeStop = new();
    public bool isSafeStopSet => safeStop.isStopSet;

    private readonly Dictionary<long, TimeCache<T>> usersCache = new();
    private readonly SemaphoreSlim cacheLock = new(1, 1);
    private long requestCount = 0;

    public TimeSpan clearCacheTime { get; private set; } = TimeSpan.FromDays(1);
    public DateTime startTime { get; private set; } = DateTime.UtcNow;
    public PageResourceLoader pageResourceLoader { get; protected set; } = new();
    public LocalizationPack? localizationPack { get; set; } = null;

    public CancellationTokenSource cancellationTokenSource { get; set; } = new();
    public Func<Task>? onStopRequest { get; set; } = null;
    public bool skipMessagesBeforeStart { get; set; } = true;
    public string? resourcePath { set => pageResourceLoader = new(value); }
    public ILogger logger { get; set; } = NullLogger.Instance;


    public BaseBotWorker(UserFactoryDelegate userFactory) {
        this.userFactory = userFactory;
    }



    protected virtual Task StartHandleAsync() => throw new NotImplementedException();
    protected virtual Task StopHandleAsync() => Task.CompletedTask;




    public async Task StartAsync() {
        logger.LogInformation("Starting bot worker");
        startTime = DateTime.UtcNow;
        await StartHandleAsync();
        logger.LogInformation("Bot worker started successfully");
    }




    public async Task StopAsync() {
        logger.LogInformation("Stopping bot worker");

        if (onStopRequest is not null) {
            await onStopRequest.Invoke();
        }

        await StopHandleAsync();

        safeStop.Stop();
        await safeStop.WaitStopAsync();

        cancellationTokenSource.Cancel();

        logger.LogInformation("Bot worker stopped successfully");
    }




    public Task<DisposeAction> CriticalAsync() => safeStop.CriticalAsync();




    private async Task<T?> HandleUpdateCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken) {
        if (
            callbackQuery.Message is not { } message ||
            callbackQuery.Data is not string clickedNavigation ||
            (skipMessagesBeforeStart && message.Date < startTime.ToUniversalTime())
        ) {
            await client.AnswerCallbackQuery(callbackQuery.Id);
            return null;
        }

        var user = await GetOrCreateChatUserAsync(callbackQuery.From.Id, message, cancellationToken);

        if (!await user.HandlePermissiveAsync(message)) {
            await client.AnswerCallbackQuery(callbackQuery.Id);
            return user;
        }

        if (safeStop.isStopSet) {
            var stopMessage = await user.HandleStoppingProcess(message); // TODO: Make it easier
            if (stopMessage is not null) {
                await user.ShowAlertAsync(stopMessage, callbackQuery.Id);
            }
            await client.AnswerCallbackQuery(callbackQuery.Id);
            return user;
        }

        try {
            await user.HandleCallbackAsync(callbackQuery.Id, clickedNavigation, message.MessageId, message.Chat.Id);
            await client.AnswerCallbackQuery(callbackQuery.Id);
        } catch (Exception ex) {
            try {
                await user.HandleErrorAsync(ex);
            } catch (Exception ex2) {
                logger.LogError(ex2, "Error in user.HandleErrorAsync for user {UserId}", user.chatId);
            }
        }

        return user;
    }




    private async Task<T?> HandleSuccessfulPaymentAsync(Message message, CancellationToken cancellationToken) {
        if (message.SuccessfulPayment is not { } payment) {
            return null;
        }

        var user = await GetOrCreateChatUserAsync(message.Chat.Id, message, cancellationToken);
        await user.HandleSuccessPaymentAsync(payment);
        return user;
    }




    private async Task<T?> HandleUpdateMessageAsync(Message message, CancellationToken cancellationToken) {
        if (message.Type is MessageType.SuccessfulPayment) {
            return await HandleSuccessfulPaymentAsync(message, cancellationToken);
        }

        if (skipMessagesBeforeStart && message.Date < startTime.ToUniversalTime()) {
            return null;
        }

        var user = await GetOrCreateChatUserAsync(message.Chat.Id, message, cancellationToken);
        if (!await user.HandlePermissiveAsync(message)) {
            return user;
        }

        if (safeStop.isStopSet) {
            var stopMessage = await user.HandleStoppingProcess(message); // TODO: Make it easier
            if (stopMessage is not null) {
                await user.SendTextMessageAsync(stopMessage);
            }
            return user;
        }

        if (!user.acceptLicense) {
            await user.HandleAcceptLicense(message);
            return user;
        }

        if (message.Photo is PhotoSize[] photo) {
            await user.HandlePhotoAsync(photo, message);
            return user;
        }

        if (message.Text is not { } messageText) {
            await user.HandleOtherMessageAsync(message);
            return user;
        }

        messageText = messageText.Trim();

        if (messageText == "") {
            return user;
        }

        if (user.enableCommands && messageText.StartsWith("/")) {
            string[] parts = messageText.Split(" ");
            string command = parts[0].Substring(1).Trim().ToLower();
            string[] arguments = parts.Skip(1).Select(x => x.Trim()).ToArray();

            await user.HandleCommandAsync(command, arguments, message);
        } else {
            await user.HandleMessageAsync(messageText, message);
        }

        return user;
    }




    private async Task<T?> HandlePreCheckoutQueryAsync(PreCheckoutQuery query, CancellationToken cancellationToken) {
        var user = await GetOrCreateChatUserAsync(query.From.Id, null, cancellationToken);
        var error = await user.HandlePreCheckoutQueryAsync(query);
        await client.AnswerPreCheckoutQuery(query.Id, errorMessage: error, cancellationToken);
        return user;
    }




    protected async Task UpdateHandlerAsync(Update update, CancellationToken cancellationToken) {
        T? user = null;
        try {
            try {
                switch (update.Type) {
                    case UpdateType.Message when update.Message is { } message:
                        user = await HandleUpdateMessageAsync(message, cancellationToken);
                    break;

                    case UpdateType.CallbackQuery when update.CallbackQuery is { } callbackQuery:
                        user = await HandleUpdateCallbackQueryAsync(callbackQuery, cancellationToken);
                    break;

                    case UpdateType.PreCheckoutQuery when update.PreCheckoutQuery is { } preCheckoutQuery:
                        user = await HandlePreCheckoutQueryAsync(preCheckoutQuery, cancellationToken);
                    break;
                }
            } catch (Exception ex) {          
                if (user is not null) {       
                    await user.HandleErrorAsync(ex);
                }
            } finally {
                if (user is not null) {
                    user.End();
                    await user.EndAsync();
                }
            }
        } catch (Exception ex2) {
            logger.LogCritical(ex2, "Critical error in UpdateHandlerAsync");
        }
    }




    protected Task ErrorHandlerAsync(Exception exception, CancellationToken cancellationToken) {
        logger.LogError(exception, "Unhandled error in bot worker");
        return Task.CompletedTask;
    }




    public async Task ClearCacheAsync() {
        await cacheLock.WaitAsync();
        try {
            var users = usersCache.Where(c => c.Value.time < DateTime.UtcNow - clearCacheTime).ToList();
            if (users.Count > 0) {
                logger.LogDebug("Clearing {Count} users from cache", users.Count);
            }

            foreach (var user in users) {
                user.Value.value.Dispose();
                usersCache.Remove(user.Key);
            }

            foreach (var user in usersCache) {
                user.Value.value.callbackFactory.ClearCache();
            }
        } finally {
            cacheLock.Release();
        }
    }




    public async Task<T> GetOrCreateChatUserAsync(long chatId, Message? message = null, CancellationToken cancellationToken = default) {
        await cacheLock.WaitAsync(cancellationToken);
        try {
            if (usersCache.TryGetValue(chatId, out var existingUser)) {
                existingUser.Update();

                existingUser.value.Begin(message);
                await existingUser.value.BeginAsync(message);

                return existingUser.value;
            }

            logger.LogInformation("Creating new user instance for chat {ChatId}", chatId);

            var user = userFactory(this, chatId, client, cancellationToken);

            await user.CreateAsync(message);

            user.Begin(message);
            await user.BeginAsync(message);

            usersCache[chatId] = new TimeCache<T>(user);
            return user;
        } finally {
            cacheLock.Release();

            if (Interlocked.Increment(ref requestCount) % 100 == 0) {
                await ClearCacheAsync();
            }
        }
    }
}
