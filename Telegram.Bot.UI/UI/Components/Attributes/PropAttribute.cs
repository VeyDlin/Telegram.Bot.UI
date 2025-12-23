namespace Telegram.Bot.UI.Components.Attributes;

/// <summary>
/// Marks a property as bindable from HTML attributes, child elements, or element content.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PropAttribute : Attribute {
    /// <summary>
    /// Gets the custom HTML attribute/element name, or null to use the property name in kebab-case.
    /// </summary>
    public string? name { get; }

    /// <summary>
    /// Initializes a new instance of the PropAttribute class.
    /// </summary>
    /// <param name="name">The custom HTML attribute/element name, or null to use the property name in kebab-case.</param>
    public PropAttribute(string? name = null) => this.name = name;
}