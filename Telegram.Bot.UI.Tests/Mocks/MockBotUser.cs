using Localization;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI;
using Telegram.Bot.UI.BotWorker;
using Telegram.Bot.UI.Runtime;
using Telegram.Bot.UI.Services.Loader;

namespace Telegram.Bot.UI.Tests.Mocks;

/// <summary>
/// Button with text and callback data
/// </summary>
public class MockButton {
    public string Text { get; set; } = "";
    public string? CallbackData { get; set; }
}

/// <summary>
/// Type of message sent
/// </summary>
public enum MockMessageType {
    Text,
    Photo,
    Document,
    Audio,
    Video
}

/// <summary>
/// Simple message structure for testing
/// </summary>
public class MockMessage {
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public MockButton[][] Buttons { get; set; } = [];
    public MockMessageType Type { get; set; } = MockMessageType.Text;
    public bool HasMediaStream { get; set; } = false;
    public string? FileName { get; set; }

    /// <summary>
    /// Get button texts as 2D string array (for simple assertions)
    /// </summary>
    public string[][] ButtonTexts => Buttons
        .Select(row => row.Select(b => b.Text).ToArray())
        .ToArray();
}

/// <summary>
/// Mock implementation of BaseBotUser for testing components
/// </summary>
public class MockBotUser : BaseBotUser {
    public Dictionary<string, object?> CustomExtensions { get; } = new();
    public List<MockMessage> Messages { get; } = [];
    public Dictionary<string, ScriptPage> PageCache { get; } = new();
    public List<Exception> Errors { get; } = [];
    public PageManager? PageManager { get; set; }

    private int nextMessageId = 1;

    public MockBotUser(long chatId = 12345) : base(
        CreateMockWorker(),
        chatId,
        CreateEmptyMockClient(),  // Will be replaced via reflection
        CancellationToken.None
    ) {
        // Replace client property with our tracking mock using backing field
        var backingField = typeof(BaseBotUser).GetField("<client>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (backingField == null) {
            throw new Exception("Could not find <client>k__BackingField in BaseBotUser");
        }

        backingField.SetValue(this, CreateTrackingClient());

        // Verify
        if (client == null) {
            throw new Exception("Failed to set client via reflection");
        }
    }

    private static ITelegramBotClient CreateEmptyMockClient() {
        return new Mock<ITelegramBotClient>().Object;
    }

    public override void RegisterScriptExtensions(ScriptContext context) {
        base.RegisterScriptExtensions(context);

        foreach (var (name, value) in CustomExtensions) {
            context.SetValue(name, value);
        }
    }

    public override ScriptPage? GetOrCreateCachedPage(string pageId, PageManager pageManager) {
        if (PageCache.TryGetValue(pageId, out var cached)) {
            return cached;
        }
        var page = pageManager.GetPage(pageId, this);
        if (page is not null) {
            PageCache[pageId] = page;
        }
        return page;
    }

    public override Task HandleErrorAsync(Exception exception) {
        Errors.Add(exception);
        return Task.CompletedTask;
    }

    public override async Task HandleCommandAsync(string cmd, string[] arguments, Message message) {
        if (PageManager is null) {
            Errors.Add(new Exception("PageManager is null"));
            return;
        }

        // Clear messages before loading new page (for testing predictability)
        Messages.Clear();
        Errors.Clear();

        // Try to load page by command name
        var pageId = cmd switch {
            "start" => "home",
            "home" => "home",
            _ => cmd // Try command name as page id (e.g., /form-demo -> form-demo)
        };

        var page = GetOrCreateCachedPage(pageId, PageManager);
        if (page is not null) {
            try {
                var result = await page.SendPageAsync();
                if (Messages.Count == 0) {
                    Errors.Add(new Exception($"SendPageAsync completed but no message was added. lastMessage: {result?.MessageId}"));
                }
            } catch (Exception ex) {
                Errors.Add(new Exception($"SendPageAsync threw: {ex.GetType().Name}: {ex.Message}"));
                throw;
            }
        } else {
            Errors.Add(new Exception($"Page '{pageId}' not found"));
        }
    }

    /// <summary>
    /// Track received photos for testing
    /// </summary>
    public List<PhotoSize[]> ReceivedPhotos { get; } = new();

    /// <summary>
    /// Track received documents for testing
    /// </summary>
    public List<Document> ReceivedDocuments { get; } = new();

    public override async Task HandlePhotoAsync(PhotoSize[] photo, Message message) {
        ReceivedPhotos.Add(photo);
        // Forward to active page if it has a handler
        await ForwardPhotoToActivePageAsync(photo, message);
    }

    public override async Task HandleDocumentAsync(Document document, Message message) {
        ReceivedDocuments.Add(document);
        // Forward to active page if it has a handler
        await ForwardDocumentToActivePageAsync(document, message);
    }

