# JavaScript API Reference

This document describes the JavaScript API available in `.page` files.

## Table of Contents

- [UI Namespace](#ui-namespace)
- [Global Functions](#global-functions)
- [Page Lifecycle Hooks](#page-lifecycle-hooks)
- [Global Variables](#global-variables)
- [ViewModel Access](#viewmodel-access)
- [Async Operations](#async-operations)

---

## UI Namespace

All page control functions are available through the `UI` namespace object. This keeps the global scope clean and makes the API clear.

### UI.navigate(pageId, subPage?, props?)

Navigates to another page using the page cache.

```javascript
UI.navigate('settings');              // Navigate as sub-page
UI.navigate('home', false);           // Navigate as main page (clears history)
UI.navigate('editor', true, { id: 5 }); // Navigate with props
```

**Parameters:**
- `pageId` (string) - The ID of the page to navigate to
- `subPage` (boolean, default: `true`) - If true, preserves back navigation
- `props` (object, optional) - Props to pass to the target page's ViewModel

### UI.navigateFresh(pageId, subPage?, props?)

Navigates to a page with a fresh instance (bypasses cache, clears page state).

```javascript
UI.navigateFresh('editor');           // Fresh instance as sub-page
UI.navigateFresh('home', false);      // Fresh instance as main page
```

### UI.sendPage(pageId)

Sends a new message with the specified page (doesn't replace current message).

```javascript
UI.sendPage('confirmation');          // Sends new message
```

### UI.back()

Navigates back to the parent page.

```javascript
UI.back();
```

### UI.close()

Deletes the current message.

```javascript
UI.close();
```

### UI.dispose()

Disposes the page (clears keyboard, frees memory, but doesn't delete message).

```javascript
UI.dispose();
```

### UI.refresh()

Refreshes the current page (re-renders and updates the message).

```javascript
UI.refresh();
```

### UI.clearKeyboard()

Removes the inline keyboard from the current message without deleting it.

```javascript
UI.clearKeyboard();
```

### UI.toast(text)

Shows a brief notification (Telegram callback query answer). Only works during button click callbacks.

```javascript
UI.toast('Settings saved!');
UI.toast('Item added to cart');
```

### UI.alert(text)

Shows an alert popup that requires user to dismiss (Telegram callback query answer with showAlert). Only works during button click callbacks.

```javascript
UI.alert('Are you sure you want to delete this item?');
UI.alert('Operation completed successfully');
```

### UI.status(type)

Shows a chat action indicator (typing, uploading, etc.).

```javascript
UI.status('typing');           // Shows "typing..."
UI.status('upload_photo');     // Shows "uploading photo..."
UI.status('upload_video');     // Shows "uploading video..."
UI.status('upload_document');  // Shows "uploading document..."
UI.status('record_video');     // Shows "recording video..."
UI.status('record_voice');     // Shows "recording voice..."
```

### UI.nextPage() / UI.prevPage()

Navigate between pages in a multi-page view.

```javascript
UI.nextPage();    // Go to next page
UI.prevPage();    // Go to previous page
```

### UI.goToPage(index)

Go to a specific page index in multi-page view.

```javascript
UI.goToPage(0);   // Go to first page
UI.goToPage(2);   // Go to third page
```

### UI.getPageCount() / UI.getCurrentPage()

Get pagination information.

```javascript
var total = UI.getPageCount();     // Total number of pages
var current = UI.getCurrentPage(); // Current page index (0-based)
```

---

## Global Functions

### component(id)

Gets a component by its ID for programmatic access.

```javascript
var checkbox = component('myCheckbox');
checkbox.toggle();

var radio = component('languageSelector');
radio.select('en');

var switcher = component('themeSwitch');
switcher.cycleTo('dark');
```

### $t(key)

Translates a localization key using the current user's language.

```javascript
var text = $t('Save');
UI.toast($t('Settings saved!'));
```

---

## Page Lifecycle Hooks

Define these functions in your `<script>` block to hook into page lifecycle events.

### onMounted()

Called when the page is first created and ready.

```xml
<script>
function onMounted() {
    console.log('Page mounted');
    loadData();
}
</script>
```

### onUnmounted()

Called when the page is being disposed.

```xml
<script>
function onUnmounted() {
    console.log('Page unmounted');
    cleanup();
}
</script>
```

### beforeRender()

Called before each render. Variables returned from this function are available in templates.

```xml
<script>
function beforeRender() {
    return {
        items: loadItems(),
        count: items.length
    };
}
</script>
```

### afterRender()

Called after each render completes.

```xml
<script>
function afterRender() {
    console.log('Render complete');
}
</script>
```

### onRefresh()

Called when the page is explicitly refreshed via `UI.refresh()`.

```xml
<script>
function onRefresh() {
    console.log('Page refreshed');
    reloadData();
}
</script>
```

### onPhoto(photoData)

Called when a user sends a photo to the bot while this page is active.

```xml
<script>
function onPhoto(photoData) {
    console.log('Photo received:', photoData.fileId);
    console.log('File size:', photoData.fileSize);
    VModel.ProcessPhoto(photoData.fileId);
    UI.refresh();
}
</script>
```

**Photo Data Structure:**
```javascript
{
    fileId: string,         // Telegram file ID
    fileUniqueId: string,
    width: number,
    height: number,
    fileSize: number,
    messageId: number,      // Message ID containing the photo
    caption: string         // Photo caption (if any)
}
```

### onDocument(documentData)

Called when a user sends a document to the bot while this page is active.

```xml
<script>
function onDocument(documentData) {
    console.log('Document received:', documentData.fileName);
    console.log('MIME type:', documentData.mimeType);
    VModel.ProcessDocument(documentData.fileId);
    UI.refresh();
}
</script>
```

**Document Data Structure:**
```javascript
{
    fileId: string,         // Telegram file ID
    fileUniqueId: string,
    fileName: string,
    mimeType: string,
    fileSize: number,
    messageId: number,      // Message ID containing the document
    caption: string         // Document caption (if any)
}
```

### Example: Complete Lifecycle

```xml
<view>
    <title>My Page</title>
    <message>Items: {{ count }}</message>
    <components>
        <command v-for="item in items" :title="item.name" @click="selectItem(item)" />
    </components>
</view>

<script>
var items = [];

function onMounted() {
    console.log('Loading data...');
    items = VModel.loadItems();
}

function beforeRender() {
    return {
        items: items,
        count: items.length
    };
}

function selectItem(item) {
    toast('Selected: ' + item.name);
}

function onUnmounted() {
    console.log('Cleanup...');
    items = [];
}
</script>
```

---

## Global Variables

These variables are automatically available in the script context.

### Base

Provides access to the current `ScriptPage` instance. This object represents the page itself and its navigation context.

```javascript
// Page information
var pageId = Base.pageId;           // Current page ID
var pageTitle = Base.title;         // Current page title
var parentPage = Base.parent;       // Parent page (if navigated as sub-page)

// Page directory for relative resources
var dir = Base.pageDirectory;
```

**Common Base Properties:**
- `pageId` - The ID of the current page
- `title` - The page title
- `parent` - Parent page reference (for back navigation)
- `pageDirectory` - Directory containing the page file

### User

Provides access to the `BaseBotUser` instance and its properties. This is your primary way to access user-specific information and bot functionality.

```javascript
// User information
var chatId = User.chatId;
var language = User.localization.code;

// Localization
var translatedText = User.L('Hello');           // Translate immediately
var lazyText = User.LS('Goodbye');             // Lazy localization

// Send messages
await User.SendTextMessageAsync('Hello!');
await User.SendPhotoAsync(photoBytes);
await User.DeleteMessageAsync(messageId);

// Chat actions
await User.SendChatActionAsync('typing');

// Logger
User.logger.LogInformation('User clicked button');
```

**Common User Properties:**
- `chatId` - User's Telegram chat ID
- `localization.code` - Current language code (e.g., "en", "ru")
- `parseMode` - Default parse mode (Markdown, Html, etc.)
- `client` - ITelegramBotClient instance
- `worker` - IBotWorker instance
- `logger` - ILogger instance
- `callbackFactory` - CallbackFactory instance

### VModel

Access to the ViewModel instance (if configured via `vmodel` attribute in `<view>`).

```javascript
var count = VModel.Count;
VModel.Increment();
var items = VModel.GetItems();
```

### props

Props passed to the page via navigation. Available if the page was opened with props.

```javascript
// Page opened with: UI.navigate('editor', true, { itemId: 5, mode: 'edit' })
var itemId = props.itemId;  // 5
var mode = props.mode;      // 'edit'
```

### page

Access to the current ScriptPage instance (advanced usage).

```javascript
// Get page directory
var dir = page.pageDirectory;

// Access compiled page data
var pageId = page.pageId;
```

### callbackQueryId

Available in event handlers during button callbacks. The Telegram callback query ID.

```javascript
function handleClick() {
    // callbackQueryId is automatically available in button click handlers
    UI.toast('Clicked!');
}
```

---

## ViewModel Access

ViewModels provide C# business logic to JavaScript.

### Accessing Properties

```javascript
// Read property
var value = VModel.PropertyName;

// Write property
VModel.PropertyName = newValue;
```

### Calling Methods

```javascript
// Synchronous method
var result = VModel.Calculate(a, b);

// Method with no return
VModel.Save();
```

### Async Methods

C# async methods (`Task` and `Task<T>`) are automatically handled by the JavaScript engine. You **must** use `await` to get the actual result:

```javascript
// C# method: public async Task<string> LoadDataAsync()
var data = await VModel.LoadDataAsync();   // Correct - returns string

// Without await - WRONG! Returns a Task object, not the actual data
var task = VModel.LoadDataAsync();         // Wrong - don't do this!
```

**Important:** All methods returning `Task` or `Task<T>` in C# must be awaited in JavaScript, otherwise you'll receive a Task object instead of the expected result.

### Example

```xml
<view>
    <title>Counter Demo</title>
    <message>Count: {{ count }}<br/>Status: {{ status }}</message>
    <components>
        <row>
            <command title="-" @click="decrement()" />
            <command title="{{ count }}" @click="reset()" />
            <command title="+" @click="increment()" />
        </row>
    </components>
</view>

<script>
function beforeRender() {
    return {
        count: VModel.Count,
        status: VModel.GetStatus()
    };
}

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

---

## Async Operations

### Sync vs Async Functions

The UI namespace provides both sync and async versions of most functions:

| Sync (no await) | Async (with await) |
|-----------------|-------------------|
| `UI.refresh()` | `await UI.refreshAsync()` |
| `UI.navigate(id)` | `await UI.navigateAsync(id)` |
| `UI.navigateFresh(id)` | `await UI.navigateFreshAsync(id)` |
| `UI.toast(text)` | `await UI.toastAsync(text)` |
| `UI.alert(text)` | `await UI.alertAsync(text)` |
| `UI.close()` | `await UI.closeAsync()` |
| `UI.dispose()` | `await UI.disposeAsync()` |
| `UI.sendPage(id)` | `await UI.sendPageAsync(id)` |
| `UI.back()` | `await UI.backAsync()` |
| `UI.clearKeyboard()` | `await UI.clearKeyboardAsync()` |
| `UI.nextPage()` | `await UI.nextPageAsync()` |
| `UI.prevPage()` | `await UI.prevPageAsync()` |
| `UI.goToPage(i)` | `await UI.goToPageAsync(i)` |

Sync functions execute immediately and block until complete. Async functions return Promises and can be awaited.

### Calling C# Async Methods

Always use `await` for C# async methods:

```javascript
// Correct - waits for result
var data = await VModel.LoadDataAsync();

// Wrong - returns Task, not data!
var task = VModel.LoadDataAsync();
```

### Using User (BotUser) Methods

```javascript
// These are async - use await
await User.SendTextMessageAsync('Hello!');
await User.SendPhotoAsync(photoBytes);
await User.DeleteMessageAsync(messageId);
await User.SendChatActionAsync('typing');
```

### Long-Running Operations

For long operations, navigate to a "wait" page first:

```javascript
async function startProcess() {
    UI.navigate('wait-page');
    await VModel.StartLongProcessAsync();
    UI.navigate('result-page');
}
```

---

## Localization

### $t(key) Function

Translates a localization key:

```javascript
var text = $t('Save');
toast($t('Settings saved!'));
```

### In Templates

```xml
<title>{{ $t('Settings') }}</title>
```

### With Binding

```xml
<command :title="$t('Save')" />
<command :title="'✅ ' + $t('Confirm')" />
```

---

## Event Object

Event handlers receive an `event` object with context-specific data.

### Checkbox Events

```javascript
function onCheckboxChange(event) {
    console.log('Selected:', event.selected);  // boolean
}
```

### Radio Events

```javascript
function onRadioSelect(event) {
    console.log('ID:', event.select.id);
    console.log('Title:', event.select.title);
}
```

### Switch Events

```javascript
function onSwitchChange(event) {
    console.log('ID:', event.id);
    console.log('Title:', event.title);
    console.log('Index:', event.index);
    console.log('Count:', event.count);
}
```

### General Callback Events

```javascript
function onClick(event) {
    console.log('Callback ID:', event.callbackQueryId);
    console.log('Message ID:', event.messageId);
    console.log('Chat ID:', event.chatId);
}
```

---

## Console Logging

Use `console.log()` for debugging (outputs to server logs via Jint engine):

```javascript
console.log('Debug message');
console.log('User:', Base.chatId);
console.log('Value:', someVariable);
```

**Note:** Console output appears in your application's logs, not in the Telegram chat or browser console.

---

## Error Handling

Use try-catch for error handling:

```javascript
function riskyOperation() {
    try {
        VModel.DoSomething();
        toast('Success!');
    } catch (e) {
        alert('Error: ' + e.message);
    }
}
```

---

## Best Practices

1. **Use ViewModels for business logic** - Keep JavaScript light, delegate to C#
2. **Refresh after state changes** - Call `refresh()` after modifying ViewModel
3. **Use beforeRender for data** - Return data objects for template rendering
4. **Handle navigation properly** - Use `subPage=true` to preserve back navigation
5. **Show feedback** - Use `toast()` for confirmations, `alert()` for important messages
