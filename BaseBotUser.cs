using Localization;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.BotWorker;

namespace Telegram.Bot.UI;


public abstract class BaseBotUser {
    public long chatId { get; private set; }
    public IBotWorker worker { get; private set; }
    public ITelegramBotClient client { get; private set; }
    public CancellationToken cancellationToken { get; private set; }
    public CallbackFactory callbackFactory { get; private set; } = new();
    public LocalizationManager localization { get; private set; } = new();
    public ParseMode parseMode { get; set; } = ParseMode.Markdown;
    public bool acceptLicense { get; set; } = true;
    public bool enableCommands { get; set; } = true;



    public BaseBotUser(IBotWorker worker, long chatId, ITelegramBotClient client, CancellationToken token) {
        this.worker = worker;
        this.chatId = chatId;
        this.client = client;
        cancellationToken = token;
        localization = new(worker.localizationPack);
    }





    public string L(string text) => localization.Translate(text) ?? string.Empty;
    public LocalizedString LS(string text) => new(localization, text);
    public string? LN(string? text) => localization.Translate(text);





    public virtual void Begin() { }

    public virtual Task HandleMessageAsync(string text, Message message) => Task.CompletedTask;

    public virtual Task HandlePhotoAsync(PhotoSize[] photo, Message message) => Task.CompletedTask;

    public virtual Task HandleCommandAsync(string cmd, string[] arguments, Message message) => Task.CompletedTask;

    public virtual Task HandleErrorAsync(Exception exception) => Task.CompletedTask;

    public virtual Task HandleOtherMessageAsync(Message message) => Task.CompletedTask;

    public virtual Task<bool> HandlePermissiveAsync(Message message) => Task.FromResult(true);

    public virtual Task HandleAcceptLicense(Message message) => Task.CompletedTask;

    public virtual Task HandleSuccessPayment(SuccessfulPayment payment) => Task.CompletedTask;

    public virtual Task<string?> HandleStoppingProcess(Message message) => Task.FromResult<string?>(null);





    public bool HandleCallbackAsync(string callbackQueryId, string callbackId, int messageId, long chatId) {
        return callbackFactory.InvokeAsync(callbackQueryId, callbackId, messageId, chatId);
    }





    public Task SendChatActionAsync(ChatAction action, CancellationToken cancellationToken = default) {
        return client.SendChatAction(
            chatId: chatId,
            action: action,
            cancellationToken: cancellationToken
        );
    }





    public void SendChatLongAction(ChatAction action, CancellationTokenSource token, int delay = 4000) {
        _ = Task.Run(async () => {
            while (!token.IsCancellationRequested) {
                await SendChatActionAsync(ChatAction.UploadPhoto, token.Token);
                await Task.Delay(delay);
            }
        });
    }





