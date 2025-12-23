namespace Telegram.Bot.UI.Components.Attributes;

/// <summary>
/// Specifies a custom string representation for an enum field, used during property parsing.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class EnumMemberAttribute : Attribute {
    /// <summary>
    /// Gets the custom string name for this enum member.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the EnumMemberAttribute class.
    /// </summary>
    /// <param name="name">The custom string name for this enum member.</param>
    public EnumMemberAttribute(string name) => Name = name;
}