using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.Components;
using Telegram.Bot.UI.Components.Attributes;
using Telegram.Bot.UI.Menu.Modal;
using Telegram.Bot.UI.Menu.Selectors;
using Telegram.Bot.UI.Parsing;

namespace Telegram.Bot.UI.Menu;


[Component("radio-modal")]
public class MenuRadioModal : AutoComponent {
    public IEnumerable<MenuSelector> buttons { get; set; } = [];
    public IEnumerable<MenuModalDetails>? details { get; set; } = [];
    public string? title { get; set; }

    private MenuRadioModalPage? modalPage;
    public override int columns {
        get => modalPage?.columns ?? 1;
        set {
            if (modalPage != null) {
                modalPage.columns = value;
            }
        }
    }
    public int selected => modalPage?.selected ?? 0;
    public string? selectedId => modalPage?.selectedId;
    public string? selectedTitle => modalPage?.selectButton?.title;
    public MenuSelector? selectButton => modalPage?.selectButton;

    private string? callbackId = null;

    // Handler for select events - using property for JavaScript compatibility
    public Func<MenuSelector, Task>? onSelect {
        get => modalPage?.onSelect;
        set {
            if (modalPage != null) {
                modalPage.onSelect = value;
            }
        }
    }

    [Prop("title")]
    public string? titleAttr { get; set; }

    [Prop("message")]
    public string? messageAttr { get; set; }

    [Prop("maxItems")]
    public string? maxItemsAttr { get; set; }

    [Prop("maxRows")]
    public string? maxRowsAttr { get; set; }

    [Bind("selected")]
    public string? selectedBinding { get; set; }

    [Event("select")]
    public string? onSelectHandler { get; set; }


    // Parameterless constructor for AutoComponent
    public MenuRadioModal() { }


    internal override void ApplyDefinition(AngleSharp.Dom.IElement element) {
        base.ApplyDefinition(element);

        // Parse options
        var options = ParseOptions();
        buttons = options.Select(opt => new MenuSelector {
            id = opt.id,
            title = opt.lang ? botUser.L(render(opt.title)) : render(opt.title)
        }).ToList();

        // Parse modal details from options (title, index, message, webPreview)
        // Use the rendered buttons list for title (handles :title binding properly)
        var optionElements = element.QuerySelectorAll(":scope > option").ToList();
        var buttonsList = buttons.ToList();
        details = optionElements.Select((opt, index) => {
            // Parse <message> inside option if present
            var messageElement = opt.QuerySelector(":scope > message");
            MessageDefinition? message = messageElement is not null
                ? HtmlPageParser.ParseMessage(messageElement)
                : null;

            // Get title from the rendered buttons list (handles :title binding)
            var renderedTitle = index < buttonsList.Count ? buttonsList[index].title : "";

            return new MenuModalDetails {
                id = opt.GetAttribute("value") ?? "",
                title = renderedTitle,
                index = index,
                message = message,
                webPreview = opt.HasAttribute("webPreview")
                    ? opt.GetAttribute("webPreview")?.ToLower() is "true" or "1" or "yes"
                    : true
            };
        }).ToList();

        // Parse fallback message from modal level
        var fallbackMessageElement = element.QuerySelector(":scope > message");
        MessageDefinition? fallbackMessage = fallbackMessageElement is not null
            ? HtmlPageParser.ParseMessage(fallbackMessageElement)
            : null;

        // Get title: first check child element, then attribute
        var titleFromElement = GetChildElementContent("title");
        title = !string.IsNullOrEmpty(titleFromElement) ? titleFromElement : GetProp(nameof(titleAttr), "");

        // Parse pagination settings
        var maxItemsStr = GetProp(nameof(maxItemsAttr), "");
        var maxRowsStr = GetProp(nameof(maxRowsAttr), "");
        int? maxItems = int.TryParse(maxItemsStr, out var mi) ? mi : null;
        int? maxRows = int.TryParse(maxRowsStr, out var mr) ? mr : null;

        // Create modal page
        modalPage = new(buttons, details, botUser) {
            fallbackMessage = fallbackMessage,
            scriptContext = scriptContext,
            textTags = (parent as Runtime.ScriptPage)?.textTags,
            maxItems = maxItems,
            maxRows = maxRows
        };
    }


    public async Task InitializeAsync() {
        // Apply initial selection from binding
        var selectedValue = GetProp("selectedBinding", "");
        if (!string.IsNullOrEmpty(selectedValue) && modalPage is not null) {
            await modalPage.SelectAsync(selectedValue);
        }

        // Wire up event handler
        if (HasEvent("onSelectHandler") && modalPage is not null) {
            modalPage.onSelect += async (selector) => {
                await InvokeEvent("onSelectHandler", new { select = new { id = selector.id, title = selector.title } });
            };
        }
    }


    public Task SelectAsync(string id) => modalPage?.SelectAsync(id) ?? Task.CompletedTask;


    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(callbackId);
        modalPage?.Dispose();
    }


    public override async Task<List<InlineKeyboardButton>> BuildAsync() {
        if (hide || modalPage is null) {
            return new();
        }

        botUser.callbackFactory.Unsubscribe(callbackId);

        callbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (callbackQueryId, messageId, chatId) => {
            modalPage.parent = parent;
            await modalPage.UpdatePageAsync(messageId, chatId);
        }, $"RadioModal[{title}]");

        // Re-evaluate title using GetProp for reactive bindings (:title="expr")
        // This ensures dynamic expressions like 'Size: ' + selectedSize are updated
        var buttonText = GetProp(nameof(titleAttr), "");
        if (string.IsNullOrEmpty(buttonText)) {
            // Fallback to child element or selected button title
            var titleFromElement = GetChildElementContent("title");
            buttonText = !string.IsNullOrEmpty(titleFromElement)
                ? (scriptContext is not null ? await scriptContext.RenderAsync(titleFromElement) : titleFromElement)
                : selectButton?.title ?? "";
        }

        return new() {
            InlineKeyboardButton.WithCallbackData(
                text: buttonText,
                callbackData: callbackId
            )
        };
    }
}