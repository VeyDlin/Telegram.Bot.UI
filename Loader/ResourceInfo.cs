using HeyRed.Mime;

namespace Telegram.Bot.UI.Loader.DataTypes;


public class ResourceInfo {
    public required string name { get; set; }
    public required string path { get; set; }
    public required byte[] data { get; set; }
    public required string sha256 { get; set; }


    public string GetMimeType() => MimeGuesser.GuessMimeType(data);
    public string GetExtension() => MimeTypesMap.GetExtension(GetMimeType()).ToLower();
}