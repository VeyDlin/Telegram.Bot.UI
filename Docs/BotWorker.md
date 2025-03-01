# Bot Worker

Bot Worker - subsystem for managing the lifecycle of a Telegram bot, processing updates, and interacting with users.

## IBotWorker

`IBotWorker` - interface that defines the basic functionality for all types of Bot Workers.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `DateTime startTime { get; }` | Bot start time. |
| `PageResourceLoader pageResourceLoader { get; }` | Page resource loader. |
| `LocalizationPack? localizationPack { get; set; }` | Localization package for the bot. |
| `Func<Task>? onStopRequest { get; set; }` | Function called when a bot stop request is made. |

### Public Methods

| Interface | Description |
|-----------|-------------|
| `Task StartAsync()` | Starts the bot and begins processing messages. |
| `Task StopAsync()` | Stops the bot and terminates message processing. |
| `Task<DisposeAction> CriticalAsync()` | Performs an operation in a critical section, preventing the bot from stopping until exiting the section. |

## BaseBotWorker<T>

`BaseBotWorker<T>` - abstract class implementing the main bot logic and user management.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `DateTime startTime { get; }` | Bot start time. |
| `PageResourceLoader pageResourceLoader { get; }` | Page resource loader. |
| `LocalizationPack? localizationPack { get; set; }` | Localization package for the bot. |
| `Func<Task>? onStopRequest { get; set; }` | Function called when a bot stop request is made. |
| `CancellationTokenSource cancellationTokenSource { get; set; }` | Cancellation token source for asynchronous operations. |
| `bool skipMessagesBeforeStart { get; set; }` | Flag to skip messages sent before the bot starts. Default is `true`. |
| `string? resourcePath { set; }` | Path to bot resources. When set, creates a new instance of `PageResourceLoader`. |
| `bool isSafeStopSet { get; }` | Flag indicating that bot shutdown has been initiated. |

### Public Methods

| Interface | Description |
|-----------|-------------|
| `BaseBotWorker(UserFactoryDelegate userFactory)` | Constructor that accepts a factory for creating user instances. |
| `Task StartAsync()` | Starts the bot and begins processing messages. |
| `Task StopAsync()` | Stops the bot and terminates message processing. |
| `Task<DisposeAction> CriticalAsync()` | Performs an operation in a critical section, preventing the bot from stopping until exiting the section. |

### Protected Methods

| Interface | Description |
|-----------|-------------|
| `virtual Task StartHandleAsync()` | Abstract method for starting update processing. Overridden in derived classes. |
| `virtual Task StopHandleAsync()` | Virtual method for stopping update processing. |
| `Task UpdateHandlerAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)` | Processes updates from the Telegram API. |
| `Task ErrorHandlerAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)` | Handles errors that occur during update processing. |

### Delegates

| Interface | Description |
|-----------|-------------|
| `delegate T UserFactoryDelegate(IBotWorker worker, long chatId, ITelegramBotClient client, CancellationToken token)` | Delegate for creating bot user instances. |

## BotWorkerPulling<T>

`BotWorkerPulling<T>` - class for working with the bot via Long Polling method (periodic API requests).

### Public Properties

| Interface | Description |
|-----------|-------------|
| `string botToken { init; }` | Telegram bot token. When set, creates an instance of `TelegramBotClient`. |

### Public Methods

| Interface | Description |
|-----------|-------------|
| `BotWorkerPulling(UserFactoryDelegate userFactory)` | Constructor that accepts a factory for creating user instances. |

### Protected Methods

| Interface | Description |
|-----------|-------------|
| `override Task StartHandleAsync()` | Starts the process of receiving updates from the Telegram API via Long Polling. |
| `override Task StopHandleAsync()` | Stops the update receiving process. |

## BotWorkerWebHook<T>

`BotWorkerWebHook<T>` - class for working with the bot via WebHook.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `required string botHostAddress { get; init; }` | Host address to which Telegram will send updates. |
| `required string botSecretToken { get; init; }` | Secret token for verifying the authenticity of requests from Telegram. |
| `required string botToken { get; init; }` | Telegram bot token. |
| `required string botRoute { get; init; }` | URL path where updates will be received. |

### Public Methods

| Interface | Description |
|-----------|-------------|
| `BotWorkerWebHook(UserFactoryDelegate userFactory)` | Constructor that accepts a factory for creating user instances. |
| `Task UpdateHandlerAsync(Update update)` | Processes an update received via WebHook. |
| `bool ValidateTelegramHeader(string? xTelegramHeader)` | Validates the request header against the secret token. |

### Protected Methods

| Interface | Description |
|-----------|-------------|
| `override Task StartHandleAsync()` | Registers the WebHook with the Telegram API. |
| `override Task StopHandleAsync()` | Removes the WebHook registration and stops processing. |

## BotWorkerWebHookServer<T>

`BotWorkerWebHookServer<T>` - class for working with the bot via WebHook with a built-in HTTP server for receiving updates.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `required int port { get; init; }` | Port on which the HTTP server will be launched to receive updates. |

### Public Methods

| Interface | Description |
|-----------|-------------|
| `BotWorkerWebHookServer(UserFactoryDelegate userFactory)` | Constructor that accepts a factory for creating user instances. |

### Protected Methods

| Interface | Description |
|-----------|-------------|
| `override Task StartHandleAsync()` | Starts the HTTP server and registers the WebHook with the Telegram API. |
| `override Task StopHandleAsync()` | Stops the HTTP server, removes the WebHook registration, and terminates processing. |
| `Task HandlePostRequestAsync(HttpListenerContext context)` | Processes an HTTP POST request containing an update from the Telegram API. |
