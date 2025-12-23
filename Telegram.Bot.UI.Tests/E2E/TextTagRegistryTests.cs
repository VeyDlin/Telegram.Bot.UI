using FluentAssertions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.UI.TextTags;

namespace Telegram.Bot.UI.Tests.E2E;

/// <summary>
/// Tests for TextTagRegistry processing.
/// Tests verify exact string output from the full registry processing.
/// </summary>
public class TextTagRegistryTests {
    // Zero-width joiner character (U+200D)
    private const char ZeroWidthJoiner = '\u200D';
    private readonly TextTagRegistry registry;

    public TextTagRegistryTests() {
        registry = new TextTagRegistry();
        registry.ScanAssembly(typeof(TextTagRegistry).Assembly);
    }

    [Fact]
    public async Task Registry_ProcessesBrTag() {
        // Note: In HTML5, <br/> is parsed as <br>World</br> since custom tags aren't void
        // The tag preserves inner content
        var html = "Hello<br/>World";
        var result = await registry.ProcessContentAsync(html, ParseMode.Html, null);

        result.Should().Be("Hello\nWorld");
    }

    [Fact]
    public async Task Registry_ProcessesBrTagWithExplicitClosing() {
        // In HTML5, <br> is a void element, so </br> is treated as another <br> element
        // This results in two newlines
        var html = "Hello<br></br>World";
        var result = await registry.ProcessContentAsync(html, ParseMode.Html, null);

        // <br> creates \n, </br> is parsed as another <br> creating another \n
        result.Should().Be("Hello\n\nWorld");
    }

    [Fact]
    public async Task Registry_ProcessesSpaceTag() {
        var html = "Hello<space count=\"3\"/>World";
        var result = await registry.ProcessContentAsync(html, ParseMode.Html, null);

        result.Should().Be("Hello   World");
    }

    [Fact]
    public async Task Registry_ProcessesTabTag() {
        var html = "Hello<tab/>World";
        var result = await registry.ProcessContentAsync(html, ParseMode.Html, null);

        result.Should().Be("Hello\tWorld");
    }

    [Fact]
    public async Task Registry_ProcessesWallpaperTag() {
        var html = "<wallpaper url=\"https://example.com/img.jpg\"/>Hello";
        var result = await registry.ProcessContentAsync(html, ParseMode.Html, null);

        result.Should().Be($"<a href=\"https://example.com/img.jpg\">{ZeroWidthJoiner}</a>Hello");
    }

    [Fact]
    public async Task Registry_ProcessesWallpaperTagWithClosingTag() {
        var html = "<wallpaper url=\"https://example.com/img.jpg\"></wallpaper>Hello";
        var result = await registry.ProcessContentAsync(html, ParseMode.Html, null);

        result.Should().Be($"<a href=\"https://example.com/img.jpg\">{ZeroWidthJoiner}</a>Hello");
    }

    [Fact]
    public async Task Registry_ProcessesMultipleTags() {
        var html = "Hello<br/>World<space count=\"3\"/>!";
        var result = await registry.ProcessContentAsync(html, ParseMode.Html, null);

        result.Should().Be("Hello\nWorld   !");
    }

    [Fact]
    public async Task Registry_ProcessesTagsInMarkdownMode() {
        var html = "<wallpaper url=\"https://example.com/img.jpg\"/>Hello<br/>World";
        var result = await registry.ProcessContentAsync(html, ParseMode.Markdown, null);

        // In Markdown mode, wallpaper creates markdown link, br returns empty (but preserves content)
        result.Should().Be("[ ](https://example.com/img.jpg)HelloWorld");
    }

    [Fact]
    public async Task Registry_PreservesNormalContent() {
        var html = "<b>Bold</b> and <i>italic</i> text";
        var result = await registry.ProcessContentAsync(html, ParseMode.Html, null);

        result.Should().Be("<b>Bold</b> and <i>italic</i> text");
    }

    [Fact]
    public async Task Registry_HasTag_ReturnsTrueForRegisteredTags() {
        registry.HasTag("wallpaper").Should().BeTrue();
        registry.HasTag("br").Should().BeTrue();
        registry.HasTag("space").Should().BeTrue();
        registry.HasTag("tab").Should().BeTrue();
    }

    [Fact]
    public async Task Registry_HasTag_ReturnsFalseForUnknownTags() {
        registry.HasTag("unknown").Should().BeFalse();
        registry.HasTag("custom").Should().BeFalse();
    }

    [Fact]
    public async Task Registry_HandlesEmptyContent() {
        var result = await registry.ProcessContentAsync("", ParseMode.Html, null);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Registry_HandlesNullContent() {
        var result = await registry.ProcessContentAsync(null!, ParseMode.Html, null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Registry_GetRegisteredTags_ReturnsAllTags() {
        var tags = registry.GetRegisteredTags().ToList();

        tags.Should().Contain("wallpaper");
        tags.Should().Contain("br");
        tags.Should().Contain("space");
        tags.Should().Contain("tab");
    }
}
