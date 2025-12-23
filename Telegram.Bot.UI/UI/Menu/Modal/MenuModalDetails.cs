using Telegram.Bot.UI.Parsing;

namespace Telegram.Bot.UI.Menu.Modal;


public class MenuModalDetails {
    public required string id { get; init; }
    public required string title { get; init; }
    public required int index { get; init; }
    public MessageDefinition? message { get; init; }
    public object? model { get; init; }
    public bool? webPreview { get; set; } = true;
    public string? pageResource { get; init; }
}
