using Telegram.Bot.UI.MenuBuilder.Elements.Selectors;
using Telegram.Bot.UI.Utils;

namespace Telegram.Bot.UI.MenuBuilder.Elements.Modal;


public class MenuCheckboxModalPage : MessagePage {
    private MenuCheckboxGroup buttons;
    public int columns { get => buttons.columns; set => buttons.columns = value; }
    public List<int> selected => buttons.selected;
    public IEnumerable<string> selectedId => buttons.selectedId;
    public List<MenuSelector> selectButton => buttons.selectButton;
    public string temp { get => buttons.temp; set => buttons.temp = value; }

    public IEnumerable<MenuModalDetails>? details { get; private set; }
    public MenuModalDetails? lastSelectDetails { get; private set; }

    public override string title => string.Join(", ", buttons.selectButton.Select(x => x.title));
    public override string? pageResource => lastSelectDetails?.pageResource ?? parrent?.pageResource;
    public List<MenuModalDetails> selectDetails => details?.Where(x => buttons.selectButton.Select(x => x.id).Contains(x.id)).ToList() ?? new();

    public event MenuCheckboxGroup.UpdateHandler? onUpdate;



    public MenuCheckboxModalPage(IEnumerable<MenuSelector> buttons, IEnumerable<MenuModalDetails>? details, BaseBotUser botUser) : base(botUser) {
        this.buttons = MenuCheckboxGroup(buttons);
        this.buttons.onUpdate += async (selectButton, isSelect) => {
            lastSelectDetails = details?.Where(x => x.id == selectButton.id).FirstOrNull();
            if (onUpdate is not null) {
                await onUpdate.Invoke(selectButton, isSelect);
            }
        };

        this.details = details;
        backToParent = true;
    }



    public Task SelectAsync(string id) => buttons.SelectAsync(id);



    protected override void OnDispose() {
        buttons.Dispose();
    }



    public override string? RequestMessageResource() => lastSelectDetails?.messageResource ?? parrent?.RequestMessageResource();
    public override Task<(string resource, WallpaperLoader loader)?> RequestWallpaperAsync() => Task.FromResult(lastSelectDetails?.wallpaper);
    public override async Task<object?> RequestModelAsync() {
        if (lastSelectDetails?.model is not null) {
            return lastSelectDetails?.model;
        }
        if (parrent is not null) {
            return await parrent.RequestModelAsync();
        }
        return null;
    }





    public override Task<List<ButtonsPage>?> RequestPageComponentsAsync() {
        webPreview = lastSelectDetails?.webPreview ?? parrent?.webPreview ?? true;

        return ButtonsPage.PageTask([
            [buttons]
        ]);
    }
}
