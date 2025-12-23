namespace Telegram.Bot.UI.TextTags;

/// <summary>
/// Marks a class as a custom text tag processor with the specified tag name.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class TextTagAttribute : Attribute {
    /// <summary>
    /// Gets the HTML tag name for this text tag.
    /// </summary>
    public string tagName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextTagAttribute"/> class.
    /// </summary>
    /// <param name="tagName">The HTML tag name for this text tag.</param>
    public TextTagAttribute(string tagName) => this.tagName = tagName;
}
