using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.UI.BotWorker;


public class BotWorkerWebHook<T> : BaseBotWorker<T> where T : BaseBotUser {
    protected TelegramBotClient? botClient;
    public required string botHostAddress { protected get; init; }
    public required string botSecretToken { protected get; init; }
    public required string botToken { protected get; init; }
    public required string botRoute { protected get; init; }


    public BotWorkerWebHook(UserFactoryDelegate userFactory) : base(userFactory) { }



    protected override async Task StartHandleAsync() {
        botClient = new(botToken);

        await botClient.SetWebhook(
            url: $"{botHostAddress}/{botRoute}",
            allowedUpdates: Array.Empty<UpdateType>(),
            secretToken: botSecretToken,
            cancellationToken: cancellationTokenSource.Token
        );
    }





    public async Task UpdateHandlerAsync(Update update) {
        try {
            await UpdateHandlerAsync(botClient!, update, cancellationTokenSource.Token);
        } catch (Exception ex) {
            try {
                await ErrorHandlerAsync(botClient!, ex, cancellationTokenSource.Token);
            } catch { }
        }
    }





    public bool ValidateTelegramHeader(string? xTelegramHeader) {
        return string.Equals(xTelegramHeader, botSecretToken, StringComparison.Ordinal);
    }





    protected override async Task StopHandleAsync() {
        if (botClient is not null) {
            await botClient.DeleteWebhook();
        }

        cancellationTokenSource.Cancel();
    }
}