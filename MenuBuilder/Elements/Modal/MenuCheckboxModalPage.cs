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
    private List<MenuModalDetails> selectDetails => details?.Where(x => buttons.selectButton.Select(x => x.id).Contains(x.id)).ToList() ?? new();

    public event MenuCheckboxGroup.UpdateHandler? onUpdate;



    public MenuCheckboxModalPage(IEnumerable<MenuSelector> buttons, IEnumerable<MenuModalDetails>? details, BaseBotUser botUser) : base(botUser) {
        this.buttons = MenuCheckboxGroup(buttons);
        this.buttons.onUpdate += (selectButton, isSelect) => {
            lastSelectDetails = details?.Where(x => x.id == selectButton.id).FirstOrNull();
            onUpdate?.Invoke(selectButton, isSelect);
        };

        this.details = details;
        backToParent = true;
    }



    public void Select(string id) => buttons.Select(id);



    protected override void OnDispose() {
        buttons.Dispose();
    }



    public override string? RequestMessageResource() => lastSelectDetails?.messageResource ?? parrent?.RequestMessageResource();
    public override (string resource, WallpaperLoader loader)? RequestWallpaper() => lastSelectDetails?.wallpaper;
    public override object? RequestModel() => lastSelectDetails?.model ?? parrent?.RequestModel();





    public override List<ButtonsPage> RequestPageComponents() {
        webPreview = lastSelectDetails?.webPreview ?? parrent?.webPreview ?? true;

        return ButtonsPage.Page([
            [buttons]
        ]);
    }
}
