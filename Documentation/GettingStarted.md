# Getting Started Guide

This guide will help you get started with Telegram.Bot.UI using declarative `.page` files.

## Table of Contents

- [Installation](#installation)
- [Basic Setup](#basic-setup)
- [Your First Page](#your-first-page)
- [Understanding Key Concepts](#understanding-key-concepts)
- [Common Patterns](#common-patterns)
- [Next Steps](#next-steps)

---

## Installation

Install the NuGet package:

```bash
dotnet add package Telegram.Bot.UI
```

---

## Basic Setup

### 1. Create Project Structure

```
MyTelegramBot/
├── Program.cs
├── MyBotUser.cs
├── ViewModels/
│   └── CounterViewModel.cs
└── Resources/
    ├── Pages/
    │   ├── home.page
    │   └── counter.page
    └── Lang.lpack (optional)
```

### 2. Create BotUser Class

Create `MyBotUser.cs`:

```csharp
using Telegram.Bot.Types;
using Telegram.Bot.UI;
using Telegram.Bot.UI.Runtime;

public class MyBotUser : BaseBotUser {
    private readonly PageManager _pageManager;
    private readonly Dictionary<string, ScriptPage> _pageCache = new();

    public MyBotUser(
        PageManager pageManager,
        IBotWorker worker,
        long chatId,
        ITelegramBotClient client,
        CancellationToken token
    ) : base(worker, chatId, client, token) {
        _pageManager = pageManager;
        parseMode = ParseMode.Html;  // Use HTML by default
    }

    public override async Task HandleCommandAsync(string cmd, string[] args, Message message) {
        switch (cmd) {
            case "start":
            case "home":
                var page = GetOrCreateCachedPage("home", _pageManager);
                if (page != null) await page.SendPageAsync();
                break;
            default:
                await SendTextMessageAsync($"Unknown command: /{cmd}");
                break;
        }
    }

    public override ScriptPage? GetOrCreateCachedPage(string pageId, PageManager pageManager) {
        if (_pageCache.TryGetValue(pageId, out var cached)) return cached;
        var page = pageManager.GetPage(pageId, this);
        if (page != null) _pageCache[pageId] = page;
        return page;
    }
}
```

### 3. Setup Program.cs

```csharp
using Telegram.Bot.UI;
using Telegram.Bot.UI.BotWorker;
using Telegram.Bot.UI.Loader;
using Microsoft.Extensions.Logging;

// Setup logging
var loggerFactory = LoggerFactory.Create(builder => {
    builder
        .SetMinimumLevel(LogLevel.Information)
        .AddConsole();
});

// Setup PageManager
var pagesPath = Path.Combine("Resources", "Pages");
var vmodelAssembly = typeof(Program).Assembly;  // Assembly containing ViewModels
var pageManager = new PageManager(pagesPath, vmodelAssembly);
pageManager.LoadAll();

Console.WriteLine($"Loaded {pageManager.pageCount} pages");

// Create bot worker (using polling)
var bot = new BotWorkerPulling<MyBotUser>((worker, chatId, client, token) => {
    var user = new MyBotUser(pageManager, worker, chatId, client, token);
    user.logger = loggerFactory.CreateLogger<MyBotUser>();
    return user;
}) {
    botToken = "YOUR_BOT_TOKEN",
    resourceLoader = new ResourceLoader("Resources"),
    logger = loggerFactory.CreateLogger<BotWorkerPulling<MyBotUser>>()
};

// Start the bot
await bot.StartAsync();
Console.WriteLine("Bot started. Press Ctrl+C to stop.");

// Wait for shutdown
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) => {
    e.Cancel = true;
    cts.Cancel();
};
await Task.Delay(-1, cts.Token);

// Stop the bot
await bot.StopAsync();
```

---

## Your First Page

Create `Resources/Pages/home.page`:

```xml
<view id="home">
    <title>Welcome</title>
    <message>
        Hello! Welcome to your Telegram bot.<br/>
        <br/>
        Choose an option below:
    </message>
    <components>
        <command title="Counter Demo" @click="UI.navigate('counter')" />
        <open title="Settings" target="settings" />
        <row>
            <open type="link" title="GitHub" target="https://github.com" />
            <open type="link" title="Help" target="https://example.com/help" />
        </row>
    </components>
</view>
```

Create `Resources/Pages/counter.page`:

```xml
<view id="counter" vmodel="CounterViewModel">
    <title>Counter Demo</title>
    <message>
        Current count: <b>{{ VModel.Count }}</b><br/>
        Status: {{ VModel.GetStatus() }}
    </message>
    <components>
        <row>
            <command title="➖" @click="decrement()" />
            <command title="{{ VModel.Count }}" @click="reset()" />
            <command title="➕" @click="increment()" />
        </row>
        <command title="◀ Back" @click="UI.back()" />
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

Create `ViewModels/CounterViewModel.cs`:

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

---

## Understanding Key Concepts

### UI Namespace

All page control functions are in the `UI` namespace:

```javascript
UI.navigate('page-id');    // Navigate to another page
UI.refresh();              // Refresh current page
UI.back();                 // Go back to parent
UI.toast('Message');       // Show notification
UI.alert('Warning');       // Show alert
UI.close();                // Delete message
```

### User Object

Access to `BaseBotUser` properties and methods:

```javascript
User.chatId                           // User's chat ID
User.localization.code                // Current language
await User.SendTextMessageAsync('Hi') // Send message
User.logger.LogInformation('Info')    // Log message
```

### Base Object

Access to current `ScriptPage` properties:

```javascript
Base.pageId                           // Current page ID
Base.title                            // Current page title
Base.parent                           // Parent page reference
```

### Template Syntax

Use `{{ expression }}` for dynamic content:

```xml
<message>Count: {{ VModel.Count }}</message>
<command :title="'Items: ' + items.length" />
```

### Component Binding

```xml
<!-- Static attribute -->
<command title="Click Me" />

<!-- Dynamic binding with : -->
<command :title="dynamicTitle" />

<!-- Event handler with @ -->
<command @click="handleClick()" />
```

### Directives

```xml
<!-- v-for: Loop through array -->
<command v-for="item in items" :title="item.name" />

<!-- v-if: Conditional rendering -->
<command v-if="VModel.Count > 0" title="Reset" />
<command v-else title="Start" />
```

---

## Common Patterns

### Navigation Pattern

```xml
<view id="menu">
    <title>Main Menu</title>
    <message>Select a category:</message>
    <components>
        <!-- Navigate as sub-page (preserves back button) -->
        <command title="Products" @click="UI.navigate('products')" />

        <!-- Navigate as main page (clears history) -->
        <command title="Home" @click="UI.navigate('home', false)" />

        <!-- Navigate with props -->
        <command title="Edit Item #5" @click="openEditor(5)" />
    </components>
</view>

<script>
function openEditor(itemId) {
    UI.navigate('editor', true, { itemId: itemId });
}
</script>
```

### List with v-for

```xml
<view vmodel="ItemListViewModel">
    <title>Items</title>
    <message>Total items: {{ VModel.Items.length }}</message>
    <components>
        <command
            v-for="(item, index) in VModel.Items"
            :key="item.Id"
            :title="(index + 1) + '. ' + item.Name"
            @click="selectItem(item.Id)" />
    </components>
</view>

<script>
function selectItem(id) {
    VModel.SelectItem(id);
    UI.navigate('item-details', true, { itemId: id });
}
</script>
```

### Pagination with Card

```xml
<view vmodel="ProductListViewModel">
    <title>Products</title>
    <message>Showing {{ VModel.Products.length }} products</message>
    <components>
        <card id="productList" max-items="5">
            <command
                v-for="product in VModel.Products"
                :title="product.Name + ' - $' + product.Price"
                @click="viewProduct(product.Id)" />
        </card>
        <navigate target="productList" />
    </components>
</view>
```

### Radio Selection

```xml
<view vmodel="SettingsViewModel">
    <title>Settings</title>
    <message>Select theme:</message>
    <components>
        <radio id="themeSelector" :selected="VModel.Theme" @select="changeTheme(event.select.id)">
            <option value="light" title="Light Theme" />
            <option value="dark" title="Dark Theme" />
            <option value="auto" title="Auto" />
        </radio>
        <command title="Save" @click="save()" />
    </components>
</view>

<script>
function changeTheme(themeId) {
    VModel.Theme = themeId;
}

function save() {
    VModel.SaveSettings();
    UI.toast('Settings saved!');
    UI.back();
}
</script>
```

### Conditional Rendering

```xml
<view vmodel="StatusViewModel">
    <title>Status</title>
    <message v-if="VModel.IsLoading">Loading...</message>
    <message v-else-if="VModel.HasError">{{ VModel.ErrorMessage }}</message>
    <message v-else>{{ VModel.Data }}</message>

    <components>
        <command v-if="!VModel.IsLoading" title="Refresh" @click="refresh()" />
        <command v-if="VModel.HasError" title="Retry" @click="retry()" />
    </components>
</view>
```

### Lifecycle Hooks

```xml
<view vmodel="DataViewModel">
    <title>Data View</title>
    <message>{{ message }}</message>
    <components>
        <command title="Reload" @click="reload()" />
    </components>
</view>

<script>
var message = '';

function onMounted() {
    // Called when page is first created
    console.log('Page mounted');
    loadData();
}

function beforeRender() {
    // Return data for templates
    return {
        message: VModel.GetFormattedData()
    };
}

function onUnmounted() {
    // Cleanup when page is disposed
    console.log('Page unmounted');
}

function loadData() {
    UI.status('typing');
    VModel.LoadDataAsync();
    UI.refresh();
}

function reload() {
    loadData();
}
</script>
```

### Form Input via Photo Handler

```xml
<view vmodel="PhotoEditorViewModel">
    <title>Photo Editor</title>
    <message v-if="!VModel.HasPhoto">Please send a photo to edit.</message>
    <message v-else>Photo received! Apply filters below.</message>

    <components v-if="VModel.HasPhoto">
        <command title="Brightness" @click="adjustBrightness()" />
        <command title="Contrast" @click="adjustContrast()" />
        <command title="Done" @click="finalize()" />
    </components>
</view>

<script>
function onPhoto(photoData) {
    UI.status('typing');
    VModel.ProcessPhoto(photoData.fileId);
    UI.refresh();
}

function adjustBrightness() {
    VModel.ApplyBrightness();
    UI.toast('Brightness adjusted');
    UI.refresh();
}

function finalize() {
    VModel.FinalizeAndSend();
    UI.toast('Photo sent!');
    UI.back();
}
</script>
```

---

## Next Steps

### Essential Reading

1. **[Components.md](Components.md)** - Learn about all available components:
   - command, open, checkbox, radio, switch
   - card, navigate, row
   - Directives: v-for, v-if, v-bind, v-on

2. **[JavaScriptAPI.md](JavaScriptAPI.md)** - Complete API reference:
   - UI namespace methods
   - Base object properties
   - Lifecycle hooks
   - Global functions

3. **[ViewModels.md](ViewModels.md)** - C# integration:
   - Creating ViewModels
   - IPropsReceiver interface
   - Async methods
   - Constructor patterns

4. **[Pages.md](Pages.md)** - Advanced page features:
   - Resource loading
   - Media attachments
   - Conditional messages
   - Auto-pagination

### Recommended Next Projects

1. **Todo List Bot** - Practice with:
   - Lists with v-for
   - Checkboxes
   - Data persistence in ViewModels

2. **Settings Bot** - Learn about:
   - Radio buttons
   - Switches
   - State management
   - Navigation patterns

3. **Content Bot** - Explore:
   - External resource loading
   - Photo/document handling
   - Media pages
   - Localization

### Advanced Topics

- **Custom Components** - Create reusable components with `ComponentAttribute`
- **Custom Text Tags** - Extend template syntax with `TextTagAttribute`
- **WebHook Deployment** - Deploy with ASP.NET or standalone server
- **Localization** - Multi-language support with .lpack files
- **Database Integration** - Connect ViewModels to your database
- **Resource Virtualization** - Load resources from database or cloud storage

### Getting Help

- Check the [Documentation](../README.md#documentation) for detailed references
- Review the demo project at [Telegram.Bot.UI.Demo](https://github.com/VeyDlin/Telegram.Bot.UI.Demo)
- Study the source code documentation in `.temp/Telegram.Bot.UI/`

---

## Quick Reference Card

### Page Structure
```xml
<view id="page-id" vmodel="ViewModelName">
    <title>Page Title</title>
    <message>Message content with {{ templates }}</message>
    <components>
        <command title="Button" @click="handler()" />
    </components>
</view>
<script>
function handler() {
    VModel.DoSomething();
    UI.refresh();
}
</script>
```

### Common Components
```xml
<command title="Action" @click="action()" />
<open title="Page" target="page-id" />
<open type="link" title="Link" target="https://url.com" />
<checkbox title="Option" :selected="value" @update="handler(event)" />
<radio :selected="value" @select="handler(event.select.id)">
    <option value="id" title="Title" />
</radio>
<switch :value="current" @update="handler(event)">
    <option value="id" title="Title" />
</switch>
<row>
    <command title="Left" />
    <command title="Right" />
</row>
```

### JavaScript Essentials
```javascript
// Navigation
UI.navigate('page');
UI.back();
UI.close();
UI.refresh();

// Feedback
UI.toast('Message');
UI.alert('Warning');
UI.status('typing');

// Data
VModel.Property
VModel.Method()
await VModel.AsyncMethod()

// User Info
User.chatId
User.localization.code
await User.SendTextMessageAsync('Hi')

// Components
var comp = component('id');
comp.select(value);

// Localization
$t('Key')
```
