using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.UI.MenuBuilder.Elements;


public class MenuSplit : MenuElement {
    public override Task<List<InlineKeyboardButton>> BuildAsync() => Task.FromResult<List<InlineKeyboardButton>>(new());
}
