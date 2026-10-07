using System.Collections.Immutable;
using System.Net;
using MimeDetective;
using MimeDetective.Engine;
using Tavstal.YggdrasilSharp.Models;

namespace Tavstal.YggdrasilSharp.Utils.Helpers;

/// <summary>
/// Provides helper methods for file input/output operations, including saving files,
/// verifying MIME types, and checking for file infections.
/// </summary>
public static class IOHelper
{
    /// <summary>
    /// Saves a file to the specified path after verifying its MIME type.
    /// </summary>
    /// <param name="filePath">The path where the file will be saved.</param>
    /// <param name="stream">The stream containing the file data to save.</param>
    /// <param name="acceptedMimeTypes">An optional array of accepted MIME types for validation.</param>
    /// <param name="logger">An optional logger.</param>
    /// <returns>A task that represents the asynchronous operation. The task result describes the outcome of the save.</returns>
    public static async Task<FileSaveResult> SaveFileAsync(string filePath, Stream stream, string[]? acceptedMimeTypes = null, ILogger? logger = null)
    {
        try
        {
            if (acceptedMimeTypes != null && !VerifyMimeType(stream, acceptedMimeTypes))
                return new FileSaveResult
                {
                    Success = false,
                    StatusCode = HttpStatusCode.UnsupportedMediaType,
                    Message = "Invalid MIME type."
                };

            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fileStream);
            fileStream.Close();
            return new FileSaveResult
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = "File saved successfully."
            };
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error saving file");
            return new FileSaveResult
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Message = $"Unexpected error occured while saving the file."
            };
        }
    }

    /// <summary>
    /// Verifies the MIME type of the given file content against the expected MIME types.
    /// </summary>
    /// <param name="fileContent">The stream containing the file data to inspect.</param>
    /// <param name="expectedMimeTypes">An array of expected MIME types to validate against.</param>
    /// <returns>True if the file's MIME type matches one of the expected MIME types; otherwise, false.</returns>
    public static bool VerifyMimeType(Stream fileContent, string[] expectedMimeTypes)
    {
        var inspector = new ContentInspectorBuilder().Build();
        var result = inspector.Inspect(fileContent);
        ImmutableArray<MimeTypeMatch> mimeType = result.ByMimeType();

        if (mimeType == null)
            return false;

        var mimeTypeString = mimeType.FirstOrDefault()?.MimeType;
        if (mimeTypeString == null)
            return false;

        return expectedMimeTypes.Contains(mimeTypeString);
    }
}
