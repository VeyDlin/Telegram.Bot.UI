using System.Text.RegularExpressions;

namespace Telegram.Bot.UI.Parsing;

/// <summary>
/// Provides XML preprocessing capabilities to convert Vue-like syntax into valid XML attributes.
/// </summary>
public static partial class XmlPreprocessor {
    /// <summary>
    /// Regex for matching event binding syntax (@attr="value").
    /// </summary>
    [GeneratedRegex(@"\s@([a-zA-Z][a-zA-Z0-9]*)\s*=")]
    private static partial Regex EventBindingRegex();

    /// <summary>
    /// Regex for matching value binding syntax (:attr="value").
    /// </summary>
    [GeneratedRegex(@"\s:([a-zA-Z][a-zA-Z0-9]*)\s*=")]
    private static partial Regex ValueBindingRegex();

    /// <summary>
    /// Regex for matching boolean attributes without values.
    /// Matches attributes like v-else, md, pre, lang, hide followed by whitespace, />, or > but NOT followed by =.
    /// </summary>
    [GeneratedRegex(@"\s(v-else|md|pre|lang|hide)(?=\s|/?>)")]
    private static partial Regex BooleanAttributeRegex();

    /// <summary>
    /// Processes XML markup by converting Vue-like shorthand syntax into valid XML attributes.
    /// Transforms @attr into v-on-attr, :attr into v-bind-attr, and boolean attributes into explicit key="true".
    /// </summary>
    /// <param name="xml">The XML markup to process.</param>
    /// <returns>The processed XML markup with expanded attribute syntax.</returns>
    public static string Process(string xml) {
        xml = EventBindingRegex().Replace(xml, " v-on-$1=");
        xml = ValueBindingRegex().Replace(xml, " v-bind-$1=");
        xml = BooleanAttributeRegex().Replace(xml, " $1=\"true\"");

        return xml;
    }
}