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
using Tavstal.YggdrasilSharp.Services.Database;

namespace Tavstal.YggdrasilSharp.Controllers.User;

/// <summary>
/// Controller for managing user capes.
/// </summary>
[Route("/user")]
[Authorize(AuthenticationSchemes = "Bearer,Basic")]
public class UserCapesController : CustomControllerBase
{
    private readonly CustomUserManager _userManager;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UserCapesController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="userManager">The custom user manager.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="settings">Application settings.</param>
    public UserCapesController(ILogger<UserCapesController> logger, CustomUserManager userManager, CustomUserStore userStore, Settings settings) : base(logger, userStore, settings)
    {
        _userManager = userManager;
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

                return CodeResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return CodeResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Capes.Select))
                return CodeResult(HttpStatusCode.Forbidden, "Permission denied.");

            UserCape? cape = await UserStore.UserCapes.FindAsync(x => x.UserId == user.Id && x.CapeId == capeId);
            if (cape == null)
                return CodeResult(HttpStatusCode.NotFound, "Cape not found for the user");

            if (cape.IsSelected)
                return CodeResult(HttpStatusCode.BadRequest, "Cape is already selected");

            UserCape? currentlySelectedCape =
                await UserStore.UserCapes.FindAsync(x => x.UserId == user.Id && x.IsSelected);
            if (currentlySelectedCape != null)
            {
                currentlySelectedCape.IsSelected = false;
                await UserStore.UserCapes.UpdateAsync(currentlySelectedCape);
            }

            cape.IsSelected = true;
            await UserStore.UserCapes.UpdateAsync(cape, true);
            return CodeResult(HttpStatusCode.OK, "Cape selected successfully");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Error while selecting cape.");
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
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
                return CodeResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Capes.Unselect))
                return CodeResult(HttpStatusCode.Forbidden, "Permission denied.");

            UserCape? currentlySelectedCape =
                await UserStore.UserCapes.FindAsync(x => x.UserId == user.Id && x.IsSelected);
            if (currentlySelectedCape == null)
                return CodeResult(HttpStatusCode.NotFound, "No cape is currently selected for the user");

            currentlySelectedCape.IsSelected = false;
            await UserStore.UserCapes.UpdateAsync(currentlySelectedCape, true);
            return CodeResult(HttpStatusCode.OK, "Selected cape cleared successfully");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Error while clearing selected cape.");
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
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
    [EnableRateLimiting(RateLimits.ADMIN)]
        [TextResponse(StatusCodes.Status200OK),
        TextResponse(StatusCodes.Status400BadRequest),
        TextResponse(StatusCodes.Status401Unauthorized),
        TextResponse(StatusCodes.Status403Forbidden),
        TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SelectCapeAdmin([BindRequired, FromRoute, MinLength(32), MaxLength(36)] string userId, [BindRequired, FromRoute] ulong capeId)
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

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Capes.SelectOther))
                return CodeResult(HttpStatusCode.Forbidden, "Permission denied.");

            CustomUser? targetUser = await UserStore.FindUserByIdAsync(userId);
            if (targetUser == null)
                return CodeResult(HttpStatusCode.NotFound, "Target user not found");

            if (!await _userManager.HasHigherRoleThanAsync(user, targetUser))
                return CodeResult(HttpStatusCode.Forbidden, "You do not have permission to manage this user.");

            UserCape? cape = await UserStore.UserCapes.FindAsync(x => x.UserId == targetUser.Id && x.CapeId == capeId);
            if (cape == null)
                return CodeResult(HttpStatusCode.NotFound, "Cape not found for the user");

            if (cape.IsSelected)
                return CodeResult(HttpStatusCode.BadRequest, "Cape is already selected");

            UserCape? currentlySelectedCape =
                await UserStore.UserCapes.FindAsync(x => x.UserId == targetUser.Id && x.IsSelected);
            if (currentlySelectedCape != null)
            {
                currentlySelectedCape.IsSelected = false;
                await UserStore.UserCapes.UpdateAsync(currentlySelectedCape);
            }

            cape.IsSelected = true;
            await UserStore.UserCapes.UpdateAsync(cape, true);
            return CodeResult(HttpStatusCode.OK, "Cape selected successfully");
        }
        catch (Exception ex) 
        {
            Logger.LogCritical(ex, "Error while selecting cape for another user.");
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
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
    [EnableRateLimiting(RateLimits.ADMIN)]
        [TextResponse(StatusCodes.Status200OK),
            TextResponse(StatusCodes.Status401Unauthorized),
            TextResponse(StatusCodes.Status403Forbidden),
            TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearSelectedCapeAdmin([BindRequired, FromRoute, MinLength(32), MaxLength(36)] string userId)
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

            if (!await _userManager.HasPermissionAsync(user, CustomPermissions.Capes.UnselectOther))
                return CodeResult(HttpStatusCode.Forbidden, "Permission denied.");

            CustomUser? targetUser = await UserStore.FindUserByIdAsync(userId);
            if (targetUser == null)
                return CodeResult(HttpStatusCode.NotFound, "Target user not found");

            if (!await _userManager.HasHigherRoleThanAsync(user, targetUser))
                return CodeResult(HttpStatusCode.Forbidden, "You do not have permission to manage this user.");

            UserCape? currentlySelectedCape =
                await UserStore.UserCapes.FindAsync(x => x.UserId == targetUser.Id && x.IsSelected);
            if (currentlySelectedCape == null)
                return CodeResult(HttpStatusCode.NotFound, "No cape is currently selected for the user");

            currentlySelectedCape.IsSelected = false;
            await UserStore.UserCapes.UpdateAsync(currentlySelectedCape, true);
            return CodeResult(HttpStatusCode.OK, "Selected cape cleared successfully");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Error while clearing selected cape for another user.");
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    #endregion
}