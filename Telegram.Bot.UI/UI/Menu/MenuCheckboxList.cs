using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.Components;
using Telegram.Bot.UI.Components.Attributes;
using Telegram.Bot.UI.Menu.Selectors;
using Telegram.Bot.UI.Runtime;

namespace Telegram.Bot.UI.Menu;


/// <summary>
/// A multi-select checkbox list component.
/// Displays multiple buttons, each can be independently toggled on/off.
/// Similar to MenuRadio but allows multiple selections.
/// </summary>
[Component("checkbox-list")]
public class MenuCheckboxList : AutoComponent {
    public List<MenuSelector> buttons { get; set; } = [];
    private HashSet<string> checkedIds = new();
    public List<string> callbackIdList = new();

    public int checkedCount => checkedIds.Count;
    public IReadOnlySet<string> selectedIds => checkedIds;
    public IEnumerable<MenuSelector> checkedButtons => buttons.Where(b => checkedIds.Contains(b.id));

    // Event handler for JavaScript API
    public Func<MenuSelector, bool, Task>? onUpdate { get; set; }

    private readonly object checkLock = new();

    [Prop("template")]
    public string template { get; set; } = "{{ (self.isChecked ? '✅ ' : '') + self.title }}";

    [Bind("selected")]
    public string? selectedBinding { get; set; }

    [Event("update")]
    public string? onUpdateHandler { get; set; }


    internal override void ApplyDefinition(AngleSharp.Dom.IElement element) {
        base.ApplyDefinition(element);

        // Parse options
        var options = ParseOptions();
        buttons = options.Select(opt => new MenuSelector {
            id = opt.id,
            title = opt.lang ? botUser.L(render(opt.title)) : render(opt.title)
        }).ToList();
    }


    public async Task InitializeAsync() {
        // Apply initial selection from binding (comma-separated list)
        var selectedValue = GetProp("selectedBinding", "");
        if (!string.IsNullOrEmpty(selectedValue)) {
            var ids = selectedValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(s => s.Trim())
                                   .ToList();
            await SetCheckedAsync(ids);
        }
    }

    // Helper to invoke both JS property handler and XML event handler
    private async Task InvokeUpdateHandlersAsync(MenuSelector selector, bool isChecked) {
        // Invoke JavaScript property handler
        if (onUpdate is not null) {
            await onUpdate.Invoke(selector, isChecked);
        }

        // Invoke XML event handler
        if (HasEvent("onUpdateHandler")) {
            await InvokeEvent("onUpdateHandler", new {
                item = new { id = selector.id, title = selector.title },
                isChecked = isChecked,
                selectedIds = checkedIds.ToArray(),
                selectedCount = checkedIds.Count
            });
        }
    }


    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackIdList);
    }


    public async Task SetButtonsAsync(List<MenuSelector> buttons) {
        this.buttons = buttons;
        await BuildAsync();
    }


    public async Task SetCheckedAsync(string id, bool isChecked) {
        bool changed = false;
        lock (checkLock) {
            if (isChecked) {
                changed = checkedIds.Add(id);
            } else {
                changed = checkedIds.Remove(id);
            }
        }

        if (changed) {
            var button = buttons.FirstOrDefault(b => b.id == id);
            if (button is not null) {
                await InvokeUpdateHandlersAsync(button, isChecked);
            }
        }
    }

    public async Task SetCheckedAsync(IEnumerable<string> ids) {
        lock (checkLock) {
            checkedIds.Clear();
            foreach (var id in ids) {
                checkedIds.Add(id);
            }
        }
    }

    public bool IsChecked(string id) => checkedIds.Contains(id);

    /// <summary>
    /// Toggle a checkbox by ID. Returns the new checked state.
    /// </summary>
    public async Task<bool> ToggleAsync(string id) {
        bool newState;
        lock (checkLock) {
            if (checkedIds.Contains(id)) {
                checkedIds.Remove(id);
                newState = false;
            } else {
                checkedIds.Add(id);
                newState = true;
            }
        }

        var button = buttons.FirstOrDefault(b => b.id == id);
        if (button is not null) {
            await InvokeUpdateHandlersAsync(button, newState);
        }

        return newState;
    }

    /// <summary>
    /// Synchronous toggle for JavaScript API
    /// </summary>
    public bool toggle(string id) => ToggleAsync(id).GetAwaiter().GetResult();

    /// <summary>
    /// Synchronous setChecked for JavaScript API
    /// </summary>
    public void setChecked(string id, bool isChecked) => SetCheckedAsync(id, isChecked).GetAwaiter().GetResult();


    public override async Task<List<InlineKeyboardButton>> BuildAsync() {
        if (hide) {
            return new();
        }

        // Re-read binding on each render (for reactivity)
        var selectedValue = GetProp("selectedBinding", "");
        if (!string.IsNullOrEmpty(selectedValue)) {
            var ids = selectedValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(s => s.Trim())
                                   .ToHashSet();
            lock (checkLock) {
                checkedIds = ids;
            }
        }

        botUser.callbackFactory.Unsubscribe(callbackIdList);
        callbackIdList.Clear();

        var list = new List<InlineKeyboardButton>();

        foreach (var (button, index) in MenuSelector.WithIndex(buttons)) {
            var buttonId = button.id; // Capture for closure
            var callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (callbackQueryId, messageId, chatId) => {
                bool newState;
                lock (checkLock) {
                    if (checkedIds.Contains(buttonId)) {
                        checkedIds.Remove(buttonId);
                        newState = false;
                    } else {
                        checkedIds.Add(buttonId);
                        newState = true;
                    }
                }

                // Set callbackQueryId for toast/alert functions before invoking handlers
                scriptContext?.SetValue("callbackQueryId", callbackQueryId);

                // Invoke handlers
                await InvokeUpdateHandlersAsync(button, newState);

                await parent.UpdatePageAsync(messageId, chatId);
            }, $"CheckboxList[{button.id}]");

            callbackIdList.Add(callbackId);

            // Render template with isolated component context
            var templateValue = GetRawProp(nameof(template));
            var ctx = new ComponentContext(scriptContext!);
            ctx.SetSelf(new {
                isChecked = checkedIds.Contains(button.id),
                title = button.title,
                index = index
            });
            var text = await ctx.RenderAsync(templateValue);

            list.Add(
                InlineKeyboardButton.WithCallbackData(
                    text: text,
                    callbackData: callbackId
                )
            );
        }

        return list;
    }
}
