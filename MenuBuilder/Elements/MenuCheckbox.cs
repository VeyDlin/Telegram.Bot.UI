using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.UI.MenuBuilder.Elements;


public class MenuCheckbox : MenuElement {
    public required string title { get; set; }
    public string temp { get; set; } = "{{ if selected }}✅{{ end }} {{ title }}";
    public bool isSelected { get; private set; }
    private string? callbackId = null;
    public delegate void UpdateHandler(bool isSelected);
    public event UpdateHandler? onUpdate;



    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackId);
    }





    public void Select(bool isSelected) {
        this.isSelected = isSelected;
        onUpdate?.Invoke(isSelected);
    }





    public override List<InlineKeyboardButton> Build() {
        if (hide) {
            return new();
        }

        botUser.callbackFactory.Unsubscribe(callbackId);

        callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (callbackQueryId, messageId, chatId) => {
            isSelected = !isSelected;

            onUpdate?.Invoke(isSelected);
            await parrent.UpdatePageAsync(messageId, chatId);
        });

        var models = parrent.InheritedRequestModel();
        models.Add(new {
            selected = isSelected,
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
