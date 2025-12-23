using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.Components;
using Telegram.Bot.UI.Components.Attributes;
using Telegram.Bot.UI.Runtime;

namespace Telegram.Bot.UI.Menu;

/// <summary>
/// Represents a checkbox menu component that can be toggled between checked and unchecked states.
/// </summary>
[Component("checkbox")]
public class MenuCheckbox : AutoComponent {
    /// <summary>
    /// Gets or sets the checkbox title text.
    /// </summary>
    [Prop("title")]
    public string title { get; set; } = "";

    /// <summary>
    /// Gets or sets the display template. Default template adds checkmark when selected.
    /// </summary>
    [Prop("template")]
    public string template { get; set; } = "{{ (self.isSelected ? '✅ ' : '') + self.title }}";

    /// <summary>
    /// Gets or sets the binding for the selected state.
    /// </summary>
    [Bind("selected")]
    public string? selectedBinding { get; set; }

    /// <summary>
    /// Gets or sets the event handler invoked when checkbox state changes.
    /// </summary>
    [Event("update")]
    public string? onUpdateHandler { get; set; }

    /// <summary>
    /// Gets whether the checkbox is currently selected.
    /// </summary>
    public bool isSelected { get; private set; }

    private string? callbackId = null;
    private readonly object selectedLock = new();

    /// <summary>
    /// Gets or sets the update callback for JavaScript API. Using Action instead of event so Jint can assign to it.
    /// </summary>
    public Action<object>? onUpdate { get; set; }

    /// <summary>
    /// Asynchronously sets the selected state.
    /// </summary>
    /// <param name="value">The value to set.</param>
    public async Task SelectAsync(bool value) {
        lock (selectedLock) {
            isSelected = value;
        }
    }

    /// <summary>
    /// Toggles the checkbox state between selected and unselected.
    /// </summary>
    public void toggle() {
        lock (selectedLock) {
            isSelected = !isSelected;
        }
    }

    /// <summary>
    /// Sets the checkbox selected state.
    /// </summary>
    /// <param name="value">The value to set.</param>
    public void select(bool value) {
        lock (selectedLock) {
            isSelected = value;
        }
    }

    /// <summary>
    /// Unsubscribes from callback when component is disposed.
    /// </summary>
    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackId);
    }

    /// <summary>
    /// Builds the inline keyboard button for the checkbox component.
    /// </summary>
    /// <returns>A list containing the checkbox button.</returns>
    public override async Task<List<InlineKeyboardButton>> BuildAsync() {
        if (hide) {
            return [];
        }

        var bindingValue = GetRawProp(nameof(selectedBinding));
        if (!string.IsNullOrEmpty(bindingValue)) {
            lock (selectedLock) {
                isSelected = GetPropBool(nameof(selectedBinding), false);
            }
        }

        botUser.callbackFactory.Unsubscribe(callbackId);

        callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (callbackQueryId, messageId, chatId) => {
            bool newState;
            lock (selectedLock) {
                isSelected = !isSelected;
                newState = isSelected;
            }

            onUpdate?.Invoke(new { selected = newState });

            await InvokeEvent(nameof(onUpdateHandler), new { selected = newState });
            await parent.UpdatePageAsync(messageId, chatId);
        });

        var titleRaw = GetRawProp(nameof(title));
        var templateValue = GetRawProp(nameof(template));

        var ctx = new ComponentContext(scriptContext!);

        // Capture isSelected inside lock for thread safety
        bool currentSelected;
        lock (selectedLock) {
            currentSelected = isSelected;
        }

        string displayTitle;
        if (titleRaw.Contains("self.")) {
            ctx.SetSelf(new { isSelected = currentSelected, title = "" });
            displayTitle = await ctx.RenderAsync(titleRaw);
        } else {
            var actualTitle = titleRaw;
            if (titleRaw.Contains("{{")) {
                actualTitle = await ctx.RenderAsync(titleRaw);
            }
            ctx.SetSelf(new { isSelected = currentSelected, title = actualTitle });
            displayTitle = await ctx.RenderAsync(templateValue);
        }

        return [
            InlineKeyboardButton.WithCallbackData(
                text: displayTitle,
                callbackData: callbackId
            )
        ];
    }
}