namespace Telegram.Bot.UI.Utils;

/// <summary>
/// Represents a cached value with timestamp tracking.
/// </summary>
/// <typeparam name="T">The type of the cached value.</typeparam>
public class TimeCache<T> {
    /// <summary>
    /// Gets or sets the cached value.
    /// </summary>
    public T value { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the value was last updated.
    /// </summary>
    public DateTime time { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Updates the timestamp to the current UTC time.
    /// </summary>
    public void Update() => time = DateTime.UtcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeCache{T}"/> class with the specified value.
    /// </summary>
    /// <param name="value">The initial cached value.</param>
    public TimeCache(T value) {
        this.value = value;
    }
}