using System.Text;

namespace Telegram.Bot.UI.Loader.DataTypes;


public class TextResource : BaseResource {
    private string? text = null;



    public string GetText(Dictionary<string, string>? values = null) {
        lock (locker) {
            if (text is not null) {
                return TemplateText(text, values);
            }

            if (info?.data is null) {
                throw new Exception("data is null");
            }

            text = Encoding.UTF8.GetString(info.data);

            return TemplateText(text, values);
        }
    }





    private static string TemplateText(string text, Dictionary<string, string>? values) {
        if (values is null) {
            return text;
        }

        foreach (var pair in values) {
            text = text.Replace("{" + pair.Key.ToUpper() + "}", pair.Value);
        }
        return text;
    }
}
