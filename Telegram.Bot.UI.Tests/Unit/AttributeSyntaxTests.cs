using FluentAssertions;
using Telegram.Bot.UI.Components;
using Telegram.Bot.UI.Parsing;
using Telegram.Bot.UI.Tests.Mocks;

namespace Telegram.Bot.UI.Tests.Unit;

/// <summary>
/// Tests that attr="{{ val }}" and attr="val" are equivalent across all components.
/// Both syntaxes should produce the same result when the value is a literal.
/// </summary>
public class AttributeSyntaxTests : IDisposable {
    private readonly MockBotWorker Worker;
    private readonly long ChatId = 12345;

    public AttributeSyntaxTests() {
        var pagesPath = FindPagesPath();
        Worker = new MockBotWorker(pagesPath);
    }

    public void Dispose() { }

    [Fact]
    public async Task Radio_Template_LiteralAndMustache_AreEquivalent() {
        // Test that template="X" and template="{{ 'X' }}" produce same result
        var user = await Worker.SimulateMessageAsync(ChatId, "/attr-syntax-test");

        user.Errors.Should().BeEmpty("Page should render without errors");
        user.Messages.Should().HaveCount(1);

        var buttons = user.Messages[0].Buttons.SelectMany(r => r).Select(b => b.Text).ToList();

        // Both radios should render identically
        // radio1 uses template="[{{ self.title }}]"
        // radio2 uses template="{{ '[' + self.title + ']' }}"
        buttons.Should().Contain("[Option A]", "Literal template syntax should work");
        buttons.Should().Contain("[Option B]", "Mustache template syntax should work");
    }

    [Fact]
    public async Task Command_Title_LiteralAndMustache_AreEquivalent() {
        var user = await Worker.SimulateMessageAsync(ChatId, "/attr-syntax-test");

        user.Errors.Should().BeEmpty();

        var buttons = user.Messages[0].Buttons.SelectMany(r => r).Select(b => b.Text).ToList();

        // Both commands should have same title
        buttons.Where(b => b == "Test Button").Should().HaveCount(2,
            "title=\"Test Button\" and title=\"{{ 'Test Button' }}\" should be equivalent");
    }

    [Fact]
    public async Task Radio_TemplateWithSelf_Works() {
        var user = await Worker.SimulateMessageAsync(ChatId, "/attr-syntax-test");

        user.Errors.Should().BeEmpty();

        var buttons = user.Messages[0].Buttons.SelectMany(r => r).Select(b => b.Text).ToList();

        // self.title should be accessible in template
        buttons.Should().Contain(b => b.Contains("Option"), "self.title should be rendered in template");
    }

    [Fact]
    public void Parser_AttributeValue_PreservesMustacheSyntax() {
        var html = """
            <view id="test">
                <title>Test</title>
                <message>Test</message>
                <components>
                    <command title="{{ 'Hello' }}"></command>
                </components>
            </view>
            """;

        var registry = new ComponentRegistry();
        registry.ScanAssembly(typeof(ComponentRegistry).Assembly);

        var parser = new HtmlPageParser(registry);
        var def = parser.Parse(html);

        def.components.Should().HaveCount(1);
        var comp = def.components[0] as HtmlComponentDefinition;
        comp.Should().NotBeNull();

        // The attribute value should preserve the mustache syntax
        var titleAttr = comp!.element.GetAttribute("title");
        titleAttr.Should().Be("{{ 'Hello' }}");
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
