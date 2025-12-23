using Jint;
using Jint.Native;
using Microsoft.Extensions.Logging;
using System.Net;
using Telegram.Bot.UI.Menu;

namespace Telegram.Bot.UI.Runtime;


/// <summary>
/// JavaScript execution context for ScriptPage.
/// Provides JS engine, global functions, lifecycle hooks, and template rendering.
/// </summary>
/// <remarks>
/// Key features:
/// - Jint JavaScript engine with CLR interop
/// - Global functions: navigate, refresh, alert, toast, component()
/// - Lifecycle hooks: onMounted, onUnmounted, beforeRender, afterRender
/// - Template rendering with {{ expression }} syntax
/// - Props passing between pages
/// </remarks>
public partial class ScriptContext : IDisposable {
    #region Fields

    private Engine engine { get; set; }
    private Dictionary<string, MenuElement> components { get; set; } = new();
    private BaseBotUser botUser { get; set; }
    private object? vmodel { get; set; }
    private ScriptPage? page { get; set; }
    private Dictionary<string, object?>? pageProps { get; set; }

    #endregion


    #region Properties

    /// <summary>
    /// Gets the Jint JavaScript engine instance.
    /// </summary>
    public Engine Engine => engine;

    /// <summary>
    /// Flag indicating navigation occurred. When true, skips page update after event.
    /// </summary>
    public bool navigated { get; set; } = false;

    /// <summary>
    /// Returns true if page has photo handlers registered.
    /// </summary>
    public bool HasPhotoHandler => onPhotoCallbacks.Count > 0;

    /// <summary>
    /// Returns true if page has document handlers registered.
    /// </summary>
    public bool HasDocumentHandler => onDocumentCallbacks.Count > 0;

    #endregion


    #region Callbacks

    private List<Func<object?>> onMountedCallbacks = new();
    private List<Func<object?>> onUnmountedCallbacks = new();
    private List<Func<object?>> beforeRenderCallbacks = new();
    private List<Func<object?>> afterRenderCallbacks = new();
    private List<Func<object?>> onRefreshCallbacks = new();
    private List<Func<object, object?>> onPhotoCallbacks = new();
    private List<Func<object, object?>> onDocumentCallbacks = new();

    #endregion


    #region Constructor

    /// <summary>
    /// Creates a new ScriptContext.
    /// </summary>
    /// <param name="botUser">Bot user context.</param>
    /// <param name="vmodel">Optional ViewModel to expose as VModel in JavaScript.</param>
    public ScriptContext(BaseBotUser botUser, object? vmodel = null) {
        this.botUser = botUser;
        this.vmodel = vmodel;

        engine = new Engine(options => {
            options.AllowClr(
                typeof(BaseBotUser).Assembly,
                typeof(Task).Assembly,
                typeof(MenuElement).Assembly  // For card, radio, etc. components
            );
        });

        SetupGlobals();
        SetProps(null);
    }

    #endregion


    #region Page and Props

    /// <summary>
    /// Sets the page reference for refresh/navigate functions.
    /// </summary>
    public void SetPage(ScriptPage scriptPage) {
        this.page = scriptPage;
        uiNamespace?.SetPage(scriptPage);

        // Base = current page context (title, parent, etc.)
        engine.SetValue("Base", scriptPage);
    }


    /// <summary>
    /// Sets props for the current page
    /// </summary>
    public void SetProps(Dictionary<string, object?>? props) {
        pageProps = props;
        if (props != null && props.Count > 0) {
            // Convert C# Dictionary to JavaScript object
            var jsObject = engine.Intrinsics.Object.Construct(Array.Empty<JsValue>());
            foreach (var kvp in props) {
                var value = ConvertToJsValue(kvp.Value);
                jsObject.Set(kvp.Key, value);
            }
            engine.SetValue("props", jsObject);
        } else {
            // Set empty JavaScript object
            engine.SetValue("props", engine.Intrinsics.Object.Construct(Array.Empty<JsValue>()));
        }
    }


