namespace Tavstal.YggdrasilSharp.Models.Common;

/// <summary>
/// Represents units of file size measurement.
/// </summary>
public enum EFileSizeUnit
{
    /// <summary>
    /// Represents a file size in bytes.
    /// </summary>
    Bytes = 1,

    /// <summary>
    /// Represents a file size in kilobytes (1 KB = 1024 bytes).
    /// </summary>
    Kilobytes = 1024,

    /// <summary>
    /// Represents a file size in megabytes (1 MB = 1024 * 1024 bytes).
    /// </summary>
    Megabytes = 1024 * 1024,

    /// <summary>
    /// Represents a file size in gigabytes (1 GB = 1024 * 1024 * 1024 bytes).
    /// </summary>
    Gigabytes = 1024 * 1024 * 1024
}