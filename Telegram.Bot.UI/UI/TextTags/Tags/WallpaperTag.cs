using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.UI.TextTags.Tags;

/// <summary>
/// Processes wallpaper tags to create invisible links that display custom chat backgrounds in Telegram.
/// </summary>
[TextTag("wallpaper")]
public class WallpaperTag : ITextTag {
    /// <summary>
    /// Zero-width joiner character (U+200D) used for creating invisible links.
    /// </summary>
    private const char ZeroWidthJoiner = '\u200D';

    /// <summary>
    /// Processes a wallpaper tag and returns an invisible link to the wallpaper URL.
    /// </summary>
    /// <param name="attributes">The tag attributes (requires 'url' attribute with wallpaper URL).</param>
    /// <param name="innerContent">The inner content of the tag.</param>
    /// <param name="mode">The parse mode for the output.</param>
    /// <returns>An invisible link to the wallpaper in the appropriate format for the parse mode.</returns>
    public string Process(
        IReadOnlyDictionary<string, string> attributes,
        string? innerContent,
        ParseMode mode
    ) {
        if (!attributes.TryGetValue("url", out var url) || string.IsNullOrEmpty(url)) {
            return innerContent ?? "";
        }

        var result = mode switch {
            ParseMode.Markdown or ParseMode.MarkdownV2 => $"[ ]({url})",
            _ => $"<a href=\"{url}\">{ZeroWidthJoiner}</a>"
        };

        return result + (innerContent ?? "");
    }
}
