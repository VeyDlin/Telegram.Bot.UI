# Pages Guide

This document covers creating, configuring, and managing declarative `.page` files.

## Table of Contents

- [Page Structure](#page-structure)
- [Page File Format](#page-file-format)
- [PageManager Setup](#pagemanager-setup)
- [Resource Loading](#resource-loading)
- [Virtual Resources](#virtual-resources)
- [Hot Reload](#hot-reload)
- [Navigation](#navigation)
- [Page Caching](#page-caching)

---

## Page Structure

A `.page` file consists of two main sections:

```xml
<view>
    <!-- View definition -->
    <title>Page Title</title>
    <message>Message content</message>
    <components>
        <!-- UI components -->
    </components>
</view>

<script>
// JavaScript logic
</script>
```

### View Section

The `<view>` element defines the page content:

| Element | Description |
|---------|-------------|
| `<title>` | Page title (displayed at top of message) |
| `<message>` | Message body content |
| `<components>` | Container for UI buttons/controls |

### View Attributes

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | string | required | Unique page identifier |
| `vmodel` | string | - | ViewModel class name |
| `vmodel-props` | JSON | - | JSON object passed to ViewModel |
| `resource` | string | - | External resource path for page content |
| `web-preview` | boolean | `true` | Enable link preview in messages |
| `back-title` | string | `"◀ {{ parent.title }}"` | Custom back button template |
| `back-to-parent` | boolean | `true` | Show back button to parent page |
| `max-items` | number | - | Auto-pagination: max items per page |
| `max-rows` | number | - | Auto-pagination: max rows per page |

### Media Attributes

| Attribute | Type | Description |
|-----------|------|-------------|
| `photo` | string | Path to photo resource or file ID |
| `document` | string | Path to document resource or file ID |
| `audio` | string | Path to audio resource or file ID |
| `video` | string | Path to video resource or file ID |

### Script Section

The `<script>` element contains JavaScript for:
- Lifecycle hooks (`onMounted`, `onUnmounted`, etc.)
- Event handlers
- Helper functions
- Data processing

---

## Page File Format

### Basic Page

```xml
<view>
    <title>Welcome</title>
    <message>Hello! Welcome to our bot.</message>
    <components>
        <command title="Start" @click="start()" />
        <open title="Help" target="help" />
    </components>
</view>

<script>
function start() {
    UI.navigate('main-menu');
}
</script>
```

> **Note:** HTML mode is used by default. Use `<br/>` for line breaks in messages.

### Page with ViewModel

```xml
<view vmodel="CounterViewModel" vmodel-props='{"initialCount": 0}'>
    <title>Counter</title>
    <message>Current count: {{ VModel.Count }}<br/>Status: {{ VModel.GetStatus() }}</message>
    <components>
        <row>
            <command title="-" @click="decrement()" />
            <command title="{{ VModel.Count }}" @click="reset()" />
            <command title="+" @click="increment()" />
        </row>
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
    UI.refresh();
}
</script>
```

### Page with Photo

```xml
<view photo="backgrounds/welcome.png">
    <title>Photo Demo</title>
    <message>This message includes a photo.</message>
    <components>
        <command title="Next" @click="UI.navigate('next')" />
    </components>
</view>
```

### Page with Web Preview

```xml
<view web-preview="true">
    <title>Link Preview</title>
    <message>Check out this article: https://example.com/article<br/><br/>The link above will show a preview.</message>
</view>
```

### Page with Document

```xml
<view document="files/manual.pdf">
    <title>User Manual</title>
    <message>Here is the user manual.</message>
    <components>
        <command title="Back" @click="UI.back()" />
    </components>
</view>
```

### Page with Conditional Message

Use `v-if`, `v-else-if`, and `v-else` on `<message>` elements for conditional content:

```xml
<view vmodel="StatusViewModel">
    <title>Status</title>
    <message v-if="VModel.Status === 'loading'">Loading data...</message>
    <message v-else-if="VModel.Status === 'error'">Error occurred. Please try again.</message>
    <message v-else>Data loaded successfully!</message>
    <components>
        <command title="Refresh" @click="refresh()" />
    </components>
</view>

<script>
function refresh() {
    VModel.LoadData();
    UI.refresh();
}
</script>
```

### Message Attributes

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `resource` | string | - | Load message content from external file |
| `md` | boolean | `false` | Enable Markdown parsing |
| `pre` | boolean | `false` | Preserve whitespace and newlines |
| `v-if` | expression | - | Conditional rendering condition |
| `v-else-if` | expression | - | Alternative condition |
| `v-else` | - | - | Default when all conditions are false |

---

## PageManager Setup

### Basic Setup

```csharp
// Path to pages directory
var pagesPath = Path.Combine("Resources", "Pages");

// Assembly containing ViewModel classes
var vmodelAssembly = typeof(MyViewModel).Assembly;

// Create PageManager
var pageManager = new PageManager(pagesPath, vmodelAssembly);

// Load all pages from directory
pageManager.LoadAll();

// Log loaded pages
Console.WriteLine($"Loaded {pageManager.PageCount} pages");
Console.WriteLine($"Pages: {string.Join(", ", pageManager.GetPageIds())}");
```

### Directory Structure

```
Resources/
└── Pages/
    ├── home.page
    ├── settings.page
    ├── help.page
    └── admin/
        ├── dashboard.page
        └── users.page
```

Page IDs are derived from file paths:
- `home.page` → `home`
- `settings.page` → `settings`
- `admin/dashboard.page` → `admin/dashboard`

### Getting Pages

```csharp
// Get page for a specific user
var page = pageManager.GetPage("settings", botUser);

// Send the page
await page.SendPageAsync();
```

---

## Resource Loading

### IResourceLoader Interface

```csharp
public interface IResourceLoader {
    string? BasePath { get; }
    string? ResolvePath(string name);
    byte[] GetBytes(string name);
    string GetText(string name);
    bool Exists(string name);
    void ClearCache();
    void ClearCache(string name);
}
```

### Default ResourceLoader

```csharp
var resourceLoader = new ResourceLoader("Resources");

// Use with bot worker
var bot = new BotWorkerPulling<MyBotUser>(...) {
    resourceLoader = resourceLoader,
    // ...
};
```

### Resource Access in Pages

```xml
<!-- Photo from resources -->
<view photo="images/banner.png">
    ...
</view>
```

### Resource Paths

Resources can be specified with different prefixes:

**Absolute paths (from resource base):**
```xml
<view photo="images/banner.png">
    <!-- Resolves to: Resources/images/banner.png -->
</view>
```

**Page-relative paths:**
```xml
<!-- Using @/ prefix - relative to page file directory -->
<view photo="@/images/banner.png">
    <!-- If page is at Pages/admin/dashboard.page -->
    <!-- Resolves to: Pages/admin/images/banner.png -->
</view>

<!-- Using ./ prefix - current directory -->
<view photo="./banner.png">
    <!-- If page is at Pages/admin/dashboard.page -->
    <!-- Resolves to: Pages/admin/banner.png -->
</view>

<!-- Using ../ prefix - parent directory -->
<view photo="../images/shared.png">
    <!-- If page is at Pages/admin/dashboard.page -->
    <!-- Resolves to: Pages/images/shared.png -->
</view>
```

**In message resource loading:**
```xml
<view>
    <!-- Absolute from base -->
    <message resource="texts/welcome.md" />

    <!-- Relative to page -->
    <message resource="@/texts/welcome.md" />
</view>
```

---

## Virtual Resources

Implement `IResourceLoader` for custom resource sources (database, cloud storage, etc.).

### Database-Backed ResourceLoader

```csharp
public class DatabaseResourceLoader : IResourceLoader {
    private readonly DatabaseContext _db;
    private readonly Dictionary<string, byte[]> _cache = new();

    public string? BasePath => null;

    public DatabaseResourceLoader(DatabaseContext db) {
        _db = db;
    }

    public byte[] GetBytes(string name) {
        if (_cache.TryGetValue(name, out var cached)) {
            return cached;
        }

        var resource = _db.Resources.FirstOrDefault(r => r.Path == name);
        if (resource == null) {
            throw new FileNotFoundException($"Resource not found: {name}");
        }

        _cache[name] = resource.Data;
        return resource.Data;
    }

    public string GetText(string name) {
        var bytes = GetBytes(name);
        return Encoding.UTF8.GetString(bytes);
    }

    public bool Exists(string name) {
        return _cache.ContainsKey(name) ||
               _db.Resources.Any(r => r.Path == name);
    }

    public string? ResolvePath(string name) {
        return Exists(name) ? name : null;
    }

    public void ClearCache() {
        _cache.Clear();
    }

    public void ClearCache(string name) {
        _cache.Remove(name);
    }
}
```

### Usage

```csharp
var dbResourceLoader = new DatabaseResourceLoader(dbContext);

var bot = new BotWorkerPulling<MyBotUser>(...) {
    resourceLoader = dbResourceLoader,
    // ...
};
```

### Hybrid ResourceLoader

Combine file system and database:

```csharp
public class HybridResourceLoader : IResourceLoader {
    private readonly ResourceLoader _fileLoader;
    private readonly DatabaseResourceLoader _dbLoader;

    public HybridResourceLoader(string basePath, DatabaseContext db) {
        _fileLoader = new ResourceLoader(basePath);
        _dbLoader = new DatabaseResourceLoader(db);
    }

    public byte[] GetBytes(string name) {
        // Try file system first
        if (_fileLoader.Exists(name)) {
            return _fileLoader.GetBytes(name);
        }
        // Fall back to database
        return _dbLoader.GetBytes(name);
    }

    // ... other methods
}
```

---

## Hot Reload

Page definitions are loaded at startup. To see changes during development:

### Manual Cache Clear

```csharp
// In BotUser - clear page cache to force reload
public void ClearPageCache() {
    foreach (var page in pageCache.Values) {
        page.Dispose();
    }
    pageCache.Clear();
}
```

### Development Workflow

1. Edit `.page` files
2. Send `/reset` command in bot to clear user's page cache
3. Navigate to page again - fresh definition will be loaded

### Reload Command Example

Add a command to clear the page cache during development:

```csharp
public override async Task HandleCommandAsync(string cmd, string[] arguments, Message message) {
    switch (cmd) {
        case "reset":
            ClearPageCache();
            await SendTextMessageAsync("Page cache cleared. Use /start to begin fresh.");
            break;
        // ... other commands
    }
}
```

---

## Navigation

### Navigation Methods

```javascript
// Navigate using cached page (preserves state)
UI.navigate('settings');

// Navigate with fresh page instance
UI.navigateFresh('settings');

// Navigate as main page (clears navigation history)
UI.navigate('home', false);

// Send new message (doesn't replace current)
UI.sendPage('confirmation');

// Go back to parent page
UI.back();

// Close current page
UI.close();
```

### Sub-Pages vs Main Pages

**Sub-Page (default):**
- Preserves parent page reference
- `UI.back()` returns to parent
- User can navigate back

**Main Page:**
- Clears navigation history
- `UI.back()` does nothing
- Fresh start

```javascript
// Sub-page navigation
UI.navigate('details');        // Can go back
UI.navigate('details', true);  // Explicit sub-page

// Main page navigation
UI.navigate('home', false);    // No back navigation
```

### Programmatic Navigation in C#

```csharp
// In BotUser command handler
public override async Task HandleCommandAsync(string cmd, string[] arguments, Message message) {
    switch (cmd) {
        case "start":
            var page = pageManager.GetPage("home", this);
            if (page != null) {
                await page.SendPageAsync();
            }
            break;
    }
}
```

---

## Page Caching

### User-Level Caching

Cache pages per user to preserve state (pagination, selections):

```csharp
public class MyBotUser : BaseBotUser {
    private Dictionary<string, ScriptPage> pageCache = new();

    public override ScriptPage? GetOrCreateCachedPage(string pageId, PageManager pageManager) {
        if (pageCache.TryGetValue(pageId, out var cached)) {
            return cached;
        }

        var page = pageManager.GetPage(pageId, this);
        if (page != null) {
            pageCache[pageId] = page;
        }
        return page;
    }

    public void ClearPageCache() {
        foreach (var page in pageCache.Values) {
            page.Dispose();
        }
        pageCache.Clear();
    }
}
```

### Benefits of Caching

1. **State Preservation** - Pagination position, form inputs retained
2. **Performance** - No re-parsing of page definitions
3. **Memory Efficiency** - Component instances reused

### When to Clear Cache

- User logs out
- Major state change (language switch)
- User requests reset (`/reset` command)
- After hot reload in development

---

## Best Practices

### 1. Organize Pages by Feature

```
Pages/
├── auth/
│   ├── login.page
│   └── register.page
├── settings/
│   ├── main.page
│   ├── profile.page
│   └── notifications.page
└── shop/
    ├── catalog.page
    ├── cart.page
    └── checkout.page
```

### 2. Keep Pages Focused

Each page should have a single purpose:
- `settings.page` - Settings menu
- `language.page` - Language selection
- `theme.page` - Theme selection

### 3. Use ViewModels for Complex Logic

Keep JavaScript light, delegate to ViewModels:

```xml
<script>
// Good: ViewModel handles logic
function save() {
    VModel.Save();
    UI.toast('Saved!');
}

// Avoid: Complex logic in JavaScript
function save() {
    var data = collectFormData();
    validateData(data);
    transformData(data);
    // ...
}
</script>
```

### 4. Handle Errors Gracefully

```xml
<script>
function performAction() {
    try {
        VModel.DoSomething();
        UI.toast('Success!');
    } catch (e) {
        UI.alert('Error: ' + e.message);
    }
}
</script>
```

### 5. Provide User Feedback

```javascript
// Show loading state
UI.status('typing');

// Perform action
VModel.ProcessData();

// Show result
UI.toast('Done!');
UI.refresh();
```
