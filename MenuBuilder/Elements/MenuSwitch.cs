using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.MenuBuilder.Elements.Selectors;

namespace Telegram.Bot.UI.MenuBuilder.Elements;


public class MenuSwitch : MenuElement {
    public required List<MenuSelector> buttons { get; init; }
    public int selected { get; set; } = 0;
    public string selectedId { get => buttons[selected].id!; }
    public MenuSelector selectButton { get => buttons[selected]; }
    private string? callbackId = null;
    public delegate void UpdateHandler(MenuSelector selectButton);
    public event UpdateHandler? onUpdate;



    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackId);
    }





    public void Select(string id) {
        var select = buttons.Select((button, index) => (button, index)).Where(x => x.button.id == id);

        if (select.Any() && select.First().index != selected) {
            selected = select.First().index;
            onUpdate?.Invoke(selectButton);
        }
    }





    public override List<InlineKeyboardButton> Build() {
        if (hide) {
            return new();
        }

        if (selected > buttons.Count() || selected < 0) {
            throw new Exception($"{nameof(selected)} out of range. buttons.Count(): {buttons.Count()}; selectedPage: {selected}");
        }

        botUser.callbackFactory.Unsubscribe(callbackId);

        callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (callbackQueryId, messageId, chatId) => {
            selected++;
            selected = selected >= buttons.Count() ? 0 : selected;

            onUpdate?.Invoke(selectButton);
            await parrent.UpdatePageAsync(messageId, chatId);
        });


        var button = buttons[selected];

        return new() {
            InlineKeyboardButton.WithCallbackData(
                text: TemplateEngine.Render(button.title, parrent.InheritedRequestModel(), botUser.localization),
                callbackData: callbackId
            )
        };
    }
}
