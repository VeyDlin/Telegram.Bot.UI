using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.Components;
using Telegram.Bot.UI.Components.Attributes;
using Telegram.Bot.UI.Runtime;

namespace Telegram.Bot.UI.Menu;

/// <summary>
/// Represents a navigation panel component for paginated content with previous, next, and counter buttons.
/// </summary>
[Component("navigate")]
public class MenuNavigatePanel : AutoComponent {
    /// <summary>
    /// Gets or sets the previous button title attribute.
    /// </summary>
    [Prop("prevTitle")]
    public string? prevTitleAttr { get; set; }

    /// <summary>
    /// Gets or sets the next button title attribute.
    /// </summary>
    [Prop("nextTitle")]
    public string? nextTitleAttr { get; set; }

    /// <summary>
    /// Gets or sets the counter display template attribute.
    /// </summary>
    [Prop("counterTitle")]
    public string? counterTitleAttr { get; set; }

    /// <summary>
    /// Gets or sets whether to show the counter button attribute.
    /// </summary>
    [Prop("showCounter")]
    public string? showCounterAttr { get; set; }

    /// <summary>
    /// Gets or sets whether to enable carousel mode (wrap around) attribute.
    /// </summary>
    [Prop("carousel")]
    public string? carouselAttr { get; set; }

    /// <summary>
    /// Gets or sets whether to hide boundary buttons attribute.
    /// </summary>
    [Prop("hideBoundary")]
    public string? hideBoundaryAttr { get; set; }

    /// <summary>
    /// Gets or sets the message to show when reaching boundary attribute.
    /// </summary>
    [Prop("boundaryMessage")]
    public string? boundaryMessageAttr { get; set; }

    /// <summary>
    /// Gets or sets the target card component ID attribute.
    /// </summary>
    [Prop("target")]
    public string? targetAttr { get; set; }

    /// <summary>
    /// Gets or sets the event handler for counter button clicks.
    /// </summary>
    [Event("click")]
    public string? onClickHandler { get; set; }

    private string? prevCallbackId;
    private string? nextCallbackId;
    private string? counterCallbackId;

    /// <summary>
    /// Gets or sets the current page index.
    /// </summary>
    public int currentPage { get; set; } = 0;

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    public int totalPages { get; set; } = 1;

    /// <summary>
    /// Gets or sets the synchronous callback for page changes.
    /// </summary>
    public Action<int>? onPageChange { get; set; }

    /// <summary>
    /// Gets or sets the asynchronous callback for page changes.
    /// </summary>
    public Func<int, Task>? onPageChangeAsync { get; set; }

    /// <summary>
    /// Gets the previous button title. Defaults to "◀".
    /// </summary>
    public string prevTitle => GetProp(nameof(prevTitleAttr), "◀");

    /// <summary>
    /// Gets the next button title. Defaults to "▶".
    /// </summary>
    public string nextTitle => GetProp(nameof(nextTitleAttr), "▶");

    /// <summary>
    /// Gets the counter display template.
    /// </summary>
    public string counterTitle => GetRawProp(nameof(counterTitleAttr), "{{ self.currentPage + 1 }} / {{ self.pageCount }}");

    /// <summary>
    /// Gets whether to show the counter button. Defaults to true.
    /// </summary>
    public bool showCounter => GetPropBool(nameof(showCounterAttr), true);

    /// <summary>
    /// Gets whether carousel mode is enabled. Defaults to true.
    /// </summary>
    public bool carousel => GetPropBool(nameof(carouselAttr), true);

    /// <summary>
    /// Gets whether to hide boundary buttons. Defaults to false.
    /// </summary>
    public bool hideBoundary => GetPropBool(nameof(hideBoundaryAttr), false);

    /// <summary>
    /// Gets the message to show when reaching boundary.
    /// </summary>
    public string? boundaryMessage => GetProp(nameof(boundaryMessageAttr), "");

    /// <summary>
    /// Gets the target card component ID.
    /// </summary>
    public string? target => string.IsNullOrEmpty(GetProp(nameof(targetAttr), "")) ? null : GetProp(nameof(targetAttr), "");


    /// <summary>
    /// Unsubscribes from all callbacks when component is disposed.
    /// </summary>
    protected override void OnDispose() {
        botUser.callbackFactory.Unsubscribe(prevCallbackId);
        botUser.callbackFactory.Unsubscribe(nextCallbackId);
        botUser.callbackFactory.Unsubscribe(counterCallbackId);
    }

