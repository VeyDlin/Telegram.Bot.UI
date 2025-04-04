﻿using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.UI.MenuBuilder.Elements;


public class MenuLink : MenuElement {
    public required string title { get; set; }
    public required string url { get; set; }
    public string temp { get; set; } = "{{ title }}";



    public override List<InlineKeyboardButton> Build() {
        if (hide) {
            return new();
        }

        var models = parrent.InheritedRequestModel();
        models.Add(new {
            title = TemplateEngine.Render(title, models, botUser.localization),
            url = url
        });

        return new() {
            InlineKeyboardButton.WithUrl(
                text: TemplateEngine.Render(temp, models, botUser.localization),
                url: url
            )
        };
    }
}