    private ITelegramBotClient CreateTrackingClient() {
        var mock = new Mock<ITelegramBotClient> { DefaultValue = DefaultValue.Mock };

        // Setup for any Message request
        mock.Setup(c => c.SendRequest(
            It.IsAny<IRequest<Message>>(),
            It.IsAny<CancellationToken>()
        )).Returns((IRequest<Message> req, CancellationToken _) => {
            if (req is SendMessageRequest sendReq) {
                var msg = new MockMessage {
                    Id = nextMessageId++,
                    Text = sendReq.Text ?? "",
                    Buttons = ExtractButtons(sendReq.ReplyMarkup),
                    Type = MockMessageType.Text
                };
                Messages.Add(msg);
                return Task.FromResult(CreateTelegramMessage(msg.Id));
            } else if (req is EditMessageTextRequest editReq) {
                var msg = Messages.FirstOrDefault(m => m.Id == editReq.MessageId);
                if (msg != null) {
                    msg.Text = editReq.Text ?? "";
                    msg.Buttons = ExtractButtons(editReq.ReplyMarkup);
                }
                return Task.FromResult(CreateTelegramMessage(editReq.MessageId));
            } else if (req is SendPhotoRequest photoReq) {
                // Handle photo message - track type
                var msg = new MockMessage {
                    Id = nextMessageId++,
                    Text = photoReq.Caption ?? "[Photo]",
                    Buttons = ExtractButtons(photoReq.ReplyMarkup),
                    Type = MockMessageType.Photo
                };
                Messages.Add(msg);
                return Task.FromResult(CreateTelegramMessage(msg.Id));
            } else if (req is SendDocumentRequest docReq) {
                // Handle document message - track type
                var msg = new MockMessage {
                    Id = nextMessageId++,
                    Text = docReq.Caption ?? "[Document]",
                    Buttons = ExtractButtons(docReq.ReplyMarkup),
                    Type = MockMessageType.Document
                };
                Messages.Add(msg);
                return Task.FromResult(CreateTelegramMessage(msg.Id));
            } else if (req is DeleteMessageRequest) {
                // Handle delete message - just return true (success)
                return Task.FromResult(CreateTelegramMessage(1));
            } else if (req is EditMessageReplyMarkupRequest replyMarkupReq) {
                // Handle EditMessageReplyMarkup - update buttons on existing message
                var msg = Messages.FirstOrDefault(m => m.Id == replyMarkupReq.MessageId);
                if (msg != null) {
                    msg.Buttons = ExtractButtons(replyMarkupReq.ReplyMarkup);
                }
                return Task.FromResult(CreateTelegramMessage(replyMarkupReq.MessageId));
            } else if (req is EditMessageCaptionRequest captionReq) {
                // Handle EditMessageCaption - update caption (text) and buttons on media message
                var msg = Messages.FirstOrDefault(m => m.Id == captionReq.MessageId);
                if (msg != null) {
                    msg.Text = captionReq.Caption ?? "";
                    msg.Buttons = ExtractButtons(captionReq.ReplyMarkup);
                }
                return Task.FromResult(CreateTelegramMessage(captionReq.MessageId));
            }
            // Fallback
            return Task.FromResult(CreateTelegramMessage(1));
        });

        // Setup for bool requests (AnswerCallbackQuery)
        mock.Setup(c => c.SendRequest(
            It.IsAny<IRequest<bool>>(),
            It.IsAny<CancellationToken>()
        )).Returns(Task.FromResult(true));

        return mock.Object;
    }

    private static MockButton[][] ExtractButtons(ReplyMarkup? markup) {
        if (markup is not InlineKeyboardMarkup inline) {
            return [];
        }

        return inline.InlineKeyboard
            .Select(row => row.Select(btn => new MockButton {
                Text = btn.Text,
                CallbackData = btn.CallbackData
            }).ToArray())
            .ToArray();
    }

    private Message CreateTelegramMessage(int id) {
        // Telegram.Bot uses System.Text.Json with specific naming policy
        var options = new System.Text.Json.JsonSerializerOptions {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
        };
        return System.Text.Json.JsonSerializer.Deserialize<Message>(
            $$"""{"message_id":{{id}},"chat":{"id":{{chatId}},"type":"private"},"date":1704067200}""",
            options
        )!;
    }

    private static string? resourcesPath;

    /// <summary>
    /// Set the resources path for MockBotUser instances
    /// </summary>
    public static void SetResourcesPath(string path) {
        resourcesPath = path;
    }

    private static IBotWorker CreateMockWorker() {
        var mock = new Mock<IBotWorker>();
        var path = resourcesPath ?? "/test/resources";
        var resourceLoader = new ResourceLoader(path);

        mock.Setup(w => w.resourceLoader).Returns(resourceLoader);
        mock.Setup(w => w.localizationPack).Returns((LocalizationPack?)null);
        mock.Setup(w => w.startTime).Returns(DateTime.UtcNow);
        mock.Setup(w => w.logger).Returns(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);

        return mock.Object;
    }
}