    public Task<Message> SendUrlButtonsAsync(string message, string text, string url) {
        return client.SendMessage(
            chatId: chatId, 
            text: message,
            cancellationToken: cancellationToken,
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl(text, url))
        );
    }





    public Task DeleteMessageAsync(int messageId) {
        return client.DeleteMessage(
            chatId: chatId,
            messageId: messageId
        );
    }





    public Task<Message> SendTextMessageAsync(string? text, IReplyMarkup? markup = null, ParseMode? mode = null, bool webPreview = true) {
        return client.SendMessage(
            chatId: chatId,
            text: string.IsNullOrEmpty(text) ? "..." : text,
            parseMode: mode ?? parseMode,
            cancellationToken: cancellationToken,
            replyMarkup: markup,
            linkPreviewOptions: new() {
                IsDisabled = !webPreview
            }
        );
    }


    public Task<Message> SendDocumentAsync(string text, InputFile file, IReplyMarkup? markup = null, ParseMode? mode = null) {
        return client.SendDocument(
            chatId: chatId,
            document: file,
            caption: text,
            parseMode: mode ?? parseMode,
            cancellationToken: cancellationToken,
            replyMarkup: markup
        );
    }



    public Task<Message> SendDocumentAsync(string text, byte[] image, IReplyMarkup? markup = null, ParseMode? mode = null) {
        return SendDocumentAsync(text, InputFile.FromStream(new MemoryStream(image)), markup, mode);
    }



    public Task<Message> SendDocumentAsync(string text, string imageId, IReplyMarkup? markup = null, ParseMode? mode = null) {
        return SendDocumentAsync(text, InputFile.FromString(imageId), markup, mode);
    }





    public Task<Message> SendPhotoAsync(InputFile file, string? text = null, IReplyMarkup? markup = null, ParseMode? mode = null) {
        return client.SendPhoto(
            chatId: chatId,
            photo: file,
            caption: text,
            parseMode: mode ?? parseMode,
            cancellationToken: cancellationToken,
            replyMarkup: markup
        );
    }



    public Task<Message> SendPhotoAsync(byte[] image, string? text = null, IReplyMarkup? markup = null, ParseMode? mode = null) {
        return SendPhotoAsync(InputFile.FromStream(new MemoryStream(image)), text, markup, mode);
    }



    public Task<Message> SendPhotoAsync(string imageId, string? text = null, IReplyMarkup? markup = null, ParseMode? mode = null) {
        return SendPhotoAsync(InputFile.FromString(imageId), text, markup, mode);
    }






    public Task<Message> EditMessageTextAsync(int messageId, long chatId, string? text = null, InlineKeyboardMarkup? markup = null, ParseMode? mode = null, bool webPreview = true) {
        return client.EditMessageText(
           chatId: chatId,
           messageId: messageId,
           text: string.IsNullOrEmpty(text) ? "..." : text,
           parseMode: mode ?? parseMode,
           replyMarkup: markup,
           linkPreviewOptions: new() {
               IsDisabled = !webPreview
           }
        );
    }





    public Task<Message> EditMessageMediaAsync(int messageId, long chatId, InputMedia media, InlineKeyboardMarkup? markup = null) {
        return client.EditMessageMedia(
           chatId: chatId,
           messageId: messageId,
           media: media,
           replyMarkup: markup
        );
    }



    public Task<Message> EditMessageImageAsync(int messageId, long chatId, byte[] image, string? text = null, InlineKeyboardMarkup? markup = null) {
        return EditMessageMediaAsync(
            messageId,
            chatId,
            new InputMediaPhoto(InputFile.FromStream(new MemoryStream(image))) {
                Caption = text,
                ParseMode = ParseMode.Markdown
            },
            markup
        );
    }



    public Task<Message> EditMessageImageAsync(int messageId, long chatId, string imageId, InlineKeyboardMarkup? markup = null) {
        return EditMessageMediaAsync(
            messageId,
            chatId,
            new InputMediaPhoto(InputFile.FromString(imageId)),
            markup
        );
    }



    public Task<Message> EditMessageDocumentAsync(int messageId, long chatId, byte[] image, InlineKeyboardMarkup? markup = null) {
        return EditMessageMediaAsync(
            messageId,
            chatId,
            new InputMediaDocument(InputFile.FromStream(new MemoryStream(image))),
            markup
        );
    }



    public Task<Message> EditMessageDocumentAsync(int messageId, long chatId, string imageId, InlineKeyboardMarkup? markup = null) {
        return EditMessageMediaAsync(
            messageId,
            chatId,
            new InputMediaDocument(InputFile.FromString(imageId)),
            markup
        );
    }





    public Task ShowAlertAsync(string? text, string callbackQueryId, bool showAlert = false) {
        return client.AnswerCallbackQuery(
            callbackQueryId: callbackQueryId,
            text: string.IsNullOrEmpty(text) ? "..." : text,
            showAlert: showAlert,
            cancellationToken: cancellationToken
        );
    }





    public string EscapeText(string? text, ParseMode? mode = null) {
        if (string.IsNullOrEmpty(text)) {
            return text ?? "";
        }

        switch (mode ?? parseMode) {
            case ParseMode.Markdown: {
                char[] markdownSpecialChars = { '_', '*', '`', '[' };
                foreach (var specialChar in markdownSpecialChars) {
                    text = text.Replace(specialChar.ToString(), "\\" + specialChar);
                }
                return text;
            }

            case ParseMode.MarkdownV2: {
                char[] markdownV2SpecialChars = { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
                foreach (var specialChar in markdownV2SpecialChars) {
                    text = text.Replace(specialChar.ToString(), "\\" + specialChar);
                }
                return text.Replace("\\", "\\\\");
            }

            case ParseMode.Html: {
                text = text
                    .Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;");
                return text;
            }
        }

        return text;
    }
}
