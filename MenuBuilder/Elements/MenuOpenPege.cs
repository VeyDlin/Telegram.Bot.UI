using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.UI.MenuBuilder.Elements;


public class MenuOpenPege : MenuElement {
    public required MessagePage page { get; set; }
    public string? title { get; set; }
    public bool changeParrent { get; set; } = true;
    public string temp { get; set; } = "{{ title }}";
    private string? callbackId = null;



    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackId);
    }



    public override async Task<List<InlineKeyboardButton>> BuildAsync() {
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

        var models = await parrent.InheritedRequestModelAsync();
        models.Add(new {
            title = TemplateEngine.Render(title ?? page.title ?? "...", models, botUser.localization),
            changeParrent = changeParrent
        });

        return new() {
            InlineKeyboardButton.WithCallbackData(
                text: TemplateEngine.Render(temp, models, botUser.localization),
                callbackData: callbackId
            )
        };
    }
}
