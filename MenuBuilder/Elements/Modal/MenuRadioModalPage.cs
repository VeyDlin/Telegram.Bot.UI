using Telegram.Bot.UI.MenuBuilder.Elements.Selectors;
using Telegram.Bot.UI.Utils;

namespace Telegram.Bot.UI.MenuBuilder.Elements.Modal;


public class MenuRadioModalPage : MessagePage {
    private MenuRadio buttons;
    public int columns { get => buttons.columns; set => buttons.columns = value; }
    public int selected => buttons.selected;
    public string selectedId => buttons.selectedId;
    public MenuSelector selectButton => buttons.selectButton;
    public string temp { get => buttons.temp; set => buttons.temp = value; }

    public IEnumerable<MenuModalDetails>? details { get; private set; }

    public override string title => buttons.selectButton.title;
    public override string? pageResource => selectDetails?.pageResource ?? parrent?.pageResource;
    private MenuModalDetails? selectDetails => details?.Where(x => x.id == buttons.selectButton.id).FirstOrNull();

    public event MenuRadio.SelectHandler? onSelect {
        add => buttons.onSelect += value;
        remove => buttons.onSelect -= value;
    }



    public MenuRadioModalPage(IEnumerable<MenuSelector> buttons, IEnumerable<MenuModalDetails>? details, BaseBotUser botUser) : base(botUser) {
        this.buttons = MenuRadio(buttons);
        this.details = details;
        backToParent = true;
    }



    public Task SelectAsync(string id) => buttons.SelectAsync(id);



    protected override void OnDispose() {
        buttons.Dispose();
    }



    public override string? RequestMessageResource() => selectDetails?.messageResource ?? parrent?.RequestMessageResource();
    public override Task<(string resource, WallpaperLoader loader)?> RequestWallpaperAsync() => Task.FromResult(selectDetails?.wallpaper);
    public override async Task<object?> RequestModelAsync() {
        if (selectDetails?.model is not null) {
            return selectDetails?.model;
        }
        if (parrent is not null) {
            return await parrent.RequestModelAsync();
        }
        return null;
    }


    public override Task<List<ButtonsPage>?> RequestPageComponentsAsync() {
        webPreview = selectDetails?.webPreview ?? parrent?.webPreview ?? true;

        return ButtonsPage.PageTask([
            [buttons]
        ]);
    }
}
