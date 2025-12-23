using FluentAssertions;
using Telegram.Bot.UI.Runtime;
using Xunit;

namespace Telegram.Bot.UI.Tests.Unit;

/// <summary>
/// Tests for TemplateParser - proper bracket counting parser.
/// </summary>
public class TemplateParserTests {

    #region ContainsTemplates

    [Theory]
    [InlineData("Hello {{ name }}", true)]
    [InlineData("No templates here", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("Single { brace", false)]
    [InlineData("{{ nested {{ }} }}", true)]
    public void ContainsTemplates_DetectsCorrectly(string? input, bool expected) {
        TemplateParser.ContainsTemplates(input).Should().Be(expected);
    }

    #endregion

    #region Parse - Basic Cases

    [Fact]
    public void Parse_SimpleExpression_ExtractsCorrectly() {
        // "Hello {{ name }}!"
        //  0     6        16
        var matches = TemplateParser.Parse("Hello {{ name }}!");

        matches.Should().HaveCount(1);
        matches[0].Expression.Should().Be("name");
        matches[0].Start.Should().Be(6);
        matches[0].End.Should().Be(16); // Position after }}
    }

    [Fact]
    public void Parse_MultipleExpressions_ExtractsAll() {
        var matches = TemplateParser.Parse("{{ a }} and {{ b }}");

        matches.Should().HaveCount(2);
        matches[0].Expression.Should().Be("a");
        matches[1].Expression.Should().Be("b");
    }

    [Fact]
    public void Parse_ExpressionWithSpaces_TrimsCorrectly() {
        var matches = TemplateParser.Parse("{{   spaced   }}");

        matches.Should().HaveCount(1);
        matches[0].Expression.Should().Be("spaced");
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsEmpty() {
        TemplateParser.Parse("").Should().BeEmpty();
        TemplateParser.Parse(null!).Should().BeEmpty();
    }

    [Fact]
    public void Parse_NoTemplates_ReturnsEmpty() {
        TemplateParser.Parse("Just plain text").Should().BeEmpty();
    }

    #endregion

    #region Parse - Nested Braces (the main improvement over regex)

    [Fact]
    public void Parse_NestedObjectLiteral_ExtractsCorrectly() {
        // This would FAIL with regex /.+?/
        var matches = TemplateParser.Parse("{{ { key: 'value' } }}");

        matches.Should().HaveCount(1);
        matches[0].Expression.Should().Be("{ key: 'value' }");
    }

    [Fact]
    public void Parse_NestedFunctionWithObject_ExtractsCorrectly() {
        var matches = TemplateParser.Parse("{{ items.map(x => ({ id: x.id })) }}");

        matches.Should().HaveCount(1);
        matches[0].Expression.Should().Be("items.map(x => ({ id: x.id }))");
    }

    [Fact]
    public void Parse_MultipleNestedBraces_ExtractsCorrectly() {
        var matches = TemplateParser.Parse("{{ { a: { b: { c: 1 } } } }}");

        matches.Should().HaveCount(1);
        matches[0].Expression.Should().Be("{ a: { b: { c: 1 } } }");
    }

    #endregion

    #region Parse - Strings

    [Fact]
    public void Parse_StringWithBraces_IgnoresBracesInString() {
        var matches = TemplateParser.Parse("{{ \"text with } brace\" }}");

        matches.Should().HaveCount(1);
        matches[0].Expression.Should().Be("\"text with } brace\"");
    }

    [Fact]
    public void Parse_SingleQuotedString_IgnoresBracesInString() {
        var matches = TemplateParser.Parse("{{ 'text with }} inside' }}");

        matches.Should().HaveCount(1);
        matches[0].Expression.Should().Be("'text with }} inside'");
    }

    [Fact]
    public void Parse_EscapedQuote_HandlesCorrectly() {
        var matches = TemplateParser.Parse("{{ \"escaped \\\" quote\" }}");

        matches.Should().HaveCount(1);
        matches[0].Expression.Should().Be("\"escaped \\\" quote\"");
    }

    [Fact]
    public void Parse_TemplateLiteral_HandlesCorrectly() {
        var matches = TemplateParser.Parse("{{ `template ${var}` }}");

        matches.Should().HaveCount(1);
        matches[0].Expression.Should().Be("`template ${var}`");
    }

    #endregion

    #region Parse - Ternary and Comparisons

    [Fact]
    public void Parse_TernaryOperator_ExtractsCorrectly() {
        var matches = TemplateParser.Parse("{{ a > b ? 'yes' : 'no' }}");

        matches.Should().HaveCount(1);
        matches[0].Expression.Should().Be("a > b ? 'yes' : 'no'");
    }

    [Fact]
    public void Parse_ComparisonOperators_ExtractsCorrectly() {
        var matches = TemplateParser.Parse("{{ x >= 0 && y <= 10 }}");

        matches.Should().HaveCount(1);
        matches[0].Expression.Should().Be("x >= 0 && y <= 10");
    }

    #endregion

    #region Render

    [Fact]
    public void Render_SimpleTemplate_ReplacesCorrectly() {
        var result = TemplateParser.Render(
            "Hello {{ name }}!",
            expr => expr == "name" ? "World" : "?"
        );

        result.Should().Be("Hello World!");
    }

    [Fact]
    public void Render_MultipleTemplates_ReplacesAll() {
        var result = TemplateParser.Render(
            "{{ a }} + {{ b }} = {{ c }}",
            expr => expr switch {
                "a" => "1",
                "b" => "2",
                "c" => "3",
                _ => "?"
            }
        );

        result.Should().Be("1 + 2 = 3");
    }

    [Fact]
    public void Render_NoTemplates_ReturnsOriginal() {
        var result = TemplateParser.Render("No templates", _ => "replaced");

        result.Should().Be("No templates");
    }

    [Fact]
    public void Render_EmptyInput_ReturnsEmpty() {
        TemplateParser.Render("", _ => "x").Should().Be("");
        TemplateParser.Render(null!, _ => "x").Should().BeNull();
    }

    #endregion

    #region RenderAsync

    [Fact]
    public async Task RenderAsync_SimpleTemplate_ReplacesCorrectly() {
        var result = await TemplateParser.RenderAsync(
            "Value: {{ getValue() }}",
            async expr => {
                await Task.Delay(1); // Simulate async
                return "42";
            }
        );

        result.Should().Be("Value: 42");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Parse_UnclosedTemplate_SkipsIt() {
        // Unclosed {{ should be ignored
        var matches = TemplateParser.Parse("Start {{ unclosed and {{ closed }}");

        matches.Should().HaveCount(1);
        matches[0].Expression.Should().Be("closed");
    }

    [Fact]
    public void Parse_AdjacentTemplates_ParsesBoth() {
        var matches = TemplateParser.Parse("{{ a }}{{ b }}");

        matches.Should().HaveCount(2);
        matches[0].Expression.Should().Be("a");
        matches[1].Expression.Should().Be("b");
    }

    [Fact]
    public void Parse_TemplateAtEnd_ParsesCorrectly() {
        var matches = TemplateParser.Parse("End: {{ x }}");

        matches.Should().HaveCount(1);
        matches[0].Expression.Should().Be("x");
    }

    [Fact]
    public void Parse_OnlyTemplate_ParsesCorrectly() {
        var matches = TemplateParser.Parse("{{ only }}");

        matches.Should().HaveCount(1);
        matches[0].Expression.Should().Be("only");
    }

    #endregion
}
