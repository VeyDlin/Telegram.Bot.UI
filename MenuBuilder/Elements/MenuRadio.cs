using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.MenuBuilder.Elements.Selectors;

namespace Telegram.Bot.UI.MenuBuilder.Elements;


public class MenuRadio : MenuElement {
    public required List<MenuSelector> buttons { get; init; }
    public string temp { get; set; } = "{{ if selected }}✅{{ end }} {{ title }}";
    public int selected { get; private set; } = 0;
    public string selectedId => selectButton.id;
    public MenuSelector selectButton => buttons[selected];
    public List<string> callbackIdList = new();
    public delegate void SelectHandler(MenuSelector selectButton);
    public event SelectHandler? onSelect;



    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackIdList);
    }



    public void Select(string id) {
        var select = buttons.Select((button, index) => (button, index)).Where(x => x.button.id == id);

        if (select.Any() && select.First().index != selected) {
            selected = select.First().index;
            onSelect?.Invoke(selectButton);
        }
    }



    public override List<InlineKeyboardButton> Build() {
        if (hide) {
            return new();
        }

        if (selected > buttons.Count() || selected < 0) {
            throw new Exception($"{nameof(selected)} out of range. buttons.Count(): {buttons.Count()}; selectedPage: {selected}");
        }

        botUser.callbackFactory.Unsubscribe(callbackIdList);
        callbackIdList.Clear();

        var list = new List<InlineKeyboardButton>();

        foreach (var (button, index) in MenuSelector.WithIndex(buttons)) {
            var callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (callbackQueryId, messageId, chatId) => {
                selected = index;
                onSelect?.Invoke(selectButton);
                await parrent.UpdatePageAsync(messageId, chatId);
            });

            callbackIdList.Add(callbackId);


            var models = parrent.InheritedRequestModel();
            models.Add(new {
                selected = selected == index,
                title = TemplateEngine.Render(button.title, models, botUser.localization)
            });

            list.Add(
                InlineKeyboardButton.WithCallbackData(
                    text: TemplateEngine.Render(temp, models, botUser.localization),
                    callbackData: callbackId
                )
            );
        }

        return list;
    }
}