    /// <summary>
    /// Finds the target card component by ID.
    /// </summary>
    /// <returns>The target MenuCard or null if not found.</returns>
    private MenuCard? FindTargetCard() {
        if (string.IsNullOrEmpty(target)) {
            return null;
        }

        if (scriptContext is not null) {
            return scriptContext.GetComponent<MenuCard>(target);
        }

        return null;
    }

    /// <summary>
    /// Navigates to a specific page using either card-based or callback-based navigation.
    /// </summary>
    /// <param name="newPage">The page index to navigate to.</param>
    private async Task NavigateToPageAsync(int newPage) {
        var targetCard = FindTargetCard();
        if (targetCard is not null) {
            targetCard.GoToPage(newPage);
            currentPage = newPage;

            if (parent.lastMessage is not null) {
                await parent.UpdatePageAsync(parent.lastMessage.MessageId, parent.lastMessage.Chat.Id);
            }
        } else if (onPageChangeAsync is not null) {
            await onPageChangeAsync.Invoke(newPage);
        } else if (onPageChange is not null) {
            onPageChange.Invoke(newPage);
        }
    }

    /// <summary>
    /// Builds the inline keyboard buttons for the navigation panel.
    /// </summary>
    /// <returns>A list of inline keyboard buttons.</returns>
    public override async Task<List<InlineKeyboardButton>> BuildAsync() {
        var targetCard = FindTargetCard();
        if (targetCard is not null) {
            totalPages = targetCard.pageCount;
            currentPage = targetCard.currentPage;
        }

        if (hide || totalPages <= 1) {
            return new();
        }

        var buttons = new List<InlineKeyboardButton>();

        botUser.callbackFactory.Unsubscribe(prevCallbackId);
        botUser.callbackFactory.Unsubscribe(nextCallbackId);
        botUser.callbackFactory.Unsubscribe(counterCallbackId);

        var canGoPrev = carousel || currentPage > 0;
        if (!hideBoundary || canGoPrev) {
            prevCallbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (qid, mid, cid) => {
                if (canGoPrev) {
                    var newPage = currentPage - 1;
                    if (newPage < 0) {
                        newPage = carousel ? totalPages - 1 : 0;
                    }
                    await NavigateToPageAsync(newPage);
                } else if (!string.IsNullOrEmpty(boundaryMessage)) {
                    await botUser.ShowAlertAsync(render(boundaryMessage), qid, showAlert: false);
                }
            });
            buttons.Add(InlineKeyboardButton.WithCallbackData(render(prevTitle), prevCallbackId));
        }

        if (showCounter) {
            string counterText;
            if (scriptContext is not null) {
                var ctx = new ComponentContext(scriptContext);
                ctx.SetSelf(new { currentPage, pageCount = totalPages });
                counterText = await ctx.RenderAsync(counterTitle);
            } else {
                counterText = counterTitle
                    .Replace("self.currentPage + 1", (currentPage + 1).ToString())
                    .Replace("self.pageCount", totalPages.ToString());
                counterText = render(counterText);
            }

            if (HasEvent("onClickHandler")) {
                counterCallbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (qid, mid, cid) => {
                    await InvokeEvent("onClickHandler", new { currentPage, totalPages });
                });
                buttons.Add(InlineKeyboardButton.WithCallbackData(counterText, counterCallbackId));
            } else {
                counterCallbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (qid, mid, cid) => {
                    await botUser.client.AnswerCallbackQuery(qid);
                });
                buttons.Add(InlineKeyboardButton.WithCallbackData(counterText, counterCallbackId));
            }
        }

        var canGoNext = carousel || currentPage < totalPages - 1;
        if (!hideBoundary || canGoNext) {
            nextCallbackId = botUser.callbackFactory.Subscribe(botUser.chatId, async (qid, mid, cid) => {
                if (canGoNext) {
                    var newPage = currentPage + 1;
                    if (newPage >= totalPages) {
                        newPage = carousel ? 0 : totalPages - 1;
                    }
                    await NavigateToPageAsync(newPage);
                } else if (!string.IsNullOrEmpty(boundaryMessage)) {
                    await botUser.ShowAlertAsync(render(boundaryMessage), qid, showAlert: false);
                }
            });
            buttons.Add(InlineKeyboardButton.WithCallbackData(render(nextTitle), nextCallbackId));
        }

        return buttons;
    }
}