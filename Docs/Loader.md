# Resource Loader (Loader)

A subsystem for managing various types of resources (text, images, audio, video) in the Telegram.Bot.UI project. It provides loading, caching, and access to resources through a unified interface.

## BaseResource

`BaseResource` - an abstract class representing a basic resource with file information and creation methods.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `ResourceInfo? info { get; set; }` | Resource information including name, path, data, and hash. |

### Public Methods

| Interface | Description |
|-----------|-------------|
| `static TResource Create<TResource>(string path) where TResource : BaseResource, new()` | Creates an instance of the specified resource type from a file at the specified path. |

## ResourceInfo

`ResourceInfo` - a class containing resource metadata and its content.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `required string name { get; set; }` | Resource file name. |
| `required string path { get; set; }` | Full path to the resource file. |
| `required byte[] data { get; set; }` | Binary resource data. |
| `required string sha256 { get; set; }` | SHA256 hash of the data for identification and integrity verification. |

### Public Methods

| Interface | Description |
|-----------|-------------|
| `string GetMimeType()` | Determines the MIME type of the resource based on its content. |
| `string GetExtension()` | Returns the file extension based on the MIME type. |

## ResourceReader<TResource>

`ResourceReader<TResource>` - a generic class for reading and managing a collection of resources of a specific type.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `List<string> paths { get; }` | List of paths to resource files. |
| `List<TResource>? resources { get; }` | Collection of loaded resources. |

### Public Methods

| Interface | Description |
|-----------|-------------|
| `ResourceReader(List<string> paths)` | Constructor initializing the reader with specified resource paths. |
| `TResource? SelectFromName(string name)` | Selects a resource by file name. |
| `TResource? SelectFromPath(string path)` | Selects a resource by full file path. |
| `TResource? SelectFromHash(string sha256)` | Selects a resource by its SHA256 hash. |
| `ResourceReader<TResource> Open(bool useCache = true)` | Loads resources into memory, with an option to use cache. |

## PageInfo

`PageInfo` - a class representing information about a page and its resources of various types.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `string? name { get; set; }` | Page name. |
| `ResourceReader<AudioResource>? audio { get; set; }` | Collection of page audio resources. |
| `ResourceReader<ImageResource>? image { get; set; }` | Collection of page images. |
| `ResourceReader<TextResource>? text { get; set; }` | Collection of page text resources. |
| `ResourceReader<VideoResource>? video { get; set; }` | Collection of page video resources. |

## PageInfo<T>

`PageInfo<T>` - a generic class extending PageInfo with typed configuration.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `T? config { get; set; }` | Typed page configuration loaded from JSON. |

## PageResourceLoader

`PageResourceLoader` - a class for loading and managing pages and their resources with caching support.

### Public Properties

| Interface | Description |
|-----------|-------------|
| `string? pagesDataPath { get; }` | Base path to the directory with page data. |

### Public Methods

| Interface | Description |
|-----------|-------------|
| `PageResourceLoader(string? pagesDataPath = null)` | Constructor initializing the loader with the specified base path. |
| `List<PageInfo> LoadPageGroup(string pageGroupNamespace)` | Loads a group of pages by the specified namespace. |
| `List<PageInfo<TConfig>> LoadPageGroup<TConfig>(string pageGroupNamespace)` | Loads a group of pages with typed configuration. |
| `PageInfo<TConfig> LoadPage<TConfig>(string pageNamespace)` | Loads a single page with typed configuration. |
| `PageInfo LoadPage(string pageNamespace)` | Loads a single page without typed configuration. |

### Private Methods

| Interface | Description |
|-----------|-------------|
| `ResourceReader<TResource>? CreateResourceReader<TResource>(string namespacePath)` | Creates a resource reader for the specified namespace. |
| `TConfig? LoadConfig<TConfig>(string pageNamespace)` | Loads page configuration from config.json file. |
| `string? GetOsPath(string namespacePath)` | Converts a namespace to a file system path. |

## Usage Example

```csharp
// Initialize the loader
var loader = new PageResourceLoader("C:/BotResources");

// Load a page (C:/BotResources/Information)
var page = loader.LoadPage("Information");

// Access a text resource (C:/BotResources/Information/text/description.*)
var welcomeText = page.text?
    .Open()
    .SelectFromName("description")?
    .GetText();

// Load an image (C:/BotResources/Information/image/logo.*)
var welcomeImage = page.image?
    .Open()
    .SelectFromName("logo")?
    .info?.data;

// Load a page with configuration (C:/BotResources/Information/config.json)
var settingsPage = loader.LoadPage<MySettingsConfig>("Information");
var defaultLang = settingsPage.config?.defaultLanguage;
```