    #endregion


    #region Value Conversion

    /// <summary>
    /// Converts C# value to Jint JsValue.
    /// </summary>
    private JsValue ConvertToJsValue(object? value) {
        if (value == null) {
            return JsValue.Null;
        }

        // Handle dictionaries (nested objects)
        if (value is Dictionary<string, object?> dict) {
            var jsObj = engine.Intrinsics.Object.Construct(Array.Empty<JsValue>());
            foreach (var kvp in dict) {
                jsObj.Set(kvp.Key, ConvertToJsValue(kvp.Value));
            }
            return jsObj;
        }

        // Handle lists/arrays
        if (value is System.Collections.IList list) {
            var items = new JsValue[list.Count];
            for (int i = 0; i < list.Count; i++) {
                items[i] = ConvertToJsValue(list[i]);
            }
            return engine.Intrinsics.Array.Construct(items);
        }

        // Handle primitive types - let Jint handle the conversion
        return JsValue.FromObject(engine, value);
    }


    #endregion


    #region Lifecycle Invocation

    /// <summary>
    /// Awaits a callback result if it's a Task.
    /// </summary>
    private async Task AwaitIfTask(object? result) {
        if (result is Task task) {
            await task;
        }
    }


    /// <summary>
    /// Calls all onMounted callbacks. Supports sync and async.
    /// </summary>
    public async Task InvokeMounted() {
        foreach (var callback in onMountedCallbacks) {
            try {
                var result = callback();
                await AwaitIfTask(result);
            } catch (Exception ex) {
                HandleError(ex);
            }
        }
    }


    /// <summary>
    /// Call all onUnmounted callbacks (supports sync and async)
    /// </summary>
    public async Task InvokeUnmounted() {
        foreach (var callback in onUnmountedCallbacks) {
            try {
                var result = callback();
                await AwaitIfTask(result);
            } catch (Exception ex) {
                HandleError(ex);
            }
        }
    }


    /// <summary>
    /// Call all beforeRender callbacks (supports sync and async)
    /// </summary>
    public async Task InvokeBeforeRender() {
        foreach (var callback in beforeRenderCallbacks) {
            try {
                var result = callback();
                await AwaitIfTask(result);
            } catch (Exception ex) {
                HandleError(ex);
            }
        }
    }


    /// <summary>
    /// Call all afterRender callbacks (supports sync and async)
    /// </summary>
    public async Task InvokeAfterRender() {
        foreach (var callback in afterRenderCallbacks) {
            try {
                var result = callback();
                await AwaitIfTask(result);
            } catch (Exception ex) {
                HandleError(ex);
            }
        }
    }


    /// <summary>
    /// Call all onRefresh callbacks (supports sync and async)
    /// </summary>
    public async Task InvokeRefresh() {
        foreach (var callback in onRefreshCallbacks) {
            try {
                var result = callback();
                await AwaitIfTask(result);
            } catch (Exception ex) {
                HandleError(ex);
            }
        }
    }


    /// <summary>
    /// Calls all onPhoto callbacks with photo data. Supports sync and async.
    /// </summary>
    public async Task InvokePhoto(object photoData) {
        foreach (var callback in onPhotoCallbacks) {
            try {
                var result = callback(photoData);
                await AwaitIfTask(result);
            } catch (Exception ex) {
                HandleError(ex);
            }
        }
    }


    /// <summary>
    /// Call all onDocument callbacks with document data (supports sync and async)
    /// </summary>
    public async Task InvokeDocument(object documentData) {
        foreach (var callback in onDocumentCallbacks) {
            try {
                var result = callback(documentData);
                await AwaitIfTask(result);
            } catch (Exception ex) {
                HandleError(ex);
            }
        }
    }


