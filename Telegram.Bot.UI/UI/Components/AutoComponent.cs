using AngleSharp.Dom;
using Jint;
using Jint.Native;
using Jint.Native.Array;
using System.Reflection;
using System.Text.RegularExpressions;
using Telegram.Bot.UI.Components.Attributes;
using Telegram.Bot.UI.Menu;
using Telegram.Bot.UI.Runtime;

namespace Telegram.Bot.UI.Components;

/// <summary>
/// Base class for auto-generated components that can be instantiated from HTML markup.
/// </summary>
public abstract class AutoComponent : MenuElement {
    private Dictionary<string, string> propTemplates = new();
    private Dictionary<string, string> eventHandlers = new();
    private Dictionary<string, string> bindings = new();
    private Dictionary<string, string> frozenProps = new();
    private Dictionary<string, object?> frozenEventContext = new();

    /// <summary>
    /// Gets the HTML element that this component was created from.
    /// </summary>
    protected IElement? htmlElement;

    /// <summary>
    /// Sets a property value with duplicate detection.
    /// </summary>
    /// <param name="propName">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <param name="source">The source of the property value.</param>
    /// <exception cref="InvalidOperationException">Thrown when the property is already set.</exception>
    private void SetPropValue(string propName, string value, string source) {
        if (propTemplates.ContainsKey(propName)) {
            throw new InvalidOperationException(
                $"Property '{propName}' is already set. Cannot set from {source}. " +
                $"Property can only be specified once (as attribute, child element, or root content)."
            );
        }
        propTemplates[propName] = value;
    }


    /// <summary>
    /// Applies the definition from an HTML element to this component.
    /// </summary>
    /// <param name="element">The HTML element to parse.</param>
    internal virtual void ApplyDefinition(IElement element) {
        htmlElement = element;
        var type = GetType();

        foreach (var prop in type.GetProperties()) {
            var propAttr = prop.GetCustomAttribute<PropAttribute>();
            if (propAttr != null) {
                var htmlName = propAttr.name ?? ToKebabCase(prop.Name);

                var bindExpr = element.GetAttribute(":" + htmlName)
                            ?? element.GetAttribute("v-bind:" + htmlName)
                            ?? element.GetAttribute("v-bind-" + htmlName);
                if (bindExpr != null) {
                    bindings[prop.Name] = bindExpr;
                    continue;
                }

                var childElement = element.QuerySelector($":scope > {htmlName}");
                string? childValue = null;
                if (childElement != null) {
                    childValue = childElement.InnerHtml;
                }

                var attrValue = element.GetAttribute(htmlName);

                string? contentValue = null;
                if (htmlName == "title") {
                    var innerContent = element.InnerHtml.Trim();
                    if (!string.IsNullOrEmpty(innerContent)) {
                        contentValue = innerContent;
                    }
                }

                if (!string.IsNullOrEmpty(childValue)) {
                    SetPropValue(prop.Name, childValue, $"child element '<{htmlName}>'");
                } else if (!string.IsNullOrEmpty(attrValue)) {
                    SetPropValue(prop.Name, attrValue, $"attribute '{htmlName}'");
                } else if (!string.IsNullOrEmpty(contentValue)) {
                    SetPropValue(prop.Name, contentValue, "root content");
                }
            }

            var eventAttr = prop.GetCustomAttribute<EventAttribute>();
            if (eventAttr != null) {
                var eventName = eventAttr.name ?? ToKebabCase(prop.Name);
                var handler = element.GetAttribute("@" + eventName)
                           ?? element.GetAttribute("v-on:" + eventName)
                           ?? element.GetAttribute("v-on-" + eventName);
                if (handler != null) {
                    eventHandlers[prop.Name] = handler;
                }
            }

            var bindAttr = prop.GetCustomAttribute<BindAttribute>();
            if (bindAttr != null) {
                var bindName = bindAttr.name ?? ToKebabCase(prop.Name);
                var expr = element.GetAttribute(":" + bindName)
                        ?? element.GetAttribute("v-bind:" + bindName)
                        ?? element.GetAttribute("v-bind-" + bindName);
                if (expr != null) {
                    bindings[prop.Name] = expr;
                }
            }
        }

        var rootContentProp = GetType()
            .GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<RootContentAttribute>() is not null);

