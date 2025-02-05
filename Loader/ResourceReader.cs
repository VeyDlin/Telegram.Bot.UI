namespace Telegram.Bot.UI.Loader.DataTypes;


public class ResourceReader<TResource> where TResource : BaseResource, new() {
    public List<string> paths { get; private set; }
    public List<TResource>? resources { get; private set; }
    private object locker = new();



    public ResourceReader(List<string> paths) {
        this.paths = paths;
    }





    public TResource? SelectFromName(string name) {
        var file = resources?
            .Select(x => x)
            .Where(x => x.info?.name == name)
            .FirstOrDefault();

        if (file is not null) {
            return file;
        }

        var files = resources?
            .Select(x => x)
            .Where(x => Path.GetFileNameWithoutExtension(x.info?.name) == name)
            .ToList();

        if (files is null || !files.Any()) {
            return null;
        }

        if (files.Count > 1) {
            throw new Exception($"There are more than one files with the name {name}");
        }

        return files[0];
    }





    public TResource? SelectFromPath(string path) => resources?.Select(x => x).Where(x => x.info?.path == path).FirstOrDefault();

    public TResource? SelectFromHash(string sha256) => resources?.Select(x => x).Where(x => x.info?.sha256 == sha256).FirstOrDefault();





    public ResourceReader<TResource> Open(bool useCache = true) {
        lock (locker) {
            if (resources is not null && useCache) {
                return this;
            }

            resources = new List<TResource>();
            foreach (var path in paths) {
                var resource = BaseResource.Create<TResource>(path);
                resources.Add(resource);
            }

            return this;
        }
    }
}
