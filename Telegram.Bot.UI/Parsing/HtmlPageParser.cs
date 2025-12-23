using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Telegram.Bot.UI.Components;

namespace Telegram.Bot.UI.Parsing;

/// <summary>
/// Provides functionality to parse HTML markup into page definitions.
/// </summary>
public class HtmlPageParser {
    private readonly ComponentRegistry? registry;
    private readonly HtmlParser parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlPageParser"/> class.
    /// </summary>
    public HtmlPageParser() {
        parser = new HtmlParser();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlPageParser"/> class with a component registry.
    /// </summary>
    /// <param name="registry">The component registry for resolving custom components.</param>
    public HtmlPageParser(ComponentRegistry registry) : this() {
        this.registry = registry;
    }

    /// <summary>
    /// Parses HTML markup into a page definition.
    /// </summary>
    /// <param name="html">The HTML markup to parse.</param>
    /// <returns>A <see cref="PageDefinition"/> representing the parsed page.</returns>
    /// <exception cref="Exception">Thrown when the HTML does not contain a valid view element or page ID.</exception>
    public PageDefinition Parse(string html) {
        if (!html.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase)) {
            html = $"<html><body>{html}</body></html>";
        }

        var doc = parser.ParseDocument(html);
        var root = doc.QuerySelector("view")
            ?? throw new Exception("Invalid page: no <view> element found");

        var page = new PageDefinition {
            id = root.GetAttribute("id") ?? throw new Exception("Page must have 'id' attribute")
        };

        // Auto-apply [Prop] attributes from element
        page.ApplyProps(root);

        foreach (var child in root.Children) {
            switch (child.TagName.ToLower()) {
                case "title":
                page.title = ParseTitle(child);
                break;
                case "photo":
                page.media = new MediaDefinition { type = MediaType.Photo, src = child.GetAttribute("src") ?? "" };
                break;
                case "document":
                page.media = new MediaDefinition { type = MediaType.Document, src = child.GetAttribute("src") ?? "" };
                break;
                case "audio":
                page.media = new MediaDefinition { type = MediaType.Audio, src = child.GetAttribute("src") ?? "" };
                break;
                case "video":
                page.media = new MediaDefinition { type = MediaType.Video, src = child.GetAttribute("src") ?? "" };
                break;
                case "message":
                page.message = ParseMessage(child);
                break;
                case "components":
                case "menu":
                page.components = ParseComponents(child);
                page.menuPages = ParseMenuPages(child);
                page.maxItems = ParseNullableInt(child.GetAttribute("max-items"));
                page.maxRows = ParseNullableInt(child.GetAttribute("max-rows"));
                break;
                case "script":
                ParseScript(child, page);
                break;
            }
        }

        return page;
    }


    /// <summary>
    /// Parses a title element into a title definition.
    /// </summary>
    /// <param name="element">The title element to parse.</param>
    /// <returns>A <see cref="TitleDefinition"/> representing the parsed title.</returns>
    private TitleDefinition ParseTitle(IElement element) {
        return new TitleDefinition {
            content = element.TextContent.Trim(),
            lang = element.HasAttribute("lang")
        };
    }

    /// <summary>
    /// Parses a message element into a message definition.
    /// </summary>
    /// <param name="element">The message element to parse.</param>
    /// <returns>A <see cref="MessageDefinition"/> representing the parsed message.</returns>
    public static MessageDefinition ParseMessage(IElement element) {
        var message = new MessageDefinition {
            md = element.HasAttribute("md"),
            pre = element.HasAttribute("pre")
        };

        // Check for resource attribute
        var resourceAttr = element.GetAttribute("resource");
        if (!string.IsNullOrEmpty(resourceAttr)) {
            message.loadResource = resourceAttr;
            return message;
        }

        // Check for <load> child
        var loadElement = element.QuerySelector("load");
        if (loadElement is not null) {
            message.loadResource = loadElement.GetAttribute("resource");
            return message;
        }

        var templates = element.QuerySelectorAll("template").ToList();
        if (templates.Count > 0) {
            message.conditions = templates.Select(t => new TemplateCondition {
                condition = t.GetAttribute("v-if") ?? t.GetAttribute("v-else-if") ?? (t.HasAttribute("v-else") ? "true" : "true"),
                content = t.InnerHtml.Trim()
            }).ToList();
            return message;
        }

        message.inlineContent = element.InnerHtml.Trim();
        return message;
    }

    /// <summary>
    /// Parses components from a container element.
    /// </summary>
    /// <param name="element">The container element containing components.</param>
    /// <returns>A list of <see cref="ComponentDefinition"/> objects.</returns>
    private List<ComponentDefinition> ParseComponents(IElement element) {
        var components = new List<ComponentDefinition>();

        var menuPages = element.QuerySelectorAll(":scope > page").ToList();
        if (menuPages.Count > 0) {
            return components;
        }

        int rowIndex = 0;

        foreach (var child in element.Children) {
            var tagName = child.TagName.ToLower();

            if (tagName == "row") {
                foreach (var rowChild in child.Children) {
                    var component = ParseComponent(rowChild);
                    if (component is not null) {
                        component.rowIndex = rowIndex;
                        components.Add(component);
                    }
                }
                rowIndex++;
            } else {
                var component = ParseComponent(child);
                if (component is not null) {
                    component.rowIndex = rowIndex;
                    components.Add(component);
                    rowIndex++;
                }
            }
        }

        return components;
    }

