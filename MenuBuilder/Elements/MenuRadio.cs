using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.MenuBuilder.Elements.Selectors;

namespace Telegram.Bot.UI.MenuBuilder.Elements;


public class MenuRadio : MenuElement {
    public List<MenuSelector> buttons { get; set; } = [];
    public string temp { get; set; } = "{{ if selected }}✅{{ end }} {{ title }}";
    public int selected { get; private set; } = 0;
    public string? selectedId => selectButton?.id;
    public MenuSelector? selectButton => selected >= 0 && selected < buttons.Count ? buttons[selected] : null;
    public List<string> callbackIdList = new();
    public delegate Task SelectHandler(MenuSelector selectButton);
    public event SelectHandler? onSelect;
    private readonly object selectedLock = new();



    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackIdList);
    }



    public async Task SetButtonsAsync(List<MenuSelector> buttons) {
        this.buttons = buttons;
        await BuildAsync();
    }



    public async Task SelectAsync(string id) {
        var select = buttons.Select((button, index) => (button, index)).Where(x => x.button.id == id);

        bool shouldInvoke = false;
        lock (selectedLock) {
            if (select.Any() && select.First().index != selected) {
                selected = select.First().index;
                shouldInvoke = true;
            }
        }

        if (shouldInvoke && onSelect is not null && selectButton is not null) {
            await onSelect.Invoke(selectButton);
        }
    }



    public override async Task<List<InlineKeyboardButton>> BuildAsync() {
        if (hide) {
            return new();
        }

        if (selected >= buttons.Count() || selected < 0) {
            throw new Exception($"{nameof(selected)} out of range. buttons.Count(): {buttons.Count()}; selectedPage: {selected}");
        }

        botUser.callbackFactory.Unsubscribe(callbackIdList);
        callbackIdList.Clear();

        var list = new List<InlineKeyboardButton>();

        foreach (var (button, index) in MenuSelector.WithIndex(buttons)) {
            var callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (callbackQueryId, messageId, chatId) => {
                lock (selectedLock) {
                    selected = index;
                }

                if (onSelect is not null && selectButton is not null) {
                    await onSelect.Invoke(selectButton);
                }
                await parent.UpdatePageAsync(messageId, chatId);
            });

            callbackIdList.Add(callbackId);


            var models = await parent.InheritedRequestModelAsync();
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

