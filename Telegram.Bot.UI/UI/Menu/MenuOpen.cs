using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.Components;
using Telegram.Bot.UI.Components.Attributes;
using Telegram.Bot.UI.Runtime;

namespace Telegram.Bot.UI.Menu;

/// <summary>
/// Defines the type of action for the open button.
/// </summary>
public enum OpenType {
    /// <summary>
    /// Opens a page within the bot.
    /// </summary>
    [EnumMember("page")] Page,

    /// <summary>
    /// Opens an external URL link.
    /// </summary>
    [EnumMember("link")] Link,

    /// <summary>
    /// Opens a Telegram Web App.
    /// </summary>
    [EnumMember("app")] WebApp
}

/// <summary>
/// Represents a button that opens a page, link, or web app.
/// </summary>
[Component("open")]
public class MenuOpen : AutoComponent {
    /// <summary>
    /// Gets or sets the type attribute (page, link, or app).
    /// </summary>
    [Prop("type")]
    public string typeAttr { get; set; } = "page";

    /// <summary>
    /// Gets or sets the target (page name, URL, or web app URL).
    /// </summary>
    [Prop("target")]
    public string target { get; set; } = "";

    /// <summary>
    /// Gets or sets the button title text.
    /// </summary>
    [Prop("title")]
    public string title { get; set; } = "";

    /// <summary>
    /// Gets or sets whether to open as a sub-page (preserves navigation history).
    /// </summary>
    [Prop("subPage")]
    public string subPageAttr { get; set; } = "true";

    /// <summary>
    /// Gets or sets the target page for programmatic back button creation.
    /// </summary>
    public MessagePage? targetPage { get; set; }

    private string? callbackId = null;

    /// <summary>
    /// Gets the open type parsed from the type attribute.
    /// </summary>
    public OpenType type => GetPropEnum<OpenType>(nameof(typeAttr), OpenType.Page);

    /// <summary>
    /// Gets whether to open as a sub-page.
    /// </summary>
    public bool subPage => GetPropBool(nameof(subPageAttr), true);

    /// <summary>
    /// Releases resources and unsubscribes from callbacks.
    /// </summary>
    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackId);
    }

    /// <summary>
    /// Builds the open button based on the specified type.
    /// </summary>
    /// <returns>A list containing a single inline keyboard button, or an empty list if hidden.</returns>
    public override async Task<List<InlineKeyboardButton>> BuildAsync() {
        if (hide) {
            return [];
        }

        var displayTitle = GetProp(nameof(title));
        var targetValue = GetProp(nameof(target));
        if (string.IsNullOrEmpty(displayTitle)) {
            displayTitle = targetValue;
        }

        switch (type) {
            case OpenType.Link:
            return [InlineKeyboardButton.WithUrl(displayTitle, targetValue)];

            case OpenType.WebApp:
            return [InlineKeyboardButton.WithWebApp(
                    text: displayTitle,
                    webApp: targetValue
                )];

            case OpenType.Page:
            default:
            botUser.callbackFactory.Unsubscribe(callbackId);
            callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (qid, mid, cid) => {
                if (targetPage is not null) {
                    await targetPage.UpdatePageAsync(mid, cid);
                } else if (parent is ScriptPage scriptPage) {
                    await scriptPage.NavigateToAsync(targetValue, subPage);
                }
            });
            return [InlineKeyboardButton.WithCallbackData(displayTitle, callbackId)];
        }
    }
}