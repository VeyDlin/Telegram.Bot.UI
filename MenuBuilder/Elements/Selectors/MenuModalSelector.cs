namespace Telegram.Bot.UI.MenuBuilder;


public class MenuModalSelector {
    public required string title { get; set; }
    public required string id { get; set; }



    public static List<MenuModalSelector> FromArray(IEnumerable<(string title, string id)> source) {
        return source.Select(x => new MenuModalSelector {
            title = x.title,
            id = x.id
        }).ToList();
    }



    public static IEnumerable<(MenuModalSelector item, int index)> WithIndex(IEnumerable<MenuModalSelector> source) {
        return source.Select((item, index) => (item, index));
    }
}