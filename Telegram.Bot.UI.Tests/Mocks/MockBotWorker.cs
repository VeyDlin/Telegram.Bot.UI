using Moq;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.UI;
using Telegram.Bot.UI.BotWorker;
using Telegram.Bot.UI.Runtime;
using Telegram.Bot.UI.Services.Loader;

namespace Telegram.Bot.UI.Tests.Mocks;

/// <summary>
/// Mock BotWorker for testing - exposes UpdateHandlerAsync publicly
/// </summary>
public class MockBotWorker : BaseBotWorker<MockBotUser> {
    private readonly Mock<ITelegramBotClient> clientMock = new();

    public PageManager PageManager { get; }

    public override ITelegramBotClient client => clientMock.Object;

    public MockBotWorker(string pagesPath) : base(CreateUserFactory()) {
        // Use Tests assembly for ViewModels (tests are now independent from Demo)
        var testsAssembly = typeof(MockBotWorker).Assembly;
        PageManager = new PageManager(pagesPath, testsAssembly);
        PageManager.LoadAll();

        skipMessagesBeforeStart = false;
    }

    private static UserFactoryDelegate CreateUserFactory() {
        return (worker, chatId, client, token) => {
            var user = new MockBotUser(chatId);
            if (worker is MockBotWorker mockWorker) {
                user.PageManager = mockWorker.PageManager;
            }
            return user;
        };
    }

    /// <summary>
    /// Simulate receiving an Update from Telegram
    /// </summary>
    public async Task SimulateUpdateAsync(Update update) {
        await UpdateHandlerAsync(update, CancellationToken.None);
    }

    /// <summary>
    /// Simulate user sending a text message (like "/start" or any text)
    /// </summary>
    public async Task<MockBotUser> SimulateMessageAsync(long chatId, string text) {
        var message = CreateMessage(chatId, text);
        var update = new Update { Message = message };
        await SimulateUpdateAsync(update);

        // Return the user that was created/used
        return await GetUserAsync(chatId);
    }

    /// <summary>
    /// Simulate user sending a photo
    /// </summary>
    public async Task<MockBotUser> SimulatePhotoAsync(long chatId, int width = 640, int height = 480, string? caption = null) {
        var message = CreatePhotoMessage(chatId, width, height, caption);
        var update = new Update { Message = message };
        await SimulateUpdateAsync(update);
        return await GetUserAsync(chatId);
    }

    /// <summary>
    /// Simulate user sending a document/file
    /// </summary>
    public async Task<MockBotUser> SimulateDocumentAsync(long chatId, string fileName = "test.txt", string? mimeType = "text/plain", string? caption = null) {
        var message = CreateDocumentMessage(chatId, fileName, mimeType, caption);
        var update = new Update { Message = message };
        await SimulateUpdateAsync(update);
        return await GetUserAsync(chatId);
    }

    private Message CreateMessage(long chatId, string text) {
        var json = $$"""
        {
            "message_id": 1,
            "chat": {"id": {{chatId}}, "type": "private"},
            "from": {"id": {{chatId}}, "is_bot": false, "first_name": "Test"},
            "date": 1735689600,
            "text": "{{text}}"
        }
        """;
        var options = new System.Text.Json.JsonSerializerOptions {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
        };
        return System.Text.Json.JsonSerializer.Deserialize<Message>(json, options)!;
    }

    private Message CreatePhotoMessage(long chatId, int width, int height, string? caption) {
        var captionJson = caption != null ? $@", ""caption"": ""{caption}""" : "";
        var json = $$"""
        {
            "message_id": 1,
            "chat": {"id": {{chatId}}, "type": "private"},
            "from": {"id": {{chatId}}, "is_bot": false, "first_name": "Test"},
            "date": 1735689600,
            "photo": [
                {"file_id": "small_photo_id", "file_unique_id": "small_unique", "width": {{width / 4}}, "height": {{height / 4}}, "file_size": 1000},
                {"file_id": "medium_photo_id", "file_unique_id": "medium_unique", "width": {{width / 2}}, "height": {{height / 2}}, "file_size": 5000},
                {"file_id": "large_photo_id", "file_unique_id": "large_unique", "width": {{width}}, "height": {{height}}, "file_size": 15000}
            ]{{captionJson}}
        }
        """;
        var options = new System.Text.Json.JsonSerializerOptions {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
        };
        return System.Text.Json.JsonSerializer.Deserialize<Message>(json, options)!;
    }

    private Message CreateDocumentMessage(long chatId, string fileName, string? mimeType, string? caption) {
        var captionJson = caption != null ? $@", ""caption"": ""{caption}""" : "";
        var mimeJson = mimeType != null ? $@", ""mime_type"": ""{mimeType}""" : "";
        var json = $$"""
        {
            "message_id": 1,
            "chat": {"id": {{chatId}}, "type": "private"},
            "from": {"id": {{chatId}}, "is_bot": false, "first_name": "Test"},
            "date": 1735689600,
            "document": {
                "file_id": "doc_file_id",
                "file_unique_id": "doc_unique",
                "file_name": "{{fileName}}"{{mimeJson}},
                "file_size": 1024
            }{{captionJson}}
        }
        """;
        var options = new System.Text.Json.JsonSerializerOptions {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
        };
        return System.Text.Json.JsonSerializer.Deserialize<Message>(json, options)!;
    }

    private async Task<MockBotUser> GetUserAsync(long chatId) {
        return await GetOrCreateChatUserAsync(chatId, null, CancellationToken.None);
    }

    /// <summary>
    /// Simulate user clicking a button on a page
    /// </summary>
    public async Task<MockBotUser> SimulateButtonClickAsync(long chatId, string buttonTitle) {
        var user = await GetUserAsync(chatId);

        // Find the button by title
        string? callbackData = null;
        foreach (var msg in user.Messages) {
            foreach (var row in msg.Buttons) {
                var btn = row.FirstOrDefault(b => b.Text == buttonTitle);
                if (btn?.CallbackData != null) {
                    callbackData = btn.CallbackData;
                    break;
                }
            }
            if (callbackData != null) {
                break;
            }
        }

        if (callbackData == null) {
            user.Errors.Add(new Exception($"Button '{buttonTitle}' not found"));
            return user;
        }

        var update = CreateCallbackQueryUpdate(chatId, callbackData);
        await SimulateUpdateAsync(update);
        return user;
    }

    private Update CreateCallbackQueryUpdate(long chatId, string callbackData) {
        var json = $$"""
        {
            "update_id": 1,
            "callback_query": {
                "id": "callback_123",
                "from": {"id": {{chatId}}, "is_bot": false, "first_name": "Test"},
                "chat_instance": "123",
                "data": "{{callbackData}}",
                "message": {
                    "message_id": 1,
                    "chat": {"id": {{chatId}}, "type": "private"},
                    "date": 1735689600
                }
            }
        }
        """;
        var options = new System.Text.Json.JsonSerializerOptions {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
        };
        return System.Text.Json.JsonSerializer.Deserialize<Update>(json, options)!;
    }
}