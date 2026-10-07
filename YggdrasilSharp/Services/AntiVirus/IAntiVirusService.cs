namespace Tavstal.YggdrasilSharp.Services.AntiVirus;

/// <summary>
/// Abstraction for scanning content for malware using an antivirus engine.
/// </summary>
public interface IAntiVirusService
{
    /// <summary>
    /// Scans the given stream for malware.
    /// </summary>
    /// <param name="stream">The content to scan. Implementations may rewind it to the beginning after scanning.</param>
    /// <returns><see langword="true"/> if the content is infected; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsInfectedAsync(Stream stream);

    /// <summary>
    /// Scans an uploaded file for malware.
    /// </summary>
    /// <param name="file">The uploaded file to scan.</param>
    /// <returns><see langword="true"/> if the file is infected; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsInfectedAsync(IFormFile file);
}
