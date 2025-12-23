using System.Collections.Concurrent;
using Telegram.Bot.UI.Demo.Services;
using Telegram.Bot.UI.Loader;

namespace Telegram.Bot.UI.Demo.ViewModels;


/// <summary>
/// ViewModel for photo editor page. Provides wallpaper URLs for filter previews.
/// </summary>
public class PhotoEditorViewModel {
    // Static service and cache shared across all instances
    private static ImgBBService? imgbbService;
    private static IResourceLoader? resourceLoader;
    private static readonly ConcurrentDictionary<string, string?> wallpaperUrls = new();
    private static readonly SemaphoreSlim loadLock = new(1, 1);


    public static void Configure(string? imgbbToken, IResourceLoader loader) {
        imgbbService = new ImgBBService { apiToken = imgbbToken };
        resourceLoader = loader;
    }


    public async Task<Dictionary<string, string?>> LoadAllWallpapersAsync() {
        var wallpaperNames = new[] {
            "off",
            "brightness-low", "brightness-medium", "brightness-high",
            "contrast-low", "contrast-medium", "contrast-high",
            "blur-low", "blur-medium", "blur-high",
            "pixelate-low", "pixelate-medium", "pixelate-high"
        };

        var result = new Dictionary<string, string?>();

        foreach (var name in wallpaperNames) {
            result[name] = await GetWallpaperUrlAsync(name);
        }

        return result;
    }


    public string? GetWallpaperUrl(string name) {
        return GetWallpaperUrlAsync(name).GetAwaiter().GetResult();
    }


    public async Task<string?> GetWallpaperUrlAsync(string name) {
        // Check cache first
        if (wallpaperUrls.TryGetValue(name, out var cached)) {
            return cached;
        }

        if (imgbbService is null || resourceLoader is null) {
            return null;
        }

        await loadLock.WaitAsync();
        try {
            // Double-check after acquiring lock
            if (wallpaperUrls.TryGetValue(name, out cached)) {
                return cached;
            }

            // Find the wallpaper file
            var extension = name == "off" ? ".png" : ".jpg";
            var relativePath = $"Pages/App/PhotoEditor/wallpapers/{name}{extension}";

            byte[] data;
            try {
                data = resourceLoader.GetBytes(relativePath);
            } catch {
                // Try alternative path
                try {
                    data = resourceLoader.GetBytes($"wallpapers/{name}{extension}");
                } catch {
                    wallpaperUrls[name] = null;
                    return null;
                }
            }

            // Upload to imgbb
            var url = await imgbbService.UploadAsync(data, $"{name}{extension}");
            wallpaperUrls[name] = url;
            return url;

        } finally {
            loadLock.Release();
        }
    }


    public static void ClearCache() {
        wallpaperUrls.Clear();
    }
}
