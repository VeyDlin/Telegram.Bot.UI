using Telegram.Bot.Types.ReplyMarkups;
using static Telegram.Bot.UI.CallbackFactory;

namespace Telegram.Bot.UI.MenuBuilder.Elements;


public class MenuCommand : MenuElement {
    public required string title { get; set; }
    public string temp { get; set; } = "{{ title }}";
    public delegate Task ClickHandler(CallbackHandler callback);
    public event CallbackHandler? onClick;
    private string? callbackId = null;



    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackId);
    }



    public override async Task<List<InlineKeyboardButton>> BuildAsync() {
        if (hide) {
            return new();
        }

        botUser.callbackFactory.Unsubscribe(callbackId);

        callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, onClick);

        var models = await parent.InheritedRequestModelAsync();
        models.Add(new {
            title = TemplateEngine.Render(title, models, botUser.localization)
        });

        return new() {
            InlineKeyboardButton.WithCallbackData(
                text: TemplateEngine.Render(temp, models, botUser.localization),
                callbackData: callbackId
            )
        };

    }
}
