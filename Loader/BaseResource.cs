using System.Security.Cryptography;


namespace Telegram.Bot.UI.Loader.DataTypes;


public abstract class BaseResource {
    public ResourceInfo? info { get; set; }
    protected object locker = new();



    public static TResource Create<TResource>(string path) where TResource : BaseResource, new() {
        var file = new FileInfo(path);
        if (!file.Exists) {
            throw new Exception($"File {path} not exists");
        }

        byte[] data = File.ReadAllBytes(path);

        using var sha = SHA256.Create();
        var sha256 = Convert.ToHexString(sha.ComputeHash(data));

        return new TResource() {
            info = new() {
                name = Path.GetFileName(path),
                path = path,
                data = data,
                sha256 = sha256
            }
        };
    }

}