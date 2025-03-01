using Localization;
using SafeStop;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.Loader;
using Telegram.Bot.UI.Loader.DataTypes;
using Telegram.Bot.UI.MenuBuilder.Elements;
using Telegram.Bot.UI.MenuBuilder.Elements.Modal;
using Telegram.Bot.UI.MenuBuilder.Elements.Selectors;

namespace Telegram.Bot.UI.MenuBuilder;


public abstract class MessagePage : IDisposable {
    public virtual string? pageResource { get; } = null;
    public virtual string? title { get; } = null;
    public virtual string? backTitle { get; } = null;
    public virtual Task<string?> text => BuildTextTemplate();
    public int selectedPage { get; set; } = 0;
    public MessagePage? parrent { get; set; }
    public bool backToParent { get; set; } = true;
    public BaseBotUser botUser { get; private set; }
    public bool webPreview { get; set; } = true;
    public Message? lastMessage { get; private set; } = null;
    private bool disposed { get; set; } = false;

    public delegate Task<string?> WallpaperLoader(byte[] wallpaperByte, ResourceInfo info);



    public MessagePage(BaseBotUser botUser) {
        this.botUser = botUser;
    }


    ~MessagePage() {
        Dispose();
    }


    public void Dispose() {
        if (!disposed) {
            OnDispose();
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }


    protected virtual void OnDispose() { }





    public string L(string text) => botUser.L(text);
    public LocalizedString LS(string text) => botUser.LS(text);
    public string? LN(string? text) => botUser.LN(text);





    public virtual List<ButtonsPage>? RequestPageComponents() => null;
    public virtual string? RequestMessageResource() => null;
    public virtual object? RequestModel() => null;
    public virtual (string resource, WallpaperLoader loader)? RequestWallpaper() => null;





    private async Task<string?> GetWallpaperUrl() {
        if (pageResource is null) {
            return null;
        }

        if (RequestWallpaper() is not (string wallpaperResource, WallpaperLoader loader)) {
            return null;
        }

        if (botUser.worker.pageResourceLoader.LoadPage(pageResource) is not PageInfo page) {
            return null;
        }

        var imageInfo = page.image?.Open()
            .SelectFromName(wallpaperResource)?
            .info;

        if (imageInfo is null) {
            return null;
        }

        if (await loader(imageInfo.data, imageInfo) is not string url) {
            return null;
        }

        return url;
    }





    public List<object> InheritedRequestModel() {
        var model = new List<object>();
        var currentPage = this;

        while (currentPage is not null) {
            if (currentPage.RequestModel() is object currentModel) {
                model.Add(currentModel);
            }

            currentPage = currentPage.parrent;
        }

        model.Reverse();
        return model;
    }





    protected virtual async Task<string?> BuildTextTemplate() {

        // wallpaper
        string wallpaper = string.Empty;
        if (await GetWallpaperUrl() is string wallpaperUrl) {
            if (botUser.parseMode == ParseMode.Html) {
                wallpaper = $"<a href=\"{wallpaperUrl}\">&#8205;</a>";
            }
            if (botUser.parseMode == ParseMode.MarkdownV2) {
                wallpaper = $"[ ]({wallpaperUrl})";
            }
        }

        // text
        var text = RenderTemplateText(RequestMessageResource(), InheritedRequestModel());

        var template = wallpaper + (text ?? string.Empty);

        return string.IsNullOrEmpty(template) ? null : template;
    }





    public string? RenderTemplateText(string? textResource, IEnumerable<object> models) {
        if (textResource is null || pageResource is null) {
            return null;
        }

        if (botUser.worker.pageResourceLoader.LoadPage(pageResource) is not PageInfo page) {
            return null;
        }

        var text = page.text?
            .Open()
            .SelectFromName(textResource)?
            .GetText();

        if (text is null) {
            return null;
        }

        return TemplateEngine.Render(text, models, botUser.localization);
    }





    public async Task OpenSubPageAsync(MessagePage page) {
        page.parrent = this;
        page.backToParent = true;
        lastMessage = await botUser.EditMessageTextAsync(lastMessage!.MessageId, lastMessage!.Chat.Id, await page.text, page.BuildSelectedPage(), webPreview: page.webPreview);
        page.lastMessage = lastMessage;
    }





    public async Task OpenPageAsync(MessagePage page) {
        page.parrent = this;
        page.backToParent = false;
        lastMessage = await botUser.EditMessageTextAsync(lastMessage!.MessageId, lastMessage!.Chat.Id, await page.text, page.BuildSelectedPage(), webPreview: page.webPreview);
        page.lastMessage = lastMessage;
    }





    public async Task SendPageAsync() {
        lastMessage = await botUser.SendTextMessageAsync(await text, BuildSelectedPage(), webPreview: webPreview);
    }





    public async Task UpdatePageAsync(int messageId, long chatId) {
        lastMessage = await botUser.EditMessageTextAsync(messageId, chatId, await text, BuildSelectedPage(), webPreview: webPreview);
    }





    public async Task DeletePageAsync() {
        if (lastMessage?.MessageId is int messageId) {
            await botUser.DeleteMessageAsync(messageId);
            lastMessage = null;
        }
    }





    private InlineKeyboardMarkup? BuildSelectedPage() {
        var buttonsPages = RequestPageComponents();
        if (buttonsPages is null && (!backToParent || parrent is null)) {
            return null;
        }

        if (buttonsPages is not null && (selectedPage > buttonsPages.Count() || selectedPage < 0)) {
            throw new Exception($"{nameof(selectedPage)} out of range. buttonsPages.Count(): {buttonsPages.Count()}; selectedPage: {selectedPage}");
        }

        var buttonsPage = buttonsPages is not null ? buttonsPages[selectedPage] : new() {
            page = new MenuElement[][] { }
        };

        if (backToParent && parrent is not null) {
            buttonsPage.page = buttonsPage.page.Concat(new MenuElement[][] {
                [ MenuOpenPege(parrent, parrent.backTitle ?? $"<< {parrent.title ?? string.Empty}") ]
            });
        }

        return new InlineKeyboardMarkup(BuildButtonsPage(buttonsPage));
    }





    private List<List<InlineKeyboardButton>> BuildButtonsPage(ButtonsPage buttonsPage) {
        List<List<InlineKeyboardButton>> menu = new();

        foreach (var row in buttonsPage.page) {
            List<InlineKeyboardButton> rowInline = new();

            foreach (var button in row) {
                if (button is MenuSplit) {
                    menu.Add(rowInline);
                    rowInline = new();
                } else {
                    var buildList = button.Build();

                    int count = 0;
                    foreach (var build in buildList) {
                        rowInline.Add(build);

                        if (++count >= button.columns) {
                            menu.Add(rowInline);
                            rowInline = new();
                            count = 0;
                        }
                    }
                }
            }

            menu.Add(rowInline);
        }

        return menu;
    }





    public Task<DisposeAction> CriticalAsync() => botUser.worker.CriticalAsync();





    protected MenuCheckbox MenuCheckbox(string title) =>
        new() { title = title, botUser = botUser, parrent = this };

    protected MenuCheckboxGroup MenuCheckboxGroup(IEnumerable<MenuSelector> buttons) =>
        new() { buttons = buttons, botUser = botUser, parrent = this };

    protected MenuCheckboxModal MenuCheckboxModal(IEnumerable<MenuSelector> buttons, string? title = null, IEnumerable<MenuModalDetails>? details = null) =>
        new(buttons, details, botUser) { buttons = buttons, title = title, botUser = botUser, parrent = this };

    protected MenuRadio MenuRadio(IEnumerable<MenuSelector> buttons) =>
        new() { buttons = buttons.ToList(), botUser = botUser, parrent = this };

    protected MenuRadioModal MenuRadioModal(IEnumerable<MenuSelector> buttons, string? title = null, IEnumerable<MenuModalDetails>? details = null) =>
        new(buttons, details, botUser) { buttons = buttons, title = title, botUser = botUser, parrent = this };

    protected MenuSwitch MenuSwitch(IEnumerable<MenuSelector> buttons) =>
        new() { buttons = buttons.ToList(), botUser = botUser, parrent = this };

    protected MenuCommand MenuCommand(string title) =>
        new() { title = title, botUser = botUser, parrent = this };

    protected MenuOpenPege MenuOpenPege(MessagePage page, string? title = null) =>
        new() { page = page, title = title, botUser = botUser, parrent = this, changeParrent = false };

    protected MenuOpenPege MenuOpenSubPege(MessagePage page, string? title = null) =>
        new() { page = page, title = title, botUser = botUser, parrent = this, changeParrent = true };

    protected MenuLink MenuLink(string url, string title) =>
        new() { url = url, botUser = botUser, title = title, parrent = this };

    protected MenuSplit MenuSplit() =>
        new() { botUser = botUser, parrent = this };

    protected MenuNavigatePanel MenuNavigatePanel() =>
        new() { botUser = botUser, parrent = this };
}
