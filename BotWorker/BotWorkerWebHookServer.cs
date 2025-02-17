using Newtonsoft.Json;
using System.Net;
using Telegram.Bot.Types;

namespace Telegram.Bot.UI.BotWorker;


public class BotWorkerWebHookServer<T> : BotWorkerWebHook<T> where T : BaseBotUser {
    private HttpListener listener = new();
    public required int port { protected get; init; }



    public BotWorkerWebHookServer(UserFactoryDelegate userFactory) : base(userFactory) { }





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

        await base.StartHandleAsync();
    }





    private async Task HandlePostRequestAsync(HttpListenerContext context) {
        try {
            using (var reader = new StreamReader(context.Request.InputStream)) {
                if (ValidateTelegramHeader(context.Request.Headers["X-Telegram-Bot-Api-Secret-Token"])) {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    return;
                }

                var body = await reader.ReadToEndAsync();            
                _ = Task.Run(async () => {
                    var update = JsonConvert.DeserializeObject<Update>(body);
                    await UpdateHandlerAsync(update!);
                });

                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
        } catch (Exception ex) {
            await ErrorHandlerAsync(botClient!, ex, cancellationTokenSource.Token);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        } finally {
            context.Response.Close();
        }
    }





    protected override async Task StopHandleAsync() {
        await base.StopHandleAsync();

        listener.Stop();
        listener.Close();
    }
}
