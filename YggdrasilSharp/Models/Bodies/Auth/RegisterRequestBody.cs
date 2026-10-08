using System.ComponentModel.DataAnnotations;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Common;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Auth;

/// <summary>
/// Represents the request body for registering a new user.
/// </summary>
public class RegisterRequestBody
{
    /// <summary>
    /// Gets or sets the username of the user.
    /// This field is required and must be between 3 and 16 characters long.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(16, MinimumLength = 3)]
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the email address of the user.
    /// This field is required and must be between 5 and 254 characters long.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    [StringLength(254, MinimumLength = 3)]
    public required string EmailAddress { get; set; }

    /// <summary>
    /// Gets or sets the password of the user.
    /// This field is required and must be between 8 and 64 characters long.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(64, MinimumLength = 8)]
    public required string Password { get; set; }

    /// <summary>
    /// Gets or sets the avatar file for the user.
    /// This field is optional and must be a PNG image with a maximum size of 500 kilobytes.
    /// </summary>
    [FormFile(500, EFileSizeUnit.Kilobytes, ["image/png"], [".png"])]
    public IFormFile? Avatar { get; set; }
}
