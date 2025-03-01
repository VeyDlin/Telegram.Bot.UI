# BaseBotUser

`BaseBotUser` - is an abstract class representing a Telegram bot user with basic functionality for processing messages and sending responses.

## Class Interface

### Public Properties

| Interface | Description |
|-----------|-------------|
| `long chatId { get; }` | User's chat identifier |
| `IBotWorker worker { get; }` | Instance of IBotWorker for bot operations |
| `ITelegramBotClient client { get; }` | Client for working with Telegram API |
| `CancellationToken cancellationToken { get; }` | Cancellation token for managing asynchronous operations |
| `CallbackFactory callbackFactory { get; }` | Factory for creating and managing callbacks |
| `LocalizationManager localization { get; }` | Localization manager for text translations |
| `ParseMode parseMode { get; set; }` | Message parsing mode (Markdown by default) |
| `bool acceptLicense { get; set; }` | License agreement acceptance flag |
| `bool enableCommands { get; set; }` | Flag for enabling command processing (for HandleCommandAsync method) |

### Public Methods

#### Core Methods

| Interface | Description |
|-----------|-------------|
| `BaseBotUser(IBotWorker worker, long chatId, ITelegramBotClient client, CancellationToken token)` | Constructor initializing the user with specified parameters |
| `string L(string text)` | Returns translated text for the user's current language |
| `LocalizedString LS(string text)` | Returns a localized string for the specified key |
| `string? LN(string? text)` | Localizes the specified text if it's not null |
| `string EscapeText(string? text, ParseMode? mode = null)` | Escapes special characters in the message text for the selected parsing mode |

#### Message Processing Methods

| Interface | Description |
|-----------|-------------|
| `virtual void Begin()` | Called immediately after an instance of the class is created |
| `virtual Task HandleMessageAsync(string text, Message message)` | Called for all text messages |
| `virtual Task HandleCommandAsync(string cmd, string[] arguments, Message message)` | Called for all messages starting with "/", if the enableCommands flag is set |
| `virtual Task HandlePhotoAsync(PhotoSize[] photo, Message message)` | Called for all messages with photos |
| `virtual Task HandleSuccessPayment(SuccessfulPayment payment)` | Called by Telegram API when interacting with the payment system |
| `virtual Task HandleErrorAsync(Exception exception)` | Called for any unhandled exception |
| `virtual Task HandleOtherMessageAsync(Message message)` | Called for all messages that don't fall into any category |
| `virtual Task<bool> HandlePermissiveAsync(Message message)` | Called to get permission before further processing, can be used to ban users |
| `virtual Task HandleAcceptLicense(Message message)` | Called for any message if the acceptLicense flag is not true. HandlePermissiveAsync is called first. Used for license agreement acceptance |
| `virtual Task<string?> HandleStoppingProcess(Message message)` | Should return a string with information to be sent to the user when actions are taken while the bot is being stopped |
| `bool HandleCallbackAsync(string callbackQueryId, string callbackId, int messageId, long chatId)` | Processes callbacks from buttons and calls the corresponding handlers |

#### Message Sending and Chat Management Methods

| Interface | Description |
|-----------|-------------|
| `Task SendChatActionAsync(ChatAction action, CancellationToken cancellationToken = default)` | Sends information about an action to the chat (typing, sending photo, etc.) |
| `void SendChatLongAction(ChatAction action, CancellationTokenSource token, int delay = 4000)` | Sends information about a long-term action to the chat at specified intervals |
| `Task<Message> SendUrlButtonsAsync(string message, string text, string url)` | Sends a message with a URL button |
| `Task DeleteMessageAsync(int messageId)` | Deletes a message by the specified identifier |
| `Task<Message> SendTextMessageAsync(string? text, IReplyMarkup? markup = null, ParseMode? mode = null, bool webPreview = true)` | Sends a text message with an optional keyboard |
| `Task<Message> SendDocumentAsync(string text, InputFile file, IReplyMarkup? markup = null, ParseMode? mode = null)` | Sends a document with a caption and optional keyboard |
| `Task<Message> SendDocumentAsync(string text, byte[] image, IReplyMarkup? markup = null, ParseMode? mode = null)` | Sends a document from a byte array with a caption and optional keyboard |
| `Task<Message> SendDocumentAsync(string text, string imageId, IReplyMarkup? markup = null, ParseMode? mode = null)` | Sends a document by ID with a caption and optional keyboard |
| `Task<Message> SendPhotoAsync(InputFile file, string? text = null, IReplyMarkup? markup = null, ParseMode? mode = null)` | Sends a photo with optional caption and keyboard |
| `Task<Message> SendPhotoAsync(byte[] image, string? text = null, IReplyMarkup? markup = null, ParseMode? mode = null)` | Sends a photo from a byte array with optional caption and keyboard |
| `Task<Message> SendPhotoAsync(string imageId, string? text = null, IReplyMarkup? markup = null, ParseMode? mode = null)` | Sends a photo by ID with optional caption and keyboard |
| `Task<Message> EditMessageTextAsync(int messageId, long chatId, string? text = null, InlineKeyboardMarkup? markup = null, ParseMode? mode = null, bool webPreview = true)` | Edits the text of an existing message |
| `Task<Message> EditMessageMediaAsync(int messageId, long chatId, InputMedia media, InlineKeyboardMarkup? markup = null)` | Edits the media content of an existing message |
| `Task<Message> EditMessageImageAsync(int messageId, long chatId, byte[] image, string? text = null, InlineKeyboardMarkup? markup = null)` | Replaces an image in an existing message |
| `Task<Message> EditMessageImageAsync(int messageId, long chatId, string imageId, InlineKeyboardMarkup? markup = null)` | Replaces an image by ID in an existing message |
| `Task<Message> EditMessageDocumentAsync(int messageId, long chatId, byte[] image, InlineKeyboardMarkup? markup = null)` | Replaces a document in an existing message |
| `Task<Message> EditMessageDocumentAsync(int messageId, long chatId, string imageId, InlineKeyboardMarkup? markup = null)` | Replaces a document by ID in an existing message |
| `Task ShowAlertAsync(string? text, string callbackQueryId, bool showAlert = false)` | Shows a popup notification or alert to the user in response to a button press |

## Usage Example

```csharp
public class MyBotUser : BaseBotUser {
    public InformationView informationView;

    public MyBotUser(IBotWorker worker, long chatId, ITelegramBotClient client, CancellationToken token) 
        : base(worker, chatId, client, token) 
    {
        informationView = new(this);
    }

    public override async Task HandleCommandAsync(string cmd, string[] arguments, Message message) {
        switch (cmd) {
            case "start":
                await informationView.SendPageAsync();
            break;
            case "help":
                await SendTextMessageAsync("This is a demonstration bot. Use /start to begin.");
            break;
            default:
                await SendTextMessageAsync($"Unknown command: {cmd}");
            break;
        }
    }
}
```





