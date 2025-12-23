using Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.UI.Loader;
using Telegram.Bot.UI.Utils;

namespace Telegram.Bot.UI.BotWorker;


/// <summary>
/// Base class for Telegram bot workers that handle updates and manage user sessions.
/// Provides user caching, update routing, and error handling infrastructure.
/// </summary>
/// <typeparam name="T">User type that extends <see cref="BaseBotUser"/>.</typeparam>
public abstract class BaseBotWorker<T> : IBotWorker where T : BaseBotUser {
    /// <summary>
    /// Factory delegate for creating user instances.
    /// </summary>
    public delegate T UserFactoryDelegate(IBotWorker worker, long chatId, ITelegramBotClient client, CancellationToken token);

    /// <summary>
    /// Telegram bot client instance.
    /// </summary>
    public abstract ITelegramBotClient client { get; }

    /// <summary>
    /// Factory for creating user instances.
    /// </summary>
    protected UserFactoryDelegate userFactory;

    private readonly Dictionary<long, TimeCache<T>> usersCache = new();
    private readonly SemaphoreSlim cacheLock = new(1, 1);
    private long requestCount = 0;

    /// <summary>
    /// If true, messages received before bot start are ignored.
    /// Default: true.
    /// </summary>
    public bool skipMessagesBeforeStart { get; set; } = true;

