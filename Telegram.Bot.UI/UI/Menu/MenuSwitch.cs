using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.Components;
using Telegram.Bot.UI.Components.Attributes;
using Telegram.Bot.UI.Menu.Selectors;
using Telegram.Bot.UI.Runtime;

namespace Telegram.Bot.UI.Menu;


/// <summary>
/// A carousel component that cycles through options on click.
/// Shows a single button where each click advances to the next option.
/// For simple ON/OFF toggles, use MenuCheckbox instead.
/// </summary>
[Component("switch")]
public class MenuSwitch : AutoComponent {
    /// <summary>
    /// Gets or sets the list of switch options.
    /// </summary>
    public List<MenuSelector> buttons { get; set; } = [];

    /// <summary>
    /// Gets or sets the index of the currently displayed option.
    /// </summary>
    public int currentIndex { get; set; } = 0;

    /// <summary>
    /// Gets the ID of the current option.
    /// </summary>
    public string? currentId => currentOption?.id;

    /// <summary>
    /// Gets the title of the current option.
    /// </summary>
    public string? currentTitle => currentOption?.title;

    /// <summary>
    /// Gets the currently displayed option object.
    /// </summary>
    public MenuSelector? currentOption => currentIndex >= 0 && currentIndex < buttons.Count ? buttons[currentIndex] : null;

    private string? callbackId = null;

    /// <summary>
    /// Gets or sets the update callback for JavaScript API. Using Func instead of event so Jint can assign to it.
    /// </summary>
    public Func<MenuSelector, Task>? onUpdate { get; set; }

    private readonly object indexLock = new();

    /// <summary>
    /// Gets or sets the binding for the selected value.
    /// </summary>
    [Bind("value")]
    public string? valueBinding { get; set; }

    /// <summary>
    /// Gets or sets the event handler invoked when the option changes.
    /// </summary>
    [Event("update")]
    public string? onUpdateHandler { get; set; }

    /// <summary>
    /// Gets or sets the display title template.
    /// </summary>
    [Prop("title")]
    public string title { get; set; } = "{{ self.title }}";

    /// <summary>
    /// Applies component definition by parsing option elements from XML.
    /// </summary>
    /// <param name="element">The XML element to parse.</param>
    internal override void ApplyDefinition(AngleSharp.Dom.IElement element) {
        base.ApplyDefinition(element);

        var options = ParseOptions();
        if (options.Count > 0) {
            buttons = options.Select(opt => new MenuSelector {
                id = opt.id,
                title = opt.lang ? botUser.L(render(opt.title)) : render(opt.title)
            }).ToList();
        } else {
            throw new InvalidOperationException("Switch component requires at least one <option> element. For simple ON/OFF toggles, use <checkbox> instead.");
        }
    }

    /// <summary>
    /// Renders the title with current option context.
    /// </summary>
    /// <returns>The rendered title string.</returns>
    private async Task<string> RenderTitleAsync() {
        var titleValue = GetRawProp(nameof(title));
        var ctx = new ComponentContext(scriptContext!);
        var current = currentOption;
        ctx.SetSelf(new {
            id = current?.id,
            title = current?.title ?? "",
            index = currentIndex,
            count = buttons.Count
        });
        return await ctx.RenderAsync(titleValue);
    }

    /// <summary>
    /// Initializes the component by applying initial value from binding.
    /// </summary>
    public async Task InitializeAsync() {
        var value = GetProp("valueBinding", "");
        if (!string.IsNullOrEmpty(value)) {
            await CycleToAsync(value);
        }
    }

    /// <summary>
    /// Invokes both JavaScript property handler and XML event handler for update.
    /// </summary>
    /// <param name="option">The selected option.</param>
    private async Task InvokeUpdateHandlersAsync(MenuSelector option) {
        if (onUpdate is not null) {
            await onUpdate.Invoke(option);
        }

        if (HasEvent("onUpdateHandler")) {
            await InvokeEvent("onUpdateHandler", new {
                id = option.id,
                title = option.title,
                index = currentIndex,
                count = buttons.Count
            });
        }
    }

