namespace Telegram.Bot.UI.Components.Attributes;

/// <summary>
/// Marks a property as an event handler that can be bound from HTML using @event or v-on:event syntax.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class EventAttribute : Attribute {
    /// <summary>
    /// Gets the custom HTML event name, or null to use the property name in kebab-case.
    /// </summary>
    public string? name { get; }

    /// <summary>
    /// Initializes a new instance of the EventAttribute class.
    /// </summary>
    /// <param name="name">The custom HTML event name, or null to use the property name in kebab-case.</param>
    public EventAttribute(string? name = null) => this.name = name;
}