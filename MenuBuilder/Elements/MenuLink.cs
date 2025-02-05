using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.UI.MenuBuilder.Elements;


public class MenuLink : MenuElement {
    public required string title { get; set; }
    public required string url { get; set; }






    public override List<InlineKeyboardButton> Build() {
        if (hide) {
            return new();
        }

        return new() {
            InlineKeyboardButton.WithUrl(
                text: TemplateEngine.Render(title, parrent.InheritedRequestModel(), botUser.localization),
                url: url
            )
        };
    }
}
