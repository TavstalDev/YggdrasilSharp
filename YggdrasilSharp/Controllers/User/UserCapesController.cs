using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Claims;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.Database;
using RateLimits = Tavstal.YggdrasilSharp.Models.RateLimiting.Constants.RateLimits;

namespace Tavstal.YggdrasilSharp.Controllers.User;

/// <summary>
/// Controller for managing user capes.
/// </summary>
[ApiController]
[Route("/user")]
[Authorize(AuthenticationSchemes = "Bearer,Basic")]
public class UserCapesController : CustomControllerBase
{
    private readonly MemoryCacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserCapesController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="userManager">The custom user manager.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="appConfiguration">Application settings.</param>
    /// <param name="cacheService">The memory cache service for caching data.</param>
    public UserCapesController(ILogger<UserCapesController> logger, CustomUserManager userManager, CustomUserStore userStore, AppConfiguration appConfiguration,
        MemoryCacheService cacheService) : base(logger, userManager, userStore, appConfiguration)
    {
        _cacheService = cacheService;
    }

    /// <summary>
    /// Selects a cape for the current user.
    /// </summary>
    /// <param name="capeId">The ID of the cape to select.</param>
    /// <returns>A success message or an appropriate HTTP status code.</returns>
    /// <response code="200">Cape selected successfully.</response>
    /// <response code="400">Cape is already selected.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to select a cape.</response>
    /// <response code="404">Cape not found for the user.</response>
    [HttpPatch("cape/{capeId}")]
    [TextResponse(StatusCodes.Status200OK),
     TextResponse(StatusCodes.Status400BadRequest),
     TextResponse(StatusCodes.Status401Unauthorized),
     TextResponse(StatusCodes.Status403Forbidden),
     TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SelectCape([BindRequired, FromRoute] ulong capeId)
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

            if (!await UserManager.HasPermissionAsync(user, CustomPermissions.Capes.Select))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            UserCape? cape = await UserStore.UserCapes.FindAsync(x => x.UserId == user.Id && x.CapeId == capeId);
            if (cape == null)
                return JsonResult(HttpStatusCode.NotFound, "Cape not found for the user");

            if (cape.IsSelected)
                return JsonResult(HttpStatusCode.BadRequest, "Cape is already selected");

            UserCape? currentlySelectedCape =
                await UserStore.UserCapes.FindAsync(x => x.UserId == user.Id && x.IsSelected);
            if (currentlySelectedCape != null)
            {
                currentlySelectedCape.IsSelected = false;
                await UserStore.UserCapes.UpdateAsync(currentlySelectedCape);
            }

            // Remove profile cache
            _cacheService.RemoveValue($"profile:{user.Id}:signed");
            _cacheService.RemoveValue($"profile:{user.Id}:unsigned");

            cape.IsSelected = true;
            await UserStore.UserCapes.UpdateAsync(cape, true);
            return JsonResult(HttpStatusCode.OK, "Cape selected successfully");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Error while selecting cape.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Clears the currently selected cape for the current user.
    /// </summary>
    /// <returns>A success message or an appropriate HTTP status code.</returns>
    /// <response code="200">Selected cape cleared successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to clear the selected cape.</response>
    /// <response code="404">No cape is currently selected for the user.</response>
    [HttpDelete("cape")]
    [TextResponse(StatusCodes.Status200OK),
     TextResponse(StatusCodes.Status401Unauthorized),
     TextResponse(StatusCodes.Status403Forbidden),
     TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearSelectedCape()
    {
        try
        {
            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await UserManager.HasPermissionAsync(user, CustomPermissions.Capes.Unselect))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            UserCape? currentlySelectedCape =
                await UserStore.UserCapes.FindAsync(x => x.UserId == user.Id && x.IsSelected);
            if (currentlySelectedCape == null)
                return JsonResult(HttpStatusCode.NotFound, "No cape is currently selected for the user");

            // Remove profile cache
            _cacheService.RemoveValue($"profile:{user.Id}:signed");
            _cacheService.RemoveValue($"profile:{user.Id}:unsigned");

            currentlySelectedCape.IsSelected = false;
            await UserStore.UserCapes.UpdateAsync(currentlySelectedCape, true);
            return JsonResult(HttpStatusCode.OK, "Selected cape cleared successfully");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Error while clearing selected cape.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    #region Admin Endpoints
    /// <summary>
    /// Selects a cape for another user (admin only).
    /// </summary>
    /// <param name="userId">The ID of the target user.</param>
    /// <param name="capeId">The ID of the cape to select.</param>
    /// <returns>A success message or an appropriate HTTP status code.</returns>
    /// <response code="200">Cape selected successfully.</response>
    /// <response code="400">Cape is already selected.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to select a cape for another user.</response>
    /// <response code="404">Target user or cape not found.</response>
    [HttpPatch("{userId}/cape/{capeId}")]
    [EnableRateLimiting(RateLimits.FixedWindow.ADMIN)]
        [TextResponse(StatusCodes.Status200OK),
        TextResponse(StatusCodes.Status400BadRequest),
        TextResponse(StatusCodes.Status401Unauthorized),
        TextResponse(StatusCodes.Status403Forbidden),
        TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SelectCapeAdmin([BindRequired, FromRoute, StringLength(36, MinimumLength = 32)] string userId, [BindRequired, FromRoute] ulong capeId)
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

            if (!await UserManager.HasPermissionAsync(user, CustomPermissions.Capes.SelectOther))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            CustomUser? targetUser = await UserStore.FindUserByIdAsync(userId);
            if (targetUser == null)
                return JsonResult(HttpStatusCode.NotFound, "Target user not found");

            if (!await UserManager.HasHigherRoleThanAsync(user, targetUser))
                return JsonResult(HttpStatusCode.Forbidden, "You do not have permission to manage this user.");

            UserCape? cape = await UserStore.UserCapes.FindAsync(x => x.UserId == targetUser.Id && x.CapeId == capeId);
            if (cape == null)
                return JsonResult(HttpStatusCode.NotFound, "Cape not found for the user");

            if (cape.IsSelected)
                return JsonResult(HttpStatusCode.BadRequest, "Cape is already selected");

            UserCape? currentlySelectedCape =
                await UserStore.UserCapes.FindAsync(x => x.UserId == targetUser.Id && x.IsSelected);
            if (currentlySelectedCape != null)
            {
                currentlySelectedCape.IsSelected = false;
                await UserStore.UserCapes.UpdateAsync(currentlySelectedCape);
            }

            // Remove profile cache
            _cacheService.RemoveValue($"profile:{targetUser.Id}:signed");
            _cacheService.RemoveValue($"profile:{targetUser.Id}:unsigned");

            cape.IsSelected = true;
            await UserStore.UserCapes.UpdateAsync(cape, true);
            return JsonResult(HttpStatusCode.OK, "Cape selected successfully");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Error while selecting cape for another user.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Clears the currently selected cape for another user (admin only).
    /// </summary>
    /// <param name="userId">The ID of the target user.</param>
    /// <returns>A success message or an appropriate HTTP status code.</returns>
    /// <response code="200">Selected cape cleared successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to clear the selected cape for another user.</response>
    /// <response code="404">Target user or selected cape not found.</response>
    [HttpDelete("{userId}/cape")]
    [EnableRateLimiting(RateLimits.FixedWindow.ADMIN)]
        [TextResponse(StatusCodes.Status200OK),
            TextResponse(StatusCodes.Status401Unauthorized),
            TextResponse(StatusCodes.Status403Forbidden),
            TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearSelectedCapeAdmin([BindRequired, FromRoute, StringLength(36, MinimumLength = 32)] string userId)
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

            if (!await UserManager.HasPermissionAsync(user, CustomPermissions.Capes.UnselectOther))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            CustomUser? targetUser = await UserStore.FindUserByIdAsync(userId);
            if (targetUser == null)
                return JsonResult(HttpStatusCode.NotFound, "Target user not found");

            if (!await UserManager.HasHigherRoleThanAsync(user, targetUser))
                return JsonResult(HttpStatusCode.Forbidden, "You do not have permission to manage this user.");

            UserCape? currentlySelectedCape =
                await UserStore.UserCapes.FindAsync(x => x.UserId == targetUser.Id && x.IsSelected);
            if (currentlySelectedCape == null)
                return JsonResult(HttpStatusCode.NotFound, "No cape is currently selected for the user");

            // Remove profile cache
            _cacheService.RemoveValue($"profile:{targetUser.Id}:signed");
            _cacheService.RemoveValue($"profile:{targetUser.Id}:unsigned");

            currentlySelectedCape.IsSelected = false;
            await UserStore.UserCapes.UpdateAsync(currentlySelectedCape, true);
            return JsonResult(HttpStatusCode.OK, "Selected cape cleared successfully");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Error while clearing selected cape for another user.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    #endregion
}