    /// <summary>
    /// Unsubscribes from callback when component is disposed.
    /// </summary>
    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackId);
    }


    /// <summary>
    /// Cycles to a specific option by ID.
    /// </summary>
    public async Task CycleToAsync(string id) {
        var match = buttons.Select((button, index) => (button, index)).Where(x => x.button.id == id);

        bool shouldInvoke = false;
        lock (indexLock) {
            if (match.Any() && match.First().index != currentIndex) {
                currentIndex = match.First().index;
                shouldInvoke = true;
            }
        }

        if (shouldInvoke && currentOption is not null) {
            await InvokeUpdateHandlersAsync(currentOption);
        }
    }

    /// <summary>
    /// Cycles to the next option in the list (wraps around).
    /// </summary>
    public async Task CycleNextAsync() {
        bool shouldInvoke = false;
        lock (indexLock) {
            var newIndex = (currentIndex + 1) % buttons.Count;
            if (newIndex != currentIndex) {
                currentIndex = newIndex;
                shouldInvoke = true;
            }
        }

        if (shouldInvoke && currentOption is not null) {
            await InvokeUpdateHandlersAsync(currentOption);
        }
    }

    /// <summary>
    /// Builds the inline keyboard button for the switch component.
    /// </summary>
    /// <returns>A list containing the switch button.</returns>
    public override async Task<List<InlineKeyboardButton>> BuildAsync() {
        if (hide) {
            return new();
        }

        var value = GetProp("valueBinding", "");
        if (!string.IsNullOrEmpty(value)) {
            var idx = buttons.FindIndex(b => b.id == value);
            if (idx >= 0) {
                currentIndex = idx;
            }
        }

        if (currentIndex >= buttons.Count || currentIndex < 0) {
            throw new Exception($"{nameof(currentIndex)} out of range. buttons.Count: {buttons.Count}; currentIndex: {currentIndex}");
        }

        botUser.callbackFactory.Unsubscribe(callbackId);

        callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (callbackQueryId, messageId, chatId) => {
            lock (indexLock) {
                currentIndex = (currentIndex + 1) % buttons.Count;
            }

            scriptContext?.SetValue("callbackQueryId", callbackQueryId);

            if (currentOption is not null) {
                await InvokeUpdateHandlersAsync(currentOption);
            }
            await parent.UpdatePageAsync(messageId, chatId);
        }, "Switch");

        var displayTitle = await RenderTitleAsync();
        if (string.IsNullOrEmpty(displayTitle)) {
            return new();
        }

        return new() {
            InlineKeyboardButton.WithCallbackData(
                text: displayTitle,
                callbackData: callbackId
            )
        };
    }

    /// <summary>
    /// Synchronous method to cycle to a specific option for JavaScript API.
    /// </summary>
    /// <param name="id">The ID of the option to cycle to.</param>
    public void cycleTo(string id) => CycleToAsync(id).GetAwaiter().GetResult();

    /// <summary>
    /// Synchronous method to cycle to next option for JavaScript API.
    /// </summary>
    public void cycleNext() => CycleNextAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Legacy compatibility method that maps to CycleToAsync.
    /// </summary>
    /// <param name="id">The ID of the option to select.</param>
    public Task SelectAsync(string id) => CycleToAsync(id);

    /// <summary>
    /// Legacy compatibility method for JavaScript API.
    /// </summary>
    /// <param name="id">The ID of the option to select.</param>
    public void select(string id) => CycleToAsync(id).GetAwaiter().GetResult();

    /// <summary>
    /// Legacy compatibility property that maps to currentIndex.
    /// </summary>
    public int selected {
        get => currentIndex;
        set => currentIndex = value;
    }

    /// <summary>
    /// Legacy compatibility property that maps to currentOption.
    /// </summary>
    public MenuSelector? selectButton => currentOption;

    /// <summary>
    /// Legacy compatibility property that maps to currentId.
    /// </summary>
    public string? selectedId => currentId;

    /// <summary>
    /// Legacy compatibility property that maps to currentTitle.
    /// </summary>
    public string? selectedTitle => currentTitle;
}