namespace Telegram.Bot.UI.MenuBuilder.Elements.Selectors;


public class MenuSelector {
    public required string title { get; set; }
    public required string id { get; set; }



    public static List<MenuSelector> FromArray(IEnumerable<(string title, string id)> source) {
        return source.Select(x => new MenuSelector {
            title = x.title,
            id = x.id
        }).ToList();
    }



    public static IEnumerable<(MenuSelector item, int index)> WithIndex(IEnumerable<MenuSelector> source) {
        return source.Select((item, index) => (item, index));
    }
}