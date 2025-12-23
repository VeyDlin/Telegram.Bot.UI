using Jint;
using Jint.Native;
using Jint.Native.Array;
using Telegram.Bot.UI.Components;
using Telegram.Bot.UI.Menu;
using Telegram.Bot.UI.Parsing;

namespace Telegram.Bot.UI.Runtime;


public class ComponentFactory {
    private ScriptPage page { get; set; }
    private ScriptContext context { get; set; }
    private PageDefinition definition { get; set; }
    private ComponentRegistry? registry { get; set; }



    public ComponentFactory(
        ScriptPage page,
        ScriptContext context,
        PageDefinition definition,
        ComponentRegistry? registry = null
    ) {
        this.page = page;
        this.context = context;
        this.definition = definition;
        this.registry = registry;
    }



    public async Task<MenuElement?> CreateAsync(ComponentDefinition compDef) {
        if (compDef is HtmlComponentDefinition html) {
            return await CreateFromHtmlAsync(html, compDef);
        }

        return null;
    }


    /// <summary>
    /// Expand components with v-for and v-if directives
    /// Returns multiple components for v-for, filters by v-if condition
    /// </summary>
    public async Task<List<MenuElement>> ExpandAndCreateAsync(List<ComponentDefinition> compDefs) {
        var result = new List<MenuElement>();
        bool previousIfWasTrue = false;

        foreach (var compDef in compDefs) {
            if (compDef is HtmlComponentDefinition html) {
                // Handle v-if / v-else-if / v-else
                if (html.vIf is not null) {
                    bool shouldRender = EvaluateVIf(html.vIf, previousIfWasTrue);
                    previousIfWasTrue = html.vIf.type == VIfType.If ? shouldRender : previousIfWasTrue;

                    if (!shouldRender) {
                        continue;
                    }
                } else {
                    // Reset if chain when no v-if directive
                    previousIfWasTrue = false;
                }

                // Handle v-for
                if (html.vFor is not null) {
                    var expandedComponents = await ExpandVForAsync(html);
                    result.AddRange(expandedComponents);
                } else {
                    var component = await CreateFromHtmlAsync(html, compDef);
                    if (component is not null) {
                        result.Add(component);
                    }
                }
            } else {
                // Other component types
                previousIfWasTrue = false;
                var component = await CreateAsync(compDef);
                if (component is not null) {
                    result.Add(component);
                }
            }
        }

        return result;
    }


    private bool EvaluateVIf(VIfDirective vIf, bool previousIfWasTrue) {
        switch (vIf.type) {
            case VIfType.If:
            return EvaluateCondition(vIf.condition!);

            case VIfType.ElseIf:
            if (previousIfWasTrue) {
                return false;  // Previous if/else-if was true, skip this
            }
            return EvaluateCondition(vIf.condition!);

            case VIfType.Else:
            return !previousIfWasTrue;  // Render only if all previous conditions were false

            default:
            return true;
        }
    }


    private bool EvaluateCondition(string condition) {
        return context.Evaluate<bool>(condition);
    }


    private async Task<List<MenuElement>> ExpandVForAsync(HtmlComponentDefinition html) {
        var result = new List<MenuElement>();
        var vFor = html.vFor!;

        // Evaluate the collection expression
        var collectionValue = context.Engine.Evaluate(vFor.expression);

        if (collectionValue.IsNull() || collectionValue.IsUndefined()) {
            return result;
        }

        // Convert to enumerable
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
        int baseRowIndex = html.rowIndex;

        foreach (var item in items) {
            // Set loop variables in context
            context.SetValue(vFor.itemName, item);
            if (vFor.indexName is not null) {
                context.SetValue(vFor.indexName, index);
            }

            // Create component with current loop context
            // Render the id template to get actual id (e.g., "item-1" from "{{ 'item-' + item.id }}")
            var renderedId = !string.IsNullOrEmpty(html.id) ? await context.RenderAsync(html.id) : $"vfor_{index}";
            var component = await CreateFromHtmlAsync(html, html, renderedId);
            if (component is not null) {
                // Freeze props to capture loop variable values before they are cleared
                // Also captures loop variable values for event handlers
                if (component is AutoComponent autoComp) {
                    var indexName = vFor.indexName ?? "index";
                    autoComp.FreezeProps(vFor.itemName, item, indexName, index);
                }

                // Each v-for item gets its own row
                component.rowIndex = baseRowIndex + index;
                result.Add(component);
            }

            index++;
        }

        // Clean up loop variables
        context.SetValue(vFor.itemName, JsValue.Undefined);
        if (vFor.indexName is not null) {
            context.SetValue(vFor.indexName, JsValue.Undefined);
        }

        return result;
    }


    private async Task<MenuElement?> CreateFromHtmlAsync(HtmlComponentDefinition html, ComponentDefinition compDef, string? overrideId = null) {
        if (registry is null) {
            return null;
        }

        var component = await registry.CreateAsync(html.tagName, html.element, context, page);
        if (component is not null) {
            component.hide = compDef.hide;
            component.columns = compDef.columns;
            component.rowIndex = compDef.rowIndex;
            component.scriptContext = context;

            var id = overrideId ?? compDef.id;
            context.RegisterComponent(id, component);

            // Initialize container components that need async setup
            if (component is MenuCard card) {
                await card.InitializeAsync();
            }
        }

        return component;
    }
}