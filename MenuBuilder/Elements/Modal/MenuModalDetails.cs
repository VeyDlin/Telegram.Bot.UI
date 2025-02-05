using static Telegram.Bot.UI.MenuBuilder.MessagePage;

namespace Telegram.Bot.UI.MenuBuilder.Elements.Modal;


public class MenuModalDetails {
    public required string id { get; init; }
    public string? messageResource { get; init; }
    public (string resource, WallpaperLoader loader)? wallpaper { get; init; }
    public object? model { get; init; }
    public bool? webPreview { get; set; } = true;
    public string? pageResource { get; }
}
