using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.UI.Utils;

/// <summary>
/// Provides text normalization utilities for Telegram messages.
/// </summary>
public static class TextNormalizer {
    /// <summary>
    /// Creates a hidden link for wallpaper preview using zero-width joiner trick.
    /// Used for legacy wallpaperUrl attribute support. For inline wallpaper tags, use the TextTag system instead.
    /// </summary>
    /// <param name="url">The wallpaper image URL.</param>
    /// <param name="parseMode">The Telegram parse mode to use.</param>
    /// <returns>A formatted hidden link string.</returns>
    public static string FormatWallpaperLink(string url, ParseMode parseMode) {
        if (string.IsNullOrEmpty(url)) {
            return "";
        }
        if (parseMode == ParseMode.Markdown) {
            return $"[ ]({url})";
        }
        return $"<a href=\"{url}\">\u200D</a>";
    }
}