using System.Text.RegularExpressions;

namespace Telegram.Bot.UI.Parsing;

public static partial class SelfClosingTagPreprocessor {
    private static readonly HashSet<string> htmlVoidElements = new(StringComparer.OrdinalIgnoreCase) {
        "area", "base", "br", "col", "embed", "hr",
        "img", "input", "link", "meta", "source", "track", "wbr"
    };

    // Matches: <TagName attributes... / >
    // Handles quoted attribute values that may contain '>' or '/'
    [GeneratedRegex(
        @"<(\w[\w-]*)((?:\s+(?:[^""'>/]|""[^""]*""|'[^']*')*?)?)\s*\/\s*>",
        RegexOptions.Singleline
    )]
    private static partial Regex SelfClosingTagRegex();

    public static string Process(string html) {
        return SelfClosingTagRegex().Replace(html, match => {
            var tagName = match.Groups[1].Value;
            if (htmlVoidElements.Contains(tagName)) {
                return match.Value;
            }
            var attributes = match.Groups[2].Value;
            return $"<{tagName}{attributes}></{tagName}>";
        });
    }
}
