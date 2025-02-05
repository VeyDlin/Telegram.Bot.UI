using Telegram.Bot.UI.Loader.DataTypes;

namespace Telegram.Bot.UI.Loader;


public class PageInfo {
    public string? name { get; set; }
    public ResourceReader<AudioResource>? audio { get; set; }
    public ResourceReader<ImageResource>? image { get; set; }
    public ResourceReader<TextResource>? text { get; set; }
    public ResourceReader<VideoResource>? video { get; set; }
}





public class PageInfo<T> : PageInfo {
    public T? config { get; set; }
}