using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System.Reflection;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.UI.Runtime;

namespace Telegram.Bot.UI.TextTags;

/// <summary>
/// Registry for discovering and processing custom text tags.
/// </summary>
public class TextTagRegistry {
    private readonly Dictionary<string, ITextTag> tags = new();

    /// <summary>
    /// Scans an assembly for text tag classes decorated with <see cref="TextTagAttribute"/> and registers them.
    /// </summary>
    /// <param name="assembly">The assembly to scan for text tags.</param>
    public void ScanAssembly(Assembly assembly) {
        var tagTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<TextTagAttribute>() is not null)
            .Where(t => typeof(ITextTag).IsAssignableFrom(t))
            .Where(t => !t.IsAbstract);

        foreach (var type in tagTypes) {
            var attr = type.GetCustomAttribute<TextTagAttribute>()!;
            var instance = (ITextTag)Activator.CreateInstance(type)!;
            tags[attr.tagName.ToLower()] = instance;
        }
    }

    /// <summary>
    /// Determines whether a text tag is registered for the specified tag name.
    /// </summary>
    /// <param name="tagName">The tag name to check.</param>
    /// <returns>True if the tag is registered; otherwise, false.</returns>
    public bool HasTag(string tagName) => tags.ContainsKey(tagName.ToLower());

    /// <summary>
    /// Gets all registered text tag names.
    /// </summary>
    /// <returns>A collection of registered tag names.</returns>
    public IEnumerable<string> GetRegisteredTags() => tags.Keys;

    /// <summary>
    /// Processes HTML content by applying all registered text tags.
    /// </summary>
    /// <param name="content">The HTML content to process.</param>
    /// <param name="mode">The parse mode for the output (HTML or Markdown).</param>
    /// <param name="context">The script context for evaluating template expressions.</param>
    /// <returns>The processed HTML content.</returns>
    public async Task<string> ProcessContentAsync(
        string content,
        ParseMode mode,
        ScriptContext? context
    ) {
        if (string.IsNullOrEmpty(content)) {
            return content;
        }

        var parser = new HtmlParser();
        var doc = parser.ParseDocument($"<body>{content}</body>");
        var container = doc.Body!;

        foreach (var (tagName, tag) in tags) {
            await ProcessTagAsync(container, tagName, tag, mode, context);
        }

        return container.InnerHtml;
    }

    /// <summary>
    /// Processes all occurrences of a specific tag within a container element.
    /// </summary>
    /// <param name="container">The container element to process.</param>
    /// <param name="tagName">The tag name to process.</param>
    /// <param name="tag">The text tag processor.</param>
    /// <param name="mode">The parse mode for the output.</param>
    /// <param name="context">The script context for evaluating template expressions.</param>
    private async Task ProcessTagAsync(
        IElement container,
        string tagName,
        ITextTag tag,
        ParseMode mode,
        ScriptContext? context
    ) {
        var elements = container.QuerySelectorAll(tagName).ToList();

        foreach (var element in elements) {
            var attributes = new Dictionary<string, string>();
            foreach (var attr in element.Attributes) {
                var value = attr.Value;

                if (context is not null && TemplateParser.ContainsTemplates(value)) {
                    value = await context.RenderAsync(value);
                }

                attributes[attr.Name.ToLower()] = value;
            }

            var innerContent = element.InnerHtml;
            if (string.IsNullOrWhiteSpace(innerContent)) {
                innerContent = null;
            }

            var result = tag.Process(attributes, innerContent, mode);

            element.OuterHtml = result;
        }
    }
}