    /// <summary>
    /// Clears all lifecycle callbacks. Called before re-running script.
    /// </summary>
    public void ClearLifecycleCallbacks() {
        onMountedCallbacks.Clear();
        onUnmountedCallbacks.Clear();
        beforeRenderCallbacks.Clear();
        afterRenderCallbacks.Clear();
        onRefreshCallbacks.Clear();
        onPhotoCallbacks.Clear();
        onDocumentCallbacks.Clear();
    }

    #endregion


    #region Global Setup

    private UiNamespace? uiNamespace;

    /// <summary>
    /// Sets up all global JavaScript functions and variables.
    /// Only essential globals are registered - page control functions are in UI namespace.
    /// </summary>
    private void SetupGlobals() {
        // UI namespace - contains all page control functions
        // Usage: UI.refresh(), UI.navigate('page'), UI.toast('message'), etc.
        uiNamespace = new UiNamespace(botUser, this);
        engine.SetValue("UI", uiNamespace);

        // User object for accessing bot user properties
        engine.SetValue("User", botUser);

        // Localization - $t('key')
        engine.SetValue("$t", new Func<string, string>(botUser.L));

        // Lifecycle hooks registration (all support sync and async callbacks)
        engine.SetValue("onMounted", new Action<Func<object?>>(callback => onMountedCallbacks.Add(callback)));
        engine.SetValue("onUnmounted", new Action<Func<object?>>(callback => onUnmountedCallbacks.Add(callback)));
        engine.SetValue("beforeRender", new Action<Func<object?>>(callback => beforeRenderCallbacks.Add(callback)));
        engine.SetValue("afterRender", new Action<Func<object?>>(callback => afterRenderCallbacks.Add(callback)));
        engine.SetValue("onRefresh", new Action<Func<object?>>(callback => onRefreshCallbacks.Add(callback)));

        // User input handlers - receive photos/documents sent by user (support sync and async)
        engine.SetValue("onPhoto", new Action<Func<object, object?>>(callback => onPhotoCallbacks.Add(callback)));
        engine.SetValue("onDocument", new Action<Func<object, object?>>(callback => onDocumentCallbacks.Add(callback)));

        // Base will be set to current page in SetPage()

        if (vmodel is not null) {
            engine.SetValue("VModel", vmodel);
        }

        // component(id) - Returns component by ID for programmatic control
        engine.SetValue("component", new Func<string, MenuElement?>(GetComponent));

        // Console for logging
        var logger = botUser.worker.logger;
        engine.SetValue("console", new {
            log = new Action<object?>(obj => logger.LogInformation("[JS] {Message}", obj)),
            warn = new Action<object?>(obj => logger.LogWarning("[JS] {Message}", obj)),
            error = new Action<object?>(obj => logger.LogError("[JS] {Message}", obj))
        });

        // Allow users to register custom extensions
        botUser.RegisterScriptExtensions(this);
    }

    #endregion


    #region Component Management

    /// <summary>
    /// Sets a value in the JavaScript engine scope.
    /// </summary>
    public void SetValue(string name, object? value) {
        engine.SetValue(name, value);
    }


    /// <summary>
    /// Registers a component by ID for JavaScript access via component().
    /// </summary>
    public void RegisterComponent(string id, MenuElement component) {
        components[id] = component;
    }


    /// <summary>
    /// Gets a component by ID.
    /// </summary>
    public MenuElement? GetComponent(string id) {
        return components.TryGetValue(id, out var component) ? component : null;
    }


    /// <summary>
    /// Gets a component by ID with type casting.
    /// </summary>
    public T? GetComponent<T>(string id) where T : MenuElement {
        return GetComponent(id) as T;
    }

    #endregion


    #region Script Execution

    /// <summary>
    /// Executes JavaScript code synchronously.
    /// </summary>
    public JsValue Execute(string script) {
        // Execute script as-is (for function definitions, variable declarations, etc.)
        // Don't wrap - we want definitions to be in global scope
        return engine.Evaluate(script);
    }


