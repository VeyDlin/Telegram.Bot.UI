# MenuBuilder

`MenuBuilder` - subsystem for creating and managing interactive menus in Telegram bots, providing a set of components for building user-friendly interfaces.

## MenuCheckbox

`MenuCheckbox` - a class representing a checkbox element in a menu.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `string title { get; set; }` | Checkbox title displayed on the button. |
| `string temp { get; set; }` | Template for displaying the checkbox. Default is `"{{ if selected }}✅{{ end }} {{ title }}"`. |
| `bool isSelected { get; }` | Flag indicating whether the checkbox is currently selected. |

### Public Methods

| Interface | Description |
|-----------|-------------|
| `void Select(bool isSelected)` | Sets or unsets the checkbox selection. |

### Events

| Interface | Description |
|-----------|-------------|
| `event UpdateHandler? onUpdate` | Event triggered when the checkbox state changes. |

### Template Model

| Property | Description |
|----------|-------------|
| `title` | Checkbox title. |
| `selected` | Flag indicating whether the checkbox is selected. |

## MenuCheckboxGroup

`MenuCheckboxGroup` - a class for creating a group of checkboxes with multiple selection capability.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `IEnumerable<MenuSelector> buttons { get; init; }` | List of selectors (choice options) in the group. |
| `string temp { get; set; }` | Template for displaying each checkbox. Default is `"{{ if selected }}✅{{ end }} {{ title }}"`. |
| `List<int> selected { get; }` | List of indices of selected checkboxes. |
| `IEnumerable<string> selectedId { get; }` | List of IDs of selected checkboxes. |
| `List<MenuSelector> selectButton { get; }` | List of MenuSelector objects for selected checkboxes. |

### Public Methods

| Interface | Description |
|-----------|-------------|
| `void Select(string id)` | Selects a checkbox by its ID. |
| `void Unselect(string id)` | Unselects a checkbox by its ID. |
| `bool IsSelect(string id)` | Returns the state (selected/not selected) of a checkbox by its ID. |

### Events

| Interface | Description |
|-----------|-------------|
| `event UpdateHandler? onUpdate` | Event triggered when the state of any checkbox in the group changes. |

### Template Model

| Property | Description |
|----------|-------------|
| `title` | Checkbox title. |
| `selected` | Flag indicating whether the checkbox is selected. |

## MenuCheckboxModal

`MenuCheckboxModal` - a class representing a button that, when pressed, opens a modal window with a group of checkboxes.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `IEnumerable<MenuSelector> buttons { get; init; }` | List of selectors to display in the modal window. |
| `string? title { get; set; }` | Modal window title. |
| `IEnumerable<MenuModalDetails>? details { get; init; }` | Additional details to display in the modal window. |

## MenuCommand

`MenuCommand` - a class for creating a button that performs an action when pressed.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `string title { get; set; }` | Button title. |
| `string temp { get; set; }` | Template for displaying the button. Default is `"{{ title }}"`. |

### Events

| Interface | Description |
|-----------|-------------|
| `event CallbackFactory.CallbackHandler? onClick` | Event triggered when the button is pressed. |

### Template Model

| Property | Description |
|----------|-------------|
| `title` | Button title. |

## MenuLink

`MenuLink` - a class for creating a link button that opens an external URL.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `string title { get; set; }` | Link button title. |
| `string url { get; set; }` | URL that opens when the button is pressed. |
| `string temp { get; set; }` | Template for displaying the button. Default is `"{{ title }}"`. |

### Template Model

| Property | Description |
|----------|-------------|
| `title` | Button title. |

## MenuNavigatePanel

`MenuNavigatePanel` - a class for creating a navigation panel between menu pages (not fully implemented yet).

## MenuOpenPege

`MenuOpenPege` - a class for creating a button that opens another menu page (MessagePage).

### Public Properties

| Interface | Description |
|-----------|-------------|
| `MessagePage page { get; set; }` | The page that opens when the button is pressed. |
| `string? title { get; set; }` | Button title. If not specified, the page's title is used. |
| `bool changeParrent { get; set; }` | Flag indicating whether to set the current page as the parent for the opened page. |
| `string temp { get; set; }` | Template for displaying the button. Default is `"{{ title }}"`. |

### Template Model

| Property | Description |
|----------|-------------|
| `title` | Button title. |
| `changeParrent` | Flag for setting the parent page. |

## MenuRadio

`MenuRadio` - a class for creating a group of radio buttons with the ability to select only one option.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `IEnumerable<MenuSelector> buttons { get; init; }` | List of selectors (choice options) in the group. |
| `string temp { get; set; }` | Template for displaying each radio button. Default is `"{{ if selected }}✅{{ end }} {{ title }}"`. |
| `int selected { get; }` | Index of the selected radio button. |
| `string selectedId { get; }` | ID of the selected radio button. |
| `MenuSelector selectButton { get; }` | MenuSelector object for the selected radio button. |

### Public Methods

| Interface | Description |
|-----------|-------------|
| `void Select(string id)` | Selects a radio button by its ID. |

### Events

| Interface | Description |
|-----------|-------------|
| `event SelectHandler? onSelect` | Event triggered when a radio button is selected. |

### Template Model

| Property | Description |
|----------|-------------|
| `title` | Radio button title. |
| `selected` | Flag indicating whether the radio button is selected. |

## MenuRadioModal

`MenuRadioModal` - a class representing a button that, when pressed, opens a modal window with a group of radio buttons.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `IEnumerable<MenuSelector> buttons { get; init; }` | List of selectors to display in the modal window. |
| `string? title { get; set; }` | Modal window title. |
| `IEnumerable<MenuModalDetails>? details { get; init; }` | Additional details to display in the modal window. |

## MenuSplit

`MenuSplit` - a class representing a separator for menu elements (line break).

## MenuSwitch

`MenuSwitch` - a class for creating a switch between multiple options (carousel).

### Public Properties

| Interface | Description |
|-----------|-------------|
| `List<MenuSelector> buttons { get; init; }` | List of selectors (options) in the switch. |
| `string temp { get; set; }` | Template for displaying the switch. Default is `"{{ title }}"`. |
| `int selected { get; set; }` | Index of the currently selected option. |
| `string selectedId { get; }` | ID of the currently selected option. |
| `MenuSelector selectButton { get; }` | MenuSelector object for the currently selected option. |

### Events

| Interface | Description |
|-----------|-------------|
| `event UpdateHandler? onUpdate` | Event triggered when switching to another option. |

### Template Model

| Property | Description |
|----------|-------------|
| `title` | Title of the currently selected option. |
| `selected` | Index of the currently selected option. |
