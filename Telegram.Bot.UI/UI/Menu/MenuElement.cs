using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.UI.Runtime;

namespace Telegram.Bot.UI.Menu;


/// <summary>
/// Base class for all inline keyboard menu elements.
/// Provides common properties, template rendering, and lifecycle management.
/// </summary>
public abstract class MenuElement : IDisposable {
    #region Properties

    /// <summary>
    /// Unique identifier for this element. Used with component() in JavaScript.
    /// </summary>
    public virtual string id { get; set; } = "";

    /// <summary>
    /// Parent page containing this element.
    /// </summary>
    public virtual required MessagePage parent { get; set; }

    /// <summary>
    /// Bot user context.
    /// </summary>
    public virtual required BaseBotUser botUser { get; set; }

    /// <summary>
    /// Script context for template rendering and JavaScript evaluation.
    /// </summary>
    public virtual ScriptContext? scriptContext { get; set; }

    /// <summary>
    /// If true, element is not rendered in the keyboard.
    /// </summary>
    public virtual bool hide { get; set; } = false;

    /// <summary>
    /// Maximum number of buttons per row when element generates multiple buttons.
    /// </summary>
    public virtual int columns { get; set; } = 3;

    /// <summary>
    /// Row index for grouping elements into keyboard rows.
    /// </summary>
    public virtual int rowIndex { get; set; } = 0;

    /// <summary>
    /// If true, title is localized using L().
    /// </summary>
    public virtual bool lang { get; set; } = false;

    private bool disposed { get; set; } = false;

    #endregion


    #region Destructor

    /// <summary>
    /// Destructor ensures disposal.
    /// </summary>
    ~MenuElement() {
        Dispose();
    }

    #endregion


    #region Abstract Methods

    /// <summary>
    /// Builds inline keyboard buttons for this element.
    /// </summary>
    /// <returns>List of buttons to add to the keyboard.</returns>
    public abstract Task<List<InlineKeyboardButton>> BuildAsync();

    #endregion


    #region Template Rendering

    /// <summary>
    /// Renders a template string synchronously.
    /// </summary>
    protected string render(string template) {
        if (scriptContext != null) {
            return scriptContext.RenderAsync(template).GetAwaiter().GetResult();
        }
        return template;
    }

    /// <summary>
    /// Renders a template string asynchronously.
    /// </summary>
    protected async Task<string> renderAsync(string template) {
        if (scriptContext != null) {
            return await scriptContext.RenderAsync(template);
        }
        return template;
    }

    /// <summary>
    /// Evaluates a JavaScript expression directly (no {{ }} parsing).
    /// Use for binding expressions like :title="item.name".
    /// </summary>
    protected string evaluate(string expression) {
        if (scriptContext != null) {
            return scriptContext.EvaluateToString(expression);
        }
        return expression;
    }


    /// <summary>
    /// Renders title template with optional localization.
    /// </summary>
    protected string renderTitle(string template, bool useLang) {
        var rendered = render(template);
        return useLang ? botUser.L(rendered) : rendered;
    }

    /// <summary>
    /// Renders title template asynchronously with optional localization.
    /// </summary>
    protected async Task<string> renderTitleAsync(string template, bool useLang) {
        var rendered = await renderAsync(template);
        return useLang ? botUser.L(rendered) : rendered;
    }

    #endregion


    #region Disposal

    /// <summary>
    /// Disposes element resources.
    /// </summary>
    public void Dispose() {
        if (!disposed) {
            OnDispose();
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }


    /// <summary>
    /// Override to add custom cleanup logic.
    /// </summary>
    protected virtual void OnDispose() { }

    #endregion
}
