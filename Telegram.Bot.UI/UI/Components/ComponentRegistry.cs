using AngleSharp.Dom;
using System.Reflection;
using Telegram.Bot.UI.Components.Attributes;
using Telegram.Bot.UI.Runtime;

namespace Telegram.Bot.UI.Components;

/// <summary>
/// Registry for discovering and creating component instances based on HTML tag names.
/// </summary>
public class ComponentRegistry {
    private Dictionary<string, Type> components = new();

    /// <summary>
    /// Scans an assembly for components decorated with ComponentAttribute and registers them.
    /// </summary>
    /// <param name="assembly">The assembly to scan for components.</param>
    public void ScanAssembly(Assembly assembly) {
        var componentTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<ComponentAttribute>() != null)
            .Where(t => typeof(AutoComponent).IsAssignableFrom(t))
            .Where(t => !t.IsAbstract);

        foreach (var type in componentTypes) {
            var attr = type.GetCustomAttribute<ComponentAttribute>()!;
            components[attr.tagName] = type;
        }
    }


    /// <summary>
    /// Creates a component instance from an HTML element and initializes it with the provided context.
    /// </summary>
    /// <param name="tagName">The HTML tag name to create.</param>
    /// <param name="element">The HTML element to parse.</param>
    /// <param name="context">The script context for evaluating expressions.</param>
    /// <param name="page">The parent script page.</param>
    /// <returns>The created component instance, or null if the tag is not registered.</returns>
    public async Task<AutoComponent?> CreateAsync(string tagName, IElement element,
                                  ScriptContext context, ScriptPage page) {
        if (!components.TryGetValue(tagName, out var type)) {
            return null;
        }

        var component = (AutoComponent)Activator.CreateInstance(type)!;
        component.parent = page;
        component.botUser = page.botUser;
        component.scriptContext = context;
        component.ApplyDefinition(element);

        // Call InitializeAsync if the component has it
        var initMethod = type.GetMethod("InitializeAsync");
        if (initMethod is not null) {
            await (Task)initMethod.Invoke(component, null)!;
        }

        return component;
    }



    /// <summary>
    /// Determines whether a component is registered for the specified tag name.
    /// </summary>
    /// <param name="tagName">The HTML tag name to check.</param>
    /// <returns>True if the tag is registered; otherwise, false.</returns>
    public bool HasComponent(string tagName) => components.ContainsKey(tagName);



    /// <summary>
    /// Gets all registered component tag names.
    /// </summary>
    /// <returns>A collection of registered tag names.</returns>
    public IEnumerable<string> GetRegisteredTags() => components.Keys;
}