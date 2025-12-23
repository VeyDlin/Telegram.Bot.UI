using System.Collections.Concurrent;
using System.Text;

namespace Telegram.Bot.UI.Loader;


/// <summary>
/// Simple resource loader with auto-resolve and caching.
/// - Resolves names without extensions (e.g., "logo" → "logo.png")
/// - Caches loaded files
/// - Provides byte[] or string content
/// </summary>
public class ResourceLoader : IResourceLoader {
    private readonly ConcurrentDictionary<string, byte[]> cache = new();
    private readonly ConcurrentDictionary<string, string?> pathCache = new();

    /// <summary>
    /// Root path for resources
    /// </summary>
    public string? BasePath { get; }


    /// <summary>
    /// Initializes a new ResourceLoader instance.
    /// </summary>
    /// <param name="basePath">The base path for resolving relative resource paths.</param>
    public ResourceLoader(string? basePath = null) {
        BasePath = basePath;
    }

    /// <summary>
    /// Resolves a resource name to its full path.
    /// Handles full paths, names without extensions, and relative paths from basePath.
    /// </summary>
    /// <param name="name">The resource name to resolve.</param>
    /// <returns>The full resolved path or null if not found.</returns>
    public string? ResolvePath(string name) {
        if (string.IsNullOrEmpty(name)) {
            return null;
        }

        return pathCache.GetOrAdd(name, key => ResolvePathInternal(key));
    }

    /// <summary>
    /// Internal method that performs the actual path resolution.
    /// </summary>
    /// <param name="name">The resource name to resolve.</param>
    /// <returns>The full resolved path or null if not found.</returns>
    private string? ResolvePathInternal(string name) {
        string fullPath;
        if (BasePath is not null && !Path.IsPathRooted(name)) {
            fullPath = Path.GetFullPath(Path.Combine(BasePath, name));
        } else {
            fullPath = Path.GetFullPath(name);
        }

        if (File.Exists(fullPath)) {
            return fullPath;
        }

        var directory = Path.GetDirectoryName(fullPath);
        var fileName = Path.GetFileName(fullPath);

        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) {
            return null;
        }

        var exactMatch = Directory.GetFiles(directory)
            .FirstOrDefault(f => Path.GetFileName(f) == fileName);

        if (exactMatch is not null) {
            return exactMatch;
        }

        var matches = Directory.GetFiles(directory)
            .Where(f => Path.GetFileNameWithoutExtension(f) == fileName)
            .ToList();

        if (matches.Count == 1) {
            return matches[0];
        }

        if (matches.Count > 1) {
            throw new InvalidOperationException(
                $"Ambiguous resource path '{name}': found multiple files with same name but different extensions: " +
                string.Join(", ", matches.Select(Path.GetFileName))
            );
        }

        return null;
    }


    /// <summary>
    /// Gets file content as byte array with caching.
    /// Throws FileNotFoundException if resource doesn't exist.
    /// </summary>
    public byte[] GetBytes(string name) {
        var path = ResolvePath(name);
        if (path is null) {
            throw new FileNotFoundException($"Resource not found: '{name}'. Base path: '{BasePath}'", name);
        }

        return cache.GetOrAdd(path, key => File.ReadAllBytes(key));
    }


    /// <summary>
    /// Gets file content as UTF-8 string with caching.
    /// Throws FileNotFoundException if resource doesn't exist.
    /// </summary>
    public string GetText(string name) {
        var bytes = GetBytes(name);
        return Encoding.UTF8.GetString(bytes);
    }


    /// <summary>
    /// Checks if a resource exists.
    /// </summary>
    public bool Exists(string name) {
        return ResolvePath(name) is not null;
    }


    /// <summary>
    /// Clears all caches.
    /// </summary>
    public void ClearCache() {
        cache.Clear();
        pathCache.Clear();
    }


    /// <summary>
    /// Clears cache for specific resource.
    /// </summary>
    public void ClearCache(string name) {
        var path = ResolvePath(name);
        if (path is not null) {
            cache.TryRemove(path, out _);
        }
        pathCache.TryRemove(name, out _);
    }


    /// <summary>
    /// Creates a new ResourceLoader with a path relative to this one's base path.
    /// </summary>
    /// <param name="subPath">The subdirectory path.</param>
    /// <returns>A new ResourceLoader instance.</returns>
    public ResourceLoader GetSubLoader(string subPath) {
        if (BasePath is null) {
            return new ResourceLoader(subPath);
        }
        return new ResourceLoader(Path.Combine(BasePath, subPath));
    }
}