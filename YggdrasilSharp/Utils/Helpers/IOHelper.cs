using System.Collections.Immutable;
using System.Diagnostics;
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
    /// Saves a file to the specified path after verifying its MIME type and scanning for infections.
    /// </summary>
    /// <param name="filePath">The path where the file will be saved.</param>
    /// <param name="stream">The stream containing the file data to save.</param>
    /// <param name="acceptedMimeTypes">An optional array of accepted MIME types for validation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result describes the outcome of the save.</returns>
    public static async Task<FileSaveResult> SaveFileAsync(string filePath, Stream stream, string[]? acceptedMimeTypes = null)
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

            var tempPath = Path.GetTempFileName();
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fileStream);
            fileStream.Close();
            if (await IsFileInfectedAsync(tempPath))
                return new FileSaveResult 
                {
                    Success = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "File is infected."
                };

            File.Move(tempPath, filePath);
            return new FileSaveResult 
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = "File saved successfully."
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving file: {ex.Message}");
            return new FileSaveResult
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Message = $"Unexpected error occured while saving the file.\n{ex.Message}"
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
    
    /// <summary>
    /// Checks if a file is infected by scanning it using the ClamAV antivirus tool.
    /// </summary>
    /// <param name="filePath">The path of the file to scan.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true if the file is infected; otherwise, false.</returns>
    public static async Task<bool> IsFileInfectedAsync(string filePath)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "clamscan",
                Arguments = $"--no-summary {filePath}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (output.Contains("Infected"))
        {
            File.Delete(filePath);
            return true;
        }

        return false;
    }
}