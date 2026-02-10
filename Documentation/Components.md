# Components Reference

This document describes all available UI components for declarative `.page` files.

## Table of Contents

- [command](#command)
- [open](#open)
- [checkbox](#checkbox)
- [checkbox-list](#checkbox-list)
- [radio](#radio)
- [switch](#switch)
- [card](#card)
- [navigate](#navigate)
- [row](#row)

---

## command

A button that executes a JavaScript callback when clicked.

### Attributes

| Attribute | Type | Description |
|-----------|------|-------------|
| `title` | string | Button text (supports `{{ }}` templates) |
| `@click` | JS expression | JavaScript code to execute on click |
| `id` | string | Component ID for JavaScript access |
| `hide` | boolean | Hide the button conditionally |
| `columns` | number | Number of columns this button spans |
| `variant` | string | Button style: `primary`, `danger`, `success` |
| `icon` | string | Custom emoji ID for button icon |

### Example

```xml
<command title="Save" @click="save()" />
<command title="Delete" @click="deleteItem(item.id)" />
<command :title="$t('Submit')" @click="submit()" />
```

### JavaScript API

```javascript
var btn = component('myButton');
btn.onClick = function() {
    console.log('Clicked!');
};
```

---

## open

A button that opens a page, external link, or Telegram Web App.

### Attributes

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `title` | string | target value | Button text |
| `target` | string | required | Page ID, URL, or Web App URL |
| `type` | enum | `page` | One of: `page`, `link`, `app` |
| `subPage` | boolean | `true` | Open as sub-page (preserves back navigation) |
| `id` | string | - | Component ID for JavaScript access |
| `hide` | boolean | `false` | Hide the button conditionally |
| `variant` | string | - | Button style: `primary`, `danger`, `success` |
| `icon` | string | - | Custom emoji ID for button icon |

### Type Values

- `page` - Opens another bot page (internal navigation)
- `link` - Opens an external URL (opens in browser)
- `app` - Opens a Telegram Web App

### Examples

```xml
<!-- Open internal page -->
<open title="Settings" target="settings" />

<!-- Open as fresh page (not sub-page) -->
<open title="Home" target="home" subPage="false" />

<!-- External link -->
<open type="link" title="Documentation" target="https://example.com/docs" />

<!-- Telegram Web App -->
<open type="app" title="Launch App" target="https://mywebapp.com" />
```

---

## checkbox

A toggleable checkbox button.

### Attributes

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `title` | string | required | Checkbox label |
| `template` | string | `{{ (self.isSelected ? '✅ ' : '') + self.title }}` | Display template |
| `:selected` | JS expression | - | Two-way binding for checked state |
| `@update` | JS expression | - | Handler called when state changes |
| `id` | string | - | Component ID for JavaScript access |
| `hide` | boolean | `false` | Hide the checkbox |
| `variant` | string | - | Button style: `primary`, `danger`, `success` |
| `icon` | string | - | Custom emoji ID for button icon |

### Template Variables

The `self` object in templates contains:
- `self.isSelected` - boolean, current checked state
- `self.title` - string, the title text

### Examples

```xml
<!-- Basic checkbox -->
<checkbox title="Enable notifications" id="notifications" />

<!-- With binding -->
<checkbox title="Dark mode" :selected="settings.darkMode" @update="saveDarkMode(event.selected)" />

<!-- Custom template -->
<checkbox title="Accept terms" template="{{ self.isSelected ? '[X] ' : '[ ] ' }}{{ self.title }}" />
```

### JavaScript API

```javascript
var cb = component('notifications');
cb.toggle();           // Toggle state
cb.select(true);       // Set to checked
cb.select(false);      // Set to unchecked
console.log(cb.isSelected);  // Get current state

cb.onUpdate = function(e) {
    console.log('New state:', e.selected);
};
```

---

## checkbox-list

A multi-select list where multiple options can be toggled on/off.

### Attributes

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `template` | string | `{{ (self.isChecked ? '✅ ' : '') + self.title }}` | Display template |
| `:selected` | JS expression | - | Binding for selected IDs (comma-separated) |
| `@update` | JS expression | - | Handler called when selection changes |
| `id` | string | - | Component ID for JavaScript access |
| `columns` | number | `1` | Number of columns for button layout |
| `max-items` | number | - | Maximum items per page (enables pagination with `<navigate>`) |
| `hide` | boolean | `false` | Hide the checkbox list |
| `variant` | string | - | Default button style for all options: `primary`, `danger`, `success` |
| `icon` | string | - | Default custom emoji ID for all option buttons |

### Child Elements

Options are defined using `<option>` child elements (same as radio). Each `<option>` can also have `variant` and `icon` attributes that override the component-level values.

### Template Variables

The `self` object in templates contains:
- `self.isChecked` - boolean, whether this option is checked
- `self.title` - string, the option title
- `self.index` - number, option index

### Examples

```xml
<!-- Basic checkbox list -->
<checkbox-list id="features" @update="onFeatureChange(event)">
    <option value="dark-mode" title="Dark Mode" />
    <option value="notifications" title="Notifications" />
    <option value="auto-save" title="Auto Save" />
</checkbox-list>

<!-- With binding and pagination -->
<checkbox-list id="tags" max-items="5" :selected="selectedTags">
    <option v-for="tag in tags" :value="tag.id" :title="tag.name" />
</checkbox-list>
<navigate target="tags" />
```

### JavaScript API

```javascript
var list = component('features');
list.toggle('dark-mode');          // Toggle option
list.setChecked('notifications', true);  // Set specific state
console.log(list.IsChecked('dark-mode'));  // Check if selected
console.log(list.selectedIds);     // Get all selected IDs
console.log(list.checkedCount);    // Get count of selected

list.onUpdate = function(e) {
    console.log('Changed:', e.item.id, 'checked:', e.isChecked);
};
```

---

## radio

A group of radio buttons where only one option can be selected.

### Attributes

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `template` | string | `{{ (self.isSelected ? '✅ ' : '') + self.title }}` | Display template |
| `:selected` | JS expression | - | Binding for selected option ID |
| `@select` | JS expression | - | Handler called when selection changes |
| `id` | string | - | Component ID for JavaScript access |
| `columns` | number | `1` | Number of columns for button layout |
| `max-items` | number | - | Maximum items per page (enables pagination with `<navigate>`) |
| `hide` | boolean | `false` | Hide the radio group |
| `variant` | string | - | Default button style for all options: `primary`, `danger`, `success` |
| `icon` | string | - | Default custom emoji ID for all option buttons |

### Child Elements

Radio buttons are defined using `<option>` child elements. Each `<option>` can also have `variant` and `icon` attributes that override the component-level values.

| Attribute | Type | Description |
|-----------|------|-------------|
| `value` | string | Option identifier |
| `title` | string | Option display text |
| `:title` | JS expression | Dynamic title (use `$t()` for localization) |
| `v-for` | expression | Generate options dynamically |
| `variant` | string | Per-option button style: `primary`, `danger`, `success` |
| `icon` | string | Per-option custom emoji ID |

### Examples

```xml
<!-- Static options -->
<radio id="language" :selected="userLanguage" @select="setLanguage(event.select.id)">
    <option value="en" title="English" />
    <option value="ru" title="Русский" />
    <option value="de" title="Deutsch" />
</radio>

<!-- Dynamic options with v-for -->
<radio id="color" @select="selectColor(event.select.id)">
    <option v-for="color in colors" :value="color.id" :title="color.name" />
</radio>
```

### JavaScript API

```javascript
var radio = component('language');
radio.select('ru');              // Select by ID
console.log(radio.selectedId);   // Get selected ID
console.log(radio.selectedTitle); // Get selected title
console.log(radio.selected);     // Get selected index

radio.onUpdate = function(e) {
    console.log('Selected:', e.selectedId, e.selectedTitle);
};
```

---

## switch

A carousel button that cycles through options on each click.

### Attributes

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `title` | string | `{{ self.title }}` | Display template |
| `:value` | JS expression | - | Binding for current value ID |
| `@update` | JS expression | - | Handler called when value changes |
| `id` | string | - | Component ID for JavaScript access |
| `hide` | boolean | `false` | Hide the switch |
| `variant` | string | - | Button style: `primary`, `danger`, `success` |
| `icon` | string | - | Custom emoji ID for button icon |

### Child Elements

Options are defined using `<option>` child elements (same as radio).

### Template Variables

The `self` object in templates contains:
- `self.id` - current option ID
- `self.title` - current option title
- `self.index` - current option index (0-based)
- `self.count` - total number of options

### Examples

```xml
<!-- Basic switch -->
<switch id="theme" :value="settings.theme" @update="saveTheme(event.id)">
    <option value="light" title="Light Theme" />
    <option value="dark" title="Dark Theme" />
    <option value="auto" title="Auto" />
</switch>

<!-- Custom template showing count -->
<switch id="filter" title="{{ self.title }} ({{ self.index + 1 }}/{{ self.count }})">
    <option value="all" title="All" />
    <option value="active" title="Active" />
    <option value="completed" title="Completed" />
</switch>
```

### JavaScript API

```javascript
var sw = component('theme');
sw.cycleTo('dark');       // Select specific option
sw.cycleNext();           // Advance to next option
console.log(sw.currentId);     // Get current ID
console.log(sw.currentTitle);  // Get current title
console.log(sw.currentIndex);  // Get current index

sw.onUpdate = function(option) {
    console.log('New value:', option.id, option.title);
};
```

---

## card

A container for grouping components with optional pagination.

### Attributes

| Attribute | Type | Description |
|-----------|------|-------------|
| `max-items` | number | Maximum items per page (enables pagination) |
| `max-rows` | number | Maximum rows per page (enables pagination) |
| `id` | string | Component ID for JavaScript access |
| `hide` | boolean | Hide the card |

### Examples

```xml
<!-- Basic card (no pagination) -->
<card>
    <command title="Option 1" @click="action1()" />
    <command title="Option 2" @click="action2()" />
    <command title="Option 3" @click="action3()" />
</card>

<!-- Card with pagination (3 items per page) -->
<card id="items" max-items="3">
    <command v-for="item in items" :title="item.name" @click="selectItem(item.id)" />
</card>
<navigate target="items" />

<!-- Manual pages -->
<card id="wizard">
    <page>
        <command title="Step 1" @click="nextStep()" />
    </page>
    <page>
        <command title="Step 2" @click="nextStep()" />
    </page>
    <page>
        <command title="Finish" @click="finish()" />
    </page>
</card>
```

### JavaScript API

```javascript
var card = component('items');
card.GoToPage(0);          // Go to first page
console.log(card.currentPage);  // Current page index
console.log(card.pageCount);    // Total pages
```

---

## navigate

Navigation controls for paginated components (card, radio, checkbox-list).

### Attributes

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `target` | string | - | ID of the paginated component to control |
| `prevTitle` | string | `◀` | Previous button text |
| `nextTitle` | string | `▶` | Next button text |
| `counterTitle` | string | `{{ self.currentPage + 1 }} / {{ self.pageCount }}` | Counter display template |
| `showCounter` | boolean | `true` | Show page counter |
| `carousel` | boolean | `true` | Enable wrap-around navigation |
| `hideBoundary` | boolean | `false` | Hide prev/next at boundaries |
| `boundaryMessage` | string | - | Toast message at boundaries |
| `@click` | JS expression | - | Handler for counter button click |
| `variant` | string | - | Button style for all navigation buttons: `primary`, `danger`, `success` |
| `icon` | string | - | Custom emoji ID for navigation buttons |

### Supported Components

Navigate can control any component that supports pagination:

- **card** - with `max-items` or `max-rows` attribute
- **radio** - with `max-items` attribute
- **checkbox-list** - with `max-items` attribute

### Examples

```xml
<!-- Basic navigation with card -->
<card id="myList" max-items="5">
    <command v-for="item in items" :title="item.name" />
</card>
<navigate target="myList" />

<!-- Navigation with radio -->
<radio id="language" max-items="5" :selected="lang" @select="lang = event.select.id">
    <option v-for="l in languages" :value="l.code" :title="l.name" />
</radio>
<navigate target="language" />

<!-- Navigation with checkbox-list -->
<checkbox-list id="features" max-items="4" :selected="selectedFeatures">
    <option v-for="f in features" :value="f.id" :title="f.name" />
</checkbox-list>
<navigate target="features" />

<!-- Custom navigation -->
<navigate
    target="myList"
    prevTitle="Previous"
    nextTitle="Next"
    counterTitle="Page {{ self.currentPage + 1 }} of {{ self.pageCount }}"
    carousel="false"
    boundaryMessage="No more items" />

<!-- Minimal navigation (no counter) -->
<navigate target="myList" showCounter="false" />
```

### Navigation in Modals

For `radio-modal` and `checkbox-modal` components, add `<navigate>` inside the modal to customize the built-in pagination. The `target` attribute is ignored (modal manages its own pagination).

```xml
<radio-modal id="items" maxItems="10" title="Select Item">
    <option v-for="item in items" :value="item.id" :title="item.name" />
    <navigate prevTitle="Back" nextTitle="Forward" showCounter="false" />
</radio-modal>
```

---

## row

Groups components on the same row.

### Examples

```xml
<!-- Two buttons on one row -->
<row>
    <command title="Yes" @click="confirm()" />
    <command title="No" @click="cancel()" />
</row>

<!-- Three buttons on one row -->
<row>
    <open title="Home" target="home" />
    <open title="Settings" target="settings" />
    <open title="Help" target="help" />
</row>
```

---

## Common Attributes

These attributes are available on most components:

| Attribute | Type | Description |
|-----------|------|-------------|
| `id` | string | Unique component ID for JavaScript access via `component(id)` |
| `hide` | boolean | Conditionally hide the component |
| `columns` | number | Number of columns this component spans (layout hint) |
| `variant` | string | Button style variant: `primary`, `danger`, `success` |
| `icon` | string | Custom emoji ID to display as button icon |

### variant

Sets the visual style of a button. Available values:

- `primary` — primary action style
- `danger` — destructive/warning action style
- `success` — confirmation/positive action style

If not specified, the default Telegram button style is used.

```xml
<command title="Save" variant="success" />
<command title="Delete" variant="danger" />
```

### icon

Sets a custom emoji icon on the button using a custom emoji ID.

```xml
<command title="Settings" icon="5368324170671202286" />
```

### Per-option variant and icon

For `<radio>`, `<checkbox-list>`, and `<switch>` components, `variant` and `icon` can be set on individual `<option>` elements. Per-option values take priority over the component-level values.

```xml
<radio id="actions" variant="primary">
    <option value="save" title="Save" variant="success" />
    <option value="delete" title="Delete" variant="danger" />
    <option value="edit" title="Edit" />  <!-- inherits variant="primary" -->
</radio>
```

---

## Directives

### v-for

Generates multiple components from an array.

```xml
<!-- Basic syntax: item in array -->
<command v-for="item in items" :title="item.name" @click="select(item.id)" />

<!-- With index: (item, index) in array -->
<command v-for="(item, index) in items" :title="(index + 1) + '. ' + item.name" />

<!-- With :key for tracking (optional but recommended) -->
<command v-for="item in items" :key="item.id" :title="item.name" />
```

**Note:** The v-for directive works on components and also on `<option>` elements within radio/switch components.

### v-if / v-else-if / v-else

Conditionally renders components based on expressions.

```xml
<!-- Show component only if condition is true -->
<command v-if="VModel.Count > 0" title="Reset" @click="reset()" />

<!-- Multiple conditions with v-else-if -->
<command v-if="VModel.Status === 'pending'" title="Resume" />
<command v-else-if="VModel.Status === 'active'" title="Pause" />
<command v-else title="Start" />

<!-- Simple if/else -->
<command v-if="VModel.IsLoggedIn" title="Logout" @click="logout()" />
<command v-else title="Login" @click="login()" />
```

**Important:**
- `v-else-if` and `v-else` must immediately follow a `v-if` or `v-else-if` element
- Only one element in a v-if chain will be rendered
- The condition is re-evaluated on each render

### v-bind / :

Binds an attribute to a JavaScript expression.

```xml
<!-- Bind title to a variable -->
<command :title="dynamicTitle" />

<!-- Bind to expression -->
<checkbox :selected="settings.enabled" />
<command :title="'Count: ' + VModel.Count" />

<!-- Bind with localization -->
<command :title="$t('Save')" />
```

### v-on / @

Binds an event to a JavaScript handler.

```xml
<!-- Button click -->
<command @click="handleClick()" />

<!-- Checkbox update with event object -->
<checkbox @update="onCheckboxUpdate(event)" />

<!-- Radio selection -->
<radio @select="handleSelect(event.select.id)" />
```

---

## Template Syntax

Use `{{ expression }}` for dynamic text:

```xml
<command title="Count: {{ counter }}" />
<command title="{{ user.name }} ({{ user.role }})" />
<command title="{{ items.length }} items" />
```

### Localization

Use the `$t()` function for localization:

```xml
<!-- With binding -->
<command :title="$t('Save')" />

<!-- In templates -->
<title>{{ $t('Settings') }}</title>

<!-- Combined with other text -->
<command :title="'✅ ' + $t('Confirm')" />
```
