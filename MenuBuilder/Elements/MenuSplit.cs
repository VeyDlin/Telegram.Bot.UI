using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.UI.MenuBuilder.Elements;


public class MenuSplit : MenuElement {
    public override List<InlineKeyboardButton> Build() => new();
}
