using Telegram.Bot.UI.Tests.Mocks;

namespace Telegram.Bot.UI.Tests.E2E;

/// <summary>
/// Base class for comprehensive E2E tests.
///
/// Test philosophy:
/// - Visit every page, click every button
/// - Verify state after every action:
///   - No errors
///   - Exact message count
///   - Exact text match
///   - Exact button match
/// - Don't adapt tests to bugs - if test fails, it's catching an issue
/// </summary>
public abstract class ComprehensiveTestBase : IDisposable {
    protected readonly MockBotWorker Worker;
    protected readonly long ChatId = 12345;
    protected MockBotUser? CurrentUser;

    protected ComprehensiveTestBase() {
        var pagesPath = FindPagesPath();
        Worker = new MockBotWorker(pagesPath);
    }

    /// <summary>
    /// Navigate to a page via command (e.g., "/start", "/checkbox-demo")
    /// </summary>
    protected async Task<MockBotUser> NavigateToAsync(string command) {
        CurrentUser = await Worker.SimulateMessageAsync(ChatId, "/" + command.TrimStart('/'));
        return CurrentUser;
    }

    /// <summary>
    /// Click a button by exact text match
    /// </summary>
    protected async Task<MockBotUser> ClickButtonAsync(string buttonText) {
        if (CurrentUser == null) {
            throw new InvalidOperationException("No current user - navigate to a page first");
        }

        var lastMessage = CurrentUser.Messages.LastOrDefault();
        if (lastMessage == null) {
            throw new InvalidOperationException("No messages to click buttons on");
        }

        // Find button by exact text match
        string? callbackData = null;
        foreach (var row in lastMessage.Buttons) {
            var btn = row.FirstOrDefault(b => b.Text == buttonText);
            if (btn?.CallbackData != null) {
                callbackData = btn.CallbackData;
                break;
            }
        }

        if (callbackData == null) {
            var available = lastMessage.ButtonTexts.SelectMany(r => r).ToList();
            throw new InvalidOperationException(
                $"Button '{buttonText}' not found.\nAvailable buttons: [{string.Join(", ", available.Select(b => $"\"{b}\""))}]"
            );
        }

        await CurrentUser.callbackFactory.InvokeAsync("test", callbackData, lastMessage.Id, ChatId);
        return CurrentUser;
    }

    /// <summary>
    /// Click a button that contains the given text
    /// </summary>
    protected async Task<MockBotUser> ClickButtonContainingAsync(string partialText) {
        if (CurrentUser == null) {
            throw new InvalidOperationException("No current user - navigate to a page first");
        }

        var lastMessage = CurrentUser.Messages.LastOrDefault();
        if (lastMessage == null) {
            throw new InvalidOperationException("No messages to click buttons on");
        }

        // Find button containing text
        string? callbackData = null;
        foreach (var row in lastMessage.Buttons) {
            var btn = row.FirstOrDefault(b => b.Text.Contains(partialText));
            if (btn?.CallbackData != null) {
                callbackData = btn.CallbackData;
                break;
            }
        }

        if (callbackData == null) {
            var available = lastMessage.ButtonTexts.SelectMany(r => r).ToList();
            throw new InvalidOperationException(
                $"Button containing '{partialText}' not found.\nAvailable buttons: [{string.Join(", ", available.Select(b => $"\"{b}\""))}]"
            );
        }

        await CurrentUser.callbackFactory.InvokeAsync("test", callbackData, lastMessage.Id, ChatId);
        return CurrentUser;
    }

    /// <summary>
    /// Verify current state matches expected
    /// </summary>
    protected void Verify(string expectedText, string[][] expectedButtons, string context = "") {
        if (CurrentUser == null) {
            throw new InvalidOperationException("No current user - navigate to a page first");
        }

        StrictAssert.VerifySingleMessage(CurrentUser, expectedText, expectedButtons, context);
    }

    /// <summary>
    /// Verify no errors occurred
    /// </summary>
    protected void VerifyNoErrors(string context = "") {
        if (CurrentUser == null) {
            throw new InvalidOperationException("No current user - navigate to a page first");
        }

        StrictAssert.VerifyNoErrors(CurrentUser, context);
    }

    /// <summary>
    /// Get current message text
    /// </summary>
    protected string GetCurrentText() {
        if (CurrentUser == null || CurrentUser.Messages.Count == 0) {
            return "";
        }
        return CurrentUser.Messages.Last().Text;
    }

    /// <summary>
    /// Get current buttons
    /// </summary>
    protected string[][] GetCurrentButtons() {
        if (CurrentUser == null || CurrentUser.Messages.Count == 0) {
            return [];
        }
        return CurrentUser.Messages.Last().ButtonTexts;
    }

    public void Dispose() { }

    private static string FindPagesPath() {
        // Use Tests resources (copied from Demo)
        var current = Directory.GetCurrentDirectory();
        while (current != null) {
            var testsCandidate = Path.Combine(current, "Telegram.Bot.UI.Tests", "Resources", "Pages");
            if (Directory.Exists(testsCandidate)) {
                return testsCandidate;
            }
            current = Directory.GetParent(current)?.FullName;
        }
        throw new DirectoryNotFoundException("Could not find Tests pages directory. Make sure Telegram.Bot.UI.Tests/Resources/Pages exists.");
    }
}
