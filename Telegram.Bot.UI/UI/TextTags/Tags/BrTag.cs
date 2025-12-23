using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.UI.TextTags.Tags;

/// <summary>
/// Processes br tags to insert line breaks in message text.
/// </summary>
[TextTag("br")]
public class BrTag : ITextTag {
    /// <summary>
    /// Processes a br tag and returns a newline character in HTML mode.
    /// </summary>
    /// <param name="attributes">The tag attributes.</param>
    /// <param name="innerContent">The inner content of the tag.</param>
    /// <param name="mode">The parse mode for the output.</param>
    /// <returns>A newline character in HTML mode, or empty string in other modes.</returns>
    public string Process(
        IReadOnlyDictionary<string, string> attributes,
        string? innerContent,
        ParseMode mode
    ) {
        var result = mode == ParseMode.Html ? "\n" : "";
        return result + (innerContent ?? "");
    }
}
