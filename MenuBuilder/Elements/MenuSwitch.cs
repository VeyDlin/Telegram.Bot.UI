using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.MenuBuilder.Elements.Selectors;

namespace Telegram.Bot.UI.MenuBuilder.Elements;


public class MenuSwitch : MenuElement {
    public List<MenuSelector> buttons { get; set; } = [];
    public string temp { get; set; } = "{{ title }}";
    public int selected { get; set; } = 0;
    public string selectedId { get => buttons[selected].id!; }
    public MenuSelector selectButton { get => buttons[selected]; }
    private string? callbackId = null;
    public delegate Task UpdateHandler(MenuSelector selectButton);
    public event UpdateHandler? onUpdate;



    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackId);
    }



    public async Task SelectAsync(string id) {
        var select = buttons.Select((button, index) => (button, index)).Where(x => x.button.id == id);

        if (select.Any() && select.First().index != selected) {
            selected = select.First().index;
            if (onUpdate is not null) {
                await onUpdate.Invoke(selectButton);
            }
        }
    }



    public override async Task<List<InlineKeyboardButton>> BuildAsync() {
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

            if (onUpdate is not null) {
                await onUpdate.Invoke(selectButton);
            }
            await parrent.UpdatePageAsync(messageId, chatId);
        });


        var button = buttons[selected];

        var models = await parrent.InheritedRequestModelAsync();
        models.Add(new {
            selected = selected,
            title = TemplateEngine.Render(button.title, models, botUser.localization)
        });

        return new() {
            InlineKeyboardButton.WithCallbackData(
                text: TemplateEngine.Render(temp, models, botUser.localization),
                callbackData: callbackId
            )
        };
    }
}
