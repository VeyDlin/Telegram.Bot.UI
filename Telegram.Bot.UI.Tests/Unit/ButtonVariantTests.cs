using FluentAssertions;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.Tests.Mocks;

namespace Telegram.Bot.UI.Tests.Unit;

/// <summary>
/// Tests for button variant and icon support (Telegram Bot API 9.4).
/// </summary>
public class ButtonVariantTests : IDisposable {
    private readonly MockBotWorker Worker;
    private readonly long ChatId = 12345;

    public ButtonVariantTests() {
        var pagesPath = FindPagesPath();
        Worker = new MockBotWorker(pagesPath);
    }

    public void Dispose() { }

    [Fact]
    public async Task Command_VariantPrimary_SetsStyle() {
        var user = await Worker.SimulateMessageAsync(ChatId, "/button-variant-test");

        user.Errors.Should().BeEmpty();

        var btn = FindButton(user, "Primary");
        btn.Should().NotBeNull();
        btn!.Style.Should().Be(KeyboardButtonStyle.Primary);
    }

    [Fact]
    public async Task Command_VariantDanger_SetsStyle() {
        var user = await Worker.SimulateMessageAsync(ChatId, "/button-variant-test");

        user.Errors.Should().BeEmpty();

        var btn = FindButton(user, "Danger");
        btn.Should().NotBeNull();
        btn!.Style.Should().Be(KeyboardButtonStyle.Danger);
    }

    [Fact]
    public async Task Command_VariantSuccess_SetsStyle() {
        var user = await Worker.SimulateMessageAsync(ChatId, "/button-variant-test");

        user.Errors.Should().BeEmpty();

        var btn = FindButton(user, "Success");
        btn.Should().NotBeNull();
        btn!.Style.Should().Be(KeyboardButtonStyle.Success);
    }

    [Fact]
    public async Task Command_NoVariant_StyleIsNull() {
        var user = await Worker.SimulateMessageAsync(ChatId, "/button-variant-test");

        user.Errors.Should().BeEmpty();

        var btn = FindButton(user, "Default");
        btn.Should().NotBeNull();
        btn!.Style.Should().BeNull();
    }

    [Fact]
    public async Task Command_Icon_SetsIconCustomEmojiId() {
        var user = await Worker.SimulateMessageAsync(ChatId, "/button-variant-test");

        user.Errors.Should().BeEmpty();

        var btn = FindButton(user, "Icon");
        btn.Should().NotBeNull();
        btn!.IconCustomEmojiId.Should().Be("5368324170671202286");
    }

    [Fact]
    public async Task Command_VariantAndIcon_BothSet() {
        var user = await Worker.SimulateMessageAsync(ChatId, "/button-variant-test");

        user.Errors.Should().BeEmpty();

        var btn = FindButton(user, "Both");
        btn.Should().NotBeNull();
        btn!.Style.Should().Be(KeyboardButtonStyle.Primary);
        btn.IconCustomEmojiId.Should().Be("5368324170671202286");
    }

    [Fact]
    public async Task Radio_PerOptionVariant_SetsStyleOnEachButton() {
        var user = await Worker.SimulateMessageAsync(ChatId, "/button-variant-test");

        user.Errors.Should().BeEmpty();

        var allButtons = user.Messages[0].Buttons.SelectMany(r => r).ToList();

        var opt1 = allButtons.FirstOrDefault(b => b.Text.Contains("Option 1"));
        var opt2 = allButtons.FirstOrDefault(b => b.Text.Contains("Option 2"));
        var opt3 = allButtons.FirstOrDefault(b => b.Text.Contains("Option 3"));

        opt1.Should().NotBeNull();
        opt1!.Style.Should().Be(KeyboardButtonStyle.Primary);

        opt2.Should().NotBeNull();
        opt2!.Style.Should().Be(KeyboardButtonStyle.Danger);

        opt3.Should().NotBeNull();
        opt3!.Style.Should().BeNull();
    }

    [Fact]
    public async Task CheckboxList_PerOptionVariantAndIcon_SetsOnEachButton() {
        var user = await Worker.SimulateMessageAsync(ChatId, "/button-variant-test");

        user.Errors.Should().BeEmpty();

        var allButtons = user.Messages[0].Buttons.SelectMany(r => r).ToList();

        var chk1 = allButtons.FirstOrDefault(b => b.Text.Contains("Check 1"));
        var chk2 = allButtons.FirstOrDefault(b => b.Text.Contains("Check 2"));
        var chk3 = allButtons.FirstOrDefault(b => b.Text.Contains("Check 3"));

        chk1.Should().NotBeNull();
        chk1!.Style.Should().Be(KeyboardButtonStyle.Success);

        chk2.Should().NotBeNull();
        chk2!.IconCustomEmojiId.Should().Be("5368324170671202286");

        chk3.Should().NotBeNull();
        chk3!.Style.Should().BeNull();
        chk3!.IconCustomEmojiId.Should().BeNull();
    }

    private static MockButton? FindButton(MockBotUser user, string text) {
        return user.Messages[0].Buttons
            .SelectMany(r => r)
            .FirstOrDefault(b => b.Text == text);
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
