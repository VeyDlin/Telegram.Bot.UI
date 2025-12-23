using Serilog;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;

namespace Telegram.Bot.UI.Demo.Services;

public class ImgBBService {
    private static readonly ConcurrentDictionary<string, string> urlCache = new();
    private static readonly HttpClient httpClient = new();

    public string? apiToken { get; set; }


    public async Task<string?> UploadAsync(byte[] data, string filename) {
        if (apiToken is null) {
            Log.Warning("ImgBB API token not configured");
            return null;
        }

        // Check cache by SHA256
        var hash = ComputeSha256(data);
        if (urlCache.TryGetValue(hash, out var cachedUrl)) {
            Log.Debug("ImgBB cache hit for {Filename} ({Hash})", filename, hash[..8]);
            return cachedUrl;
        }

        try {
            Log.Debug("Uploading {Filename} to ImgBB ({Size} bytes)", filename, data.Length);

            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(data), "image", filename);

            var response = await httpClient.PostAsync(
                $"https://api.imgbb.com/1/upload?key={apiToken}",
                content
            );

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) {
                Log.Error("ImgBB upload failed: {StatusCode} - {Response}", response.StatusCode, json);
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            var url = doc.RootElement
                .GetProperty("data")
                .GetProperty("url")
                .GetString();

            if (url is not null) {
                urlCache.TryAdd(hash, url);
                Log.Debug("ImgBB upload success: {Url}", url);
            }

            return url;

        } catch (Exception ex) {
            Log.Error(ex, "ImgBB upload error for {Filename}", filename);
            return null;
        }
    }


    public async Task<string?> UploadFileAsync(string filePath) {
        if (!File.Exists(filePath)) {
            Log.Error("File not found: {FilePath}", filePath);
            return null;
        }

        var data = await File.ReadAllBytesAsync(filePath);
        var filename = Path.GetFileName(filePath);
        return await UploadAsync(data, filename);
    }


    private static string ComputeSha256(byte[] data) {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
