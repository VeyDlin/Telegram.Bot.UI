using Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot.UI.Loader;

namespace Telegram.Bot.UI.BotWorker;

/// <summary>
/// Defines the interface for bot worker services that manage bot lifecycle and resources.
/// </summary>
public interface IBotWorker {
    /// <summary>
    /// Gets the time when the bot worker was started.
    /// </summary>
    public DateTime startTime { get; }

    /// <summary>
    /// Gets the resource loader for loading bot resources.
    /// </summary>
    public IResourceLoader resourceLoader { get; }

    /// <summary>
    /// Gets or sets the localization pack for multi-language support.
    /// </summary>
    public LocalizationPack? localizationPack { get; set; }

    /// <summary>
    /// Gets the logger instance for logging bot events.
    /// </summary>
    public ILogger logger { get; }

    /// <summary>
    /// Starts the bot worker asynchronously.
    /// </summary>
    public Task StartAsync();

    /// <summary>
    /// Stops the bot worker asynchronously.
    /// </summary>
    public Task StopAsync();
}