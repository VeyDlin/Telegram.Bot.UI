using FluentAssertions;
using Telegram.Bot.UI.Tests.Mocks;

namespace Telegram.Bot.UI.Tests.Unit;

/// <summary>
/// Tests for radio component pagination with navigate.
/// </summary>
public class RadioPaginationTests : IDisposable {
    private readonly MockBotWorker Worker;
    private readonly long ChatId = 12345;

    public RadioPaginationTests() {
        var pagesPath = FindPagesPath();
        Worker = new MockBotWorker(pagesPath);
    }

    public void Dispose() { }

    [Fact]
    public async Task Radio_WithMaxItems_ShowsOnlyPageItems() {
        var user = await Worker.SimulateMessageAsync(ChatId, "/radio-pagination-test");

        user.Errors.Should().BeEmpty();
        user.Messages.Should().HaveCount(1);

        var buttons = user.Messages[0].Buttons.SelectMany(r => r).Select(b => b.Text).ToList();

        // Should show only first 3 items (max-items="3")
        buttons.Should().Contain(b => b.Contains("Item 1"));
        buttons.Should().Contain(b => b.Contains("Item 2"));
        buttons.Should().Contain(b => b.Contains("Item 3"));
        buttons.Should().NotContain(b => b.Contains("Item 4"));
        buttons.Should().NotContain(b => b.Contains("Item 5"));
    }

    [Fact]
    public async Task Radio_Navigate_ShowsWhenMultiplePages() {
        var user = await Worker.SimulateMessageAsync(ChatId, "/radio-pagination-test");

        user.Errors.Should().BeEmpty();

        var buttons = user.Messages[0].Buttons.SelectMany(r => r).Select(b => b.Text).ToList();

        // Should have navigation buttons
        buttons.Should().Contain("◀");
        buttons.Should().Contain(b => b.Contains("1 / 2")); // counter
        buttons.Should().Contain("▶");
    }

    [Fact]
    public async Task Radio_NavigateNext_ShowsSecondPage() {
        var user = await Worker.SimulateMessageAsync(ChatId, "/radio-pagination-test");
        user.Errors.Should().BeEmpty();

        // Find and click "Next" button
        var nextButton = user.Messages[0].Buttons
            .SelectMany(r => r)
            .FirstOrDefault(b => b.Text == "▶");
        nextButton.Should().NotBeNull();

        await Worker.SimulateButtonClickAsync(ChatId, nextButton!.Text);

        var buttons = user.Messages[0].Buttons.SelectMany(r => r).Select(b => b.Text).ToList();

        // Should now show items 4-5 (second page)
        buttons.Should().Contain(b => b.Contains("Item 4"));
        buttons.Should().Contain(b => b.Contains("Item 5"));
        buttons.Should().NotContain(b => b.Contains("Item 1"));
        buttons.Should().NotContain(b => b.Contains("Item 2"));
    }

    [Fact]
    public async Task Radio_SelectionPersistsAcrossPages() {
        var user = await Worker.SimulateMessageAsync(ChatId, "/radio-pagination-test");
        user.Errors.Should().BeEmpty();

        // Select Item 1 on first page
        var item1 = user.Messages[0].Buttons
            .SelectMany(r => r)
            .FirstOrDefault(b => b.Text.Contains("Item 1"));
        await Worker.SimulateButtonClickAsync(ChatId, item1!.Text);

        // Navigate to second page
        var nextButton = user.Messages[0].Buttons
            .SelectMany(r => r)
            .FirstOrDefault(b => b.Text == "▶");
        await Worker.SimulateButtonClickAsync(ChatId, nextButton!.Text);

        // Navigate back to first page
        var prevButton = user.Messages[0].Buttons
            .SelectMany(r => r)
            .FirstOrDefault(b => b.Text == "◀");
        await Worker.SimulateButtonClickAsync(ChatId, prevButton!.Text);

        var buttons = user.Messages[0].Buttons.SelectMany(r => r).Select(b => b.Text).ToList();

        // Item 1 should still be selected (have checkmark)
        buttons.Should().Contain(b => b.Contains("✅") && b.Contains("Item 1"));
    }

    private static string FindPagesPath() {
        var current = Directory.GetCurrentDirectory();
        while (current != null) {
            var testsCandidate = Path.Combine(current, "Telegram.Bot.UI.Tests", "Resources", "Pages");
            if (Directory.Exists(testsCandidate)) {
                return testsCandidate;
            }
            current = Directory.GetParent(current)?.FullName;
        }
        throw new DirectoryNotFoundException("Could not find Tests pages directory.");
    }
}
