using AngleSharp.Dom;
using System.Reflection;
using Telegram.Bot.UI.Components.Attributes;

namespace Telegram.Bot.UI.Parsing;

/// <summary>
/// Represents a page definition parsed from HTML markup.
/// </summary>
public class PageDefinition {
    /// <summary>
    /// Gets or sets the unique identifier for the page.
    /// </summary>
    [Prop] public required string id { get; set; }

    /// <summary>
    /// Gets or sets the resource path for the page content.
    /// </summary>
    [Prop] public string? resource { get; set; }

    /// <summary>
    /// Gets or sets the view model binding expression.
    /// </summary>
    [Prop] public string? vmodel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether web preview is enabled for links.
    /// </summary>
    [Prop("web-preview")] public bool webPreview { get; set; } = true;

    /// <summary>
    /// Gets or sets the custom title for the back button.
    /// </summary>
    [Prop("back-title")] public string? backTitle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether navigation should return to parent page.
    /// </summary>
    [Prop("back-to-parent")] public bool backToParent { get; set; } = true;

    /// <summary>
    /// Gets or sets the title definition for the page.
    /// </summary>
    public TitleDefinition? title { get; set; }

    /// <summary>
    /// Gets or sets the media definition for the page.
    /// </summary>
    public MediaDefinition? media { get; set; }

    /// <summary>
    /// Gets or sets the message definition for the page.
    /// </summary>
    public MessageDefinition? message { get; set; }

    /// <summary>
    /// Gets or sets the list of component definitions for the page.
    /// </summary>
    public List<ComponentDefinition> components { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of menu page definitions for multi-page menus.
    /// </summary>
    public List<MenuPageDefinition>? menuPages { get; set; }

    /// <summary>
    /// Gets or sets the navigation component that appears at the bottom of the page.
    /// </summary>
    public ComponentDefinition? navigate { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items for auto-pagination.
    /// </summary>
    public int? maxItems { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of rows for auto-pagination.
    /// </summary>
    public int? maxRows { get; set; }

    /// <summary>
    /// Gets or sets the script definition for the page.
    /// </summary>
    public ScriptDefinition? script { get; set; }


    /// <summary>
    /// Apply props from HTML attributes and child elements
    /// </summary>
    public void ApplyProps(IElement element) {
        foreach (var prop in GetType().GetProperties()) {
            var propAttr = prop.GetCustomAttribute<PropAttribute>();
            if (propAttr is null) {
                continue;
            }

            var htmlName = propAttr.name ?? ToKebabCase(prop.Name);

            // Check attribute first
            var attrValue = element.GetAttribute(htmlName);

            // Check child element
            var childElement = element.QuerySelector($":scope > {htmlName}");
            var childValue = childElement?.TextContent.Trim();

            var value = attrValue ?? childValue;
            if (value is null) {
                continue;
            }

            // Set property based on type
            if (prop.PropertyType == typeof(string)) {
                prop.SetValue(this, value);
            } else if (prop.PropertyType == typeof(bool)) {
                prop.SetValue(this, value.ToLower() is "true" or "1" or "yes");
            } else if (prop.PropertyType == typeof(int)) {
                if (int.TryParse(value, out var intVal)) {
                    prop.SetValue(this, intVal);
                }
            }
        }
    }


    /// <summary>
    /// Converts a property name from PascalCase to kebab-case.
    /// </summary>
    /// <param name="name">The property name to convert.</param>
    /// <returns>The kebab-case representation of the property name.</returns>
    private static string ToKebabCase(string name) {
        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "-" + char.ToLower(c) : char.ToLower(c).ToString()));
    }
}

/// <summary>
/// Represents a page title definition.
/// </summary>
public class TitleDefinition {
    /// <summary>
    /// Gets or sets the title content.
    /// </summary>
    public string content { get; set; } = "";

    /// <summary>
    /// Gets or sets a value indicating whether the title should be translated using language resources.
    /// </summary>
    public bool lang { get; set; } = false;
}

/// <summary>
/// Represents a message definition for a page.
/// </summary>
public class MessageDefinition {
    /// <summary>
    /// Gets or sets the resource path to load message content from.
    /// </summary>
    public string? loadResource { get; set; }

    /// <summary>
    /// Gets or sets the inline message content.
    /// </summary>
    public string? inlineContent { get; set; }

