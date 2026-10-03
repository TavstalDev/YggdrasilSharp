using System.Net;

namespace Tavstal.YggdrasilSharp.Models;

/// <summary>
/// Represents the outcome of an attempt to save an uploaded file.
/// </summary>
public class FileSaveResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the file was saved successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code describing the outcome.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the message describing the outcome.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
