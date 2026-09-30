using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Bodies.Launcher;
using Tavstal.YggdrasilSharp.Models.Claims;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Database.Launcher;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using RateLimits = Tavstal.YggdrasilSharp.Models.RateLimiting.Constants.RateLimits;

namespace Tavstal.YggdrasilSharp.Controllers.Launcher;

/// <summary>
/// Controller for managing launcher versions and their associated data.
/// </summary>
[ApiController]
[Route("/launcher")]
public class LauncherController : CustomControllerBase
{
    private readonly CustomUserManager _userManager;
    private readonly IRepository<LauncherVersion> _launcherVersionRepo;
    private readonly IRepository<LauncherVersionData> _launcherVersionDataRepo;
    private readonly IRepository<FileData> _fileDataRepository;
    private readonly MemoryCacheService _memoryCacheService;
    private readonly TimeSpan CacheTTL = TimeSpan.FromHours(1);
    
    /// <summary>
    /// Initializes a new instance of the <see cref="LauncherController"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for logging.</param>
    /// <param name="userManager">Custom user manager for user operations.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="launcherVersionRepo">Repository for <see cref="LauncherVersion"/> entities.</param>
    /// <param name="launcherVersionDataRepo">Repository for <see cref="LauncherVersionData"/> entities.</param>
    /// <param name="fileDataRepository">Repository for <see cref="FileData"/> entities.</param>
    /// <param name="memoryCacheService">Service for caching launcher data.</param>
    /// <param name="appConfiguration">Application settings.</param>
    public LauncherController(ILogger<LauncherController> logger, CustomUserManager userManager, CustomUserStore userStore, IRepository<LauncherVersion> launcherVersionRepo, IRepository<LauncherVersionData> launcherVersionDataRepo,
       IRepository<FileData> fileDataRepository, MemoryCacheService memoryCacheService, AppConfiguration appConfiguration) : base(logger, userStore, appConfiguration)
    {
        _userManager = userManager;
        _launcherVersionRepo = launcherVersionRepo;
        _launcherVersionDataRepo = launcherVersionDataRepo;
        _fileDataRepository = fileDataRepository;
        _memoryCacheService = memoryCacheService;
    }

