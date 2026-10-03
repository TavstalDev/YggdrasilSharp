using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Utils.Helpers;

namespace Tavstal.YggdrasilSharp.Models.Database;

/// <summary>
/// Represents a file stored in the system, including its metadata such as hash, name, type, and owner.
/// Provides methods for file operations like checking existence, retrieving data, saving, and deleting files.
/// </summary>
public class FileData
{
    /// <summary>
    /// Gets or sets the unique identifier for the file.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }
    
    /// <summary>
    /// Gets or sets the hash of the file, used for identifying its content.
    /// </summary>
    [StringLength(64)]
    public required string Hash { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    [StringLength(255)]
    public required string FileName { get; set; }
    
    /// <summary>
    /// Gets or sets the MIME type of the file content.
    /// </summary>
    [StringLength(100)]
    public required string ContentType { get; set; }
    
    /// <summary>
    /// Gets or sets the type of the file, indicating its purpose.
    /// </summary>
    public required EFileDataType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier of the file's owner, if any.
    /// </summary>
    [StringLength(36)]
    public string? UserId { get; set; }

    /// <summary>
    /// Determines the directory name where the file is stored based on its type.
    /// </summary>
    /// <returns>The name of the directory containing the file.</returns>
    private string GetContainingDirectoryName()
    {
        switch (Type)
        {
            case EFileDataType.PROFILE_PICTURE:
                return "avatars";
            case EFileDataType.SKIN:
                return "skins";
            case EFileDataType.CAPE:
                return "capes";
            case EFileDataType.NEWS_BANNER:
                return "news";
            default:
                return "misc";
        }
    }
    
    /// <summary>
    /// Checks if the file exists in the storage directory.
    /// </summary>
    /// <returns>True if the file exists; otherwise, false.</returns>
    public bool Exists()
    {
        string dirPath = Program.UploadDir;
        if (!Directory.Exists(dirPath))
            return false;
        string filePath = Path.Combine(dirPath, GetContainingDirectoryName(), FileName);
        return File.Exists(filePath);
    }
    
    /// <summary>
    /// Retrieves the file data as a byte array.
    /// </summary>
    /// <returns>The file data as a byte array, or null if the file does not exist.</returns>
    public byte[]? GetFileData()
    {
        if (!Exists())
            return null;
        string dirPath = Program.UploadDir;
        string filePath = Path.Combine(dirPath, GetContainingDirectoryName(), FileName);
        return File.ReadAllBytes(filePath);
    }

    /// <summary>
    /// Saves the file to the storage directory using the provided stream.
    /// </summary>
    /// <param name="stream">The stream containing the file data to save.</param>
    /// <returns>A task that represents the asynchronous operation. The task result describes the outcome of the save.</returns>
    public async Task<FileSaveResult> SaveFileAsync(Stream stream)
    {
        string dirPath = Program.UploadDir;
        if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);
        
        string containingDir = GetContainingDirectoryName();
        string containingDirPath = Path.Combine(dirPath, containingDir);
        if (!Directory.Exists(containingDirPath))
            Directory.CreateDirectory(containingDirPath);
        
        string filePath = Path.Combine(containingDirPath, FileName);
        return await IOHelper.SaveFileAsync(filePath, stream);
    }
    
    /// <summary>
    /// Deletes the file from the storage directory.
    /// </summary>
    public void DeleteFile()
    {
        if (!Exists())
            return;
        string dirPath = Program.UploadDir;
        string filePath = Path.Combine(dirPath, GetContainingDirectoryName(), FileName);
        File.Delete(filePath);
    }
    
    /// <summary>
    /// Generates a URL for accessing the file based on its hash and the specified base URL.
    /// </summary>
    /// <param name="baseUrl">The base URL to use for constructing the file URL.</param>
    /// <param name="isYggdrasil">
    /// A boolean indicating whether the URL should follow the Yggdrasil format.
    /// If true, the URL will point to the Yggdrasil textures endpoint; otherwise, it will point to the files endpoint.
    /// </param>
    /// <returns>
    /// A string representing the constructed URL for the file.
    /// </returns>
    public string GetUrl(string baseUrl, bool isYggdrasil = false)
    {
        return isYggdrasil ? $"{baseUrl}/yggdrasil/textures/{Hash}" : $"{baseUrl}/files/{Hash}";
    }
    
    /// <summary>
    /// Retrieves the file as a stream.
    /// </summary>
    /// <returns>A stream containing the file data, or an empty stream if the file does not exist.</returns>
    public Stream GetFileStream()
    {
        if (!Exists())
            return Stream.Null;
        string dirPath = Program.UploadDir;
        string filePath = Path.Combine(dirPath, GetContainingDirectoryName(), FileName);
        return new FileStream(filePath, FileMode.Open, FileAccess.Read);
    }
    
    /* ######################################################################
     *                         NAVIGATION PROPERTIES
     * ###################################################################### */
    
    /// <summary>
    /// Gets or sets the user associated with this file.
    /// </summary>
    [ForeignKey("UserId")]
    [JsonIgnore]
    public CustomUser? User { get; set; }
}
