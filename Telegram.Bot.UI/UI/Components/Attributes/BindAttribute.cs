namespace Telegram.Bot.UI.Components.Attributes;

/// <summary>
/// Marks a property as supporting reactive data binding from HTML attributes using :prop or v-bind:prop syntax.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class BindAttribute : Attribute {
    /// <summary>
    /// Gets the custom HTML attribute name for this binding, or null to use the property name in kebab-case.
    /// </summary>
    public string? name { get; }

    /// <summary>
    /// Initializes a new instance of the BindAttribute class.
    /// </summary>
    /// <param name="name">The custom HTML attribute name, or null to use the property name in kebab-case.</param>
    public BindAttribute(string? name = null) => this.name = name;
}