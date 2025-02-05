using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.UI.MenuBuilder.Elements;


public class MenuOpenPege : MenuElement {
    public required MessagePage page { get; set; }
    public string? title { get; set; }
    public bool changeParrent { get; set; } = true;
    private string? callbackId = null;



    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackId);
    }





    public override List<InlineKeyboardButton> Build() {
        if (hide) {
            return new();
        }

        botUser.callbackFactory.Unsubscribe(callbackId);

        callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (callbackQueryId, messageId, chatId) => {
            if (changeParrent) {
                page.parrent = parrent;
            }
            await page.UpdatePageAsync(messageId, chatId);
        });

        return new() {
            InlineKeyboardButton.WithCallbackData(
                text: TemplateEngine.Render(title ?? page.title ?? "...", parrent.InheritedRequestModel(), botUser.localization),
                callbackData: callbackId
            )
        };
    }
}
