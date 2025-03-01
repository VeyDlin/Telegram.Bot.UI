# MessagePage

`MessagePage` - an abstract class representing a message page with an interactive menu for a Telegram bot.

## Class Interface

### Public Properties

| Interface | Description |
|-----------|-------------|
| `virtual string? pageResource { get; }` | Virtual property representing the resource for the page. Used to load texts and images. |
| `virtual string? title { get; }` | Page title. Can be displayed in the interface and is used in navigation buttons. |
| `virtual string? backTitle { get; }` | Text for the button to return to this page. If not specified, the template "<< {title}" will be used. |
| `virtual Task<string?> text { get; }` | Text content of the page. By default uses the `BuildTextTemplate()` method. |
| `int selectedPage { get; set; }` | Index of the currently selected components page. Used for navigation in multi-page menus. |
| `MessagePage? parrent { get; set; }` | Reference to the parent page. Used for navigation back. |
| `bool backToParent { get; set; }` | If `true`, a button to return to the parent page will be added. Default is `true`. |
| `BaseBotUser botUser { get; }` | Bot user associated with this page. |
| `bool webPreview { get; set; }` | Enable or disable link previews in the message text. Default is `true`. |
| `Message? lastMessage { get; }` | Last sent message associated with this page. |

### Public Methods

| Interface | Description |
|-----------|-------------|
| `MessagePage(BaseBotUser botUser)` | Constructor initializing the page with the specified bot user. |
| `void Dispose()` | Releases resources used by the page. |
| `string L(string text)` | Localizes the specified text for the user's current language. |
| `LocalizedString LS(string text)` | Returns a localized string for the specified key. |
| `string? LN(string? text)` | Localizes the specified text if it's not null. |
| `virtual List<ButtonsPage>? RequestPageComponents()` | Returns page components (buttons, controls, etc.). |
| `virtual string? RequestMessageResource()` | Returns the message resource key to display. |
| `virtual object? RequestModel()` | Returns the data model for the page template. |
| `virtual (string resource, WallpaperLoader loader)? RequestWallpaper()` | Returns the resource and handler for the background image. |
| `virtual void OnDispose()` | Called when releasing resources. |
| `List<object> InheritedRequestModel()` | Collects data models from the current and all parent pages. |
| `string? RenderTemplateText(string? textResource, IEnumerable<object> models)` | Renders the template text using the specified models. |
| `Task<string?> BuildTextTemplate()` | Forms the page text template, including the background image. |
| `Task OpenSubPageAsync(MessagePage page)` | Opens a subpage, saving the current page as the parent with the ability to return. |
| `Task OpenPageAsync(MessagePage page)` | Opens a new page, replacing the current one, without the ability to automatically return. |
| `Task SendPageAsync()` | Sends the page as a new message. |
| `Task UpdatePageAsync(int messageId, long chatId)` | Updates an existing page by the specified message ID. |
| `Task DeletePageAsync()` | Deletes the page message. |
| `Task<DisposeAction> CriticalAsync()` | Performs an operation in a critical section, preventing parallel execution. |

### Protected Methods

| Interface | Description |
|-----------|-------------|
| `MenuCheckbox MenuCheckbox(string title)` | Creates a checkbox element with the specified title. |
| `MenuCheckboxGroup MenuCheckboxGroup(IEnumerable<MenuSelector> buttons)` | Creates a group of checkboxes from the specified selector buttons. |
| `MenuCheckboxModal MenuCheckboxModal(IEnumerable<MenuSelector> buttons, string? title, IEnumerable<MenuModalDetails>? details)` | Creates a modal window with a group of checkboxes. |
| `MenuRadio MenuRadio(IEnumerable<MenuSelector> buttons)` | Creates a group of radio buttons (with single selection). |
| `MenuRadioModal MenuRadioModal(IEnumerable<MenuSelector> buttons, string? title, IEnumerable<MenuModalDetails>? details)` | Creates a modal window with a group of radio buttons. |
| `MenuSwitch MenuSwitch(IEnumerable<MenuSelector> buttons)` | Creates a switch between multiple options. |
| `MenuCommand MenuCommand(string title)` | Creates a command button with the specified title. |
| `MenuOpenPege MenuOpenPege(MessagePage page, string? title)` | Creates a button to open another page. |
| `MenuOpenPege MenuOpenSubPege(MessagePage page, string? title)` | Creates a button to open a subpage. |
| `MenuLink MenuLink(string url, string title)` | Creates a button-link to an external URL. |
| `MenuSplit MenuSplit()` | Creates a separator between menu items. |
| `MenuNavigatePanel MenuNavigatePanel()` | Creates a navigation panel for moving between component pages. |

## Usage Example

```csharp
public class UserAgreementView : MessagePage {
    public override string pageResource => "UserAgreement";
    public override string title => "{{ 'User agreement' | L }}";
    private MenuRadio languageRadio;
    private MenuCommand acceptCommand;

    public UserAgreementView(BaseBotUser botUser) : base(botUser) {
        languageRadio = MenuRadio(MenuSelector.FromArray(new[] {
            ("English", "en"),
            ("Русский", "ru")
        }));

        var user = (MyBotUser)botUser;

        using var context = user.DBContext();
        var userTable = user.GetUserTable(context);

        languageRadio.Select(userTable.language);

        languageRadio.onSelect += select => {
            using var context = user.dbcontext;
            var userTable = user.GetUserTable(context);

            user.localization.code = select.id;
            userTable.language = select.id;
            context.SaveChanges();
        };

        acceptCommand = MenuCommand("{{ 'I agree' | L }}");
        acceptCommand.onClick += async (callbackQueryId, messageId, chatId) => {
            using var context = user.dbcontext;
            var userTable = user.GetUserTable(context);

            userTable.acceptLicense = true;
            user.acceptLicense = true;
            context.SaveChanges();

            await user.DeleteMessageAsync(messageId);
            await user.informationView.SendPageAsync();
        };
    }

    protected override void OnDispose() {
        languageRadio.Dispose();
        acceptCommand.Dispose();
    }

    public override string? RequestMessageResource() => $"description-{nudeUser.localization.code}";

    public override List<ButtonsPage> RequestPageComponents() {
        return ButtonsPage.Page([
            [languageRadio],
            [acceptCommand],
        ]);
    }
}
```
