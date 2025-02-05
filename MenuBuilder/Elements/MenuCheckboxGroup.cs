using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.MenuBuilder.Elements.Selectors;

namespace Telegram.Bot.UI.MenuBuilder.Elements;


public class MenuCheckboxGroup : MenuElement {
    public required IEnumerable<MenuSelector> buttons { get; init; }
    public string temp { get; set; } = "{{ if selected }}✅{{ end }} {{ title }}";
    public List<int> selected { get; private set; } = [];
    public IEnumerable<string> selectedId => selectButton.Select(x => x.id);
    public List<MenuSelector> selectButton => buttons.Where((x, i) => selected.Contains(i)).ToList();
    public List<string> callbackIdList = new();
    public delegate void UpdateHandler(MenuSelector selectButton, bool isSelect);
    public event UpdateHandler? onUpdate;





    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackIdList);
    }





    public void Select(string id) {
        if (ButtonFromId(id) is not (MenuSelector button, int index)) {
            return;
        }

        if (!selected.Contains(index)) {
            selected.Add(index);
            onUpdate?.Invoke(button, true);
        }
    }





    public void Unselect(string id) {
        if (ButtonFromId(id) is not (MenuSelector button, int index)) {
            return;
        }

        if (selected.Contains(index)) {
            selected.RemoveAt(index);
            onUpdate?.Invoke(button, false);
        }
    }





    public bool IsSelect(string id) {
        if (ButtonFromId(id) is not (MenuSelector button, int index)) {
            return false;
        }

        return selected.Contains(index);
    }





    private (MenuSelector button, int index)? ButtonFromId(string id) {
        var select = buttons.Where(x => x.id == id).Select((button, index) => (button, index));
        if (!select.Any()) {
            return null;
        }

        return select.First();
    }





    public override List<InlineKeyboardButton> Build() {
        if (hide) {
            return new();
        }


        botUser.callbackFactory.Unsubscribe(callbackIdList);
        callbackIdList.Clear();

        var list = new List<InlineKeyboardButton>();

        foreach (var (button, index) in MenuSelector.WithIndex(buttons)) {
            var callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (callbackQueryId, messageId, chatId) => {

                if (selected.Contains(index)) {
                    selected.RemoveAt(index);
                } else {
                    selected.Add(index);
                }

                onUpdate?.Invoke(selectButton[index], selected.Contains(index));
                await parrent.UpdatePageAsync(messageId, chatId);
            });

            callbackIdList.Add(callbackId);

            var models = parrent.InheritedRequestModel();
            models.Add(new {
                selected = selected.Contains(index),
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
