using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.UI.TextTags.Tags;

/// <summary>
/// Processes p (paragraph) tags to insert double line breaks in message text.
/// </summary>
[TextTag("p")]
public class PTag : ITextTag {
    /// <summary>
    /// Processes a p tag and returns double newline (paragraph break) in HTML mode.
    /// </summary>
    /// <param name="attributes">The tag attributes.</param>
    /// <param name="innerContent">The inner content of the tag.</param>
    /// <param name="mode">The parse mode for the output.</param>
    /// <returns>Double newline in HTML mode, or empty string in other modes.</returns>
    public string Process(
        IReadOnlyDictionary<string, string> attributes,
        string? innerContent,
        ParseMode mode
    ) {
        var newline = mode == ParseMode.Html ? "\n" : "";
        return (innerContent ?? "") + newline;
    }
}
