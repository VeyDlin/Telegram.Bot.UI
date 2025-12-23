using Newtonsoft.Json;
using System.Net;
using Telegram.Bot.Types;

namespace Telegram.Bot.UI.BotWorker;

/// <summary>
/// Bot worker implementation that uses webhooks with a built-in HTTP server to receive updates from Telegram.
/// </summary>
/// <typeparam name="T">The type of bot user, must derive from BaseBotUser.</typeparam>
public class BotWorkerWebHookServer<T> : BotWorkerWebHook<T> where T : BaseBotUser {
    private HttpListener listener = new();

    /// <summary>
    /// Gets or sets the port number for the HTTP server to listen on.
    /// </summary>
    public required int port { protected get; init; }

    /// <summary>
    /// Initializes a new instance of the BotWorkerWebHookServer class.
    /// </summary>
    /// <param name="userFactory">The factory delegate for creating bot user instances.</param>
    public BotWorkerWebHookServer(UserFactoryDelegate userFactory) : base(userFactory) { }

    /// <summary>
    /// Starts the HTTP server to listen for webhook requests and registers the webhook with Telegram.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task StartHandleAsync() {
        _ = Task.Run(async () => {
            listener.Prefixes.Add($"http://*:{port}/");
            listener.Start();

            while (!cancellationTokenSource.Token.IsCancellationRequested) {
                try {
                    var context = await listener.GetContextAsync();
                    if (context.Request.HttpMethod == "POST" && context?.Request?.Url?.AbsolutePath == $"/{botRoute}") {
                        await HandlePostRequestAsync(context);
                    } else {
                        context!.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        context.Response.Close();
                    }
                } catch (HttpListenerException) when (cancellationTokenSource.Token.IsCancellationRequested) {
                    // Expected when listener is stopped
                    break;
                } catch (ObjectDisposedException) when (cancellationTokenSource.Token.IsCancellationRequested) {
                    // Expected when listener is disposed
                    break;
                }
            }
        });

        await base.StartHandleAsync();
    }

    /// <summary>
    /// Handles incoming POST requests from Telegram webhook.
    /// </summary>
    /// <param name="context">The HTTP listener context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandlePostRequestAsync(HttpListenerContext context) {
        try {
            using (var reader = new StreamReader(context.Request.InputStream)) {
                if (!ValidateTelegramHeader(context.Request.Headers["X-Telegram-Bot-Api-Secret-Token"])) {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    return;
                }

                var body = await reader.ReadToEndAsync();
                _ = Task.Run(async () => {
                    try {
                        var update = JsonConvert.DeserializeObject<Update>(body);
                        await UpdateHandlerAsync(update!);
                    } catch (Exception ex) {
                        await ErrorHandlerAsync(ex, cancellationTokenSource.Token);
                    }
                });

                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
        } catch (Exception ex) {
            await ErrorHandlerAsync(ex, cancellationTokenSource.Token);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        } finally {
            context.Response.Close();
        }
    }

    /// <summary>
    /// Stops the HTTP server and deletes the webhook.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task StopHandleAsync() {
        await base.StopHandleAsync();

        listener.Stop();
        listener.Close();
    }
}