    /// <summary>
    /// Parses menu pages from a container element for multi-page menu support.
    /// </summary>
    /// <param name="element">The container element containing menu pages.</param>
    /// <returns>A list of <see cref="MenuPageDefinition"/> objects, or null if no pages are found.</returns>
    private List<MenuPageDefinition>? ParseMenuPages(IElement element) {
        var pageElements = element.QuerySelectorAll(":scope > page").ToList();
        if (pageElements.Count == 0) {
            return null;
        }

        var pages = new List<MenuPageDefinition>();

        foreach (var pageElement in pageElements) {
            var page = new MenuPageDefinition();
            int rowIndex = 0;

            foreach (var child in pageElement.Children) {
                var tagName = child.TagName.ToLower();

                if (tagName == "row") {
                    foreach (var rowChild in child.Children) {
                        var component = ParseComponent(rowChild);
                        if (component is not null) {
                            component.rowIndex = rowIndex;
                            page.components.Add(component);
                        }
                    }
                    rowIndex++;
                } else {
                    var component = ParseComponent(child);
                    if (component is not null) {
                        component.rowIndex = rowIndex;
                        page.components.Add(component);
                        rowIndex++;
                    }
                }
            }

            pages.Add(page);
        }

        return pages;
    }

    /// <summary>
    /// Parses a single component element into a component definition.
    /// </summary>
    /// <param name="element">The element to parse.</param>
    /// <returns>A <see cref="ComponentDefinition"/> object, or null if the element is not recognized.</returns>
    private ComponentDefinition? ParseComponent(IElement element) {
        var tagName = element.TagName.ToLower();
        var id = element.GetAttribute("id") ?? Guid.NewGuid().ToString();

        if (registry is null || !registry.HasComponent(tagName)) {
            return null;
        }

        var def = new HtmlComponentDefinition {
            id = id,
            tagName = tagName,
            element = element,
            vFor = ParseVFor(element),
            vIf = ParseVIf(element)
        };

        def.lang = element.HasAttribute("lang");
        def.hide = ParseBool(element.GetAttribute("hide"), false);

        var columnsAttr = element.GetAttribute("columns");
        if (!string.IsNullOrEmpty(columnsAttr)) {
            def.columns = ParseInt(columnsAttr, def.columns);
        }

        return def;
    }

    /// <summary>
    /// Parses a v-for directive from an element.
    /// </summary>
    /// <param name="element">The element containing the v-for directive.</param>
    /// <returns>A <see cref="VForDirective"/> object, or null if no v-for directive is found.</returns>
    /// <exception cref="Exception">Thrown when the v-for syntax is invalid.</exception>
    private VForDirective? ParseVFor(IElement element) {
        var vFor = element.GetAttribute("v-for");
        if (string.IsNullOrEmpty(vFor)) {
            return null;
        }

        var match = System.Text.RegularExpressions.Regex.Match(vFor,
            @"^\s*\(?\s*(\w+)\s*(?:,\s*(\w+))?\s*\)?\s+in\s+(.+)$");

        if (!match.Success) {
            throw new Exception($"Invalid v-for syntax: {vFor}");
        }

        return new VForDirective {
            itemName = match.Groups[1].Value,
            indexName = match.Groups[2].Success ? match.Groups[2].Value : null,
            expression = match.Groups[3].Value.Trim(),
            key = element.GetAttribute(":key") ?? element.GetAttribute("v-bind:key")
        };
    }

    /// <summary>
    /// Parses v-if, v-else-if, or v-else directives from an element.
    /// </summary>
    /// <param name="element">The element containing the directive.</param>
    /// <returns>A <see cref="VIfDirective"/> object, or null if no directive is found.</returns>
    private VIfDirective? ParseVIf(IElement element) {
        var vIf = element.GetAttribute("v-if");
        if (!string.IsNullOrEmpty(vIf)) {
            return new VIfDirective { condition = vIf, type = VIfType.If };
        }

        var vElseIf = element.GetAttribute("v-else-if");
        if (!string.IsNullOrEmpty(vElseIf)) {
            return new VIfDirective { condition = vElseIf, type = VIfType.ElseIf };
        }

        if (element.HasAttribute("v-else")) {
            return new VIfDirective { condition = null, type = VIfType.Else };
        }

        return null;
    }

    /// <summary>
    /// Parses a script element and adds it to the page definition.
    /// </summary>
    /// <param name="element">The script element to parse.</param>
    /// <param name="page">The page definition to add the script to.</param>
    private void ParseScript(IElement element, PageDefinition page) {
        var code = element.TextContent.Trim();

        if (page.script is null) {
            page.script = new ScriptDefinition { code = code };
        } else {
            page.script.code += "\n" + code;
        }
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
    /// Parses a nullable integer value from a string attribute.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <returns>The parsed integer value, or null if parsing fails or the value is empty.</returns>
    private static int? ParseNullableInt(string? value) {
        if (string.IsNullOrEmpty(value)) {
            return null;
        }
        return int.TryParse(value, out var result) ? result : null;
    }
}