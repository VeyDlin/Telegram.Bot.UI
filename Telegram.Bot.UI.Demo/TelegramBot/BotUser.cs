using Microsoft.EntityFrameworkCore;
using Serilog;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.UI.BotWorker;
using Telegram.Bot.UI.Demo.Database;
using Telegram.Bot.UI.Demo.Database.Tables;
using Telegram.Bot.UI.Demo.PhotoFilter;
using Telegram.Bot.UI.Runtime;
using Message = Telegram.Bot.Types.Message;

namespace Telegram.Bot.UI.Demo.TelegramBot;


public class BotUser : BaseBotUser {
    private DatabaseFactory dbFactory { get; set; }
    private PageManager pageManager { get; set; }

    public AppDatabaseContext Context() => dbFactory.Context();

    // Cache pages per user to preserve state (pagination position, etc.)
    private Dictionary<string, ScriptPage> pageCache = new();

    // Photo editor state
    private byte[]? pendingPhotoBytes;
    private SemaphoreSlim photoProcessingSemaphore = new(1, 1);


    public BotUser(
        DatabaseFactory dbFactory,
        PageManager pageManager,
        IBotWorker worker,
        long chatId,
        ITelegramBotClient client,
        CancellationToken token
    ) : base(worker, chatId, client, token) {
        this.dbFactory = dbFactory;
        this.pageManager = pageManager;

        parseMode = ParseMode.Html;
    }


    public override void Begin(Message? message) {
        using var context = Context();
        var userTable = GetUserTable(context);
        context.SaveChanges();

        localization.code = userTable.language;
        acceptLicense = userTable.acceptLicense;
    }


    private ScriptPage? GetOrCreatePage(string pageId) {
        return GetOrCreateCachedPage(pageId, pageManager);
    }


    public override ScriptPage? GetOrCreateCachedPage(string pageId, PageManager pageManager) {
        if (pageCache.TryGetValue(pageId, out var cached)) {
            return cached;
        }

        var page = pageManager.GetPage(pageId, this);
        if (page is not null) {
            pageCache[pageId] = page;
        }
        return page;
    }


    public void ClearPageCache() {
        foreach (var page in pageCache.Values) {
            page.Dispose();
        }
        pageCache.Clear();
    }


    public UserTable GetUserTable(AppDatabaseContext context) {
        var userTable = context.GetUserOrCreate(chatId, () =>
            new() {
                acceptLicense = false,
                language = "en",
                telegramId = chatId
            }
        );
        return userTable;
    }


    public override async Task HandleCommandAsync(string cmd, string[] arguments, Message message) {
        Log.Debug($"CMD {message.Chat.Id}: /{cmd} {string.Join(" ", arguments)}");

        switch (cmd) {
            case "start":
            case "home":
            case "demo": {
                var homePage = GetOrCreatePage("home");
                if (homePage is not null) {
                    await homePage.SendPageAsync();
                }
            }
            break;

            case "checkbox": {
                var page = GetOrCreatePage("checkbox-demo");
                if (page is not null) {
                    await page.SendPageAsync();
                }
            }
            break;

            case "radio": {
                var page = GetOrCreatePage("radio-demo");
                if (page is not null) {
                    await page.SendPageAsync();
                }
            }
            break;

            case "switch": {
                var page = GetOrCreatePage("switch-demo");
                if (page is not null) {
                    await page.SendPageAsync();
                }
            }
            break;

            case "reset": {
                ClearPageCache();
                await SendTextMessageAsync("Page cache cleared. Use /start to begin fresh.");
            }
            break;

            case "lang":
            case "language": {
                var page = GetOrCreatePage("language");
                if (page is not null) {
                    await page.SendPageAsync();
                }
            }
            break;

            case "ping": {
                await SendTextMessageAsync("`pong`", mode: ParseMode.MarkdownV2);
            }
            break;

            case "id": {
                await SendTextMessageAsync($"`{chatId}`", mode: ParseMode.MarkdownV2);
            }
            break;

            case "pages": {
                var pageIds = string.Join("\n", pageManager.GetPageIds().Select(id => $"• {id}"));
                await SendTextMessageAsync($"<b>Available pages:</b>\n{pageIds}");
            }
            break;

            case "admin": {
                await AdminCommand(arguments);
            }
            break;

            default: {
                // Try to open page by command name
                var page = GetOrCreatePage(cmd);
                if (page is not null) {
                    await page.SendPageAsync();
                }
            }
            break;
        }
    }


