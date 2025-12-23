using System.Reflection;
using Telegram.Bot.UI.Components;
using Telegram.Bot.UI.Parsing;
using Telegram.Bot.UI.Runtime;
using Telegram.Bot.UI.TextTags;

namespace Telegram.Bot.UI;


/// <summary>
/// Central manager for loading, compiling, and instantiating UI pages.
/// Handles page file discovery, component/tag registration, and page lifecycle.
/// </summary>
public class PageManager {
    /// <summary>
    /// Gets the component registry containing all registered UI components.
    /// </summary>
    public ComponentRegistry registry { get; private set; } = new();

    /// <summary>
    /// Gets the text tag registry containing all registered text formatting tags.
    /// </summary>
    public TextTagRegistry textTags { get; private set; } = new();

    private Dictionary<string, CompiledPage> pages { get; set; } = new();
    private HtmlPageParser parser { get; set; }
    private string pagesPath { get; set; }
    private Assembly? vmodelAssembly { get; set; }


    /// <summary>
    /// Creates a new PageManager instance.
    /// </summary>
    /// <param name="pagesPath">Directory path containing .page files.</param>
    /// <param name="vmodelAssembly">Assembly containing custom ViewModels, components, and text tags. If null, only built-in components are available.</param>
    public PageManager(string pagesPath, Assembly? vmodelAssembly = null) {
        this.pagesPath = pagesPath;
        this.vmodelAssembly = vmodelAssembly;

        // Scan core assembly for built-in components
        registry.ScanAssembly(typeof(PageManager).Assembly);

        // Scan core assembly for built-in text tags
        textTags.ScanAssembly(typeof(PageManager).Assembly);

        // Scan vmodel assembly for custom components and text tags
        if (vmodelAssembly is not null) {
            registry.ScanAssembly(vmodelAssembly);
            textTags.ScanAssembly(vmodelAssembly);
        }

        parser = new HtmlPageParser(registry);
    }


    /// <summary>
    /// Loads all .page files from the configured pages directory recursively.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">Thrown when pages directory does not exist.</exception>
    public void LoadAll() {
        if (!Directory.Exists(pagesPath)) {
            throw new DirectoryNotFoundException($"Pages directory not found: {pagesPath}");
        }

        var files = Directory.GetFiles(pagesPath, "*.page", SearchOption.AllDirectories);

        foreach (var file in files) {
            var xml = File.ReadAllText(file);
            var definition = parser.Parse(xml);
            var compiled = Compile(definition, file);
            pages[definition.id] = compiled;
        }
    }


    /// <summary>
    /// Loads a single page from XML content.
    /// </summary>
    /// <param name="pageId">Unique identifier for the page.</param>
    /// <param name="xml">XML content of the page.</param>
    /// <param name="filePath">Optional file path for error reporting and relative asset resolution.</param>
    public void Load(string pageId, string xml, string? filePath = null) {
        var definition = parser.Parse(xml);
        var compiled = Compile(definition, filePath ?? "");
        pages[pageId] = compiled;
    }


    /// <summary>
    /// Compiles a page definition into a CompiledPage with resolved ViewModel type.
    /// </summary>
    private CompiledPage Compile(PageDefinition definition, string filePath) {
        Type? vmodelType = null;

        if (!string.IsNullOrEmpty(definition.vmodel) && vmodelAssembly is not null) {
            vmodelType = vmodelAssembly.GetType(definition.vmodel);
        }

        return new CompiledPage {
            definition = definition,
            vmodelType = vmodelType,
            filePath = filePath,
            directory = Path.GetDirectoryName(filePath) ?? ""
        };
    }


