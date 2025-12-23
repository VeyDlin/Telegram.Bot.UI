namespace Telegram.Bot.UI;


/// <summary>
/// Reasons why a callback query was rejected.
/// </summary>
public enum RejectedCallback {
    /// <summary>
    /// Message was sent before bot started (stale button).
    /// </summary>
    Skip,

    /// <summary>
    /// User doesn't have permission to perform this action.
    /// </summary>
    Permission,

    /// <summary>
    /// Callback ID not found (expired or unknown action).
    /// </summary>
    Unknown
}