    public async Task<JsValue> ExecuteAsync(string script) {
        string wrappedScript;
        if (script.Contains("await")) {
            wrappedScript = $"(async () => {{ {script} }})()";
        } else {
            wrappedScript = script;
        }

        var result = engine.Evaluate(wrappedScript);

        if (result.IsPromise()) {
            var task = result.UnwrapIfPromise().ToObject() as Task;
            if (task is not null) {
                await task;
            }
        }

        return result;
    }


    /// <summary>
    /// Evaluates JavaScript and returns typed result.
    /// </summary>
    public T? Evaluate<T>(string script) {
        var result = Execute(script);
        return ConvertToClr<T>(result);
    }


    /// <summary>
    /// Evaluates JavaScript asynchronously and returns typed result.
    /// </summary>
    public async Task<T?> EvaluateAsync<T>(string script) {
        string wrappedScript;
        if (script.Contains("await")) {
            wrappedScript = $"(async () => {{ {script} }})()";
        } else {
            wrappedScript = script;
        }

        var result = engine.Evaluate(wrappedScript);

        if (result.IsPromise()) {
            var task = result.UnwrapIfPromise().ToObject() as Task;
            if (task is not null) {
                await task;
            }
        }

        return ConvertToClr<T>(result);
    }


    private T? ConvertToClr<T>(JsValue value) {
        if (value.IsUndefined() || value.IsNull()) {
            return default;
        }

        if (value.ToObject() is T typed) {
            return typed;
        }
        return default;
    }


    /// <summary>
    /// Executes script and returns result as object.
    /// </summary>
    public object? GetModel(string script) {
        if (string.IsNullOrWhiteSpace(script)) {
            return null;
        }

        var result = Execute(script);
        if (result.IsUndefined() || result.IsNull()) {
            return null;
        }

        return result.ToObject();
    }


    /// <summary>
    /// Gets a value from the JavaScript engine by name.
    /// </summary>
    public object? GetValue(string name) {
        var value = engine.GetValue(name);
        if (value.IsUndefined() || value.IsNull()) {
            return null;
        }
        return value.ToObject();
    }

    #endregion


    #region Template Rendering

    /// <summary>
    /// Evaluates a JavaScript expression and returns result as string.
    /// Use for binding expressions like :title="item.name".
    /// Does NOT parse {{ }} - evaluates expression directly.
    /// </summary>
    public string EvaluateToString(string expression) {
        if (string.IsNullOrEmpty(expression)) {
            return string.Empty;
        }

        // Decode HTML entities (XML parser encodes < > as &lt; &gt;)
        var cleanExpression = WebUtility.HtmlDecode(expression);

        var result = engine.Evaluate(cleanExpression);
        if (result.IsNull() || result.IsUndefined()) {
            return string.Empty;
        }
        return result.ToString();
    }


    /// <summary>
    /// Renders a template string, replacing {{ expression }} with evaluated results.
    /// Supports async C# methods - they are automatically awaited.
    /// Uses proper bracket-counting parser instead of regex.
    /// </summary>
    public async Task<string> RenderAsync(string template) {
        if (string.IsNullOrEmpty(template)) {
            return template;
        }

        return await TemplateParser.RenderAsync(template, async expression => {
            // Strip "await" keyword if present - we'll await C# Tasks automatically
            var cleanExpression = expression.Trim();
            if (cleanExpression.StartsWith("await ")) {
                cleanExpression = cleanExpression.Substring(6).Trim();
            }

            // Decode HTML entities in JavaScript expressions
            // XML parsing encodes < and > as &lt; and &gt; which breaks JS comparisons
            cleanExpression = WebUtility.HtmlDecode(cleanExpression);

            var result = engine.Evaluate(cleanExpression);
            var resultValue = await UnwrapResultAsync(result);
            return resultValue?.ToString() ?? string.Empty;
        });
    }