    /// <summary>
    /// Gets or sets the list of conditional templates for dynamic message content.
    /// </summary>
    public List<TemplateCondition>? conditions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether markdown mode is enabled.
    /// </summary>
    public bool md { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether newlines should be preserved.
    /// </summary>
    public bool pre { get; set; } = false;

    /// <summary>
    /// Gets or sets the URL expression for wallpaper preview.
    /// </summary>
    public string? wallpaperUrl { get; set; }
}

/// <summary>
/// Defines the types of media that can be included in a page.
/// </summary>
public enum MediaType { Photo, Document, Audio, Video }

/// <summary>
/// Represents a media definition for a page.
/// </summary>
public class MediaDefinition {
    /// <summary>
    /// Gets or sets the media type.
    /// </summary>
    public MediaType type { get; set; }

    /// <summary>
    /// Gets or sets the media source path or file ID.
    /// </summary>
    public string src { get; set; } = "";
}

/// <summary>
/// Represents a conditional template for dynamic content rendering.
/// </summary>
public class TemplateCondition {
    /// <summary>
    /// Gets or sets the condition expression.
    /// </summary>
    public string condition { get; set; } = "";

    /// <summary>
    /// Gets or sets the content to render when the condition is true.
    /// </summary>
    public string content { get; set; } = "";
}

/// <summary>
/// Represents a script definition for a page.
/// </summary>
public class ScriptDefinition {
    /// <summary>
    /// Gets or sets the JavaScript code to execute.
    /// </summary>
    public string code { get; set; } = "";
}

/// <summary>
/// Base class for component definitions.
/// </summary>
public abstract class ComponentDefinition {
    /// <summary>
    /// Gets or sets the unique identifier for the component.
    /// </summary>
    public required string id { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the component text should be translated using language resources.
    /// </summary>
    public bool lang { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the component should be hidden.
    /// </summary>
    public bool hide { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of columns for component layout.
    /// </summary>
    public int columns { get; set; } = 3;

    /// <summary>
    /// Gets or sets the row index for component positioning.
    /// </summary>
    public int rowIndex { get; set; } = 0;
}


/// <summary>
/// Component definition using AngleSharp HTML element.
/// </summary>
public class HtmlComponentDefinition : ComponentDefinition {
    /// <summary>
    /// Gets or sets the HTML tag name of the component.
    /// </summary>
    public required string tagName { get; set; }

    /// <summary>
    /// Gets or sets the AngleSharp element representing the component.
    /// </summary>
    public required IElement element { get; set; }

    /// <summary>
    /// Gets or sets the v-for directive for list rendering.
    /// </summary>
    public VForDirective? vFor { get; set; }

    /// <summary>
    /// Gets or sets the v-if directive for conditional rendering.
    /// </summary>
    public VIfDirective? vIf { get; set; }

    /// <summary>
    /// Gets or sets the slots defined in this component.
    /// </summary>
    public Dictionary<string, SlotDefinition> slots { get; set; } = [];
}


/// <summary>
/// Represents a v-for directive: v-for="(item, index) in items".
/// </summary>
public class VForDirective {
    /// <summary>
    /// Gets or sets the name of the item variable in the loop.
    /// </summary>
    public required string itemName { get; set; }

    /// <summary>
    /// Gets or sets the name of the index variable in the loop.
    /// </summary>
    public string? indexName { get; set; }

    /// <summary>
    /// Gets or sets the expression that evaluates to the collection to iterate over.
    /// </summary>
    public required string expression { get; set; }

    /// <summary>
    /// Gets or sets the key expression for item tracking.
    /// </summary>
    public string? key { get; set; }
}


/// <summary>
/// Represents a v-if / v-else-if / v-else directive.
/// </summary>
public class VIfDirective {
    /// <summary>
    /// Gets or sets the condition expression for the directive.
    /// </summary>
    public string? condition { get; set; }

    /// <summary>
    /// Gets or sets the type of conditional directive.
    /// </summary>
    public VIfType type { get; set; }
}

/// <summary>
/// Defines the types of v-if conditional directives.
/// </summary>
public enum VIfType { If, ElseIf, Else }


/// <summary>
/// Represents a slot definition from a template element.
/// </summary>
public class SlotDefinition {
    /// <summary>
    /// Gets or sets the name of the slot.
    /// </summary>
    public required string name { get; set; }

    /// <summary>
    /// Gets or sets the list of properties passed to the slot.
    /// </summary>
    public List<string> props { get; set; } = [];

    /// <summary>
    /// Gets or sets the template element for the slot.
    /// </summary>
    public required IElement template { get; set; }
}

/// <summary>
/// Represents a menu page definition for multi-page menus.
/// </summary>
public class MenuPageDefinition {
    /// <summary>
    /// Gets or sets the list of component definitions for the menu page.
    /// </summary>
    public List<ComponentDefinition> components { get; set; } = [];
}