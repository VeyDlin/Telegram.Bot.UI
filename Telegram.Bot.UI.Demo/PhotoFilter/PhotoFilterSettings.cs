namespace Telegram.Bot.UI.Demo.PhotoFilter;


public enum FilterLevel {
    Off,
    Low,
    Medium,
    High
}


public struct PhotoFilterSettings {
    public bool applyInvert { get; set; } = false;

    public FilterLevel brightness { get; set; } = FilterLevel.Off;
    public FilterLevel contrast { get; set; } = FilterLevel.Off;
    public FilterLevel blur { get; set; } = FilterLevel.Off;
    public FilterLevel pixelate { get; set; } = FilterLevel.Off;

    public PhotoFilterSettings() { }
}