    public override Task<bool> HandlePermissiveAsync(Message message) {
        if (message.Chat.Type != ChatType.Private) {
            return Task.FromResult(false);
        }

        using var context = Context();
        var userTable = GetUserTable(context);

        if (userTable.HasRole(UserRole.banned)) {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }


    public override async Task HandleAcceptLicense(Message message) {
        var page = pageManager.GetPage("user-agreement", this);
        if (page is not null) {
            await page.SendPageAsync();
        }
    }


    public void AcceptLicenseFromPage() {
        using var context = Context();
        var userTable = GetUserTable(context);
        userTable.acceptLicense = true;
        acceptLicense = true;
        context.SaveChanges();
    }


    public void SetLanguage(string code) {
        using var context = Context();
        var userTable = GetUserTable(context);
        userTable.language = code;
        localization.code = code;
        context.SaveChanges();
    }


    public override async Task HandleErrorAsync(Exception exception) {
        Log.Error(exception.ToString());

        // Always show full error for debugging
        var errorText = exception.ToString();
        // Telegram has 4096 char limit, truncate if needed
        if (errorText.Length > 4000) {
            errorText = errorText.Substring(0, 4000) + "\n...[truncated]";
        }
        await SendTextMessageAsync($"<pre>{EscapeText(errorText, ParseMode.Html)}</pre>", mode: ParseMode.Html);
    }


    private async Task AdminCommand(string[] arguments) {
        using var context = Context();
        var userTable = GetUserTable(context);

        if (!userTable.HasRole(UserRole.admin)) {
            return;
        }

        if (!arguments.Any()) {
            var roles = String.Join(", ", Enum.GetValues(typeof(UserRole)).Cast<UserRole>().Select(role => role.ToString()));
            var info = @$"
`/admin set-role [id] [role] ({roles})`

`/admin del-user [id]`
".Trim();
            await SendTextMessageAsync(info, mode: ParseMode.MarkdownV2);
            return;
        }

        var length = arguments.Length;
        switch (arguments[0]) {
            case "set-role": {
                if (length == 3) {
                    var id = long.Parse(arguments[1]);
                    var role = arguments[2];
                    if (await context.userTable.FirstOrDefaultAsync(x => x.telegramId == id) is UserTable row) {
                        if (Enum.TryParse(role, out UserRole newRole)) {
                            row.role = newRole;
                            await context.SaveChangesAsync();
                            await SendTextMessageAsync(string.Join(" ", arguments));
                        }
                    }
                }
            }
            break;

            case "del-user": {
                if (length == 2) {
                    var id = long.Parse(arguments[1]);
                    if (await context.userTable.FirstOrDefaultAsync(x => x.telegramId == id) is UserTable row) {
                        context.userTable.Remove(row);
                        await context.SaveChangesAsync();
                        await SendTextMessageAsync(string.Join(" ", arguments));
                    }
                }
            }
            break;
        }
    }


    public override async Task HandlePhotoAsync(Telegram.Bot.Types.PhotoSize[] photo, Message message) {
        // Download the photo
        var file = await client.GetFile(photo.Last().FileId);
        if (file?.FilePath is not string filePath) {
            return;
        }

        using var memoryStream = new MemoryStream();
        await client.DownloadFile(filePath, memoryStream);
        pendingPhotoBytes = memoryStream.ToArray();

        // Clear old editor from cache (fresh state for new photo)
        if (pageCache.TryGetValue("photo-editor", out var oldPage)) {
            oldPage.Dispose();
            pageCache.Remove("photo-editor");
        }

        // Open photo editor page
        var editorPage = GetOrCreatePage("photo-editor");
        if (editorPage is not null) {
            await editorPage.SendPageAsync();
        } else {
            Log.Debug($"Photo received from {message.Chat.Id} but no photo-editor page found");
        }
    }


    public async Task GeneratePhoto(bool applyInvert, string brightness, string contrast, string blur, string pixelate, Func<object?>? onComplete = null) {
        if (!photoProcessingSemaphore.Wait(10)) {
            return;
        }

        var uploadPhotoToken = new CancellationTokenSource();
        var semaphoreReleased = false;

        try {
            if (pendingPhotoBytes is null) {
                return;
            }

            // Send UploadPhoto action
            SendChatLongAction(ChatAction.UploadPhoto, uploadPhotoToken);

            // Build filter settings
            var settings = new PhotoFilterSettings {
                applyInvert = applyInvert,
                brightness = Enum.Parse<FilterLevel>(brightness, true),
                contrast = Enum.Parse<FilterLevel>(contrast, true),
                blur = Enum.Parse<FilterLevel>(blur, true),
                pixelate = Enum.Parse<FilterLevel>(pixelate, true)
            };

            // Small delay for demo purposes (to show the wait page)
            await Task.Delay(500);

            // Apply filter
            Log.Debug($"Start filter: {chatId}");
            var result = await PhotoFilterWorker.Filter(pendingPhotoBytes, settings);
            Log.Debug($"End filter: {chatId}");

            uploadPhotoToken.Cancel();
            pendingPhotoBytes = null;

            // Send result photo
            await SendPhotoAsync(result);

            // Call completion callback (dispose pages, etc.)
            if (onComplete is not null) {
                var callbackResult = onComplete();
                if (callbackResult is Task task) {
                    await task;
                }
            }

        } catch (Exception ex) {
            uploadPhotoToken.Cancel();
            pendingPhotoBytes = null;
            await HandleErrorAsync(ex);
        } finally {
            if (!semaphoreReleased) {
                photoProcessingSemaphore.Release();
            }
        }
    }


    public override async Task HandleDocumentAsync(Telegram.Bot.Types.Document document, Message message) {
        // Try to forward to active page first
        var handled = await ForwardDocumentToActivePageAsync(document, message);
        if (!handled) {
            // Navigate to document page and forward the document there
            var documentPage = GetOrCreatePage("document");
            if (documentPage is not null) {
                await documentPage.SendPageAsync();
                await ForwardDocumentToActivePageAsync(document, message);
            } else {
                Log.Debug($"Document received from {message.Chat.Id} but no document page found");
            }
        }
    }
}