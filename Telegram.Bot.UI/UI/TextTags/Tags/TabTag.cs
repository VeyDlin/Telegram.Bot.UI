using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.UI.TextTags.Tags;

/// <summary>
/// Processes tab tags to insert one or more tab characters in HTML mode.
/// </summary>
[TextTag("tab")]
public class TabTag : ITextTag {
    /// <summary>
    /// Processes a tab tag and returns the specified number of tab characters in HTML mode.
    /// </summary>
    /// <param name="attributes">The tag attributes (supports 'count' attribute to specify number of tabs).</param>
    /// <param name="innerContent">The inner content of the tag.</param>
    /// <param name="mode">The parse mode for the output.</param>
    /// <returns>Tab characters in HTML mode, or empty string in other modes.</returns>
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
            result = new string('\t', count);
        }

        return result + (innerContent ?? "");
    }
}
