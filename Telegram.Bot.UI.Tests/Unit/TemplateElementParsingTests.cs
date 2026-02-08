using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using FluentAssertions;
using Telegram.Bot.UI.Parsing;

namespace Telegram.Bot.UI.Tests.Unit;

/// <summary>
/// Tests for template element parsing in HtmlPageParser.
/// HTML5 template elements store content in a DocumentFragment (Content property).
/// </summary>
public class TemplateElementParsingTests {

    [Fact]
    public void Template_ContentProperty_ContainsChildNodes() {
        // HTML5 template elements store content in Content, not in regular children
        var parser = new HtmlParser();
        var html = @"
            <html><body>
                <template id=""test"">
                    <b>Hello World</b>
                </template>
            </body></html>";

        var doc = parser.ParseDocument(html);
        var template = doc.QuerySelector("template") as IHtmlTemplateElement;

        template.Should().NotBeNull();

        // Content property should have the child nodes
        var content = template!.Content;
        content.Should().NotBeNull();
        var contentHtml = content!.ToHtml().Trim();
        contentHtml.Should().Contain("Hello World", $"Content.ToHtml() should contain 'Hello World', got: '{contentHtml}'");
    }

    [Fact]
    public void HtmlPageParser_ParseMessage_ExtractsTemplateConditionsCorrectly() {
        var parser = new HtmlParser();
        var html = @"
            <html><body>
                <message>
                    <template v-if=""VModel.isLoading"">
                        <b>Loading...</b>
                    </template>
                    <template v-else-if=""VModel.hasResult"">
                        Result: {{ VModel.resultText }}
                    </template>
                    <template v-else>
                        <b>Default State</b>
                    </template>
                </message>
            </body></html>";

        var doc = parser.ParseDocument(html);
        var messageElement = doc.QuerySelector("message");
        messageElement.Should().NotBeNull();

        var message = HtmlPageParser.ParseMessage(messageElement!);

        message.conditions.Should().NotBeNull();
        message.conditions.Should().HaveCount(3);

        // First template: v-if="VModel.isLoading"
        message.conditions![0].condition.Should().Be("VModel.isLoading");
        message.conditions[0].content.Should().Contain("Loading");

        // Second template: v-else-if="VModel.hasResult"
        message.conditions[1].condition.Should().Be("VModel.hasResult");
        message.conditions[1].content.Should().Contain("{{ VModel.resultText }}");

        // Third template: v-else
        message.conditions[2].condition.Should().Be("true");
        message.conditions[2].content.Should().Contain("Default State");
    }

    [Fact]
    public void HtmlPageParser_ParseMessage_HandlesEmptyTemplate() {
        var parser = new HtmlParser();
        var html = @"
            <html><body>
                <message>
                    <template v-if=""VModel.isEmpty"">
                    </template>
                    <template v-else>
                        <b>Has Content</b>
                    </template>
                </message>
            </body></html>";

        var doc = parser.ParseDocument(html);
        var messageElement = doc.QuerySelector("message");

        var message = HtmlPageParser.ParseMessage(messageElement!);

        message.conditions.Should().HaveCount(2);
        // Empty template should have empty content (or whitespace-only)
        message.conditions![0].content.Should().BeEmpty();
        message.conditions[1].content.Should().Contain("Has Content");
    }

    [Fact]
    public void HtmlPageParser_ParseMessage_PreservesComplexExpressions() {
        var parser = new HtmlParser();
        var html = @"
            <html><body>
                <message>
                    <template v-if=""VModel.count > 0"">
                        Count: {{ VModel.count }}
                        Name: {{ VModel.getName() }}
                    </template>
                </message>
            </body></html>";

        var doc = parser.ParseDocument(html);
        var messageElement = doc.QuerySelector("message");

        var message = HtmlPageParser.ParseMessage(messageElement!);

        message.conditions.Should().HaveCount(1);
        message.conditions![0].condition.Should().Be("VModel.count > 0");
        message.conditions[0].content.Should().Contain("{{ VModel.count }}");
        message.conditions[0].content.Should().Contain("{{ VModel.getName() }}");
    }
}
