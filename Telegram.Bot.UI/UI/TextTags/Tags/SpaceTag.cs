using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.UI.TextTags.Tags;

/// <summary>
/// Processes space tags to insert one or more space characters in HTML mode.
/// </summary>
[TextTag("space")]
public class SpaceTag : ITextTag {
    /// <summary>
    /// Processes a space tag and returns the specified number of space characters in HTML mode.
    /// </summary>
    /// <param name="attributes">The tag attributes (supports 'count' attribute to specify number of spaces).</param>
    /// <param name="innerContent">The inner content of the tag.</param>
    /// <param name="mode">The parse mode for the output.</param>
    /// <returns>Space characters in HTML mode, or empty string in other modes.</returns>
    public string Process(
        IReadOnlyDictionary<string, string> attributes,
        string? innerContent,
        ParseMode mode
    ) {
        string result;
        if (mode != ParseMode.Html) {
            result = "";
        } else {
            var count = 1;
            if (attributes.TryGetValue("count", out var countStr)) {
                int.TryParse(countStr, out count);
            }
            result = new string(' ', count);
        }

        return result + (innerContent ?? "");
    }
}
