using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.UI.MenuBuilder.Elements;


public class MenuCheckbox : MenuElement {
    public required string title { get; set; }
    public string temp { get; set; } = "{{ if selected }}✅{{ end }} {{ title }}";
    public bool isSelected { get; private set; }
    private string? callbackId = null;
    public delegate Task UpdateHandler(bool isSelected);
    public event UpdateHandler? onUpdate;



    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackId);
    }





    public async Task SelectAsync(bool isSelected) {
        this.isSelected = isSelected;
        if (onUpdate is not null) {
            await onUpdate.Invoke(isSelected);
        }
    }





    public override async Task<List<InlineKeyboardButton>> BuildAsync() {
        if (hide) {
            return new();
        }

        botUser.callbackFactory.Unsubscribe(callbackId);

        callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (callbackQueryId, messageId, chatId) => {
            isSelected = !isSelected;

            if (onUpdate is not null) {
                await onUpdate.Invoke(isSelected);
            }
            await parrent.UpdatePageAsync(messageId, chatId);
        });

        var models = await parrent.InheritedRequestModelAsync();
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