    /// <summary>
    /// Time after which inactive users are evicted from cache.
    /// Default: 1 day.
    /// </summary>
    public TimeSpan clearCacheTime { get; private set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Cache eviction policy. Called on every request.
    /// Parameters: (requestCount, cache, clearCacheTime).
    /// Returns: keys to evict.
    /// Default: every 100 requests, evict users older than clearCacheTime.
    /// </summary>
    public Func<long, Dictionary<long, TimeCache<T>>, TimeSpan, IEnumerable<long>> cacheEvictionPolicy { get; set; }

    /// <summary>
    /// Bot start time (UTC).
    /// </summary>
    public DateTime startTime { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Resource loader for page files and assets.
    /// </summary>
    public IResourceLoader resourceLoader { get; set; } = new ResourceLoader();

    /// <summary>
    /// Localization pack for multi-language support.
    /// </summary>
    public LocalizationPack? localizationPack { get; set; } = null;

    /// <summary>
    /// Cancellation token source for graceful shutdown.
    /// </summary>
    public CancellationTokenSource cancellationTokenSource { get; set; } = new();

    /// <summary>
    /// Logger instance.
    /// </summary>
    public ILogger logger { get; set; } = NullLogger.Instance;


    /// <summary>
    /// Creates a new bot worker instance.
    /// </summary>
    /// <param name="userFactory">Factory delegate for creating user instances.</param>
    public BaseBotWorker(UserFactoryDelegate userFactory) {
        this.userFactory = userFactory;

        cacheEvictionPolicy = (count, cache, timeout) => {
            if (count % 100 != 0) {
                return [];
            }
            return cache
                .Where(c => c.Value.time < DateTime.UtcNow - timeout)
                .Select(c => c.Key);
        };
    }


    /// <summary>
    /// Starts receiving updates. Override in derived classes.
    /// </summary>
    protected virtual Task StartHandleAsync() => throw new NotImplementedException();


    /// <summary>
    /// Stops receiving updates. Override in derived classes.
    /// </summary>
    protected virtual Task StopHandleAsync() => Task.CompletedTask;


    /// <summary>
    /// Starts the bot worker.
    /// </summary>
    public async Task StartAsync() {
        logger.LogInformation("Starting bot worker");
        startTime = DateTime.UtcNow;
        await StartHandleAsync();
        logger.LogInformation("Bot worker started successfully");
    }


    /// <summary>
    /// Stops the bot worker gracefully.
    /// </summary>
    public async Task StopAsync() {
        logger.LogInformation("Stopping bot worker");
        await StopHandleAsync();
        cancellationTokenSource.Cancel();
        logger.LogInformation("Bot worker stopped successfully");
    }


    private async Task<T?> HandleUpdateCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken) {
        if (callbackQuery.Message is not { } message || callbackQuery.Data is not string clickedNavigation) {
            await client.AnswerCallbackQuery(callbackQuery.Id);
            return null;
        }

        var user = await GetOrCreateChatUserAsync(callbackQuery.From.Id, message, cancellationToken);

        if (skipMessagesBeforeStart && message.Date < startTime.ToUniversalTime()) {
            await user.HandleRejectedCallbackAsync(RejectedCallback.Skip, callbackQuery.Id);
            return user;
        }

        if (!await user.HandlePermissiveAsync(message)) {
            await user.HandleRejectedCallbackAsync(RejectedCallback.Permission, callbackQuery.Id);
            return user;
        }

        try {
            var isHandle = await user.HandleCallbackAsync(callbackQuery.Id, clickedNavigation, message.MessageId, message.Chat.Id);
            if (isHandle) {
                await client.AnswerCallbackQuery(callbackQuery.Id);
            } else {
                await user.HandleRejectedCallbackAsync(RejectedCallback.Unknown, callbackQuery.Id);
            }
        } catch (Exception ex) {
            logger.LogError(ex, "Error in callback for user {UserId}", user.chatId);
            try {
                await user.HandleErrorAsync(ex);
                await client.AnswerCallbackQuery(callbackQuery.Id);
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

        if (!user.acceptLicense) {
            await user.HandleAcceptLicense(message);
            return user;
        }

        if (message.Photo is PhotoSize[] photo) {
            await user.HandlePhotoAsync(photo, message);
            return user;
        }

        if (message.Document is Document document) {
            await user.HandleDocumentAsync(document, message);
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


    /// <summary>
    /// Routes incoming updates to appropriate handlers.
    /// </summary>
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
                logger.LogError(ex, "Error in UpdateHandlerAsync for user {UserId}", user?.chatId);
                if (user is not null) {
                    try {
                        await user.HandleErrorAsync(ex);
                    } catch (Exception ex2) {
                        logger.LogError(ex2, "Error in HandleErrorAsync for user {UserId}", user.chatId);
                    }
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


    /// <summary>
    /// Handles unhandled exceptions from the bot client.
    /// </summary>
    protected Task ErrorHandlerAsync(Exception exception, CancellationToken cancellationToken) {
        logger.LogError(exception, "Unhandled error in bot worker");
        return Task.CompletedTask;
    }


    /// <summary>
    /// Runs cache eviction based on configured policy.
    /// </summary>
    public async Task ClearCacheAsync() {
        var count = Interlocked.Increment(ref requestCount);

        await cacheLock.WaitAsync();
        try {
            // Run eviction policy inside lock to prevent race conditions
            var keysToEvict = cacheEvictionPolicy(count, usersCache, clearCacheTime).ToList();

            if (keysToEvict.Count == 0) {
                return;
            }

            logger.LogDebug("Clearing {Count} users from cache", keysToEvict.Count);

            foreach (var key in keysToEvict) {
                if (usersCache.TryGetValue(key, out var user)) {
                    user.value.Dispose();
                    usersCache.Remove(key);
                }
            }

            foreach (var user in usersCache) {
                user.Value.value.callbackFactory.ClearCache();
            }
        } finally {
            cacheLock.Release();
        }
    }


    /// <summary>
    /// Gets existing user from cache or creates a new one.
    /// </summary>
    /// <param name="chatId">Telegram chat ID.</param>
    /// <param name="message">Optional message that triggered the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User instance.</returns>
    public async Task<T> GetOrCreateChatUserAsync(
        long chatId,
        Message? message = null,
        CancellationToken cancellationToken = default
    ) {
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
            await ClearCacheAsync();
        }
    }
}
