namespace Telegram.Bot.UI.Components.Attributes;

/// <summary>
/// Marks a property as the root content property that receives the direct text content of an element.
/// For example, in &lt;command&gt;Click me&lt;/command&gt;, the text "Click me" will be assigned to this property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class RootContentAttribute : Attribute {
}