namespace Telegram.Bot.UI.MenuBuilder;


public class ButtonsPage {
    public required IEnumerable<IEnumerable<MenuElement>> page { get; set; }



    public static List<ButtonsPage> Page(params IEnumerable<IEnumerable<MenuElement>>[] pagePram) {
        var pages = new List<ButtonsPage>();
        return pages.Concat(pagePram.Select(x => new ButtonsPage() { page = x })).ToList();
    }
}