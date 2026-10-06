using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using SkiaSharp;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Claims;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using Tavstal.YggdrasilSharp.Utils.Helpers;
using RateLimits = Tavstal.YggdrasilSharp.Models.RateLimiting.Constants.RateLimits;

namespace Tavstal.YggdrasilSharp.Controllers.User;

/// <summary>
/// Controller for managing user avatars, including retrieving, uploading, and deleting avatars.
/// </summary>
[ApiController]
[Route("/user")]
[Authorize(AuthenticationSchemes = "Bearer,Basic")]
public class AvatarController : CustomControllerBase
{
    private readonly CustomUserManager _userManager;
    private readonly IRepository<FileData> _fileDataRepo;
    private readonly MemoryCacheService _memoryCache;
    private static readonly TimeSpan CacheSlidingTtl = TimeSpan.FromDays(1);
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AvatarController"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for logging.</param>
    /// <param name="userManager">Service for managing users.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="fileDataRepo">Repository for managing file data (avatars).</param>
    /// <param name="cacheService">Service for caching data in memory.</param>
    /// <param name="appConfiguration">Application settings.</param>
    public AvatarController(ILogger<AvatarController> logger, CustomUserManager userManager, CustomUserStore userStore,
        IRepository<FileData> fileDataRepo, MemoryCacheService cacheService, AppConfiguration appConfiguration) : base(logger, userStore, appConfiguration)
    {
        _userManager = userManager;
        _fileDataRepo = fileDataRepo;
        _memoryCache = cacheService;
    }
    
    /// <summary>
    /// Retrieves the current user's avatar.
    /// </summary>
    /// <returns>The avatar file or an appropriate HTTP status code.</returns>
    /// <response code="200">Avatar retrieved successfully.</response>
    /// <response code="304">Avatar not modified (ETag matches).</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">No avatar found for the user.</response>
    /// <response code="500">An error occurred while retrieving the avatar.</response>
    [HttpGet("avatar")]
    [TextResponse(StatusCodes.Status200OK),
     TextResponse(StatusCodes.Status304NotModified),
     TextResponse(StatusCodes.Status401Unauthorized),
     TextResponse(StatusCodes.Status404NotFound),
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAvatar()
    {
        try
        {
            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Account.View.Avatar))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            string cacheKey = $"avatar:{user.Id}";
            if (!_memoryCache.TryGetValue(cacheKey, out (byte[], string, string) cachedAvatar))
            {
                FileData? existingAvatar =
                    await _fileDataRepo.FindAsync(x =>
                        x.UserId == user.Id && x.Type == EFileDataType.PROFILE_PICTURE);
                if (existingAvatar == null)
                    return JsonResult(HttpStatusCode.NotFound, "No avatar found.");
                byte[]? bytes = existingAvatar.GetFileData();
                if (bytes == null)
                    return JsonResult(HttpStatusCode.InternalServerError, "Failed to retrieve avatar data.");

                _memoryCache.SetValue(cacheKey, (bytes, existingAvatar.ContentType, existingAvatar.Hash), slidingExpiration: CacheSlidingTtl);
                Response.Headers.ETag = $"\"{existingAvatar.Hash}\"";
                Response.Headers.CacheControl = "public,max-age=3600,immutable";
                return File(existingAvatar.GetFileStream(), existingAvatar.ContentType, enableRangeProcessing: true);
            }

            string etag = $"\"{cachedAvatar.Item3}\"";
            if (Request.Headers.TryGetValue("If-None-Match", out var incomingEtag) && incomingEtag == etag)
                return CodeResult(HttpStatusCode.NotModified);

            Response.Headers.CacheControl = "public,max-age=3600,immutable";
            return File(cachedAvatar.Item1, cachedAvatar.Item2, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "An error occurred while retrieving the user's avatar.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Uploads a new avatar for the current user.
    /// </summary>
    /// <param name="file">The avatar file to upload.</param>
    /// <returns>A success message or an appropriate HTTP status code.</returns>
    /// <response code="200">Avatar uploaded successfully.</response>
    /// <response code="400">Invalid file format or file size exceeds the limit.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to upload an avatar.</response>
    /// <response code="500">An error occurred while processing the avatar upload.</response>
    [HttpPost("avatar")]
    [EnableRateLimiting(RateLimits.FixedWindow.UPLOAD)]
    [Consumes("multipart/form-data")]
    [TextResponse(StatusCodes.Status200OK),
     TextResponse(StatusCodes.Status400BadRequest),
     TextResponse(StatusCodes.Status401Unauthorized),
     TextResponse(StatusCodes.Status403Forbidden),
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadAvatar([BindRequired, FormFile(500, EFileSizeUnit.Kilobytes)] IFormFile file)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return JsonResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Account.Create.Avatar))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            if (file.Length > 1024 * 500)
                return JsonResult(HttpStatusCode.BadRequest, "File size exceeds the 500 KB limit.");

            if (!file.FileName.EndsWith(".png"))
                return JsonResult(HttpStatusCode.BadRequest, "Only PNG files are allowed.");

            await using var stream = file.OpenReadStream();
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            stream.Position = 0;

            if (!await SkiaHelper.IsValidFormatAsync(stream, SKEncodedImageFormat.Png, Logger))
                return JsonResult(HttpStatusCode.BadRequest, "Invalid image format (not a real PNG).");

            FileData fd = new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.png",
                ContentType = "image/png",
                Type = EFileDataType.PROFILE_PICTURE,
                UserId = user.Id
            };
            var uploadResult = await fd.SaveFileAsync(stream);
            if (!uploadResult.Success)
                return JsonResult(uploadResult.StatusCode, uploadResult.Message);
            
