using FluentAssertions;
using Telegram.Bot.UI.Components;
using Telegram.Bot.UI.Parsing;
using Telegram.Bot.UI.Tests.Mocks;

namespace Telegram.Bot.UI.Tests.Unit;

/// <summary>
/// Tests for v-if conditions on command components.
/// </summary>
public class CommandVIfTests : IDisposable {
    private readonly MockBotWorker Worker;
    private readonly long ChatId = 12345;

    public CommandVIfTests() {
        var pagesPath = FindPagesPath();
        Worker = new MockBotWorker(pagesPath);
    }

    public void Dispose() { }

    [Fact]
    public void ParseComponents_RequiresExplicitCloseTags() {
        // HTML5 doesn't support self-closing for custom elements
        // This test documents that <command></command> is required, not <command />
        var html = """
            <view id="test" back-to-parent="false">
                <title>Test</title>
                <message>Test</message>
                <components>
                    <command v-if="showBtn1" title="BTN1"></command>
                    <command v-if="showBtn2" title="BTN2"></command>
                    <command title="Toggle1"></command>
                </components>
            </view>
            """;

        var registry = new ComponentRegistry();
        registry.ScanAssembly(typeof(ComponentRegistry).Assembly);

        var parser = new HtmlPageParser(registry);
        var def = parser.Parse(html);

        def.components.Should().HaveCount(3);
    }

    [Fact]
    public async Task Command_VIf_ShowsOnlyWhenTrue() {
        // showBtn1=true, showBtn2=true, showBtn3=false
        var user = await Worker.SimulateMessageAsync(ChatId, "/command-vif-test");

        user.Errors.Should().BeEmpty("Page should render without errors");
        user.Messages.Should().HaveCount(1);

        var buttons = user.Messages[0].Buttons.SelectMany(r => r).Select(b => b.Text).ToList();
        var buttonList = string.Join(", ", buttons);

        buttons.Should().Contain("BTN1", $"BTN1 should be visible (showBtn1=true). All buttons: [{buttonList}]");
        buttons.Should().Contain("BTN2", $"BTN2 should be visible (showBtn2=true). All buttons: [{buttonList}]");
        buttons.Should().NotContain("BTN3", $"BTN3 should be hidden (showBtn3=false). All buttons: [{buttonList}]");
        buttons.Should().Contain("Toggle1", $"All buttons: [{buttonList}]");
        buttons.Should().Contain("Toggle2", $"All buttons: [{buttonList}]");
        buttons.Should().Contain("Toggle3", $"All buttons: [{buttonList}]");
    }

    [Fact]
    public async Task Command_VIf_HidesAfterToggle() {
        var user = await Worker.SimulateMessageAsync(ChatId, "/command-vif-test");
        user.Errors.Should().BeEmpty();

        // Toggle1 to hide BTN1
        await Worker.SimulateButtonClickAsync(ChatId, "Toggle1");
        user.Errors.Should().BeEmpty();

        var buttons = user.Messages[0].Buttons.SelectMany(r => r).Select(b => b.Text).ToList();

        buttons.Should().NotContain("BTN1", "BTN1 should be hidden after toggle");
        buttons.Should().Contain("BTN2", "BTN2 should still be visible");
    }

    [Fact]
    public async Task Command_VIf_ShowsAfterToggle() {
        var user = await Worker.SimulateMessageAsync(ChatId, "/command-vif-test");
        user.Errors.Should().BeEmpty();

        // Toggle3 to show BTN3
        await Worker.SimulateButtonClickAsync(ChatId, "Toggle3");
        user.Errors.Should().BeEmpty();

        var buttons = user.Messages[0].Buttons.SelectMany(r => r).Select(b => b.Text).ToList();

        buttons.Should().Contain("BTN3", "BTN3 should be visible after toggle");
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
