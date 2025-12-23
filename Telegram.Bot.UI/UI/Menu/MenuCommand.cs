using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.Components;
using Telegram.Bot.UI.Components.Attributes;

namespace Telegram.Bot.UI.Menu;


/// <summary>
/// Button that executes a callback when clicked.
/// Use for actions like save, delete, submit, etc.
/// </summary>
/// <example>
/// XML: &lt;command title="Save" @click="save()" /&gt;
/// </example>
[Component("command")]
public class MenuCommand : AutoComponent {
    /// <summary>
    /// Button title text.
    /// </summary>
    [Prop("title")]
    public string title { get; set; } = "";

    /// <summary>
    /// JavaScript expression to execute on click (from @click attribute).
    /// </summary>
    [Event("click")]
    public string? onClickHandler { get; set; }

    private string? callbackId = null;

    /// <summary>
    /// C# callback for programmatic click handling.
    /// Can be set from JavaScript: component('id').onClick = function() { ... }
    /// </summary>
    public Action? onClick { get; set; }


    /// <summary>
    /// Unsubscribes from callback factory on disposal.
    /// </summary>
    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackId);
    }


    /// <summary>
    /// Builds the command button.
    /// </summary>
    public override async Task<List<InlineKeyboardButton>> BuildAsync() {
        if (hide) {
            return [];
        }

        botUser.callbackFactory.Unsubscribe(callbackId);

        callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (callbackQueryId, messageId, chatId) => {
            if (scriptContext is not null) {
                scriptContext.navigated = false;
                // Set callbackQueryId for toast/alert functions before invoking handlers
                scriptContext.SetValue("callbackQueryId", callbackQueryId);
            }

            // Invoke C# event (for JavaScript API)
            onClick?.Invoke();

            // Invoke XML event handler
            await InvokeEvent(nameof(onClickHandler), new { callbackQueryId, messageId, chatId });

            // Skip update if navigation happened (page already changed)
            if (scriptContext?.navigated != true) {
                await parent.UpdatePageAsync(messageId, chatId);
            }
        });

        return [
            InlineKeyboardButton.WithCallbackData(
                text: GetProp(nameof(title)),
                callbackData: callbackId
            )
        ];
    }
}
