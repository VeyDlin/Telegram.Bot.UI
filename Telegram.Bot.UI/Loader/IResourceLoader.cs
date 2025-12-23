namespace Telegram.Bot.UI.Loader;


/// <summary>
/// Interface for resource loading. Implement this to provide custom resource sources
/// (e.g., from database, embedded resources, cloud storage, etc.)
/// </summary>
public interface IResourceLoader {
    /// <summary>
    /// Root path for resources (can be null for non-filesystem loaders)
    /// </summary>
    string? BasePath { get; }

    /// <summary>
    /// Resolves a resource name to its full path/identifier.
    /// </summary>
    string? ResolvePath(string name);

    /// <summary>
    /// Gets file content as byte array.
    /// Throws FileNotFoundException if resource doesn't exist.
    /// </summary>
    byte[] GetBytes(string name);

    /// <summary>
    /// Gets file content as UTF-8 string.
    /// Throws FileNotFoundException if resource doesn't exist.
    /// </summary>
    string GetText(string name);

    /// <summary>
    /// Checks if a resource exists.
    /// </summary>
    bool Exists(string name);

    /// <summary>
    /// Clears all caches.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Clears cache for specific resource.
    /// </summary>
    void ClearCache(string name);
}