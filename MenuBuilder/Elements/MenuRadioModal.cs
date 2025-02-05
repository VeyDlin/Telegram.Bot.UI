using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.MenuBuilder.Elements.Modal;
using Telegram.Bot.UI.MenuBuilder.Elements.Selectors;

namespace Telegram.Bot.UI.MenuBuilder.Elements;


public class MenuRadioModal : MenuElement {
    public IEnumerable<MenuSelector> buttons { get; init; }
    public IEnumerable<MenuModalDetails>? details { get; init; }
    public string? title { get; set; }

    private MenuRadioModalPage modalPage;
    public override int columns { get => modalPage.columns; set => modalPage.columns = value; }
    public int selected => modalPage.selected;
    public string selectedId => modalPage.selectedId;
    public MenuSelector selectButton => modalPage.selectButton;
    public string temp { get => modalPage.temp; set => modalPage.temp = value; }

    private string? callbackId = null;

    public event MenuRadio.SelectHandler? onSelect {
        add => modalPage.onSelect += value;
        remove => modalPage.onSelect -= value;
    }



    public MenuRadioModal(IEnumerable<MenuSelector> buttons, IEnumerable<MenuModalDetails>? details, BaseBotUser botUser) {
        this.buttons = buttons;
        this.details = details;
        modalPage = new(buttons, details, botUser);
    }



    public void Select(string id) => modalPage.Select(id);



    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackId);
        modalPage?.Dispose();
    }



    public override List<InlineKeyboardButton> Build() {
        if (hide) {
            return new();
        }

        botUser.callbackFactory.Unsubscribe(callbackId);

        callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (callbackQueryId, messageId, chatId) => {
            modalPage.parrent = parrent;
            await modalPage.UpdatePageAsync(messageId, chatId);
        });


        var models = parrent.InheritedRequestModel();
        models.Add(new {
            title = TemplateEngine.Render(modalPage.title, models, botUser.localization)
        });

        return new() {
            InlineKeyboardButton.WithCallbackData(
                text: TemplateEngine.Render(title ?? "{{ title }}", models, botUser.localization),
                callbackData: callbackId
            )
        };
    }
}