    /// <summary>
    /// Creates a new ScriptPage instance for the specified page ID.
    /// </summary>
    /// <param name="pageId">ID of the page to instantiate.</param>
    /// <param name="botUser">Bot user context for the page.</param>
    /// <param name="vmodel">Optional pre-created ViewModel. If null, ViewModel is auto-created from page definition.</param>
    /// <param name="props">Optional properties to pass to the page and ViewModel.</param>
    /// <returns>New ScriptPage instance, or null if page ID not found.</returns>
    public ScriptPage? GetPage(string pageId, BaseBotUser botUser, object? vmodel = null, Dictionary<string, object?>? props = null) {
        if (!pages.TryGetValue(pageId, out var compiled)) {
            return null;
        }

        object? resolvedVModel = vmodel;

        if (resolvedVModel is null && compiled.vmodelType is not null) {
            resolvedVModel = CreateVModel(compiled.vmodelType, botUser, props);
        }

        return new ScriptPage(botUser, compiled, this, resolvedVModel, registry, props);
    }


    /// <summary>
    /// Creates a page as MessagePage type.
    /// </summary>
    /// <param name="pageId">ID of the page to instantiate.</param>
    /// <param name="botUser">Bot user context for the page.</param>
    /// <param name="vmodel">Optional pre-created ViewModel.</param>
    /// <returns>Page as MessagePage, or null if page ID not found.</returns>
    public MessagePage? GetPageAsMessagePage(string pageId, BaseBotUser botUser, object? vmodel = null) {
        return GetPage(pageId, botUser, vmodel);
    }


    /// <summary>
    /// Creates a ViewModel instance for the specified type.
    /// Supports constructors with BaseBotUser parameter or parameterless.
    /// </summary>
    private object? CreateVModel(Type vmodelType, BaseBotUser botUser, Dictionary<string, object?>? props = null) {
        var constructors = vmodelType.GetConstructors();
        object? instance = null;

        foreach (var ctor in constructors.OrderByDescending(c => c.GetParameters().Length)) {
            var parameters = ctor.GetParameters();

            if (parameters.Length == 0) {
                instance = Activator.CreateInstance(vmodelType);
                break;
            }

            if (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(typeof(BaseBotUser))) {
                instance = Activator.CreateInstance(vmodelType, botUser);
                break;
            }
        }

        instance ??= Activator.CreateInstance(vmodelType);

        // Pass props if ViewModel implements IPropsReceiver
        if (instance is IPropsReceiver receiver && props is not null) {
            receiver.ReceiveProps(props);
        }

        // Call SetUser if available
        if (instance is not null) {
            var setUserMethod = vmodelType.GetMethod("SetUser");
            if (setUserMethod is not null) {
                var methodParams = setUserMethod.GetParameters();
                if (methodParams.Length == 1 && methodParams[0].ParameterType.IsAssignableFrom(botUser.GetType())) {
                    setUserMethod.Invoke(instance, [botUser]);
                }
            }
        }

        return instance;
    }


    /// <summary>
    /// Checks if a page with the specified ID is loaded.
    /// </summary>
    /// <param name="pageId">Page ID to check.</param>
    /// <returns>True if page exists, false otherwise.</returns>
    public bool HasPage(string pageId) => pages.ContainsKey(pageId);


    /// <summary>
    /// Gets the total number of loaded pages.
    /// </summary>
    public int pageCount => pages.Count;


    /// <summary>
    /// Gets all loaded page IDs.
    /// </summary>
    public IEnumerable<string> GetPageIds() => pages.Keys;
}


/// <summary>
/// Represents a compiled page ready for instantiation.
/// Contains parsed definition, resolved ViewModel type, and file location info.
/// </summary>
public class CompiledPage {
    /// <summary>
    /// Parsed page definition containing template, script, and metadata.
    /// </summary>
    public required PageDefinition definition { get; set; }

    /// <summary>
    /// Resolved ViewModel type from vmodel attribute, or null if not specified.
    /// </summary>
    public Type? vmodelType { get; set; }

    /// <summary>
    /// Full path to the .page file.
    /// </summary>
    public required string filePath { get; set; }

    /// <summary>
    /// Directory containing the .page file. Used for relative asset resolution.
    /// </summary>
    public required string directory { get; set; }
}
