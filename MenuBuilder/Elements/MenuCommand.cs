using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.UI.MenuBuilder.Elements;


public class MenuCommand : MenuElement {
    public required string title { get; set; }
    public string temp { get; set; } = "{{ title }}";
    public delegate void ClickHandler(CallbackData callback);
    public event CallbackFactory.CallbackHandler? onClick;
    private string? callbackId = null;



    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackId);
    }



    public override List<InlineKeyboardButton> Build() {
        if (hide) {
            return new();
        }

        botUser.callbackFactory.Unsubscribe(callbackId);

        callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, onClick);

        var models = parrent.InheritedRequestModel();
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
