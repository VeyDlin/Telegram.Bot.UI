using Telegram.Bot.UI.Menu.Selectors;

namespace Telegram.Bot.UI.Menu.Modal;


/// <summary>
/// Modal page for radio button selection (single selection).
/// </summary>
public class MenuRadioModalPage : MenuModalPageBase {
    private MenuRadio? buttons;
    private string? globalSelectedId;  // Persist selection across page switches

    public int selected => buttons?.selected ?? 0;
    public string? selectedId => globalSelectedId ?? buttons?.selectedId;
    public MenuSelector? selectButton => allSelectors.FirstOrDefault(s => s.id == selectedId);

    public override string title => buttons?.selectButton?.title ?? "null";

    // Handler for select events - using property for JavaScript compatibility
    public Func<MenuSelector, Task>? onSelect { get; set; }


    public MenuRadioModalPage(
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
        if (buttons?.selectButton is null) {
            return null;
        }
        return details?.FirstOrDefault(x => x.id == buttons.selectButton.id);
    }

    protected override MenuElement? GetButtonComponent() => buttons;

    protected override void DisposeButtonComponent() => buttons?.Dispose();

    protected override MenuElement CreateButtonComponent(IEnumerable<MenuSelector> selectors) {
        buttons = CreateRadio(selectors);
        buttons.onSelect += HandleSelection;
        return buttons;
    }

    protected override void EnsureButtonComponentCreated() {
        if (buttons is not null) {
            return;
        }

        buttons = CreateRadio(allSelectors);
        buttons.columns = columnsValue;
        buttons.onSelect += HandleSelection;
    }

    protected override void RestoreSelectionState(List<MenuSelector> currentSelectors) {
        if (globalSelectedId is not null && currentSelectors.Any(s => s.id == globalSelectedId)) {
            _ = buttons?.SelectAsync(globalSelectedId);
        }
    }


    private async Task HandleSelection(MenuSelector selectButton) {
        globalSelectedId = selectButton.id;
        if (onSelect != null) {
            await onSelect.Invoke(selectButton);
        }
    }


    public async Task SelectAsync(string id) {
        globalSelectedId = id;
        EnsureButtonComponentCreated();
        await buttons!.SelectAsync(id);
    }


    private MenuRadio CreateRadio(IEnumerable<MenuSelector> selectors) {
        var radio = MenuRadio(selectors);
        radio.scriptContext = scriptContext;
        return radio;
    }
}
