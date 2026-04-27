using SkiaSharp;

namespace Mealie.Application.Services.Images;

/// <summary>
/// Generates the three image variants Mealie's frontend expects:
///   original.webp     — up to 2048×2048, quality 80
///   min-original.webp — up to 1024×1024, quality 80
///   tiny-original.webp — center-cropped 300×300, quality 80
/// </summary>
public static class RecipeImageProcessor
{
    public static void SaveVariants(string imagesDir, byte[] sourceBytes)
    {
        Directory.CreateDirectory(imagesDir);

        using var original = SKBitmap.Decode(sourceBytes);
        if (original is null) return;

        SaveResized(original, Path.Combine(imagesDir, "original.webp"), 2048, 2048);
        SaveResized(original, Path.Combine(imagesDir, "min-original.webp"), 1024, 1024);
        SaveCropped(original, Path.Combine(imagesDir, "tiny-original.webp"), 300);
    }

    private static void SaveResized(SKBitmap src, string path, int maxW, int maxH)
    {
        var (w, h) = FitSize(src.Width, src.Height, maxW, maxH);
        using var resized = src.Resize(new SKImageInfo(w, h), SKSamplingOptions.Default);
        if (resized is null) return;
        Encode(resized, path);
    }

    private static void SaveCropped(SKBitmap src, string path, int size)
    {
        float scale = (float)size / Math.Min(src.Width, src.Height);
        int scaledW = (int)Math.Ceiling(src.Width * scale);
        int scaledH = (int)Math.Ceiling(src.Height * scale);

        using var scaled = src.Resize(new SKImageInfo(scaledW, scaledH), SKSamplingOptions.Default);
        if (scaled is null) return;

        int x = (scaledW - size) / 2;
        int y = (scaledH - size) / 2;
        using var cropped = new SKBitmap(size, size);
        using var canvas = new SKCanvas(cropped);
        canvas.DrawBitmap(scaled, new SKRect(x, y, x + size, y + size), new SKRect(0, 0, size, size));
        canvas.Flush();

        Encode(cropped, path);
    }

    private static (int w, int h) FitSize(int srcW, int srcH, int maxW, int maxH)
    {
        if (srcW <= maxW && srcH <= maxH) return (srcW, srcH);
        float ratio = Math.Min((float)maxW / srcW, (float)maxH / srcH);
        return ((int)(srcW * ratio), (int)(srcH * ratio));
    }

    private static void Encode(SKBitmap bitmap, string path)
    {
        using var img = SKImage.FromBitmap(bitmap);
        using var data = img.Encode(SKEncodedImageFormat.Webp, 80);
        using var fs = File.OpenWrite(path);
        data.SaveTo(fs);
    }
}