    /// <summary>
    /// Retrieves all launcher versions.
    /// </summary>
    /// <response code="200">Returns a list of launcher versions.</response>
    /// <response code="404">No launcher versions found.</response>
    [HttpGet("versions")]
    [JsonResponse(typeof(List<LauncherVersion>)), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLauncherVersions()
    {
        try
        {
            var versions = (await _launcherVersionRepo.QueryAsync(null)).ToList();
            if (versions.Count == 0)
                return JsonResult(HttpStatusCode.NotFound, "No launcher versions found.");
            return JsonResult(versions);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while retrieving launcher versions.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Retrieves the latest launcher version.
    /// </summary>
    /// <response code="200">Returns the latest launcher version.</response>
    /// <response code="404">No launcher versions found.</response>
    [HttpGet("versions/latest")]
    [JsonResponse(typeof(LauncherVersion)), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestLauncherVersion()
    {
        try
        {
            var versions = (await _launcherVersionRepo.QueryAsync(null)).ToList();
            if (versions.Count == 0)
                return JsonResult(HttpStatusCode.NotFound, "No launcher versions found.");

            var version = versions.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            
            if (version == null)
                return JsonResult(HttpStatusCode.NotFound, "No launcher versions found.");
            return JsonResult(version);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while retrieving the latest launcher version.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Retrieves details of a specific launcher version by ID.
    /// </summary>
    /// <param name="id">The ID of the launcher version.</param>
    /// <response code="200">Returns the launcher version details.</response>
    /// <response code="404">Launcher version not found.</response>
    [HttpGet("version/{id}")]
    [EnableRateLimiting(RateLimits.FixedWindow.SEARCH)]
    [JsonResponse(typeof(LauncherVersion)), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLauncherVersionDetails([BindRequired, FromRoute] ulong id)
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

            var version = await _launcherVersionRepo.FindByIdAsync(id);
            if (version == null)
                return JsonResult(HttpStatusCode.NotFound, "Launcher version not found.");
            
            var versionDetails = await _launcherVersionDataRepo.QueryAsync(x => x.VersionId == version.Id);
            return JsonResult(versionDetails);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while retrieving launcher version details.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Retrieves the download link for a specific launcher version and OS.
    /// </summary>
    /// <param name="id">The ID of the launcher version.</param>
    /// <param name="os">The operating system for the launcher version.</param>
    /// <response code="200">Returns the file to download.</response>
    /// <response code="404">Launcher version or file data not found.</response>
    /// <response code="500">Failed to retrieve the file.</response>
    [HttpGet("version/{id}/download")]
    [EnableRateLimiting(RateLimits.FixedWindow.DOWNLOAD)]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status404NotFound), TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadLauncherVersion([BindRequired, FromRoute] ulong id, [BindRequired, FromQuery] ELauncherOs os)
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

            string cacheKey = $"launcher_version:{id}:download_{os}";
            if (_memoryCacheService.TryGetValue(cacheKey, out (byte[], string) cachedVersion))
                return File(cachedVersion.Item1, cachedVersion.Item2);

            var version = await _launcherVersionRepo.FindByIdAsync(id);
            if (version == null)
                return JsonResult(HttpStatusCode.NotFound, "Launcher version not found.");

            var versionData =
                await _launcherVersionDataRepo.FindAsync(x => x.Os == os && x.VersionId == version.Id);
            if (versionData == null)
                return JsonResult(HttpStatusCode.NotFound, "Launcher version data not found.");

            var fileData = await _fileDataRepository.FindAsync(x => x.Id == versionData.FileId);
            if (fileData == null || !fileData.Exists())
                return JsonResult(HttpStatusCode.NotFound, "File data not found.");

            byte[]? bytes = fileData.GetFileData();
            if (bytes == null)
                return JsonResult(HttpStatusCode.InternalServerError, "Failed to retrieve the file.");

            _memoryCacheService.SetValue(cacheKey, (bytes, fileData.ContentType), CacheTTL);
            return File(bytes, fileData.ContentType);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while retrieving the launcher version download link.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    #region Admin Endpoints

    /// <summary>
    /// Creates a new launcher version.
    /// </summary>
    /// <param name="request">The request body containing launcher version details.</param>
    /// <response code="200">Launcher version created successfully.</response>
    /// <response code="400">Invalid request or duplicate version.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">Insufficient permissions.</response>
    [HttpPost("version")]
    [Authorize(AuthenticationSchemes = "Bearer,Basic")]
    [EnableRateLimiting(RateLimits.FixedWindow.ADMIN)]
    [Consumes("application/json")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status400BadRequest), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateLauncherVersion([Required, FromBody] CreateLauncherVersionRequest request)
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

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Launcher.CreateVersion))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            LauncherVersion? existingVersion = await _launcherVersionRepo.FindAsync(x => x.Version == request.Version);
            if (existingVersion != null)
                return JsonResult(HttpStatusCode.BadRequest,
                    "A launcher version with the same version number already exists.");

            await _launcherVersionRepo.AddAsync(new LauncherVersion
            {
                Version = request.Version,
                VersionType = request.VersionType,
                Changelog = request.Changelog,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            }, true);

            return JsonResult(HttpStatusCode.OK, "Launcher version created successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while creating a new launcher version.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Updates an existing launcher version.
    /// </summary>
    /// <param name="id">The ID of the launcher version to update.</param>
    /// <param name="request">The request body containing updated launcher version details.</param>
    /// <response code="200">Launcher version updated successfully.</response>
    /// <response code="400">Invalid request or duplicate version.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">Insufficient permissions.</response>
    /// <response code="404">Launcher version not found.</response>
    [HttpPut("version/{id}")]
    [Authorize(AuthenticationSchemes = "Bearer,Basic")]
    [EnableRateLimiting(RateLimits.FixedWindow.ADMIN)]
    [Consumes("application/json")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status400BadRequest), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLauncherVersion([BindRequired, FromRoute] ulong id, [Required, FromBody] UpdateLauncherVersionRequest request)
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

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Launcher.UpdateVersion))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            LauncherVersion? version = await _launcherVersionRepo.FindByIdAsync(id);
            if (version == null)
                return JsonResult(HttpStatusCode.NotFound, "Launcher version not found.");

            if (!string.IsNullOrEmpty(request.Version))
            {
                LauncherVersion? existingVersion =
                    await _launcherVersionRepo.FindAsync(x => x.Version == request.Version && x.Id != version.Id);
                if (existingVersion != null)
                    return JsonResult(HttpStatusCode.BadRequest,
                        "A launcher version with the same version number already exists.");

                version.Version = request.Version;
            }

            if (!string.IsNullOrEmpty(request.Changelog))
                version.Changelog = request.Changelog;

            if (request.VersionType != null)
                version.VersionType = request.VersionType.Value;

            await _launcherVersionRepo.UpdateAsync(version, true);
            return JsonResult(HttpStatusCode.OK, "Launcher version updated successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while updating the launcher version.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Deletes a launcher version.
    /// </summary>
    /// <param name="id">The ID of the launcher version to delete.</param>
    /// <response code="200">Launcher version deleted successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">Insufficient permissions.</response>
    /// <response code="404">Launcher version not found.</response>
    [HttpDelete("version/{id}")]
    [Authorize(AuthenticationSchemes = "Bearer,Basic")]
    [EnableRateLimiting(RateLimits.FixedWindow.ADMIN)]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLauncherVersion([BindRequired, FromRoute] ulong id)
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

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Launcher.DeleteVersion))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            LauncherVersion? version = await _launcherVersionRepo.FindByIdAsync(id);
            if (version == null)
                return JsonResult(HttpStatusCode.NotFound, "Launcher version not found.");

            var versions = await _launcherVersionDataRepo.QueryAsync(x => x.VersionId == version.Id);
            foreach (var ver in versions)
            {
                var fileData = await _fileDataRepository.FindByIdAsync(ver.FileId);
                if (fileData != null)
                {
                    fileData.DeleteFile();
                    await _fileDataRepository.RemoveAsync(fileData);
                }
                await _launcherVersionDataRepo.RemoveAsync(ver);
            }
            
            await _launcherVersionRepo.RemoveAsync(version, true);
            return JsonResult(HttpStatusCode.OK, "Launcher version deleted successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while deleting the launcher version.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Adds data for a specific launcher version.
    /// </summary>
    /// <param name="id">The ID of the launcher version.</param>
    /// <param name="request">The request body containing launcher version data details.</param>
    /// <response code="200">Launcher version data added successfully.</response>
    /// <response code="400">Invalid request or file type.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">Insufficient permissions.</response>
    /// <response code="404">Launcher version not found.</response>
    [HttpPost("version/{id}/data")]
    [Authorize(AuthenticationSchemes = "Bearer,Basic")]
    [EnableRateLimiting(RateLimits.FixedWindow.ADMIN)]
    [Consumes("multipart/form-data")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status400BadRequest), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddLauncherVersionData([BindRequired, FromRoute] ulong id, [Required, FromForm] CreateLauncherVersionDataRequest request)
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

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Launcher.CreateVersion))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            LauncherVersion? version = await _launcherVersionRepo.FindByIdAsync(id);
            if (version == null)
                return JsonResult(HttpStatusCode.NotFound, "Launcher version not found.");

            LauncherVersionData? versionData =
                await _launcherVersionDataRepo.FindAsync(x => x.VersionId == version.Id && x.Os == request.Os);
            if (versionData != null)
                return JsonResult(HttpStatusCode.NotFound,
                    "Launcher version data for the specified OS already exists.");

            if (request.File.Length > 1024 * 1024 * 512) // 512 MB limit
                return JsonResult(HttpStatusCode.BadRequest, "File size exceeds the 512 MB limit.");

            if (!(request.File.FileName.EndsWith(".zip") || request.File.FileName.EndsWith(".tar.gz") ||
                  request.File.FileName.EndsWith(".tar")))
                return JsonResult(HttpStatusCode.BadRequest,
                    "Invalid file type. Only .zip, .tar.gz, and .tar files are allowed.");

            await using var stream = request.File.OpenReadStream();
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            stream.Position = 0;

            FileData fd = await _fileDataRepository.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.{request.File.FileName.Split('.').Last()}",
                ContentType = request.File.ContentType,
                Type = EFileDataType.LAUNCHER
            }, true);
            fd.SaveFile(stream);

            await _launcherVersionDataRepo.AddAsync(new LauncherVersionData
            {
                VersionId = version.Id,
                FileId = fd.Id,
                Os = request.Os,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            }, true);
            return JsonResult(HttpStatusCode.OK, "Launcher version data added successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while adding launcher version data.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Deletes data for a specific launcher version.
    /// </summary>
    /// <param name="versionId">The ID of the launcher version.</param>
    /// <param name="dataId">The ID of the launcher version data to delete.</param>
    /// <response code="200">Launcher version data deleted successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">Insufficient permissions.</response>
    /// <response code="404">Launcher version data not found.</response>
    [HttpDelete("version/{versionId}/data/{dataId}")]
    [Authorize(AuthenticationSchemes = "Bearer,Basic")]
    [EnableRateLimiting(RateLimits.FixedWindow.ADMIN)]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLauncherVersionData([BindRequired, FromRoute] ulong versionId, [BindRequired, FromRoute] ulong dataId)
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

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Launcher.DeleteVersion))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            LauncherVersionData? versionData =
                await _launcherVersionDataRepo.FindAsync(x => x.Id == dataId && x.VersionId == versionId);
            if (versionData == null)
                return JsonResult(HttpStatusCode.NotFound, "Launcher version data not found.");

            FileData? fileData = await _fileDataRepository.FindByIdAsync(versionData.FileId);
            if (fileData != null)
            {
                fileData.DeleteFile();
                await _fileDataRepository.RemoveAsync(fileData);
            }

            await _launcherVersionDataRepo.RemoveAsync(versionData, true);
            return JsonResult(HttpStatusCode.OK, "Launcher version data added successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while deleting launcher version data.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    #endregion
}