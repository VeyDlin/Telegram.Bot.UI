using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.UI.Menu;

/// <summary>
/// Represents a menu split element that forces a new row in the keyboard layout.
/// </summary>
public class MenuSplit : MenuElement {
    /// <summary>
    /// Builds an empty button list to create a row break.
    /// </summary>
    /// <returns>An empty list of inline keyboard buttons.</returns>
    public override Task<List<InlineKeyboardButton>> BuildAsync() => Task.FromResult<List<InlineKeyboardButton>>(new());
}