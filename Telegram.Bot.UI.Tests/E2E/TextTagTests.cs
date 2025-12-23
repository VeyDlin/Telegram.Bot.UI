using FluentAssertions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.UI.TextTags.Tags;

namespace Telegram.Bot.UI.Tests.E2E;

/// <summary>
/// Unit tests for TextTag system.
/// Tests verify exact string output, not "contains".
/// </summary>
public class TextTagTests {
    // Zero-width joiner character (U+200D)
    private const char ZeroWidthJoiner = '\u200D';

    #region WallpaperTag Tests

    [Fact]
    public void WallpaperTag_InHtmlMode_CreatesHiddenLink() {
        var tag = new WallpaperTag();
        var result = tag.Process(
            new Dictionary<string, string> { ["url"] = "https://example.com/img.jpg" },
            null,
            ParseMode.Html
        );

        result.Should().Be($"<a href=\"https://example.com/img.jpg\">{ZeroWidthJoiner}</a>");
    }

    [Fact]
    public void WallpaperTag_InMarkdownMode_CreatesMarkdownLink() {
        var tag = new WallpaperTag();
        var result = tag.Process(
            new Dictionary<string, string> { ["url"] = "https://example.com/img.jpg" },
            null,
            ParseMode.Markdown
        );

        result.Should().Be("[ ](https://example.com/img.jpg)");
    }

    [Fact]
    public void WallpaperTag_InMarkdownV2Mode_CreatesMarkdownLink() {
        var tag = new WallpaperTag();
        var result = tag.Process(
            new Dictionary<string, string> { ["url"] = "https://example.com/img.jpg" },
            null,
            ParseMode.MarkdownV2
        );

        result.Should().Be("[ ](https://example.com/img.jpg)");
    }

    [Fact]
    public void WallpaperTag_WithoutUrl_ReturnsEmpty() {
        var tag = new WallpaperTag();
        var result = tag.Process(
            new Dictionary<string, string>(),
            null,
            ParseMode.Html
        );

        result.Should().BeEmpty();
    }

    [Fact]
    public void WallpaperTag_WithEmptyUrl_ReturnsEmpty() {
        var tag = new WallpaperTag();
        var result = tag.Process(
            new Dictionary<string, string> { ["url"] = "" },
            null,
            ParseMode.Html
        );

        result.Should().BeEmpty();
    }

    #endregion

    #region BrTag Tests

    [Fact]
    public void BrTag_InHtmlMode_ReturnsNewline() {
        var tag = new BrTag();
        var result = tag.Process(new Dictionary<string, string>(), null, ParseMode.Html);

        result.Should().Be("\n");
    }

    [Fact]
    public void BrTag_InMarkdownMode_ReturnsEmpty() {
        var tag = new BrTag();
        var result = tag.Process(new Dictionary<string, string>(), null, ParseMode.Markdown);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BrTag_InMarkdownV2Mode_ReturnsEmpty() {
        var tag = new BrTag();
        var result = tag.Process(new Dictionary<string, string>(), null, ParseMode.MarkdownV2);

        result.Should().BeEmpty();
    }

    #endregion

    #region SpaceTag Tests

    [Fact]
    public void SpaceTag_InHtmlMode_ReturnsSingleSpace() {
        var tag = new SpaceTag();
        var result = tag.Process(new Dictionary<string, string>(), null, ParseMode.Html);

        result.Should().Be(" ");
    }

    [Fact]
    public void SpaceTag_WithCount_ReturnsCorrectSpaces() {
        var tag = new SpaceTag();
        var result = tag.Process(
            new Dictionary<string, string> { ["count"] = "5" },
            null,
            ParseMode.Html
        );

        result.Should().Be("     ");
    }

    [Fact]
    public void SpaceTag_WithCount3_Returns3Spaces() {
        var tag = new SpaceTag();
        var result = tag.Process(
            new Dictionary<string, string> { ["count"] = "3" },
            null,
            ParseMode.Html
        );

        result.Should().Be("   ");
    }

    [Fact]
    public void SpaceTag_InMarkdownMode_ReturnsEmpty() {
        var tag = new SpaceTag();
        var result = tag.Process(
            new Dictionary<string, string> { ["count"] = "5" },
            null,
            ParseMode.Markdown
        );

        result.Should().BeEmpty();
    }

    #endregion

    #region TabTag Tests

    [Fact]
    public void TabTag_InHtmlMode_ReturnsSingleTab() {
        var tag = new TabTag();
        var result = tag.Process(new Dictionary<string, string>(), null, ParseMode.Html);

        result.Should().Be("\t");
    }

    [Fact]
    public void TabTag_WithCount_ReturnsCorrectTabs() {
        var tag = new TabTag();
        var result = tag.Process(
            new Dictionary<string, string> { ["count"] = "3" },
            null,
            ParseMode.Html
        );

        result.Should().Be("\t\t\t");
    }

    [Fact]
    public void TabTag_InMarkdownMode_ReturnsEmpty() {
        var tag = new TabTag();
        var result = tag.Process(
            new Dictionary<string, string> { ["count"] = "3" },
            null,
            ParseMode.Markdown
        );

        result.Should().BeEmpty();
    }

    #endregion
}
