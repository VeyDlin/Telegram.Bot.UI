namespace Telegram.Bot.UI;

/// <summary>
/// Interface for view models that receive props (properties) when instantiated by PageManager.
/// </summary>
public interface IPropsReceiver {
    /// <summary>
    /// Receives props passed to the page during instantiation.
    /// </summary>
    /// <param name="props">Dictionary of property names and values.</param>
    void ReceiveProps(Dictionary<string, object?> props);
}
