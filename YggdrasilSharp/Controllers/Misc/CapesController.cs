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
using RateLimits = Tavstal.YggdrasilSharp.Models.RateLimiting.Constants.RateLimits;

namespace Tavstal.YggdrasilSharp.Controllers.Misc;

/// <summary>
/// Controller for managing capes, including uploading and deleting capes.
/// </summary>
[Route("/capes")]
[Authorize(AuthenticationSchemes = "Bearer,Basic")]
public class CapesController : CustomControllerBase
{
    private readonly CustomUserManager _userManager;
    private readonly IRepository<Cape> _capeRepo;
    private readonly IRepository<FileData> _fileDataRepo;
    private readonly CustomDbContext _dbContext;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CapesController"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for logging.</param>
    /// <param name="userManager">Custom user manager for user operations.</param>
    /// <param name="dbContext">Database context for accessing cape data.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="capeRepo">Repository for <see cref="Cape"/> entities.</param>
    /// <param name="fileDataRepo">Repository for <see cref="FileData"/> entities.</param>
    /// <param name="appConfiguration">Application settings.</param>
    public CapesController(ILogger<CapesController> logger, CustomUserManager userManager, CustomDbContext dbContext, CustomUserStore userStore, IRepository<Cape> capeRepo, IRepository<FileData> fileDataRepo, AppConfiguration appConfiguration) 
        : base(logger, userStore, appConfiguration)
    {
        _userManager = userManager;
        _capeRepo = capeRepo;
        _fileDataRepo = fileDataRepo;
        _dbContext = dbContext;
    }
    
    /// <summary>
    /// Uploads a new cape for the authenticated user.
    /// </summary>
    /// <param name="file">The cape file to upload.</param>
    /// <response code="200">Cape uploaded successfully.</response>
    /// <response code="400">Invalid file format, size, or duplicate content.</response>
    /// <response code="401">Unauthorized. User is not authenticated.</response>
    /// <response code="403">Forbidden. Insufficient permissions.</response>
    [HttpPost]
    [EnableRateLimiting(RateLimits.FixedWindow.UPLOAD)]
    [Consumes("multipart/form-data")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status400BadRequest), 
     TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadCape([BindRequired, FormFile(500, EFileSizeUnit.Kilobytes)] IFormFile file)
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

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Capes.Create))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            if (file.Length > 1024 * 512) // 500 KB limit
                return JsonResult(HttpStatusCode.BadRequest, "File size exceeds the 500 KB limit.");

            if (!file.FileName.EndsWith(".png"))
                return JsonResult(HttpStatusCode.BadRequest, "Only PNG files are allowed.");

            await using var stream = file.OpenReadStream();
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            stream.Position = 0;

            FileData? existingCape =
                await _fileDataRepo.FindAsync(x => x.Hash == fileHash && x.Type == EFileDataType.CAPE);
            if (existingCape != null)
                return JsonResult(HttpStatusCode.BadRequest, "Cape with the same content already exists.");

            if (!await SkiaHelper.IsValidSkinAsync(stream, Logger))
                return JsonResult(HttpStatusCode.BadRequest,
                    "Invalid image format or dimensions. Expected dimensions: 64x32, 64x64, 512x256, or 512x512.");

            FileData fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.png",
                ContentType = "image/png",
                Type = EFileDataType.CAPE,
            }, true);
            fd.SaveFile(stream);
            Cape cape = await _capeRepo.AddAsync(new Cape
            {
                Name = file.FileName.Split('.')[0],
                FileId = fd.Id,
                IsPublic = false
            }, true);
            await UserStore.UserCapes.AddAsync(new UserCape
            {
                UserId = user.Id,
                CapeId = cape.Id,
                IsSelected = false,
                Reason = "Uploaded by user",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }, true);
            return JsonResult(HttpStatusCode.OK, "Cape uploaded successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error uploading cape");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    
    /// <summary>
    /// Deletes a cape by its ID.
    /// </summary>
    /// <param name="capeId">The ID of the cape to delete.</param>
    /// <response code="200">Cape deleted successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized. User is not authenticated.</response>
    /// <response code="403">Forbidden. Insufficient permissions.</response>
    /// <response code="404">Cape not found.</response>
    [HttpDelete("{capeId}")]
    [EnableRateLimiting(RateLimits.FixedWindow.ADMIN)]
        [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status400BadRequest), 
        TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden),
        TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCape([BindRequired, FromRoute] ulong capeId)
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

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Capes.Delete))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            Cape? cape = await _capeRepo.FindByIdAsync(capeId);
            if (cape == null)
                return JsonResult(HttpStatusCode.NotFound, "Cape not found");

            var fileData = await _fileDataRepo.FindByIdAsync(cape.FileId);
            if (fileData != null)
            {
                fileData.DeleteFile();
                await _fileDataRepo.RemoveAsync(fileData);
            }
            
            var userCapes = await UserStore.UserCapes.QueryAsync(x => x.CapeId == capeId);
            foreach (var userCape in userCapes)
                await UserStore.UserCapes.RemoveAsync(userCape);
            
            await _capeRepo.RemoveAsync(cape);

            await _dbContext.SaveChangesAsync();
            return JsonResult(HttpStatusCode.OK, "Cape deleted successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to delete cape with ID {capeId}");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
}