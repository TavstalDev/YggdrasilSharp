using System.Collections.Immutable;
using System.Diagnostics;
using MimeDetective;
using MimeDetective.Engine;

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
    /// <param name="fileContent">The content of the file as a byte array.</param>
    /// <param name="acceptedMimeTypes">An optional array of accepted MIME types for validation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true if the file was saved successfully; otherwise, false.</returns>
    public static async Task<bool> SaveFileAsync(string filePath, byte[] fileContent, string[]? acceptedMimeTypes = null)
    {
        try
        {
            if (acceptedMimeTypes != null && !VerifyMimeType(fileContent, acceptedMimeTypes))
                return false;

            var tempPath = Path.GetTempFileName();
            await File.WriteAllBytesAsync(tempPath, fileContent);
            if (await IsFileInfectedAsync(tempPath))
                return false;

            File.Move(tempPath, filePath);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving file: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Verifies the MIME type of the given file content against the expected MIME types.
    /// </summary>
    /// <param name="fileContent">The content of the file as a byte array.</param>
    /// <param name="expectedMimeTypes">An array of expected MIME types to validate against.</param>
    /// <returns>True if the file's MIME type matches one of the expected MIME types; otherwise, false.</returns>
    public static bool VerifyMimeType(byte[] fileContent, string[] expectedMimeTypes)
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