    /// <summary>
    /// Unwraps a JsValue result, awaiting if it's a C# Task or JS Promise.
    /// </summary>
    private async Task<object?> UnwrapResultAsync(JsValue result) {
        if (result.IsNull() || result.IsUndefined()) {
            return null;
        }

        var obj = result.ToObject();

        // Handle C# Task directly
        if (obj is Task task) {
            await task;
            var taskType = task.GetType();
            if (taskType.IsGenericType) {
                var resultProperty = taskType.GetProperty("Result");
                return resultProperty?.GetValue(task);
            }
            return null;
        }

        // Handle ValueTask<T>
        if (obj is not null) {
            var objType = obj.GetType();
            if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(ValueTask<>)) {
                // Convert ValueTask<T> to Task<T> and await
                var asTaskMethod = objType.GetMethod("AsTask");
                if (asTaskMethod is not null) {
                    var taskFromValueTask = asTaskMethod.Invoke(obj, null) as Task;
                    if (taskFromValueTask is not null) {
                        await taskFromValueTask;
                        var taskType = taskFromValueTask.GetType();
                        if (taskType.IsGenericType) {
                            var resultProperty = taskType.GetProperty("Result");
                            return resultProperty?.GetValue(taskFromValueTask);
                        }
                    }
                }
                return null;
            }
        }

        return obj;
    }

    #endregion


    #region Error Handling

    /// <summary>
    /// Handles JavaScript execution errors.
    /// </summary>
    private void HandleError(Exception ex) {
        var logger = botUser.worker.logger;
        logger.LogError(ex, "[JS] Error in script execution");

        if (vmodel is not null) {
            var method = vmodel.GetType().GetMethod("HandleErrorAsync");
            if (method is not null) {
                try {
                    var task = method.Invoke(vmodel, [ex]) as Task;
                    if (task is not null) {
                        task.GetAwaiter().GetResult();
                    }
                    return;
                } catch (Exception vmodelEx) {
                    logger.LogError(vmodelEx, "[JS] VModel.HandleErrorAsync failed, falling back to bot user handler");
                }
            }
        }

        botUser.HandleErrorAsync(ex).GetAwaiter().GetResult();
    }

    #endregion


    #region Internal Helpers

    /// <summary>
    /// Convert JavaScript object to Dictionary for props
    /// </summary>
    internal Dictionary<string, object?>? ConvertJsObjectToDict(JsValue value) {
        if (value.IsNull() || value.IsUndefined()) {
            return null;
        }

        // Try using ToObject() first - Jint often converts to ExpandoObject or Dictionary
        var obj = value.ToObject();
        if (obj is IDictionary<string, object?> dict) {
            return new Dictionary<string, object?>(dict);
        }
        if (obj is System.Dynamic.ExpandoObject expando) {
            return new Dictionary<string, object?>((IDictionary<string, object?>)expando);
        }

        // Manual conversion for ObjectInstance
        if (value is Jint.Native.Object.ObjectInstance jsObj) {
            var result = new Dictionary<string, object?>();
            foreach (var key in jsObj.GetOwnPropertyKeys()) {
                var keyStr = key.ToString();
                var propValue = jsObj.Get(key);

                // Recursively convert nested objects
                if (propValue is Jint.Native.Object.ObjectInstance && !propValue.IsArray()) {
                    result[keyStr] = ConvertJsObjectToDict(propValue);
                } else if (propValue.IsArray()) {
                    // Convert arrays
                    var arr = propValue.AsArray();
                    var list = new List<object?>();
                    for (uint i = 0; i < arr.Length; i++) {
                        var item = arr.Get(i.ToString());
                        if (item is Jint.Native.Object.ObjectInstance && !item.IsArray()) {
                            list.Add(ConvertJsObjectToDict(item));
                        } else {
                            list.Add(item.ToObject());
                        }
                    }
                    result[keyStr] = list;
                } else {
                    result[keyStr] = propValue.ToObject();
                }
            }
            return result;
        }

        return null;
    }

    #endregion


    #region Disposal

    /// <summary>
    /// Disposes the script context and clears registered components.
    /// </summary>
    public void Dispose() {
        components.Clear();
    }

    #endregion
}