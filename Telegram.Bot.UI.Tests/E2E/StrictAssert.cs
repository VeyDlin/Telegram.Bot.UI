using FluentAssertions;
using Telegram.Bot.UI.Tests.Mocks;

namespace Telegram.Bot.UI.Tests.E2E;

/// <summary>
/// Strict assertions for comprehensive E2E testing.
///
/// Every action must verify:
/// 1. No errors (exceptions) caught by bot worker
/// 2. Exact number of messages
/// 3. Exact text match (line by line comparison)
/// 4. Exact button match (all buttons, exact order and text)
/// </summary>
public static class StrictAssert {
    /// <summary>
    /// Verify page state after an action.
    /// This is the main assertion method that should be called after every user action.
    /// </summary>
    public static void VerifyState(
        MockBotUser user,
        int expectedMessageCount,
        string expectedText,
        string[][] expectedButtons,
        string context = ""
    ) {
        var ctx = string.IsNullOrEmpty(context) ? "" : $" [{context}]";

        // 1. No errors
        VerifyNoErrors(user, context);

        // 2. Exact message count
        user.Messages.Should().HaveCount(
            expectedMessageCount,
            $"should have exactly {expectedMessageCount} message(s){ctx}"
        );

        if (expectedMessageCount == 0) {
            return;
        }

        var lastMessage = user.Messages.Last();

        // 3. Exact text match
        lastMessage.Text.Should().Be(
            expectedText,
            $"message text should match exactly{ctx}"
        );

        // 4. Exact button match
        VerifyButtonsExact(lastMessage.ButtonTexts, expectedButtons, context);
    }

    /// <summary>
    /// Verify no errors occurred
    /// </summary>
    public static void VerifyNoErrors(MockBotUser user, string context = "") {
        var ctx = string.IsNullOrEmpty(context) ? "" : $" [{context}]";

        if (user.Errors.Count > 0) {
            var errorMessages = string.Join("\n", user.Errors.Select(e => $"  - {e.GetType().Name}: {e.Message}"));
            Assert.Fail($"Expected no errors{ctx}, but got {user.Errors.Count} error(s):\n{errorMessages}");
        }
    }

    /// <summary>
    /// Verify buttons match exactly (order, count, text)
    /// </summary>
    public static void VerifyButtonsExact(
        string[][] actual,
        string[][] expected,
        string context = ""
    ) {
        var ctx = string.IsNullOrEmpty(context) ? "" : $" [{context}]";

        // Check row count
        actual.Length.Should().Be(
            expected.Length,
            $"button row count should match{ctx}. Expected {expected.Length} rows, got {actual.Length}"
        );

        // Check each row
        for (int row = 0; row < expected.Length; row++) {
            actual[row].Length.Should().Be(
                expected[row].Length,
                $"button count in row {row} should match{ctx}. Expected {expected[row].Length}, got {actual[row].Length}"
            );

            for (int col = 0; col < expected[row].Length; col++) {
                actual[row][col].Should().Be(
                    expected[row][col],
                    $"button [{row}][{col}] should match exactly{ctx}"
                );
            }
        }
    }

    /// <summary>
    /// Verify state with a single message (most common case)
    /// </summary>
    public static void VerifySingleMessage(
        MockBotUser user,
        string expectedText,
        string[][] expectedButtons,
        string context = ""
    ) {
        VerifyState(user, 1, expectedText, expectedButtons, context);
    }

    /// <summary>
    /// Format buttons for error messages
    /// </summary>
    public static string FormatButtons(string[][] buttons) {
        if (buttons.Length == 0) {
            return "[]";
        }

        var lines = new List<string>();
        for (int row = 0; row < buttons.Length; row++) {
            var rowStr = string.Join(", ", buttons[row].Select(b => $"\"{b}\""));
            lines.Add($"  [{row}]: [{rowStr}]");
        }
        return "[\n" + string.Join("\n", lines) + "\n]";
    }
}
