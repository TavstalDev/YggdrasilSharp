using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Models.Responses.User;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;

namespace Tavstal.YggdrasilSharp.Controllers.User;

/// <summary>
/// Controller for managing public user-related operations.
/// </summary>
[ApiController]
[Route("/user")]
public class PublicUserController : CustomControllerBase
{
    private readonly AppConfiguration _appConfiguration;
    private readonly IRepository<FileData> _fileDataRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicUserController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="userManager">Custom user manager for user operations.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="appConfiguration">The application settings.</param>
    /// <param name="fileDataRepository">Repository for managing file data.</param>
    public PublicUserController(ILogger<PublicUserController> logger, CustomUserManager userManager, CustomUserStore userStore, AppConfiguration appConfiguration,
        IRepository<FileData> fileDataRepository)
        : base(logger, userManager, userStore, appConfiguration)
    {
        _appConfiguration = appConfiguration;
        _fileDataRepository = fileDataRepository;
    }

    /// <summary>
    /// Retrieves information about a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve information for.</param>
    /// <returns>A JSON object containing user information or an appropriate HTTP status code.</returns>
    /// <response code="200">User information retrieved successfully.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("{userId}")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserInfo([BindRequired, FromRoute, MinLength(32), MaxLength(36)] string userId)
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

            CustomUser? user = await UserStore.FindUserByIdAsync(userId);
            if (user == null)
                return JsonResult(HttpStatusCode.NotFound, "User not found.");

            string avatarUrl = string.Empty;
            if (user.Avatar != null && !string.IsNullOrEmpty(_appConfiguration.Misc.ApiUrl))
                avatarUrl = user.Avatar.GetUrl(_appConfiguration.Misc.ApiUrl);

            return JsonResult(new UserInfoResponse
            {
                UserId = user.Id,
                AvatarUrl = avatarUrl,
                DiscordId = user.DiscordId,
                UserName = user.UserName,
                CreateDate = user.CreateDate,
                LastUpdate = user.LastUpdate
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while retrieving user information.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Retrieves the avatar of a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user whose avatar is to be retrieved.</param>
    /// <returns>The avatar file or an appropriate HTTP status code.</returns>
    /// <response code="200">Avatar retrieved successfully.</response>
    /// <response code="304">Avatar not modified (ETag matches).</response>
    /// <response code="404">User or avatar not found.</response>
    [HttpGet("{userId}/avatar")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status304NotModified), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvatar([BindRequired, FromRoute, MinLength(32), MaxLength(36)] string userId)
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

            CustomUser? user = await UserStore.FindUserByIdAsync(userId);
            if (user == null)
                return JsonResult(HttpStatusCode.NotFound, "User not found.");

            FileData? existingAvatar =
                await _fileDataRepository.FindAsync(x => x.UserId == user.Id && x.Type == EFileDataType.PROFILE_PICTURE);
            if (existingAvatar == null)
                return JsonResult(HttpStatusCode.NotFound, "No avatar found.");

            string etag = $"\"{existingAvatar.Hash}\"";
            if (Request.Headers.TryGetValue("If-None-Match", out var incomingEtag) &&
                incomingEtag == etag)
            {
                return CodeResult(HttpStatusCode.NotModified);
            }

            Response.Headers.CacheControl = "public,no-cache";
            return File(existingAvatar.GetFileStream(), existingAvatar.ContentType, existingAvatar.FileName,
                enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while retrieving the user's avatar.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
}
