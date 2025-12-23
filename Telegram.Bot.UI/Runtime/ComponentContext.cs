using Jint.Native;

namespace Telegram.Bot.UI.Runtime;


/// <summary>
/// Isolated context for component rendering.
/// Each component gets its own scope with access to parent and global context.
/// </summary>
public class ComponentContext {
    private readonly ComponentContext? parent;
    private readonly Dictionary<string, object?> local = new();

    public ComponentContext(ScriptContext global, ComponentContext? parent = null) {
        Global = global;
        this.parent = parent;
    }


    /// <summary>
    /// Set a local variable in this component's scope
    /// </summary>
    public void Set(string name, object? value) {
        local[name] = value;
    }


    /// <summary>
    /// Set 'self' to an anonymous object with component's local state
    /// </summary>
    public void SetSelf(object selfObject) {
        local["self"] = selfObject;
    }


    /// <summary>
    /// Resolve a variable by name. Checks local scope first, then parent, then global.
    /// </summary>
    public object? Resolve(string name) {
        // 1. Check local scope
        if (local.TryGetValue(name, out var value)) {
            return value;
        }

        // 2. Check parent scope (for slot context)
        if (parent is not null) {
            var parentValue = parent.Resolve(name);
            if (parentValue is not null) {
                return parentValue;
            }
        }

        // 3. Fall back to global JS context
        return Global.GetValue(name);
    }


    /// <summary>
    /// Render a template string with this component's isolated scope.
    /// Supports await in {{ }} expressions.
    /// </summary>
    public async Task<string> RenderAsync(string template) {
        if (string.IsNullOrEmpty(template)) {
            return template;
        }

        // Set up local variables in JS engine temporarily
        var engine = Global.Engine;
        var savedValues = new Dictionary<string, JsValue>();

        try {
            // Save and override local variables
            foreach (var (name, value) in local) {
                try {
                    savedValues[name] = engine.GetValue(name);
                } catch (Jint.Runtime.JavaScriptException) {
                    // Variable doesn't exist in engine - this is expected for new local variables
                    savedValues[name] = JsValue.Undefined;
                }
                engine.SetValue(name, value);
            }

            // Use global render with our local scope set
            return await Global.RenderAsync(template);
        } finally {
            // Restore original values
            foreach (var (name, saved) in savedValues) {
                engine.SetValue(name, saved);
            }
        }
    }


    /// <summary>
    /// Evaluate a JavaScript expression with this component's scope
    /// </summary>
    public object? Evaluate(string expression) {
        if (string.IsNullOrEmpty(expression)) {
            return null;
        }

        var engine = Global.Engine;
        var savedValues = new Dictionary<string, JsValue>();

        try {
            // Save and override local variables
            foreach (var (name, value) in local) {
                try {
                    savedValues[name] = engine.GetValue(name);
                } catch (Jint.Runtime.JavaScriptException) {
                    // Variable doesn't exist in engine - this is expected for new local variables
                    savedValues[name] = JsValue.Undefined;
                }
                engine.SetValue(name, value);
            }

            var result = engine.Evaluate(expression);
            return result.ToObject();
        } finally {
            // Restore original values
            foreach (var (name, saved) in savedValues) {
                engine.SetValue(name, saved);
            }
        }
    }


    /// <summary>
    /// Evaluate a JavaScript expression as boolean
    /// </summary>
    public bool EvaluateBool(string? expression) {
        if (string.IsNullOrEmpty(expression)) {
            return true;
        }

        var result = Evaluate(expression);
        return result switch {
            bool b => b,
            int i => i != 0,
            double d => d != 0,
            string s => !string.IsNullOrEmpty(s),
            null => false,
            _ => true
        };
    }


    /// <summary>
    /// Create a child context for nested components (slots)
    /// </summary>
    public ComponentContext CreateChild() {
        return new ComponentContext(Global, parent: this);
    }


    /// <summary>
    /// Access to global ScriptContext for component registration etc.
    /// </summary>
    public ScriptContext Global { get; }
}