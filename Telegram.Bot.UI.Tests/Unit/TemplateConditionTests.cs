using FluentAssertions;
using Telegram.Bot.UI.Tests.Mocks;

namespace Telegram.Bot.UI.Tests.Unit;

/// <summary>
/// Tests for template v-if conditions with VModel.
/// These tests verify that &lt;template v-if="VModel.xxx"&gt; conditions work correctly.
/// </summary>
public class TemplateConditionTests : IDisposable {
    private readonly MockBotWorker Worker;
    private readonly long ChatId = 12345;

    public TemplateConditionTests() {
        var pagesPath = FindPagesPath();
        Worker = new MockBotWorker(pagesPath);
    }

    public void Dispose() { }

    [Fact]
    public async Task TemplateCondition_InitialState_ShowsDefaultTemplate() {
        // Initial state: all flags are false, so should show DEFAULT_STATE
        var user = await Worker.SimulateMessageAsync(ChatId, "/template-condition-test");

        user.Errors.Should().BeEmpty("Page should render without errors");
        user.Messages.Should().HaveCount(1, "Should have exactly one message");

        var text = user.Messages[0].Text;
        text.Should().Contain("DEFAULT_STATE", "Initial state should show default template");
        text.Should().NotContain("LOADING_STATE");
        text.Should().NotContain("RESULT_STATE");
        text.Should().NotContain("ERROR_STATE");
    }

    [Fact]
    public async Task TemplateCondition_AfterStartLoading_ShowsLoadingTemplate() {
        // Navigate to page
        var user = await Worker.SimulateMessageAsync(ChatId, "/template-condition-test");
        user.Errors.Should().BeEmpty("Page should render without errors");

        // Click "Start Loading" button
        await Worker.SimulateButtonClickAsync(ChatId, "Start Loading");
        user.Errors.Should().BeEmpty("Start Loading should work without errors");

        var text = user.Messages[0].Text;
        text.Should().Contain("LOADING_STATE", "After clicking Start Loading, should show loading template");
        text.Should().NotContain("DEFAULT_STATE");
    }

    [Fact]
    public async Task TemplateCondition_AfterSetResult_ShowsResultTemplate() {
        // Navigate to page
        var user = await Worker.SimulateMessageAsync(ChatId, "/template-condition-test");
        user.Errors.Should().BeEmpty("Page should render without errors");

        // Click "Set Result" button
        await Worker.SimulateButtonClickAsync(ChatId, "Set Result");
        user.Errors.Should().BeEmpty("Set Result should work without errors");

        var text = user.Messages[0].Text;
        text.Should().Contain("RESULT_STATE", "After clicking Set Result, should show result template");
        text.Should().Contain("Success!", "Result text should be rendered");
        text.Should().NotContain("DEFAULT_STATE");
    }

    [Fact]
    public async Task TemplateCondition_AfterSetError_ShowsErrorTemplate() {
        // Navigate to page
        var user = await Worker.SimulateMessageAsync(ChatId, "/template-condition-test");
        user.Errors.Should().BeEmpty("Page should render without errors");

        // Click "Set Error" button
        await Worker.SimulateButtonClickAsync(ChatId, "Set Error");
        user.Errors.Should().BeEmpty("Set Error should work without errors");

        var text = user.Messages[0].Text;
        text.Should().Contain("ERROR_STATE", "After clicking Set Error, should show error template");
        text.Should().Contain("Something went wrong", "Error text should be rendered");
        text.Should().NotContain("DEFAULT_STATE");
    }

    [Fact]
    public async Task TemplateCondition_AfterReset_ShowsDefaultTemplate() {
        // Navigate to page
        var user = await Worker.SimulateMessageAsync(ChatId, "/template-condition-test");

        // Set to loading state first
        await Worker.SimulateButtonClickAsync(ChatId, "Start Loading");
        user.Messages[0].Text.Should().Contain("LOADING_STATE");

        // Reset back to default
        await Worker.SimulateButtonClickAsync(ChatId, "Reset");
        user.Errors.Should().BeEmpty("Reset should work without errors");

        var text = user.Messages[0].Text;
        text.Should().Contain("DEFAULT_STATE", "After Reset, should show default template again");
        text.Should().NotContain("LOADING_STATE");
    }

    [Fact]
    public async Task TemplateCondition_Priority_FirstMatchingConditionWins() {
        // This test verifies that when multiple conditions could be true,
        // the first matching v-if/v-else-if wins (as expected)

        // Navigate to page
        var user = await Worker.SimulateMessageAsync(ChatId, "/template-condition-test");

        // Set to result state
        await Worker.SimulateButtonClickAsync(ChatId, "Set Result");

        // Now isLoading=false, hasResult=true, hasError=false
        // The v-else-if="VModel.hasResult" should match
        var text = user.Messages[0].Text;
        text.Should().Contain("RESULT_STATE");
        text.Should().NotContain("ERROR_STATE");
        text.Should().NotContain("DEFAULT_STATE");
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
