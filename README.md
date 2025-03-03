# Telegram Bot UI 🤖

Library for creating Telegram bot interfaces based on [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot)

Visit the repository with a demo project of a photo editor bot [Telegram.Bot.Demo](https://github.com/VeyDlin/Telegram.Bot.UI.Demo)

## ✨ Features

- 🔄 Different bot operation modes:
  - Long Polling
  - WebHook via controller
  - Built-in WebHook server
- 🖼️ Text templating system
- 📦 Resource loader (texts, images, etc.)
- 📄 Nested interface pages
- ⌨️ Built-in command parser
- 🛡️ User permissions management system (useful for bans)
- ⚠️ Safe bot shutdown mechanism (waits for all critical operations to complete)
- 🖌️ Page wallpaper support (via web preview)
- 📝 Built-in license agreement acceptance mechanism
- 🧰 Rich library of interactive menu components

## 🧰 Interface Components

The library provides numerous interactive components:

- `MenuCheckbox` - Checkboxes for enabling/disabling options
- `MenuCheckboxGroup` - Group of checkboxes for multiple selection
- `MenuCheckboxModal` - Modal window with checkboxes (separate page)
- `MenuCommand` - Button for triggering custom actions
- `MenuLink` - Link to external resources, channels, chats
- `MenuNavigatePanel` - Navigation between menu pages (in development)
- `MenuOpenPege` - Opening other interface pages
- `MenuRadio` - Radio buttons for selecting one of several options
- `MenuRadioModal` - Modal window with radio buttons
- `MenuSplit` - Element separator (line break)
- `MenuSwitch` - Carousel option switch (one button)

## 🚀 Getting Started

### Creating a Bot User Class

A separate instance of the user class is created for each user, where you can store state, work with the database, configure localization and interface:

```csharp
public class MyBotUser : BaseBotUser 
{
    public LanguageView languageView { get; private set; }
    public UserAgreementView userAgreementView { get; private set; }
    public InformationView informationView { get; private set; }

    public MyBotUser(IBotWorker worker, long chatId, ITelegramBotClient client, CancellationToken token) :
        base(worker, chatId, client, token) 
    {
        // Setting up pages
        languageView = new(this);
        userAgreementView = new(this);
        informationView = new(this);

        parseMode = ParseMode.Html;
    }

    public override void Begin() {
        // These values can be retrieved from the database
        localization.code = "en";
        acceptLicense = false;
    }

    public override async Task HandleCommandAsync(string cmd, string[] arguments, Message message) {
        switch (cmd) {
            case "hello":
            case "info":
            case "start": {
                await informationView.SendPageAsync();
            }
            break;
            case "lang": {
                await languageView.SendPageAsync();
            }
            break;
            case "ping": {
                await SendTextMessageAsync("`pong`", mode: ParseMode.MarkdownV2);
            }
            break;
        }
    }

    public override Task<bool> HandlePermissiveAsync(Message message) {
        // Prohibit private chats
        return Task.FromResult(message.Chat.Type != ChatType.Private);
    }

    public override async Task HandleAcceptLicense(Message message) {
        // License must be accepted first
        await userAgreementView.SendPageAsync();
    }

    public override async Task HandleErrorAsync(Exception exception) {
        // Log the error and send it in response
        Console.WriteLine(exception.ToString());
        await SendTextMessageAsync($"<pre>{EscapeText(exception.ToString(), ParseMode.Html)}</pre>", mode: ParseMode.Html);
    }
}
```

## 🔄 Bot Operation Modes

### Long Polling

A simple way for a quick start:

```csharp
var bot = new BotWorkerPulling<MyBotUser>((worker, chatId, client, token) => {
    return new MyBotUser(worker, chatId, client, token);
}) {
    botToken = "TELEGRAM_BOT_TOKEN",
    resourcePath = Path.Combine("Resources", "View"),
    localizationPack = LocalizationPack.FromJson(new FileInfo(Path.Combine("Resources", "Lang", "Lang.json")))
};

await bot.StartAsync();
```

### WebHook with ASP.NET Controller

- Wait! But the polling mode is slow! I want a webhook!
- No problem! This can be implemented like this!

```csharp
var bot = new BotWorkerWebHook<MyBotUser>((worker, chatId, client, token) => {
    return new MyBotUser(worker, chatId, client, token);
}) {
    botToken = "TELEGRAM_BOT_TOKEN",
    botSecretToken = "WEBHOOK_SECRET_TOKEN",
    botHostAddress = "https://mybot.com",
    botRoute = "TelegramBot/webhook",
    resourcePath = Path.Combine("Resources", "View"),
    localizationPack = LocalizationPack.FromJson(new FileInfo(Path.Combine("Resources", "Lang", "Lang.json")))
};

await bot.StartAsync();
builder.Services.AddSingleton(bot);
```

Controller for handling requests:

```csharp
[ApiController]
[Route("[controller]")]
public class TelegramBotController : ControllerBase {
    private readonly BotWorkerWebHook<MyBotUser> bot;

    public TelegramBotController(BotWorkerWebHook<MyBotUser> bot) {
        this.bot = bot;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Post([FromBody] Update update) {
        await bot.UpdateHandlerAsync(update);
        return Ok();
    }
}
```

### Built-in WebHook Server

For console applications or when integration with ASP.NET is not possible:

- Damn! I hate WebApi and all that DI! I want a simple console application with webhook!
- Don't worry! This is also possible!

```csharp
var bot = new BotWorkerWebHookServer<MyBotUser>((worker, chatId, client, token) => {
    return new MyBotUser(worker, chatId, client, token);
}) {
    botToken = "TELEGRAM_BOT_TOKEN",
    botSecretToken = "WEBHOOK_SECRET_TOKEN",
    botHostAddress = "https://mybot.com",
    port = 80,
    botRoute = "webhook",
    resourcePath = Path.Combine("Resources", "View"),
    localizationPack = LocalizationPack.FromJson(new FileInfo(Path.Combine("Resources", "Lang", "Lang.json")))
};

await bot.StartAsync();
```

## 📄 Creating Interface Pages

The library uses the concept of pages (classes inheriting from `MessagePage`) to represent bot interface elements.

### Language Selection Page Example

```csharp
public class LanguageView : MessagePage {
    public override string pageResource => "Language"; // There should be a folder with pageResource name in resourcePath (Resources/View/Language)
    public override string title => $"{flags[botUser.localization.code]} " + "{{ 'Language select' | L }}"; // | L - Built-in localization method
    private MenuRadio languageRadio;
    private Dictionary<string, string> flags { get; init; } = new() {
        ["ru"] = "🇷🇺",
        ["en"] = "🇺🇸"
    };

    public LanguageView(BaseBotUser botUser) : base(botUser) {
        languageRadio = MenuRadio(MenuSelector.FromArray(new[] {
            ("English", "en"),
            ("Русский", "ru")
        }));

        using var context = ((MyBotUser)botUser).Context();
        var userTable = ((MyBotUser)botUser).GetUserTable(context);

        languageRadio.Select(userTable.language);

        languageRadio.onSelect += select => {
            using var context = ((MyBotUser)botUser).Context();
            var userTable = ((MyBotUser)botUser).GetUserTable(context);

            ((MyBotUser)botUser).localization.code = select.id;
            userTable.language = select.id;
            context.SaveChanges();
        };
    }

    public override string? RequestMessageResource() => $"description-{botUser.localization.code}";

    public override List<ButtonsPage> RequestPageComponents() {
        return ButtonsPage.Page([
            [languageRadio]
        ]);
    }
}
```

### User Agreement Page Example

```csharp
public class UserAgreementView : MessagePage {
    public override string pageResource => "UserAgreement";
    public override string title => "{{ 'User agreement' | L }}";
    private MenuRadio languageRadio;
    private MenuCommand acceptCommand;

    public UserAgreementView(BaseBotUser botUser) : base(botUser) {
        languageRadio = MenuRadio(MenuSelector.FromArray(new[] {
            ("English", "en"),
            ("Русский", "ru")
        }));

        using var context = ((MyBotUser)botUser).Context();
        var userTable = ((MyBotUser)botUser).GetUserTable(context);

        languageRadio.Select(userTable.language);

        languageRadio.onSelect += select => {
            using var context = ((MyBotUser)botUser).Context();
            var userTable = ((MyBotUser)botUser).GetUserTable(context);

            ((MyBotUser)botUser).localization.code = select.id;
            userTable.language = select.id;
            context.SaveChanges();
        };

        acceptCommand = MenuCommand("{{ 'I agree' | L }}");
        acceptCommand.onClick += async (callbackQueryId, messageId, chatId) => {
            using var context = ((MyBotUser)botUser).Context();
            var userTable = ((MyBotUser)botUser).GetUserTable(context);

            userTable.acceptLicense = true;
            ((MyBotUser)botUser).acceptLicense = true;
            context.SaveChanges();

            // Delete current page
            await botUser.DeleteMessageAsync(messageId);

            // Send welcome page after accepting the agreement
            await ((MyBotUser)botUser).informationView.SendPageAsync();
        };
    }

    public override string? RequestMessageResource() => $"description-{botUser.localization.code}";

    public override List<ButtonsPage> RequestPageComponents() {
        return ButtonsPage.Page([
            [languageRadio],
            [acceptCommand]
        ]);
    }
}
```

### Information Page Example

```csharp
public class InformationView : MessagePage {
    public override string pageResource => "Information";
    public override string title => "{{ 'Information' | L }}";

    public InformationView(BaseBotUser botUser) : base(botUser) { }

    public override string? RequestMessageResource() => $"description-{botUser.localization.code}";

    public override object? RequestModel() => new {
        me = botUser.chatId // Now can be used in the templating engine
    };

    public override List<ButtonsPage> RequestPageComponents() {
        return ButtonsPage.Page([
            [
                MenuLink("https://t.me/MyBotSupport", "🆘 {{ 'Support' | L }}"),
                MenuOpenSubPege(((MyBotUser)botUser).languageView)
            ]
        ]);
    }
}
```

## 🔄 Localization

Localization uses a simple JSON format:

```json
[
  {
    "en": "Support",
    "ru": "Поддержка"
  },
  {
    "en": "I agree",
    "ru": "Я согласен"
  },
  {
    "en": "Language select",
    "ru": "Выбор языка"
  },
  {
    "en": "Information",
    "ru": "Информация"
  },
  {
    "en": "User agreement",
    "ru": "Пользовательское соглашение"
  }
]
```

## 📂 Resource Structure

Resources are organized in folders by page name:

```
Resources/
├── View/
│   ├── Language/             # pageResource = "Language"
│   │   ├── text/
│   │   │   ├── description-en.md
│   │   │   └── description-ru.md
│   │   └── image/
│   │       └── background.png
│   ├── UserAgreement/        # pageResource = "UserAgreement" 
│   │   └── text/
│   │       ├── description-en.md
│   │       └── description-ru.md
│   └── Information/          # pageResource = "Information"
│       └── text/
│           ├── description-en.md
│           └── description-ru.md
└── Lang/
    └── Lang.json           # File with localizations
```

### Resource File Example (description-en.md)

```markdown
😎 Hello, {{ me }}!
```

### Resource File Example (description-ru.md)

```markdown
😎 Привет, {{ me }}!
```

