# Telegram Bot UI 🤖

Library for creating Telegram bot interfaces based on [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot)

Visit the repository with a demo project of a photo editor bot [Telegram.Bot.UI.Demo](Telegram.Bot.UI.Demo)

[![NuGet](https://img.shields.io/nuget/v/Telegram.Bot.UI.svg)](https://www.nuget.org/packages/Telegram.Bot.UI/)
[![License](https://img.shields.io/github/license/VeyDlin/Telegram.Bot.UI)](LICENSE)

## ✨ Features

- **Two API approaches:**
  - **Declarative XML pages** (`.page` files) - Simple, hot-reloadable, Vue-like syntax
  - **C# MessagePage classes** - Full control, programmatic approach
- Different bot operation modes:
  - Long Polling
  - WebHook via controller
  - Built-in WebHook server
- Text templating system with `{{ }}` expressions
- Resource loader (texts, images, etc.) with virtual resource support
- Nested interface pages with navigation
- Built-in command parser
- User permissions management system (useful for bans)
- Safe bot shutdown mechanism (waits for all critical operations to complete)
- Page wallpaper support (via web preview)
- Built-in license agreement acceptance mechanism
- Rich library of interactive menu components
- JavaScript scripting in pages (powered by Jint)
- ViewModels for C#/JavaScript integration

## Interface Components

The library provides numerous interactive components for both declarative and programmatic APIs:

| XML Component | C# Class | Description |
|---------------|----------|-------------|
| `<command>` | `MenuCommand` | Button for triggering custom actions |
| `<open>` | `MenuOpen` | Opening pages, links, or web apps |
| `<checkbox>` | `MenuCheckbox` | Toggle for enabling/disabling options |
| `<radio>` | `MenuRadio` | Radio buttons for single selection |
| `<switch>` | `MenuSwitch` | Carousel option switch (one button) |
| `<card>` | `MenuCard` | Container with optional pagination |
| `<navigate>` | `MenuNavigatePanel` | Navigation controls for paginated content |
| `<row>` | - | Groups components on same row |
| - | `MenuCheckboxModal` | Modal window with checkboxes |
| - | `MenuRadioModal` | Modal window with radio buttons |
| - | `MenuSplit` | Element separator (line break) |

See [Documentation/Components.md](Documentation/Components.md) for detailed component reference.

## 📦 Nuget
The Telegram.Bot.UI package is available via [NuGet](https://www.nuget.org/packages/Telegram.Bot.UI)!
```
dotnet add package Telegram.Bot.UI
```

## 🚀 Quick Start with Declarative Pages

The easiest way to create bot interfaces is using declarative `.page` files with Vue-like syntax.

### 1. Create a Page File

Create `Resources/Pages/home.page`:

```xml
<view>
    <title>Welcome</title>
    <message>Hello! This is a demo bot.<br/><br/>Choose an option below:</message>
    <components>
        <command title="Counter Demo" @click="UI.navigate('counter')" />
        <open title="Settings" target="settings" />
        <row>
            <open type="link" title="GitHub" target="https://github.com" />
            <open type="link" title="Docs" target="https://example.com/docs" />
        </row>
    </components>
</view>
```

### 2. Create a Counter Page with ViewModel

Create `Resources/Pages/counter.page`:

```xml
<view vmodel="CounterViewModel">
    <title>Counter</title>
    <message>Count: {{ VModel.Count }}<br/>Status: {{ VModel.GetStatus() }}</message>
    <components>
        <row>
            <command title="➖" @click="decrement()" />
            <command title="{{ VModel.Count }}" @click="reset()" />
            <command title="➕" @click="increment()" />
        </row>
        <command title="Back" @click="UI.back()" />
    </components>
</view>

<script>
function increment() {
    VModel.Increment();
    UI.refresh();
}

function decrement() {
    VModel.Decrement();
    UI.refresh();
}

function reset() {
    VModel.Reset();
    UI.toast('Counter reset!');
    UI.refresh();
}
</script>
```

### 3. Create the ViewModel

```csharp
public class CounterViewModel {
    public int Count { get; set; } = 0;

    public void Increment() => Count++;
    public void Decrement() => Count--;
    public void Reset() => Count = 0;

    public string GetStatus() => Count switch {
        0 => "Zero",
        > 0 => "Positive",
        < 0 => "Negative"
    };
}
```

### 4. Setup Program.cs

```csharp
// Load pages
var pagesPath = Path.Combine("Resources", "Pages");
var vmodelAssembly = typeof(CounterViewModel).Assembly;
var pageManager = new PageManager(pagesPath, vmodelAssembly);
pageManager.LoadAll();

// Create bot
var bot = new BotWorkerPulling<MyBotUser>((worker, chatId, client, token) => {
    return new MyBotUser(pageManager, worker, chatId, client, token);
}) {
    botToken = "YOUR_BOT_TOKEN",
    resourceLoader = new ResourceLoader("Resources")
};

await bot.StartAsync();
```

### 5. Handle Commands in BotUser

```csharp
public class MyBotUser : BaseBotUser {
    private PageManager pageManager;
    private Dictionary<string, ScriptPage> pageCache = new();

    public MyBotUser(PageManager pageManager, IBotWorker worker, long chatId,
        ITelegramBotClient client, CancellationToken token)
        : base(worker, chatId, client, token) {
        this.pageManager = pageManager;
    }

    public override async Task HandleCommandAsync(string cmd, string[] args, Message message) {
        switch (cmd) {
            case "start":
            case "home":
                var page = GetOrCreatePage("home");
                if (page != null) await page.SendPageAsync();
                break;
            default:
                // Try to open page by command name
                var dynamicPage = GetOrCreatePage(cmd);
                if (dynamicPage != null) await dynamicPage.SendPageAsync();
                break;
        }
    }

    private ScriptPage? GetOrCreatePage(string pageId) {
        if (pageCache.TryGetValue(pageId, out var cached)) return cached;
        var page = pageManager.GetPage(pageId, this);
        if (page != null) pageCache[pageId] = page;
        return page;
    }
}
```

### Key Concepts

**UI Namespace:**
All page control functions are in the `UI` namespace:
```javascript
UI.navigate('page-id');    // Navigate to page
UI.refresh();              // Refresh current page
UI.toast('Message');       // Show notification
UI.back();                 // Go back
```

**User Object:**
Access to BaseBotUser properties and methods:
```javascript
User.chatId                           // User's chat ID
User.localization.code                // Current language
await User.SendTextMessageAsync('Hi') // Send message
```

**Base Object:**
Access to current ScriptPage properties:
```javascript
Base.pageId                           // Current page ID
Base.title                            // Current page title
Base.parent                           // Parent page reference
```

### Documentation

**Start Here:**
- [Documentation/GettingStarted.md](Documentation/GettingStarted.md) - Complete beginner's guide with examples

**Reference:**
- [Documentation/Components.md](Documentation/Components.md) - All UI components (command, radio, checkbox, etc.)
- [Documentation/JavaScriptAPI.md](Documentation/JavaScriptAPI.md) - JavaScript API (UI namespace, Base object, lifecycle hooks)
- [Documentation/ViewModels.md](Documentation/ViewModels.md) - ViewModel integration with C#
- [Documentation/Pages.md](Documentation/Pages.md) - Page structure, attributes, and configuration

---

## 🚀 Getting Started (C# API)

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
        logger.LogError(exception, "Error in bot user {ChatId}", chatId);
        await SendTextMessageAsync($"<pre>{EscapeText(exception.ToString(), ParseMode.Html)}</pre>", mode: ParseMode.Html);
    }
}
```

## Bot Operation Modes

### Long Polling

A simple way for a quick start:

```csharp
var bot = new BotWorkerPulling<MyBotUser>((worker, chatId, client, token) => {
    return new MyBotUser(worker, chatId, client, token);
}) {
    botToken = "TELEGRAM_BOT_TOKEN",
    resourceLoader = new ResourceLoader("Resources"),
    localizationPack = LocalizationPack.FromLPack(new FileInfo(Path.Combine("Resources", "Lang.lpack")))
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
    resourceLoader = new ResourceLoader("Resources"),
    localizationPack = LocalizationPack.FromLPack(new FileInfo(Path.Combine("Resources", "Lang.lpack")))
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
    resourceLoader = new ResourceLoader("Resources"),
    localizationPack = LocalizationPack.FromLPack(new FileInfo(Path.Combine("Resources", "Lang.lpack")))
};

await bot.StartAsync();
```

## 📝 Logging Configuration

The library supports Microsoft.Extensions.Logging for comprehensive logging throughout the bot lifecycle.

### Basic Logging Setup

```csharp
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder => {
    builder
        .SetMinimumLevel(LogLevel.Information)
        .AddConsole();
});

var bot = new BotWorkerPulling<MyBotUser>((worker, chatId, client, token) => {
    return new MyBotUser(worker, chatId, client, token);
}) {
    botToken = "TELEGRAM_BOT_TOKEN",
    resourceLoader = new ResourceLoader("Resources"),
    localizationPack = LocalizationPack.FromLPack(new FileInfo(Path.Combine("Resources", "Lang.lpack"))),
    logger = loggerFactory.CreateLogger<BotWorkerPulling<MyBotUser>>()
};

await bot.StartAsync();
```

### Passing Logger to User Instance

To enable logging in your user class, pass the logger in the constructor:

```csharp
var bot = new BotWorkerPulling<MyBotUser>((worker, chatId, client, token) => {
    var user = new MyBotUser(worker, chatId, client, token);
    user.logger = loggerFactory.CreateLogger<MyBotUser>();
    return user;
}) {
    botToken = "TELEGRAM_BOT_TOKEN",
    resourceLoader = new ResourceLoader("Resources"),
    logger = loggerFactory.CreateLogger<BotWorkerPulling<MyBotUser>>()
};
```

### Integration with ASP.NET Core

When using WebHook mode with ASP.NET Core, you can use dependency injection:

```csharp
builder.Services.AddSingleton(serviceProvider => {
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

    var bot = new BotWorkerWebHook<MyBotUser>((worker, chatId, client, token) => {
        var user = new MyBotUser(worker, chatId, client, token);
        user.logger = loggerFactory.CreateLogger<MyBotUser>();
        return user;
    }) {
        botToken = "TELEGRAM_BOT_TOKEN",
        botSecretToken = "WEBHOOK_SECRET_TOKEN",
        botHostAddress = "https://mybot.com",
        botRoute = "TelegramBot/webhook",
        resourceLoader = new ResourceLoader("Resources"),
        localizationPack = LocalizationPack.FromLPack(new FileInfo(Path.Combine("Resources", "Lang.lpack"))),
        logger = loggerFactory.CreateLogger<BotWorkerWebHook<MyBotUser>>()
    };

    return bot;
});
```

### Logged Events

The library logs the following events:

**BotWorker (BaseBotWorker):**
- Bot startup and shutdown (Information level)
- User cache operations (Debug level)
- New user creation (Information level)
- Errors during update handling (Error/Critical level)

**BotUser (BaseBotUser):**
- Custom error handling in `HandleErrorAsync` (your implementation)

## 📄 Creating Interface Pages (C# MessagePage - Legacy)

> **Note:** The `MessagePage` C# API is a legacy approach. For new projects, use declarative `.page` files as shown in the [Quick Start](#-quick-start-with-declarative-pages) section.

The `MessagePage` class provides programmatic control over bot pages. See the source code for implementation details.

## Localization

Localization supports two formats: `.lpack` and `.json`.

### LPack Format

Create `Resources/Lang.lpack`:

```
en: Support
ru: Поддержка

en: I agree
ru: Я согласен

en: Language select
ru: Выбор языка

en: Settings
ru: Настройки
```

Each block is separated by an empty line. Each line is `code: text` where `code` is the language code (en, ru, etc.).

### JSON Format

Alternatively, use JSON:

```json
[
  { "en": "Support", "ru": "Поддержка" },
  { "en": "I agree", "ru": "Я согласен" },
  { "en": "Language select", "ru": "Выбор языка" },
  { "en": "Settings", "ru": "Настройки" }
]
```

Load with `LocalizationPack.FromJson()` instead of `FromLPack()`.

In declarative pages, use the `$t()` function:

```xml
<!-- Using $t() function for localization -->
<command :title="$t('Save')" />

<!-- In templates -->
<title>{{ $t('Settings') }}</title>

<!-- Combined with other text -->
<command :title="'✅ ' + $t('Confirm')" />
```

## 📂 Resource Structure

Resources for declarative pages:

```
Resources/
├── Pages/
│   ├── home.page
│   ├── settings.page
│   └── components/
│       ├── counter.page
│       └── forms.page
├── images/
│   └── banner.png
├── texts/
│   ├── welcome-en.md
│   └── welcome-ru.md
└── Lang.lpack
```

### Message with External Resource

```xml
<view>
    <title>Welcome</title>
    <!-- md attribute enables Markdown parsing -->
    <message md resource="text/welcome-{{ User.localization.code }}"></message>
</view>
```

Note: `User` provides access to `BaseBotUser` properties like `localization.code`, `chatId`, etc. `Base` provides access to the current `ScriptPage` instance.

