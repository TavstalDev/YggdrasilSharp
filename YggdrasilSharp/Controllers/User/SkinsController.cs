using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Claims;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using Tavstal.YggdrasilSharp.Utils.Helpers;

namespace Tavstal.YggdrasilSharp.Controllers.User;

/// <summary>
/// Controller for managing user skins.
/// </summary>
[ApiController]
[Route("/user")]
[Authorize(AuthenticationSchemes = "Bearer,Basic")]
public class SkinsController : CustomControllerBase
{
    private readonly CustomUserManager _userManager;
    private readonly IRepository<FileData> _fileDataRepository;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SkinsController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="userManager">The custom user manager.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="fileDataRepository">Repository for managing file data (skins).</param>
    /// <param name="settings">Application settings.</param>
    public SkinsController(ILogger<SkinsController > logger, CustomUserManager userManager, CustomUserStore userStore,
        IRepository<FileData> fileDataRepository, Settings settings) : base(logger, userStore, settings)
    {
        _userManager = userManager;
        _fileDataRepository = fileDataRepository;
    }

    /// <summary>
    /// Retrieves the current user's skin.
    /// </summary>
    /// <returns>The skin file or an appropriate HTTP status code.</returns>
    /// <response code="200">Skin retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">No skin found for the user.</response>
    [HttpGet("skin")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSkin()
    {
        try
        {
            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return CodeResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Skins.View))
                return CodeResult(HttpStatusCode.Forbidden, "Permission denied.");

            FileData? skin =
                await _fileDataRepository.FindAsync(x => x.UserId == user.Id && x.Type == EFileDataType.SKIN);
            if (skin == null)
                return CodeResult(HttpStatusCode.NotFound, "No skin found for the user");

            if (!skin.Exists())
            {
                await _fileDataRepository.RemoveAsync(skin, true);
                return CodeResult(HttpStatusCode.NotFound, "No skin found for the user");
            }

            return File(skin.GetFileStream(), skin.ContentType, skin.FileName);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "An error occurred while retrieving the skin.");
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    
    /// <summary>
    /// Uploads a new skin for the current user.
    /// </summary>
    /// <param name="file">The skin file to upload. Must be a PNG file and meet size and dimension requirements.</param>
    /// <returns>A success message or an appropriate HTTP status code.</returns>
    /// <response code="200">Skin uploaded successfully.</response>
    /// <response code="400">Invalid file format, size, or dimensions.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to upload a skin.</response>
    [HttpPut("skin")]
    [EnableRateLimiting(RateLimits.UPLOAD)]
    [Consumes("multipart/form-data")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status400BadRequest), TextResponse(StatusCodes.Status401Unauthorized), 
     TextResponse(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadSkin([BindRequired, FormFile(500, EFileSizeUnit.Kilobytes)] IFormFile file)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return CodeResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return CodeResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Skins.Upload))
                return CodeResult(HttpStatusCode.Forbidden, "Permission denied.");

            if (file.Length > 1024 * 500) // 500 KB limit
                return CodeResult(HttpStatusCode.BadRequest, "File size exceeds the 500 KB limit.");

            if (!file.FileName.EndsWith(".png"))
                return CodeResult(HttpStatusCode.BadRequest, "Only PNG files are allowed.");

            FileData? existingSkin =
                await _fileDataRepository.FindAsync(x => x.UserId == user.Id && x.Type == EFileDataType.SKIN);
            if (existingSkin != null)
            {
                existingSkin.DeleteFile();
                await _fileDataRepository.RemoveAsync(existingSkin, true);
            }

            await using var stream = file.OpenReadStream();
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            stream.Position = 0;

            if (!await SkiaHelper.IsValidSkinAsync(stream, Logger))
                return CodeResult(HttpStatusCode.BadRequest,
                    "Invalid image format or dimensions. Expected dimensions: 64x32, 64x64, 512x256, or 512x512.");

            FileData fd = await _fileDataRepository.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.png",
                ContentType = "image/png",
                UserId = user.Id,
                Type = EFileDataType.SKIN,
            }, true);
            fd.SaveFile(stream);
            return CodeResult(HttpStatusCode.OK, "Skin uploaded successfully");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "An error occurred while uploading the skin.");
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    
    /// <summary>
    /// Deletes the current user's skin.
    /// </summary>
    /// <returns>A success message or an appropriate HTTP status code.</returns>
    /// <response code="200">Skin deleted successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to delete the skin.</response>
    /// <response code="404">No skin found for the user.</response>
    [HttpDelete("skin")]
    [EnableRateLimiting(RateLimits.WRITE)]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), 
     TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSkin()
    {
        try
        {
            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return CodeResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Skins.Delete))
                return CodeResult(HttpStatusCode.Forbidden, "Permission denied.");

            FileData? existingSkin =
                await _fileDataRepository.FindAsync(x => x.UserId == user.Id && x.Type == EFileDataType.SKIN);
            if (existingSkin == null)
                return CodeResult(HttpStatusCode.NotFound, "No skin found for the user");

            existingSkin.DeleteFile();
            await _fileDataRepository.RemoveAsync(existingSkin, true);
            return CodeResult(HttpStatusCode.OK, "Skin deleted successfully");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "An error occurred while deleting the skin.");
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    #region Admin Endpoints

    /// <summary>
    /// Retrieves the skin of a specific user (admin only).
    /// </summary>
    /// <param name="userId">The ID of the target user.</param>
    /// <returns>The skin file or an appropriate HTTP status code.</returns>
    /// <response code="200">Skin retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to view the skin.</response>
    /// <response code="404">No skin found for the user.</response>
    [HttpGet("{userId}/skin")]
    [EnableRateLimiting(RateLimits.ADMIN)]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), 
     TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSkinAdmin([BindRequired, FromRoute, MinLength(32), MaxLength(36)] string userId)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return CodeResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return CodeResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Skins.ViewOther))
                return CodeResult(HttpStatusCode.Forbidden, "Permission denied.");

            CustomUser? targetUser = await UserStore.FindUserByIdAsync(userId);
            if (targetUser == null)
                return CodeResult(HttpStatusCode.NotFound, "Target user not found");

            if (!await _userManager.HasHigherRoleThanAsync(user, targetUser))
                return CodeResult(HttpStatusCode.Forbidden, "You do not have permission to manage this user.");

            FileData? skin =
                await _fileDataRepository.FindAsync(x => x.UserId == targetUser.Id && x.Type == EFileDataType.SKIN);
            if (skin == null)
                return CodeResult(HttpStatusCode.NotFound, "No skin found for the user");

            if (!skin.Exists())
            {
                await _fileDataRepository.RemoveAsync(skin, true);
                return CodeResult(HttpStatusCode.NotFound, "No skin found for the user");
            }

            return File(skin.GetFileStream(), skin.ContentType, skin.FileName);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "An error occurred while retrieving the skin.");
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    
    /// <summary>
    /// Uploads a new skin for a specific user (admin only).
    /// </summary>
    /// <param name="userId">The ID of the target user.</param>
    /// <param name="file">The skin file to upload. Must be a PNG file and meet size and dimension requirements.</param>
    /// <returns>A success message or an appropriate HTTP status code.</returns>
    /// <response code="200">Skin uploaded successfully.</response>
    /// <response code="400">Invalid file format, size, or dimensions.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to upload the skin.</response>
    /// <response code="404">Target user not found.</response>
    [HttpPut("{userId}/skin")]
    [EnableRateLimiting(RateLimits.ADMIN)]
    [Consumes("multipart/form-data")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status400BadRequest), TextResponse(StatusCodes.Status401Unauthorized), 
     TextResponse(StatusCodes.Status403Forbidden), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadSkinAdmin([BindRequired, FromRoute, MinLength(32), MaxLength(36)] string userId, [BindRequired, FormFile(500, EFileSizeUnit.Kilobytes)] IFormFile file)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return CodeResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return CodeResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Skins.UploadOther))
                return CodeResult(HttpStatusCode.Forbidden, "Permission denied.");

            CustomUser? targetUser = await UserStore.FindUserByIdAsync(userId);
            if (targetUser == null)
                return CodeResult(HttpStatusCode.NotFound, "Target user not found");

            if (!await _userManager.HasHigherRoleThanAsync(user, targetUser))
                return CodeResult(HttpStatusCode.Forbidden, "You do not have permission to manage this user.");

            if (file.Length > 1024 * 500) // 500 KB limit
                return CodeResult(HttpStatusCode.BadRequest, "File size exceeds the 500 KB limit.");

            if (!file.FileName.EndsWith(".png"))
                return CodeResult(HttpStatusCode.BadRequest, "Only PNG files are allowed.");

            FileData? existingSkin =
                await _fileDataRepository.FindAsync(x => x.UserId == targetUser.Id && x.Type == EFileDataType.SKIN);
            if (existingSkin != null)
            {
                existingSkin.DeleteFile();
                await _fileDataRepository.RemoveAsync(existingSkin, true);
            }

            await using var stream = file.OpenReadStream();
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            stream.Position = 0;

            if (!await SkiaHelper.IsValidSkinAsync(stream, Logger))
                return CodeResult(HttpStatusCode.BadRequest,
                    "Invalid image format or dimensions. Expected dimensions: 64x32, 64x64, 512x256, or 512x512.");

            FileData fd = await _fileDataRepository.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.png",
                ContentType = "image/png",
                UserId = targetUser.Id,
                Type = EFileDataType.SKIN,
            }, true);
            fd.SaveFile(stream);
            return CodeResult(HttpStatusCode.OK, "Skin uploaded successfully");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "An error occurred while uploading the skin.");
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    
    /// <summary>
    /// Deletes the skin of a specific user (admin only).
    /// </summary>
    /// <param name="userId">The ID of the target user.</param>
    /// <returns>A success message or an appropriate HTTP status code.</returns>
    /// <response code="200">Skin deleted successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to delete the skin.</response>
    /// <response code="404">No skin found for the user.</response>
    [HttpDelete("{userId}/skin")]
    [EnableRateLimiting(RateLimits.ADMIN)]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), 
     TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSkinAdmin([BindRequired, FromRoute, MinLength(32), MaxLength(36)] string userId)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return CodeResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return CodeResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Skins.DeleteOther))
                return CodeResult(HttpStatusCode.Forbidden, "Permission denied.");

            CustomUser? targetUser = await UserStore.FindUserByIdAsync(userId);
            if (targetUser == null)
                return CodeResult(HttpStatusCode.NotFound, "Target user not found");

            if (!await _userManager.HasHigherRoleThanAsync(user, targetUser))
                return CodeResult(HttpStatusCode.Forbidden, "You do not have permission to manage this user.");

            FileData? existingSkin =
                await _fileDataRepository.FindAsync(x => x.UserId == targetUser.Id && x.Type == EFileDataType.SKIN);
            if (existingSkin == null)
                return CodeResult(HttpStatusCode.NotFound, "No skin found for the user");

            existingSkin.DeleteFile();
            await _fileDataRepository.RemoveAsync(existingSkin, true);
            return CodeResult(HttpStatusCode.OK, "Skin deleted successfully");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "An error occurred while deleting the skin.");
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    #endregion
}