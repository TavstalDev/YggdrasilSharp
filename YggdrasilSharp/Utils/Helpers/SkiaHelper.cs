using SkiaSharp;

namespace Tavstal.YggdrasilSharp.Utils.Helpers;

/// <summary>
/// Helper methods for image validation using SkiaSharp.
/// </summary>
public static class SkiaHelper
{
    /// <summary>
    /// Validates whether the provided stream contains an image of the specified format.
    /// The original stream is restored to position 0 after validation.
    /// </summary>
    /// <param name="stream">The stream containing the image to validate.</param>
    /// <param name="format">The expected image format.</param>
    /// <param name="logger">Optional logger used to log critical errors when validation fails.</param>
    /// <returns><see langword="true"/> if the image format matches the expected format; otherwise, <see langword="false"/>.</returns>
    public static async Task<bool> IsValidFormatAsync(Stream stream, SKEncodedImageFormat format, ILogger? logger = null)
    {
        try
        {
            using var streamCopy = new MemoryStream();
            await stream.CopyToAsync(streamCopy);
            streamCopy.Position = 0;
            using var codec = SKCodec.Create(streamCopy);
            stream.Position = 0;
            return codec.EncodedFormat == format;
        }
        catch (Exception ex)
        {
            logger?.LogCritical(ex, "Failed to validate image format.");
            return false;
        }
    }

    /// <summary>
    /// Validates whether the provided stream contains a valid Minecraft skin (PNG with standard dimensions).
    /// The original stream is restored to position 0 after validation.
    /// </summary>
    /// <param name="stream">The stream containing the skin image to validate.</param>
    /// <param name="logger">Optional logger used to log critical errors when validation fails.</param>
    /// <returns><see langword="true"/> if the image is a PNG skin with dimensions 64x32, 64x64, 512x256, or 512x512; otherwise, <see langword="false"/>.</returns>
    public static async Task<bool> IsValidSkinAsync(Stream stream, ILogger? logger = null)
    {
        try
        {
            using var streamCopy = new MemoryStream();
            await stream.CopyToAsync(streamCopy);
            streamCopy.Position = 0;
            using var codec = SKCodec.Create(streamCopy);
            stream.Position = 0;
            
            if (codec.EncodedFormat != SKEncodedImageFormat.Png)
                return false;
            
            int width = codec.Info.Width;
            int height = codec.Info.Height;
            return (width == 64 && (height == 32 || height == 64)) ||
                    (width == 512 && (height == 256 || height == 512));
        }
        catch (Exception ex)
        {
            logger?.LogCritical(ex, "Failed to validate skin.");
            return false;
        }
    }
}