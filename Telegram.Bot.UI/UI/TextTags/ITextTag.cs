using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.UI.TextTags;

/// <summary>
/// Defines a custom text tag that can process HTML-like markup into formatted text.
/// </summary>
public interface ITextTag {
    /// <summary>
    /// Processes a custom text tag and returns the formatted output.
    /// </summary>
    /// <param name="attributes">The tag attributes as key-value pairs.</param>
    /// <param name="innerContent">The inner content of the tag, or null if the tag is empty.</param>
    /// <param name="mode">The parse mode for the output (HTML or Markdown).</param>
    /// <returns>The processed text output.</returns>
    string Process(
        IReadOnlyDictionary<string, string> attributes,
        string? innerContent,
        ParseMode mode
    );
}
