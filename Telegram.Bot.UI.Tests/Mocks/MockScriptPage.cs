using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Telegram.Bot.UI.Components;
using Telegram.Bot.UI.Menu;
using Telegram.Bot.UI.Runtime;

namespace Telegram.Bot.UI.Tests.Mocks;

/// <summary>
/// Mock implementation of MessagePage for testing components.
/// Components don't actually need a real ScriptPage - they just need
/// a parent MessagePage and a ScriptContext.
/// </summary>
public class MockScriptPage : MessagePage {
    public ScriptContext Context { get; }
    public ComponentRegistry Registry { get; }
    public List<MenuElement> CreatedComponents { get; } = new();
    private readonly HtmlParser parser = new();

    public MockScriptPage(BaseBotUser botUser) : base(botUser) {
        Context = new ScriptContext(botUser);
        // Don't call SetPage - it requires ScriptPage, not MessagePage
        // Components will work fine without page navigation functions

        Registry = new ComponentRegistry();
        Registry.ScanAssembly(typeof(MenuCheckbox).Assembly);
    }

    public override string? title => "Test Page";

    /// <summary>
    /// Create a component from XML/HTML string and initialize it
    /// </summary>
    public async Task<T?> CreateComponent<T>(string html) where T : AutoComponent {
        var element = ParseElement(html);
        if (element == null) {
            return null;
        }

        var tagName = element.TagName.ToLower();

        // Create the component using registry
        var component = await CreateComponentInternal(tagName, element);
        if (component is T typed) {
            return typed;
        }

        return null;
    }

    /// <summary>
    /// Create a component from IElement
    /// </summary>
    public async Task<AutoComponent?> CreateComponent(IElement element) {
        var tagName = element.TagName.ToLower();
        return await CreateComponentInternal(tagName, element);
    }

    private IElement? ParseElement(string html) {
        // Wrap in html/body if needed
        if (!html.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase)) {
            html = $"<html><body>{html}</body></html>";
        }

        var doc = parser.ParseDocument(html);
        // Get the first element in body (skip text nodes)
        return doc.Body?.Children.FirstOrDefault();
    }

    private async Task<AutoComponent?> CreateComponentInternal(string tagName, IElement element) {
        if (!Registry.HasComponent(tagName)) {
            return null;
        }

        // Create component manually since we can't use ComponentRegistry.CreateAsync
        // (it requires ScriptPage)
        var type = GetComponentType(tagName);
        if (type == null) {
            return null;
        }

        var component = (AutoComponent)Activator.CreateInstance(type)!;
        component.parent = this;
        component.botUser = botUser;
        component.scriptContext = Context;
        component.ApplyDefinition(element);

        // Register component
        var id = element.GetAttribute("id") ?? Guid.NewGuid().ToString();
        Context.RegisterComponent(id, component);
        CreatedComponents.Add(component);

        // Call InitializeAsync if available
        var initMethod = type.GetMethod("InitializeAsync");
        if (initMethod != null) {
            await (Task)initMethod.Invoke(component, null)!;
        }

        return component;
    }

    private Type? GetComponentType(string tagName) {
        // Get component type from registry using reflection
        var registryType = typeof(ComponentRegistry);
        var componentsField = registryType.GetField("components",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (componentsField?.GetValue(Registry) is Dictionary<string, Type> components) {
            return components.TryGetValue(tagName, out var type) ? type : null;
        }

        return null;
    }

    public override Task<List<ButtonsPage>?> RequestPageComponentsAsync() {
        return Task.FromResult<List<ButtonsPage>?>(null);
    }

    protected override Task<string?> BuildTextTemplate() {
        return Task.FromResult<string?>("Test message");
    }

    protected override void OnDispose() {
        foreach (var component in CreatedComponents) {
            component.Dispose();
        }
        CreatedComponents.Clear();
        Context.Dispose();
    }
}