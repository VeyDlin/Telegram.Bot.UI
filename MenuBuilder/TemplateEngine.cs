using Localization;
using Scriban;
using Scriban.Runtime;
using Telegram.Bot.UI.Utils;

namespace Telegram.Bot.UI.MenuBuilder;



public static class TemplateEngine {
    public static string Render(string temp, LocalizationManager? local = null) {
        return Render(temp, [], local);
    }





    public static string Render(string temp, IEnumerable<object> models, LocalizationManager? local = null) {
        var scriptObject = GetScriptObject(local);
        foreach (var model in models) {
            scriptObject.Import(model);
        }

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        return Template.Parse(temp).Render(context);
    }




    private static ScriptObject GetScriptObject(LocalizationManager? local = null) {
        var scriptObject = new ScriptObject();

        if (local is not null) {
            scriptObject.Import("L", local.TryTranslate);
        }

        scriptObject.Import("declension", Declension.GetDeclension);

        return scriptObject;
    }
}
