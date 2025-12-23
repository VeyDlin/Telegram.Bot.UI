using Telegram.Bot.UI.Menu.Selectors;

namespace Telegram.Bot.UI.Menu.Modal;


/// <summary>
/// Modal page for checkbox selection (multiple selection).
/// </summary>
public class MenuCheckboxModalPage : MenuModalPageBase {
    private MenuCheckboxList? buttons;
    private HashSet<string> globalSelectedIds = new();  // Persist selections across page switches

    public int selectedCount => globalSelectedIds.Count;
    public IReadOnlySet<string> selectedIds => globalSelectedIds;
    public IEnumerable<MenuSelector> selectedButtons => allSelectors.Where(s => globalSelectedIds.Contains(s.id));

    // For message context, use the last toggled item
    private string? lastToggledId;
    public override string title => GetSelectedSummary();

    // Handler for update events - using property for JavaScript compatibility
    public Func<MenuSelector, bool, Task>? onUpdate { get; set; }


    public MenuCheckboxModalPage(
        IEnumerable<MenuSelector> buttons,
        IEnumerable<MenuModalDetails>? details,
        BaseBotUser botUser
    ) : base(buttons, details, botUser) {
    }


    protected override int GetColumnsValue() => buttons?.columns ?? columnsValue;

    protected override void SetColumnsValue(int value) {
        if (buttons != null) {
            buttons.columns = value;
        }
    }

    protected override MenuModalDetails? GetCurrentDetails() {
        // For checkbox modal, return details of last toggled item
        if (lastToggledId is null) {
            return null;
        }
        return details?.FirstOrDefault(x => x.id == lastToggledId);
    }

    protected override MenuElement? GetButtonComponent() => buttons;

    protected override void DisposeButtonComponent() => buttons?.Dispose();

    protected override MenuElement CreateButtonComponent(IEnumerable<MenuSelector> selectors) {
        buttons = CreateCheckboxList(selectors);
        buttons.onUpdate += HandleUpdate;
        // Restore checked state for items on this page
        foreach (var id in globalSelectedIds) {
            if (selectors.Any(s => s.id == id)) {
                _ = buttons.SetCheckedAsync(id, true);
            }
        }
        return buttons;
    }

    protected override void EnsureButtonComponentCreated() {
        if (buttons is not null) {
            return;
        }

        buttons = CreateCheckboxList(allSelectors);
        buttons.columns = columnsValue;
        buttons.onUpdate += HandleUpdate;

        // Restore checked state
        foreach (var id in globalSelectedIds) {
            _ = buttons.SetCheckedAsync(id, true);
        }
    }

    protected override void RestoreSelectionState(List<MenuSelector> currentSelectors) {
        if (buttons is null) {
            return;
        }

        foreach (var id in globalSelectedIds) {
            if (currentSelectors.Any(s => s.id == id)) {
                _ = buttons.SetCheckedAsync(id, true);
            }
        }
    }


    private async Task HandleUpdate(MenuSelector selector, bool isChecked) {
        lastToggledId = selector.id;

        if (isChecked) {
            globalSelectedIds.Add(selector.id);
        } else {
            globalSelectedIds.Remove(selector.id);
        }

        if (onUpdate != null) {
            await onUpdate.Invoke(selector, isChecked);
        }
    }


    public async Task SetCheckedAsync(string id, bool isChecked) {
        lastToggledId = id;

        if (isChecked) {
            globalSelectedIds.Add(id);
        } else {
            globalSelectedIds.Remove(id);
        }

        EnsureButtonComponentCreated();
        await buttons!.SetCheckedAsync(id, isChecked);
    }

    public async Task SetCheckedAsync(IEnumerable<string> ids) {
        globalSelectedIds.Clear();
        foreach (var id in ids) {
            globalSelectedIds.Add(id);
        }

        EnsureButtonComponentCreated();

        // Update all button states
        await buttons!.SetCheckedAsync(ids);
    }

    public bool IsChecked(string id) => globalSelectedIds.Contains(id);


    private string GetSelectedSummary() {
        if (globalSelectedIds.Count == 0) {
            return "None selected";
        }
        if (globalSelectedIds.Count == 1) {
            var selected = allSelectors.FirstOrDefault(s => s.id == globalSelectedIds.First());
            return selected?.title ?? "";
        }
        return $"{globalSelectedIds.Count} selected";
    }


    private MenuCheckboxList CreateCheckboxList(IEnumerable<MenuSelector> selectors) {
        var list = new MenuCheckboxList {
            buttons = selectors.ToList(),
            botUser = botUser,
            parent = this,
            scriptContext = scriptContext
        };
        return list;
    }
}