        if (rootContentProp is not null) {
            var textContent = string.Concat(
                element.ChildNodes
                    .OfType<AngleSharp.Dom.IText>()
                    .Select(t => t.TextContent)
            ).Trim();

            if (!string.IsNullOrEmpty(textContent)) {
                SetPropValue(rootContentProp.Name, textContent, "root content");
            }
        }

        lang = element.HasAttribute("lang");
        hide = ParseBool(element.GetAttribute("hide"), false);

        var columnsAttr = element.GetAttribute("columns");
        if (!string.IsNullOrEmpty(columnsAttr)) {
            columns = ParseInt(columnsAttr, columns);
        }
    }

    /// <summary>
    /// Gets a property value, evaluating templates and bindings.
    /// </summary>
    /// <param name="propName">The property name.</param>
    /// <param name="defaultValue">The default value if the property is not set.</param>
    /// <returns>The property value.</returns>
    protected string GetProp(string propName, string defaultValue = "") {
        if (frozenProps.TryGetValue(propName, out var frozen)) {
            return lang ? botUser.L(frozen) : frozen;
        }

        if (bindings.TryGetValue(propName, out var binding)) {
            return evaluate(binding);
        }

        // Check templates (values from parsing, may contain {{ }})
        if (propTemplates.TryGetValue(propName, out var template)) {
            return renderTitle(template, lang);
        }

        var prop = GetType().GetProperty(propName);
        if (prop is not null) {
            var value = prop.GetValue(this) as string;
            if (!string.IsNullOrEmpty(value)) {
                return renderTitle(value, lang);
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Freezes all prop templates and bindings by rendering them with the current script context.
    /// Call this during v-for/auto-card expansion to capture loop variable values.
    /// After freezing, GetProp will return the pre-rendered values.
    /// </summary>
    internal void FreezeProps() {
        foreach (var (propName, template) in propTemplates) {
            if (TemplateParser.ContainsTemplates(template)) {
                frozenProps[propName] = render(template);
            }
        }

        foreach (var (propName, binding) in bindings) {
            frozenProps[propName] = evaluate(binding);
        }
    }

    /// <summary>
    /// Freezes props and also captures loop variable values for event handlers.
    /// </summary>
    internal void FreezeProps(string itemName, object? itemValue, string indexName, int indexValue) {
        FreezeProps();

        frozenEventContext[itemName] = itemValue;
        frozenEventContext[indexName] = indexValue;
    }

    /// <summary>
    /// Gets the raw binding expression without evaluating it.
    /// Use when you need to evaluate the binding yourself.
    /// </summary>
    /// <param name="propName">The property name.</param>
    /// <returns>The binding expression or null if not bound.</returns>
    protected string? GetBindingExpression(string propName) {
        if (bindings.TryGetValue(propName, out var binding)) {
            return binding;
        }
        return null;
    }

    /// <summary>
    /// Gets the raw property value without rendering templates.
    /// </summary>
    /// <param name="propName">The property name.</param>
    /// <param name="defaultValue">The default value if the property is not set.</param>
    /// <returns>The raw property value.</returns>
    protected string GetRawProp(string propName, string defaultValue = "") {
        if (frozenProps.TryGetValue(propName, out var frozen)) {
            return frozen;
        }

        if (bindings.TryGetValue(propName, out var binding)) {
            return evaluate(binding);
        }

        // Check templates (values from parsing, may contain {{ }})
        if (propTemplates.TryGetValue(propName, out var template)) {
            return template;
        }

        var prop = GetType().GetProperty(propName);
        if (prop is not null) {
            var value = prop.GetValue(this) as string;
            if (!string.IsNullOrEmpty(value)) {
                return value;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Gets a property value as a boolean.
    /// </summary>
    /// <param name="propName">The property name.</param>
    /// <param name="defaultValue">The default value if the property is not set.</param>
    /// <returns>The property value as a boolean.</returns>
    protected bool GetPropBool(string propName, bool defaultValue = false) {
        var value = GetProp(propName, "");
        if (string.IsNullOrEmpty(value)) {
            return defaultValue;
        }
        return value.ToLower() is "true" or "1" or "yes";
    }

    /// <summary>
    /// Gets a property value as an integer.
    /// </summary>
    /// <param name="propName">The property name.</param>
    /// <param name="defaultValue">The default value if the property is not set.</param>
    /// <returns>The property value as an integer.</returns>
    protected int GetPropInt(string propName, int defaultValue = 0) {
        var value = GetProp(propName, "");
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets a property value as an enum.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="propName">The property name.</param>
    /// <param name="defaultValue">The default value if the property is not set.</param>
    /// <returns>The property value as an enum.</returns>
    protected T GetPropEnum<T>(string propName, T defaultValue) where T : struct, Enum {
        var value = GetProp(propName, "");
        if (string.IsNullOrEmpty(value)) {
            return defaultValue;
        }

        // Try parse by EnumMember attribute name first
        foreach (var field in typeof(T).GetFields()) {
            var attr = field.GetCustomAttribute<EnumMemberAttribute>();
            if (attr?.Name == value) {
                return (T)field.GetValue(null)!;
            }
        }

        if (Enum.TryParse<T>(value, true, out var result)) {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// Determines whether the component has an event handler for the specified event.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <returns>True if the event handler exists; otherwise, false.</returns>
    protected bool HasEvent(string eventName) => eventHandlers.ContainsKey(eventName);

    /// <summary>
    /// Invokes an event handler with optional event arguments.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <param name="eventArgs">The event arguments.</param>
    protected async Task InvokeEvent(string eventName, object? eventArgs = null) {
        if (!eventHandlers.TryGetValue(eventName, out var handler)) {
            return;
        }
        if (scriptContext is null) {
            return;
        }

        foreach (var (varName, varValue) in frozenEventContext) {
            scriptContext.SetValue(varName, varValue);
        }

        if (eventArgs is not null) {
            scriptContext.SetValue("event", eventArgs);

            var callbackProp = eventArgs.GetType().GetProperty("callbackQueryId");
            if (callbackProp is not null) {
                scriptContext.SetValue("callbackQueryId", callbackProp.GetValue(eventArgs));
            }
        }
        await scriptContext.ExecuteAsync(handler);
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

    /// <summary>
    /// Parses a boolean value from a string attribute.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="defaultValue">The default value to return if parsing fails.</param>
    /// <returns>The parsed boolean value or the default value.</returns>
    private static bool ParseBool(string? value, bool defaultValue) {
        if (string.IsNullOrEmpty(value)) {
            return defaultValue;
        }
        return value.ToLower() is "true" or "1" or "yes";
    }

    /// <summary>
    /// Parses an integer value from a string attribute.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="defaultValue">The default value to return if parsing fails.</param>
    /// <returns>The parsed integer value or the default value.</returns>
    private static int ParseInt(string? value, int defaultValue) {
        if (string.IsNullOrEmpty(value)) {
            return defaultValue;
        }
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Parses child option elements from the HTML element, supporting v-for directive for dynamic option generation.
    /// </summary>
    /// <returns>A list of <see cref="OptionInfo"/> objects.</returns>
    protected List<OptionInfo> ParseOptions() {
        if (htmlElement is null) {
            return new();
        }

        var result = new List<OptionInfo>();
        var optionElements = htmlElement.QuerySelectorAll(":scope > option");

        foreach (var opt in optionElements) {
            var vForAttr = opt.GetAttribute("v-for");
            if (!string.IsNullOrEmpty(vForAttr) && scriptContext is not null) {
                var expanded = ExpandVForOptions(opt, vForAttr);
                result.AddRange(expanded);
            } else {
                result.Add(ParseSingleOption(opt));
            }
        }

        return result;
    }

    /// <summary>
    /// Expands a v-for directive on an option element into multiple option info objects.
    /// </summary>
    /// <param name="opt">The option element containing the v-for directive.</param>
    /// <param name="vForExpr">The v-for expression.</param>
    /// <returns>A list of <see cref="OptionInfo"/> objects.</returns>
    private List<OptionInfo> ExpandVForOptions(IElement opt, string vForExpr) {
        var result = new List<OptionInfo>();

        var match = Regex.Match(
            vForExpr,
            @"^\s*(?:\(?\s*(\w+)\s*(?:,\s*(\w+))?\s*\)?)\s+in\s+(.+)$"
        );

        if (!match.Success || scriptContext is null) {
            return result;
        }

        var itemName = match.Groups[1].Value;
        var indexName = string.IsNullOrEmpty(match.Groups[2].Value) ? null : match.Groups[2].Value;
        var expression = match.Groups[3].Value.Trim();

        var collectionValue = scriptContext.Engine.Evaluate(expression);

        if (collectionValue.IsNull() || collectionValue.IsUndefined()) {
            return result;
        }

        IEnumerable<object?> items;
        if (collectionValue is ArrayInstance arr) {
            items = arr.Select(v => v.ToObject()).ToList();
        } else {
            var obj = collectionValue.ToObject();
            if (obj is System.Collections.IEnumerable enumerable) {
                items = enumerable.Cast<object?>().ToList();
            } else {
                return result;
            }
        }

        int index = 0;
        foreach (var item in items) {
            scriptContext.SetValue(itemName, item);
            if (indexName is not null) {
                scriptContext.SetValue(indexName, index);
            }

            result.Add(ParseSingleOption(opt));

            index++;
        }

        scriptContext.SetValue(itemName, JsValue.Undefined);
        if (indexName is not null) {
            scriptContext.SetValue(indexName, JsValue.Undefined);
        }

        return result;
    }

    /// <summary>
    /// Parses a single option element into an option info object.
    /// </summary>
    /// <param name="opt">The option element to parse.</param>
    /// <returns>An <see cref="OptionInfo"/> object.</returns>
    private OptionInfo ParseSingleOption(IElement opt) {
        bool? webPreview = null;
        var webPreviewAttr = opt.GetAttribute("webPreview");
        if (webPreviewAttr != null) {
            webPreview = webPreviewAttr.ToLower() is "true" or "1" or "yes";
        }

        var titleValue = opt.GetAttribute("title");
        var titleBinding = opt.GetAttribute(":title") ?? opt.GetAttribute("v-bind:title");
        if (titleBinding is not null && scriptContext is not null) {
            var evaluated = scriptContext.Engine.Evaluate(titleBinding);
            titleValue = evaluated.IsNull() || evaluated.IsUndefined() ? "" : evaluated.ToString();
        }

        var idValue = opt.GetAttribute("value");
        var valueBinding = opt.GetAttribute(":value") ?? opt.GetAttribute("v-bind:value");
        if (valueBinding is not null && scriptContext is not null) {
            var evaluated = scriptContext.Engine.Evaluate(valueBinding);
            idValue = evaluated.IsNull() || evaluated.IsUndefined() ? "" : evaluated.ToString();
        }

        return new OptionInfo {
            id = idValue ?? "",
            title = titleValue ?? "",
            lang = opt.HasAttribute("lang"),
            pageResource = opt.GetAttribute("pageResource"),
            messageResource = opt.GetAttribute("messageResource"),
            webPreview = webPreview
        };
    }

    /// <summary>
    /// Represents information about an option element.
    /// </summary>
    protected record OptionInfo {
        /// <summary>
        /// Gets or initializes the option ID.
        /// </summary>
        public string id { get; init; } = "";

        /// <summary>
        /// Gets or initializes the option title.
        /// </summary>
        public string title { get; init; } = "";

        /// <summary>
        /// Gets or initializes a value indicating whether the title should be translated.
        /// </summary>
        public bool lang { get; init; } = false;

        /// <summary>
        /// Gets or initializes the page resource path.
        /// </summary>
        public string? pageResource { get; init; } = null;

        /// <summary>
        /// Gets or initializes the message resource path.
        /// </summary>
        public string? messageResource { get; init; } = null;

        /// <summary>
        /// Gets or initializes a value indicating whether web preview is enabled.
        /// </summary>
        public bool? webPreview { get; init; } = null;
    }

    /// <summary>
    /// Gets the content from a child element by name.
    /// </summary>
    /// <param name="elementName">The name of the child element.</param>
    /// <returns>The inner HTML of the child element, or null if the element doesn't exist.</returns>
    protected string? GetChildElementContent(string elementName) {
        if (htmlElement is null) {
            return null;
        }

        var child = htmlElement.QuerySelector($":scope > {elementName}");
        if (child is not null) {
            return child.InnerHtml;
        }

        return null;
    }
}