            FileData? existingAvatar =
                await _fileDataRepo.FindAsync(x => x.UserId == user.Id && x.Type == EFileDataType.PROFILE_PICTURE);
            if (existingAvatar != null)
            {
                existingAvatar.DeleteFile();
                await _fileDataRepo.RemoveAsync(existingAvatar, true);
                _memoryCache.RemoveValue("avatar:" + user.Id);
            }
            
            await _fileDataRepo.AddAsync(fd, true);
            return JsonResult(HttpStatusCode.OK, "Avatar uploaded successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "An error occurred while uploading the user's avatar.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Deletes the current user's avatar.
    /// </summary>
    /// <returns>A success message or an appropriate HTTP status code.</returns>
    /// <response code="200">Avatar deleted successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to delete the avatar.</response>
    /// <response code="404">No avatar found to delete.</response>
    /// <response code="500">An error occurred while deleting the avatar.</response>
    [HttpDelete("avatar")]
    [EnableRateLimiting(RateLimits.FixedWindow.WRITE)]
    [TextResponse(StatusCodes.Status200OK),
     TextResponse(StatusCodes.Status401Unauthorized),
     TextResponse(StatusCodes.Status403Forbidden),
     TextResponse(StatusCodes.Status404NotFound),
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAvatar()
    {
        try
        {
            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Account.Delete.Avatar))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            FileData? existingAvatar =
                await _fileDataRepo.FindAsync(x => x.UserId == user.Id && x.Type == EFileDataType.PROFILE_PICTURE);
            if (existingAvatar == null)
                return JsonResult(HttpStatusCode.NotFound, "No avatar found to delete.");

            existingAvatar.DeleteFile();
            await _fileDataRepo.RemoveAsync(existingAvatar, true);
            _memoryCache.RemoveValue($"avatar:{user.Id}");
            return JsonResult(HttpStatusCode.OK, "Avatar deleted successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "An error occurred while deleting the user's avatar.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    #region Admin Endpoints
    
    /// <summary>
    /// Uploads an avatar for another user (admin only).
    /// </summary>
    /// <param name="userId">The ID of the target user.</param>
    /// <param name="file">The avatar file to upload.</param>
    /// <returns>A success message or an appropriate HTTP status code.</returns>
    /// <response code="200">Avatar uploaded successfully.</response>
    /// <response code="400">Invalid file format or file size exceeds the limit.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to upload an avatar for another user.</response>
    /// <response code="404">Target user not found.</response>
    /// <response code="500">An error occurred while processing the avatar upload.</response>
    [HttpPost("{userId}/avatar")]
    [EnableRateLimiting(RateLimits.FixedWindow.ADMIN)]
    [Consumes("multipart/form-data")]
    [TextResponse(StatusCodes.Status200OK),
        TextResponse(StatusCodes.Status400BadRequest),
        TextResponse(StatusCodes.Status401Unauthorized),
        TextResponse(StatusCodes.Status403Forbidden),
        TextResponse(StatusCodes.Status404NotFound),
        TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadAvatarAdmin([BindRequired, FromRoute, MinLength(32), MaxLength(36)] string userId, [BindRequired] IFormFile file)
    {
        try
        {

            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return JsonResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Account.Create.AvatarOther))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            CustomUser? targetUser = await UserStore.FindUserByIdAsync(userId);
            if (targetUser == null)
                return JsonResult(HttpStatusCode.NotFound, "Target user not found");

            if (!await _userManager.HasHigherRoleThanAsync(user, targetUser))
                return JsonResult(HttpStatusCode.Forbidden, "You do not have permission to manage this user.");

            if (file.Length > 1024 * 500)
                return JsonResult(HttpStatusCode.BadRequest, "File size exceeds the 500 KB limit.");

            if (!file.FileName.EndsWith(".png"))
                return JsonResult(HttpStatusCode.BadRequest, "Only PNG files are allowed.");

            await using var stream = file.OpenReadStream();
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            stream.Position = 0;

            if (!await SkiaHelper.IsValidFormatAsync(stream, SKEncodedImageFormat.Png, Logger))
                return JsonResult(HttpStatusCode.BadRequest, "Invalid image format (not a real PNG).");

            FileData fd = new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.png",
                ContentType = "image/png",
                Type = EFileDataType.PROFILE_PICTURE,
                UserId = targetUser.Id
            };
            var uploadResult = await fd.SaveFileAsync(stream);
            if (!uploadResult.Success)
                return JsonResult(uploadResult.StatusCode, uploadResult.Message);
            
            FileData? existingAvatar = await _fileDataRepo.FindAsync(x =>
                x.UserId == targetUser.Id && x.Type == EFileDataType.PROFILE_PICTURE);
            if (existingAvatar != null)
            {
                existingAvatar.DeleteFile();
                await _fileDataRepo.RemoveAsync(existingAvatar, true);
                _memoryCache.RemoveValue("avatar:" + targetUser.Id);
            }

            await _fileDataRepo.AddAsync(fd, true);

            return JsonResult(HttpStatusCode.OK, "Avatar uploaded successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "An error occurred while uploading an avatar for another user.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Deletes an avatar for another user (admin only).
    /// </summary>
    /// <param name="userId">The ID of the target user.</param>
    /// <returns>A success message or an appropriate HTTP status code.</returns>
    /// <response code="200">Avatar deleted successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to delete the avatar for another user.</response>
    /// <response code="404">Target user or their avatar not found.</response>
    /// <response code="500">An error occurred while deleting the avatar.</response>
    [HttpDelete("{userId}/avatar")]
    [EnableRateLimiting(RateLimits.FixedWindow.ADMIN)]
    [TextResponse(StatusCodes.Status200OK),
     TextResponse(StatusCodes.Status401Unauthorized),
     TextResponse(StatusCodes.Status403Forbidden),
     TextResponse(StatusCodes.Status404NotFound),
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAvatarAdmin([BindRequired, FromRoute, MinLength(32), MaxLength(36)] string userId)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return JsonResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Account.Delete.AvatarOther))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            CustomUser? targetUser = await UserStore.FindUserByIdAsync(userId);
            if (targetUser == null)
                return JsonResult(HttpStatusCode.NotFound, "Target user not found");

            if (!await _userManager.HasHigherRoleThanAsync(user, targetUser))
                return JsonResult(HttpStatusCode.Forbidden, "You do not have permission to manage this user.");

            FileData? existingAvatar = await _fileDataRepo.FindAsync(x =>
                x.UserId == targetUser.Id && x.Type == EFileDataType.PROFILE_PICTURE);
            if (existingAvatar == null)
                return JsonResult(HttpStatusCode.NotFound, "No avatar found to delete.");

            existingAvatar.DeleteFile();
            await _fileDataRepo.RemoveAsync(existingAvatar, true);
            _memoryCache.RemoveValue($"avatar:{targetUser.Id}");
            return JsonResult(HttpStatusCode.OK, "Avatar deleted successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "An error occurred while deleting an avatar for another user.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    #endregion
}