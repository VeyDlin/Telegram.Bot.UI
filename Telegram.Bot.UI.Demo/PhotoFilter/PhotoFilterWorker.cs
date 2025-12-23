using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Telegram.Bot.UI.Demo.PhotoFilter;


public class PhotoFilterWorker {
    public static async Task<byte[]> Filter(byte[] photoBytes, PhotoFilterSettings settings) {
        using var image = Image.Load(photoBytes);
        image.Mutate(ctx => {
            if (settings.applyInvert) {
                ctx.Invert();
            }
            if (settings.brightness != FilterLevel.Off) {
                float brightnessFactor = settings.brightness switch {
                    FilterLevel.Low => 1.1f,
                    FilterLevel.Medium => 1.2f,
                    FilterLevel.High => 1.3f,
                    _ => 1.0f
                };
                ctx.Brightness(brightnessFactor);
            }
            if (settings.contrast != FilterLevel.Off) {
                float contrastFactor = settings.contrast switch {
                    FilterLevel.Low => 1.1f,
                    FilterLevel.Medium => 1.2f,
                    FilterLevel.High => 1.3f,
                    _ => 1.0f
                };
                ctx.Contrast(contrastFactor);
            }
            if (settings.blur != FilterLevel.Off) {
                float sigma = settings.blur switch {
                    FilterLevel.Low => 2f,
                    FilterLevel.Medium => 4f,
                    FilterLevel.High => 6f,
                    _ => 0f
                };
                ctx.GaussianBlur(sigma);
            }
            if (settings.pixelate != FilterLevel.Off) {
                int pixelSize = settings.pixelate switch {
                    FilterLevel.Low => 5,
                    FilterLevel.Medium => 10,
                    FilterLevel.High => 20,
                    _ => 0
                };
                ctx.Pixelate(pixelSize);
            }
        });

        using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms);
        return ms.ToArray();
    }
}
