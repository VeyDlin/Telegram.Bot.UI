# ViewModels Guide

ViewModels provide a bridge between C# business logic and declarative `.page` files.

## Table of Contents

- [Basic ViewModel](#basic-viewmodel)
- [IPropsReceiver Interface](#ipropsreceiver-interface)
- [Page Configuration](#page-configuration)
- [Accessing from JavaScript](#accessing-from-javascript)
- [Complex ViewModels](#complex-viewmodels)
- [Lifecycle](#lifecycle)
- [Best Practices](#best-practices)

---

## Basic ViewModel

A ViewModel is a plain C# class that exposes properties and methods to JavaScript.

```csharp
namespace MyBot.ViewModels;

public class CounterViewModel {
    public int Count { get; set; } = 0;

    public void Increment() {
        Count++;
    }

    public void Decrement() {
        Count--;
    }

    public void Reset() {
        Count = 0;
    }

    public string GetStatus() {
        return Count switch {
            0 => "Zero",
            > 0 => "Positive",
            < 0 => "Negative"
        };
    }
}
```

---

## IPropsReceiver Interface

Implement `IPropsReceiver` to receive initialization props from the page definition.

```csharp
public interface IPropsReceiver {
    void ReceiveProps(Dictionary<string, object?> props);
}
```

### Example

```csharp
public class CounterViewModel : IPropsReceiver {
    public int Count { get; set; } = 0;

    public void ReceiveProps(Dictionary<string, object?> props) {
        if (props.TryGetValue("initialCount", out var value)) {
            if (value is int intVal) {
                Count = intVal;
            } else if (value is long longVal) {
                Count = (int)longVal;
            } else if (int.TryParse(value?.ToString(), out var parsed)) {
                Count = parsed;
            }
        }
    }

    // ... other methods
}
```

### Page Configuration with Props

```xml
<view vmodel="CounterViewModel" vmodel-props='{"initialCount": 10}'>
    <title>Counter</title>
    <message>Count: {{ VModel.Count }}</message>
</view>
```

---

## Page Configuration

### Specifying ViewModel in Page

```xml
<view vmodel="CounterViewModel">
    <!-- ViewModel is accessible as 'vmodel' in scripts -->
</view>
```

### With Namespace

```xml
<view vmodel="MyBot.ViewModels.PhotoEditorViewModel">
    <!-- Full namespace path -->
</view>
```

### With Props

```xml
<view vmodel="ItemViewModel" vmodel-props='{"itemId": 123, "mode": "edit"}'>
    <!-- Props are passed to ReceiveProps -->
</view>
```

### Assembly Configuration

When creating PageManager, specify the assembly containing ViewModels:

```csharp
var vmodelAssembly = typeof(CounterViewModel).Assembly;
var pageManager = new PageManager(pagesPath, vmodelAssembly);
```

---

## Accessing from JavaScript

### Properties

```javascript
// Read
var count = VModel.Count;
var name = VModel.UserName;

// Write
VModel.Count = 10;
VModel.UserName = 'John';
```

### Methods

```javascript
// Call methods
VModel.Increment();
var result = VModel.Calculate(5, 3);

// Methods with complex parameters
VModel.SaveItem({ name: 'Test', value: 42 });
```

### Async Methods

C# async methods are automatically awaited:

```javascript
// C#: public async Task<List<Item>> LoadItemsAsync()
var items = VModel.LoadItemsAsync();  // Returns result, not Task
```

### In Templates

```xml
<message>Count: {{ VModel.Count }}<br/>Status: {{ VModel.GetStatus() }}</message>
```

---

## Complex ViewModels

### With Dependencies

```csharp
public class UserSettingsViewModel : IPropsReceiver {
    private readonly DatabaseContext _db;
    private long _userId;

    public UserSettingsViewModel(DatabaseContext db) {
        _db = db;
    }

    public void ReceiveProps(Dictionary<string, object?> props) {
        if (props.TryGetValue("userId", out var id)) {
            _userId = Convert.ToInt64(id);
        }
    }

    public string Theme { get; set; } = "light";
    public bool NotificationsEnabled { get; set; } = true;

    public void Load() {
        var settings = _db.UserSettings.Find(_userId);
        if (settings != null) {
            Theme = settings.Theme;
            NotificationsEnabled = settings.Notifications;
        }
    }

    public void Save() {
        var settings = _db.UserSettings.Find(_userId) ?? new UserSettings { UserId = _userId };
        settings.Theme = Theme;
        settings.Notifications = NotificationsEnabled;
        _db.SaveChanges();
    }
}
```

### With Collections

```csharp
public class TodoListViewModel {
    public List<TodoItem> Items { get; set; } = new();

    public void AddItem(string title) {
        Items.Add(new TodoItem {
            Id = Items.Count + 1,
            Title = title,
            Completed = false
        });
    }

    public void ToggleItem(int id) {
        var item = Items.FirstOrDefault(x => x.Id == id);
        if (item != null) {
            item.Completed = !item.Completed;
        }
    }

    public void RemoveItem(int id) {
        Items.RemoveAll(x => x.Id == id);
    }

    public int CompletedCount => Items.Count(x => x.Completed);
    public int TotalCount => Items.Count;
}

public class TodoItem {
    public int Id { get; set; }
    public string Title { get; set; }
    public bool Completed { get; set; }
}
```

### Usage in Page

```xml
<view vmodel="TodoListViewModel">
    <title>Todo List</title>
    <message>Completed: {{ VModel.CompletedCount }} / {{ VModel.TotalCount }}</message>
    <components>
        <card id="todos" max-items="5">
            <checkbox
                v-for="item in VModel.Items"
                :title="item.Title"
                :selected="item.Completed"
                @update="toggleItem(item.Id)" />
        </card>
        <navigate target="todos" />
    </components>
</view>

<script>
function toggleItem(id) {
    VModel.ToggleItem(id);
    UI.refresh();
}
</script>
```

---

## Lifecycle

### ViewModel Creation

1. Page is requested via `PageManager.GetPage()`
2. ViewModel class is resolved from the specified assembly
3. New instance is created using one of these constructor patterns:
   - Constructor with `BaseBotUser` parameter: `MyViewModel(BaseBotUser botUser)`
   - Parameterless constructor: `MyViewModel()`
4. `ReceiveProps()` is called if ViewModel implements `IPropsReceiver`
5. ViewModel is attached to the ScriptPage

### Constructor Patterns

**With BaseBotUser (recommended for accessing user context):**
```csharp
public class UserProfileViewModel {
    private readonly BaseBotUser _botUser;

    public UserProfileViewModel(BaseBotUser botUser) {
        _botUser = botUser;
    }

    public long ChatId => _botUser.chatId;
    public string Language => _botUser.localization.code;

    public async Task SendNotification(string text) {
        await _botUser.SendTextMessageAsync(text);
    }
}
```

**Parameterless (for simple data models):**
```csharp
public class CounterViewModel {
    public int Count { get; set; } = 0;

    public void Increment() => Count++;
    public void Decrement() => Count--;
}
```

### ViewModel Disposal

- ViewModel lives as long as the ScriptPage instance
- When page is disposed, ViewModel becomes eligible for GC
- Implement `IDisposable` if cleanup is needed

```csharp
public class MyViewModel : IDisposable {
    private readonly Timer _timer;

    public MyViewModel() {
        _timer = new Timer(Tick, null, 0, 1000);
    }

    private void Tick(object? state) {
        // Periodic work
    }

    public void Dispose() {
        _timer?.Dispose();
    }
}
```

---

## Static Configuration

For ViewModels that need static setup (like API tokens):

```csharp
public class PhotoEditorViewModel {
    private static string? _apiToken;
    private static IResourceLoader? _resourceLoader;

    public static void Configure(string? apiToken, IResourceLoader resourceLoader) {
        _apiToken = apiToken;
        _resourceLoader = resourceLoader;
    }

    // Instance methods use static configuration
    public async Task<byte[]> ProcessImageAsync(byte[] input) {
        // Use _apiToken and _resourceLoader
    }
}
```

### Setup in Program.cs

```csharp
var resourceLoader = new ResourceLoader("Resources");
PhotoEditorViewModel.Configure(imgbbToken, resourceLoader);
```

---

## Best Practices

### 1. Keep ViewModels Focused

```csharp
// Good: Single responsibility
public class UserProfileViewModel {
    public string Name { get; set; }
    public string Email { get; set; }
    public void Save() { }
}

// Bad: Too many responsibilities
public class EverythingViewModel {
    public string UserName { get; set; }
    public List<Order> Orders { get; set; }
    public ShoppingCart Cart { get; set; }
    // ...
}
```

### 2. Expose Only What's Needed

```csharp
public class SettingsViewModel {
    // Public - accessible from JS
    public string Theme { get; set; }

    // Private - internal only
    private readonly DatabaseContext _db;

    public void Save() {
        // Use _db internally
    }
}
```

### 3. Handle Type Conversions

JavaScript numbers may come as `long` or `double`. Handle both:

```csharp
public void ReceiveProps(Dictionary<string, object?> props) {
    if (props.TryGetValue("id", out var value)) {
        _id = value switch {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            string s when int.TryParse(s, out var p) => p,
            _ => 0
        };
    }
}
```

### 4. Return Simple Types

JavaScript works best with simple types and objects:

```csharp
// Good
public List<object> GetItems() {
    return _items.Select(x => new {
        id = x.Id,
        name = x.Name,
        active = x.IsActive
    }).Cast<object>().ToList();
}

// Avoid complex C# types with circular references
```

### 5. Document Public API

```csharp
/// <summary>
/// ViewModel for the photo editor page.
/// Provides image processing and filter capabilities.
/// </summary>
public class PhotoEditorViewModel {
    /// <summary>
    /// Applies filters to the pending image.
    /// </summary>
    /// <param name="brightness">Brightness level (low, normal, high)</param>
    /// <param name="contrast">Contrast level (low, normal, high)</param>
    public void ApplyFilters(string brightness, string contrast) { }
}
```
