using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.Components;
using Telegram.Bot.UI.Components.Attributes;
using Telegram.Bot.UI.Menu.Selectors;
using Telegram.Bot.UI.Runtime;

namespace Telegram.Bot.UI.Menu;

/// <summary>
/// Represents a radio button group menu component where only one option can be selected at a time.
/// </summary>
[Component("radio")]
public class MenuRadio : AutoComponent, IPaginatedComponent {
    /// <summary>
    /// Gets or sets the list of radio button options.
    /// </summary>
    public List<MenuSelector> buttons { get; set; } = [];

    /// <summary>
    /// Gets the index of the currently selected button. -1 means nothing is selected.
    /// </summary>
    public int selected { get; private set; } = -1;

    /// <summary>
    /// Gets or sets the maximum items per page for pagination.
    /// </summary>
    [Prop("max-items")]
    public string? maxItemsAttr { get; set; }

    /// <summary>
    /// Gets the maximum items per page. Null means no pagination.
    /// </summary>
    public int? maxItems => GetPropInt(nameof(maxItemsAttr), 0) > 0 ? GetPropInt(nameof(maxItemsAttr), 0) : null;

    /// <summary>
    /// Gets or sets the current page index for pagination.
    /// </summary>
    public int currentPage { get; set; } = 0;

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int pageCount => maxItems.HasValue && buttons.Count > 0
        ? (int)Math.Ceiling((double)buttons.Count / maxItems.Value)
        : 1;

    /// <summary>
    /// Navigates to a specific page.
    /// </summary>
    public void GoToPage(int page) {
        if (page >= 0 && page < pageCount) {
            currentPage = page;
        }
    }

