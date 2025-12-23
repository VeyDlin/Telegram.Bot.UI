namespace Telegram.Bot.UI.Components.Attributes;

/// <summary>
/// Marks a class as a component that can be instantiated from HTML with the specified tag name.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ComponentAttribute : Attribute {
    /// <summary>
    /// Gets the HTML tag name for this component.
    /// </summary>
    public string tagName { get; }

    /// <summary>
    /// Initializes a new instance of the ComponentAttribute class.
    /// </summary>
    /// <param name="tagName">The HTML tag name for this component.</param>
    public ComponentAttribute(string tagName) => this.tagName = tagName;
}