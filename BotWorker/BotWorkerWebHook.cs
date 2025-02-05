using Newtonsoft.Json;
using System.Net;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.UI.BotWorker;


public class BotWorkerWebHook<T> : BaseBotWorker<T> where T : BaseBotUser {
    protected TelegramBotClient? botClient;
    private HttpListener listener = new();
    public required string botHostAddress { private get; init; }
    public required string botSecretToken { private get; init; }
    public required string botToken { private get; init; }
    public required string botRoute { private get; init; }
    public required int port { private get; init; }



    public BotWorkerWebHook(UserFactoryDelegate userFactory) : base(userFactory) { }





    protected override async Task StartHandleAsync() {
        _ = Task.Run(async () => {
            listener.Prefixes.Add($"http://*:{port}/");
            listener.Start();

            while (true) {
                var context = await listener.GetContextAsync();
                if (context.Request.HttpMethod == "POST" && context?.Request?.Url?.AbsolutePath == $"/{botRoute}") {
                    await HandlePostRequestAsync(context);
                } else {
                    context!.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.Close();
                }
            }
        });

        botClient = new(botToken);

        await botClient.SetWebhook(
            url: $"{botHostAddress}/{botRoute}",
            allowedUpdates: Array.Empty<UpdateType>(),
            secretToken: botSecretToken,
            cancellationToken: cancellationTokenSource.Token
        );
    }





    private async Task HandlePostRequestAsync(HttpListenerContext context) {
        try {
            using (var reader = new StreamReader(context.Request.InputStream)) {
                var body = await reader.ReadToEndAsync();
                var update = JsonConvert.DeserializeObject<Update>(body);
                var statusCode = UpdateHandler(context, update!);
                context.Response.StatusCode = (int)statusCode;
            }
        } catch (Exception ex) {
            await ErrorHandlerAsync(botClient!, ex, cancellationTokenSource.Token);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        } finally {
            context.Response.Close();
        }
    }





    private HttpStatusCode UpdateHandler(HttpListenerContext context, Update update) {
        if (!IsValidRequest(context.Request)) {
            return HttpStatusCode.Forbidden;
        }

        _ = Task.Run(async () => {
            try {
                await UpdateHandlerAsync(botClient!, update, cancellationTokenSource.Token);
            } catch (Exception ex) {
                try {
                    await ErrorHandlerAsync(botClient!, ex, cancellationTokenSource.Token);
                } catch { }
            }
        });

        return HttpStatusCode.OK;
    }





    private bool IsValidRequest(HttpListenerRequest request) {
        var isSecretTokenProvided = request.Headers["X-Telegram-Bot-Api-Secret-Token"] != null;
        if (!isSecretTokenProvided) {
            return false;
        }

        return string.Equals(request.Headers["X-Telegram-Bot-Api-Secret-Token"], botSecretToken, StringComparison.Ordinal);
    }





    protected override async Task StopHandleAsync() {
        if (botClient is not null) {
            await botClient.DeleteWebhook();
        }

        listener.Stop();
        listener.Close();

        cancellationTokenSource.Cancel();
    }
}
