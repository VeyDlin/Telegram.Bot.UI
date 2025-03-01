using Newtonsoft.Json;
using System.Collections.Concurrent;
using Telegram.Bot.UI.Loader.DataTypes;
using File = System.IO.File;

namespace Telegram.Bot.UI.Loader;


public class PageResourceLoader {
    private string? pagesDataPath;
    private ConcurrentDictionary<string, object> pageInfoCache = new();
    private ConcurrentDictionary<string, object> pageGroupCache = new();


    public PageResourceLoader(string? pagesDataPath = null) {
        this.pagesDataPath = pagesDataPath;
    }




    public List<PageInfo> LoadPageGroup(string pageGroupNnamespace) {
        return (List<PageInfo>)pageGroupCache.GetOrAdd($"{pageGroupNnamespace}_[null]", key => {

            var path = GetOsPath(pageGroupNnamespace);
            if (path is null) {
                throw new Exception("path is null");
            }

            var group = new List<PageInfo>();

            foreach (var pagePath in Directory.GetDirectories(path)) {
                var name = Path.GetFileName(pagePath);
                group.Add(LoadPage($"{pageGroupNnamespace}:{name}"));
            }

            return group;
        });
    }





    public List<PageInfo<TConfig>> LoadPageGroup<TConfig>(string pageGroupNnamespace) {

        return (List<PageInfo<TConfig>>)pageGroupCache.GetOrAdd($"{pageGroupNnamespace}_{typeof(TConfig).Name}", key => {
            if (GetOsPath(pageGroupNnamespace) is not string path) {
                throw new Exception("path is null");
            }

            var group = new List<PageInfo<TConfig>>();

            foreach (var pagePath in Directory.GetDirectories(path)) {
                var name = Path.GetFileName(pagePath);
                group.Add(LoadPage<TConfig>($"{pageGroupNnamespace}:{name}"));
            }

            return group;
        });
    }





    public PageInfo<TConfig> LoadPage<TConfig>(string pageNnamespace) {
        return (PageInfo<TConfig>)pageInfoCache.GetOrAdd($"{pageNnamespace}_{typeof(TConfig).Name}", key => {
            if (GetOsPath(pageNnamespace) is not string path) {
                throw new Exception("path is null");
            }

            var info = new PageInfo<TConfig>();

            info.name = Path.GetFileName(path);
            info.config = LoadConfig<TConfig>(pageNnamespace);

            foreach (var resourceDirectory in Directory.GetDirectories(path)) {
                var name = Path.GetFileName(resourceDirectory);
                if (name is null) {
                    continue;
                }

                switch (name.ToLower()) {
                    case "audio":
                        info.audio = CreateResourceReader<AudioResource>($"{pageNnamespace}:audio");
                    break;
                    case "image":
                        info.image = CreateResourceReader<ImageResource>($"{pageNnamespace}:image");
                    break;
                    case "text":
                        info.text = CreateResourceReader<TextResource>($"{pageNnamespace}:text");
                    break;
                    case "video":
                        info.video = CreateResourceReader<VideoResource>($"{pageNnamespace}:video");
                    break;
                }
            }

            return info;
        });
    }





    public PageInfo LoadPage(string pageNnamespace) {
        return (PageInfo)pageInfoCache.GetOrAdd($"{pageNnamespace}_[null]", key => {
            var info = new PageInfo() {
                name = pageNnamespace
            };

            if (GetOsPath(pageNnamespace) is not string path) {
                return info;
            }

            foreach (var resourceDirectory in Directory.GetDirectories(path)) {
                var name = Path.GetFileName(resourceDirectory);
                if (name is null) {
                    continue;
                }

                switch (name.ToLower()) {
                    case "audio":
                    info.audio = CreateResourceReader<AudioResource>($"{pageNnamespace}:audio");
                    break;
                    case "image":
                    info.image = CreateResourceReader<ImageResource>($"{pageNnamespace}:image");
                    break;
                    case "text":
                    info.text = CreateResourceReader<TextResource>($"{pageNnamespace}:text");
                    break;
                    case "video":
                    info.video = CreateResourceReader<VideoResource>($"{pageNnamespace}:video");
                    break;
                }
            }

            return info;
        });
    }





    private ResourceReader<TResource>? CreateResourceReader<TResource>(string namespacePath) where TResource : BaseResource, new() {
        var path = GetOsPath(namespacePath);
        if (path is null) {
            return null;
        }

        var resources = Directory.GetFiles(path);
        return resources.Any() ? new ResourceReader<TResource>(resources.ToList()) : null;
    }





    private TConfig? LoadConfig<TConfig>(string pageNnamespace) {
        var path = GetOsPath(pageNnamespace);
        if (path is null) {
            return default;
        }

        var config = Path.Combine(path, "config.json");

        if (!File.Exists(config)) {
            return default;
        }

        var jsonString = File.ReadAllText(config);
        return JsonConvert.DeserializeObject<TConfig>(jsonString)!;
    }





    private string? GetOsPath(string namespacePath) {
        if (pagesDataPath is null) {
            return null;
        }

        var pathList = new List<string>() { pagesDataPath };
        pathList.AddRange(namespacePath.Split(":"));
        var path = Path.Combine(pathList.ToArray());

        if (!Directory.Exists(path)) {
            return null;
        }

        return path;
    }
}