    /// <summary>
    /// Gets buttons for the current page with their original indices.
    /// </summary>
    private List<(MenuSelector button, int index)> GetPageButtons() {
        if (!maxItems.HasValue) {
            return MenuSelector.WithIndex(buttons).ToList();
        }

        var start = currentPage * maxItems.Value;
        var count = Math.Min(maxItems.Value, buttons.Count - start);

        return buttons
            .Select((b, i) => (b, i))
            .Skip(start)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Gets the ID of the currently selected button.
    /// </summary>
    public string? selectedId => selectButton?.id;

    /// <summary>
    /// Gets the title of the currently selected button.
    /// </summary>
    public string? selectedTitle => selectButton?.title;

    /// <summary>
    /// Gets the currently selected button object.
    /// </summary>
    public MenuSelector? selectButton => selected >= 0 && selected < buttons.Count ? buttons[selected] : null;

    /// <summary>
    /// Gets or sets the list of callback IDs for each button.
    /// </summary>
    public List<string> callbackIdList = new();

    /// <summary>
    /// Gets or sets the callback invoked when a button is selected.
    /// </summary>
    public Func<MenuSelector, Task>? onSelect;

    /// <summary>
    /// Gets or sets the update callback for JavaScript API. Using Action instead of event so Jint can assign to it.
    /// </summary>
    public Action<object>? onUpdate { get; set; }

    private readonly object selectedLock = new();

    /// <summary>
    /// Gets or sets the display template. Default template adds checkmark to selected option.
    /// </summary>
    [Prop("template")]
    public string template { get; set; } = "{{ (self.isSelected ? '✅ ' : '') + self.title }}";

    /// <summary>
    /// Gets or sets the binding for the selected value.
    /// </summary>
    [Bind("selected")]
    public string? selectedBinding { get; set; }

    /// <summary>
    /// Gets or sets the event handler invoked when selection changes.
    /// </summary>
    [Event("select")]
    public string? onSelectHandler { get; set; }


    /// <summary>
    /// Applies component definition by parsing option elements from XML.
    /// </summary>
    /// <param name="element">The XML element to parse.</param>
    internal override void ApplyDefinition(AngleSharp.Dom.IElement element) {
        base.ApplyDefinition(element);

        var options = ParseOptions();
        buttons = options.Select(opt => new MenuSelector {
            id = opt.id,
            title = opt.lang ? botUser.L(render(opt.title)) : render(opt.title),
            variant = opt.variant,
            icon = opt.icon
        }).ToList();
    }

    /// <summary>
    /// Initializes the component by applying initial selection from binding.
    /// </summary>
    public async Task InitializeAsync() {
        var selectedValue = GetProp("selectedBinding", "");
        if (!string.IsNullOrEmpty(selectedValue)) {
            await SelectAsync(selectedValue);
        }
    }

    /// <summary>
    /// Invokes both JavaScript property handler and XML event handler for selection.
    /// </summary>
    /// <param name="selector">The selected button.</param>
    private async Task InvokeSelectHandlersAsync(MenuSelector selector) {
        onSelect?.Invoke(selector);

        if (HasEvent("onSelectHandler")) {
            await InvokeEvent("onSelectHandler", new { select = new { id = selector.id, title = selector.title } });
        }
    }

    /// <summary>
    /// Unsubscribes from all callbacks when component is disposed.
    /// </summary>
    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackIdList);
    }

    /// <summary>
    /// Updates the list of buttons and rebuilds the component.
    /// </summary>
    /// <param name="buttons">The new list of buttons.</param>
    public async Task SetButtonsAsync(List<MenuSelector> buttons) {
        this.buttons = buttons;
        await BuildAsync();
    }

    /// <summary>
    /// Asynchronously selects a button by ID.
    /// </summary>
    /// <param name="id">The ID of the button to select.</param>
    public async Task SelectAsync(string id) {
        var select = buttons.Select((button, index) => (button, index)).Where(x => x.button.id == id);

        bool shouldInvoke = false;
        lock (selectedLock) {
            if (select.Any() && select.First().index != selected) {
                selected = select.First().index;
                shouldInvoke = true;
            }
        }

        if (shouldInvoke && selectButton is not null) {
            await InvokeSelectHandlersAsync(selectButton);
        }
    }

    /// <summary>
    /// Synchronous select method for JavaScript API.
    /// </summary>
    /// <param name="id">The ID of the button to select.</param>
    public void select(string id) => SelectAsync(id).GetAwaiter().GetResult();

    /// <summary>
    /// Builds the inline keyboard buttons for all radio options.
    /// </summary>
    /// <returns>A list of inline keyboard buttons.</returns>
    public override async Task<List<InlineKeyboardButton>> BuildAsync() {
        if (hide) {
            return new();
        }

        var selectedValue = GetProp("selectedBinding", "");
        if (!string.IsNullOrEmpty(selectedValue)) {
            var idx = buttons.FindIndex(b => b.id == selectedValue);
            if (idx >= 0) {
                selected = idx;
            }
        }

        if (selected >= buttons.Count()) {
            throw new Exception($"{nameof(selected)} out of range. buttons.Count(): {buttons.Count()}; selected: {selected}");
        }

        botUser.callbackFactory.Unsubscribe(callbackIdList);
        callbackIdList.Clear();

        var list = new List<InlineKeyboardButton>();

        // Get buttons for current page
        var pageButtons = GetPageButtons();

        foreach (var (button, index) in pageButtons) {
            var callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (callbackQueryId, messageId, chatId) => {
                lock (selectedLock) {
                    selected = index;
                }

                scriptContext?.SetValue("callbackQueryId", callbackQueryId);

                if (selectButton is not null) {
                    await InvokeSelectHandlersAsync(selectButton);
                }

                if (onUpdate is not null && selectButton is not null) {
                    onUpdate.Invoke(new { selectedId = selectButton.id, selectedTitle = selectButton.title });
                }

                await parent.UpdatePageAsync(messageId, chatId);
            }, $"Radio[{button.id}]");

            callbackIdList.Add(callbackId);

            var templateValue = GetRawProp(nameof(template));
            var ctx = new ComponentContext(scriptContext!);
            ctx.SetSelf(new { isSelected = index == selected, title = button.title, index });
            var text = await ctx.RenderAsync(templateValue);

            var btn = InlineKeyboardButton.WithCallbackData(
                text: text,
                callbackData: callbackId
            );

            // Apply per-option variant/icon, falling back to component-level
            var componentVariantStr = GetProp(nameof(variant), "");
            KeyboardButtonStyle? componentVariant = null;
            if (!string.IsNullOrEmpty(componentVariantStr) && Enum.TryParse<KeyboardButtonStyle>(componentVariantStr, true, out var pv)) {
                componentVariant = pv;
            }
            var componentIcon = GetProp(nameof(icon), "");
            var effectiveVariant = button.variant ?? componentVariant;
            var effectiveIcon = !string.IsNullOrEmpty(button.icon) ? button.icon : componentIcon;
            if (effectiveVariant.HasValue) {
                btn.Style = effectiveVariant;
            }
            if (!string.IsNullOrEmpty(effectiveIcon)) {
                btn.IconCustomEmojiId = effectiveIcon;
            }

            list.Add(btn);
        }

        return list;
    }
}