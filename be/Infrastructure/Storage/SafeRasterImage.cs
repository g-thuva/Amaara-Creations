using SkiaSharp;

namespace be.Infrastructure.Storage;

// Only freshly encoded pixels leave this boundary. Original EXIF, comments,
// profiles, animation and trailing/polyglot bytes are never stored or served.
internal static class SafeRasterImage
{
    private static readonly SemaphoreSlim Decoders = new(2);

    internal static async Task<byte[]> NormalizeAsync(byte[] source, int maximumPixels, long maximumBytes, CancellationToken ct)
    {
        await Decoders.WaitAsync(ct);
        try
        {
            using var data = SKData.CreateCopy(source);
            using var codec = SKCodec.Create(data) ?? throw Invalid();
            var width = codec.Info.Width;
            var height = codec.Info.Height;
            if (width <= 0 || height <= 0 || (long)width * height > Math.Min(maximumPixels, 24_000_000))
                throw new InvalidOperationException("Image dimensions are invalid or too large.");
            using var colorSpace = SKColorSpace.CreateSrgb();
            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul, colorSpace);
            using var bitmap = new SKBitmap(info);
            if (codec.GetPixels(info, bitmap.GetPixels()) != SKCodecResult.Success) throw Invalid();
            ct.ThrowIfCancellationRequested();
            var swapsAxes = (int)codec.EncodedOrigin >= 5;
            using var upright = new SKBitmap(new SKImageInfo(swapsAxes ? height : width, swapsAxes ? width : height, SKColorType.Rgba8888, SKAlphaType.Premul, colorSpace));
            using (var canvas = new SKCanvas(upright))
            {
                canvas.Clear(SKColors.Transparent);
                // EXIF orientations 1..8; translations keep all pixels on canvas.
                switch ((int)codec.EncodedOrigin)
                {
                    case 2: canvas.Translate(width, 0); canvas.Scale(-1, 1); break;
                    case 3: canvas.Translate(width, height); canvas.RotateDegrees(180); break;
                    case 4: canvas.Translate(0, height); canvas.Scale(1, -1); break;
                    case 5: canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
                    case 6: canvas.Translate(height, 0); canvas.RotateDegrees(90); break;
                    case 7: canvas.Translate(height, width); canvas.RotateDegrees(90); canvas.Scale(-1, 1); break;
                    case 8: canvas.Translate(0, width); canvas.RotateDegrees(-90); break;
                }
                canvas.DrawBitmap(bitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
            }
            using var image = SKImage.FromBitmap(upright);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100) ?? throw Invalid();
            if (encoded.Size > maximumBytes)
                throw new InvalidOperationException("The processed image exceeds the upload limit. Upload a smaller image.");
            return encoded.ToArray();
        }
        finally { Decoders.Release(); }
    }

    private static InvalidOperationException Invalid() => new("Image pixels could not be fully decoded. Upload a complete JPEG, PNG or WebP image.");
}
