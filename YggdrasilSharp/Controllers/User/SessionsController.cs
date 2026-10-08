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
using Tavstal.YggdrasilSharp.Serialization;
using Tavstal.YggdrasilSharp.Services.Database;
using RateLimits = Tavstal.YggdrasilSharp.Models.RateLimiting.Constants.RateLimits;

namespace Tavstal.YggdrasilSharp.Controllers.User;

/// <summary>
/// Controller for managing user sessions.
/// </summary>
[ApiController]
[Route("/user")]
[Authorize(AuthenticationSchemes = "Bearer,Basic")]
public class SessionsController : CustomControllerBase
{
    private readonly CustomDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionsController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="userManager">The custom user manager.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="appConfiguration">Application settings.</param>
    /// <param name="dbContext">The database context.</param>
    public SessionsController(ILogger<SessionsController> logger, CustomUserManager userManager, CustomUserStore userStore, AppConfiguration appConfiguration,
        CustomDbContext dbContext) : base(logger, userManager, userStore, appConfiguration)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Retrieves the current user's active sessions.
    /// </summary>
    /// <returns>A list of active sessions or an appropriate HTTP status code.</returns>
    /// <response code="200">Sessions retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to view sessions.</response>
    [HttpGet("sessions")]
    [JsonResponse(typeof(List<CustomUserLogin>)),TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSessions()
    {
        try
        {
            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await UserManager.HasPermissionAsync(user, CustomPermissions.Account.View.Sessions))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            var userLogins = await UserStore.UserLogins.QueryAsync(x => x.UserId == user.Id);
            return JsonResult(userLogins, CustomJsonContext.Default.ListCustomUserLogin);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while retrieving user sessions.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Revokes a specific session for the current user.
    /// </summary>
    /// <param name="sessionId">The ID of the session to revoke.</param>
    /// <returns>A success message or an appropriate HTTP status code.</returns>
    /// <response code="200">Session revoked successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to revoke the session.</response>
    /// <response code="404">Session not found.</response>
    [HttpDelete("sessions/{sessionId}")]
    [EnableRateLimiting(RateLimits.FixedWindow.WRITE)]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession([BindRequired, FromRoute] ulong sessionId)
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

            if (!await UserManager.HasPermissionAsync(user, CustomPermissions.Account.Delete.Session))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            var userLogin = await UserStore.UserLogins.FindAsync(x => x.Id == sessionId && x.UserId == user.Id);
            if (userLogin == null)
                return JsonResult(HttpStatusCode.NotFound, "Session not found.");

            await UserStore.UserLogins.RemoveAsync(userLogin, true);
            return JsonResult(HttpStatusCode.OK, "Session revoked successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while revoking the user session.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Revokes all sessions for the current user.
    /// </summary>
    /// <returns>A success message or an appropriate HTTP status code.</returns>
    /// <response code="200">All sessions revoked successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to revoke all sessions.</response>
    [HttpDelete("sessions")]
    [EnableRateLimiting(RateLimits.FixedWindow.WRITE)]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokeAllSessions()
    {
        try
        {
            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await UserManager.HasPermissionAsync(user, CustomPermissions.Account.Delete.Sessions))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            await _dbContext.ClearUserLoginsAsync(user.Id, true);
            return JsonResult(HttpStatusCode.OK, "All sessions revoked successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while revoking all user sessions.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    #region Admin Endpoints
    /// <summary>
    /// Retrieves the active sessions for a specific user (admin only).
    /// </summary>
    /// <param name="userId">The ID of the target user.</param>
    /// <returns>A list of active sessions or an appropriate HTTP status code.</returns>
    /// <response code="200">Sessions retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to view sessions for another user.</response>
    /// <response code="404">Target user not found.</response>
    [HttpGet("{userId}/sessions")]
    [EnableRateLimiting(RateLimits.FixedWindow.ADMIN)]
    [JsonResponse(typeof(List<CustomUserLogin>)),TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionsAdmin([BindRequired, FromRoute, StringLength(36, MinimumLength = 32)] string userId)
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

            if (!await UserManager.HasPermissionAsync(user, CustomPermissions.Account.View.SessionsOther))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            CustomUser? targetUser = await UserStore.FindUserByIdAsync(userId);
            if (targetUser == null)
                return JsonResult(HttpStatusCode.NotFound, "Target user not found");

            if (!await UserManager.HasHigherRoleThanAsync(user, targetUser))
                return JsonResult(HttpStatusCode.Forbidden, "You do not have permission to manage this user.");

            var userLogins = await UserStore.UserLogins.QueryAsync(x => x.UserId == targetUser.Id);
            return JsonResult(userLogins, CustomJsonContext.Default.ListCustomUserLogin);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while retrieving sessions for user with ID {UserId}.", userId);
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Revokes a specific session for another user (admin only).
    /// </summary>
    /// <param name="userId">The ID of the target user.</param>
    /// <param name="sessionId">The ID of the session to revoke.</param>
    /// <returns>A success message or an appropriate HTTP status code.</returns>
    /// <response code="200">Session revoked successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to revoke the session for another user.</response>
    /// <response code="404">Target user or session not found.</response>
    [HttpDelete("{userId}/sessions/{sessionId}")]
    [EnableRateLimiting(RateLimits.FixedWindow.ADMIN)]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSessionAdmin([BindRequired, FromRoute, StringLength(36, MinimumLength = 32)] string userId, [BindRequired, FromRoute] ulong sessionId)
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

            if (!await UserManager.HasPermissionAsync(user, CustomPermissions.Account.Delete.SessionOther))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            CustomUser? targetUser = await UserStore.FindUserByIdAsync(userId);
            if (targetUser == null)
                return JsonResult(HttpStatusCode.NotFound, "Target user not found");

            if (!await UserManager.HasHigherRoleThanAsync(user, targetUser))
                return JsonResult(HttpStatusCode.Forbidden, "You do not have permission to manage this user.");

            var userLogin = await UserStore.UserLogins.FindAsync(x => x.Id == sessionId && x.UserId == targetUser.Id);
            if (userLogin == null)
                return JsonResult(HttpStatusCode.NotFound, "Session not found.");

            await UserStore.UserLogins.RemoveAsync(userLogin, true);
            return JsonResult(HttpStatusCode.OK, "Session revoked successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while revoking session.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Revokes all sessions for another user (admin only).
    /// </summary>
    /// <param name="userId">The ID of the target user.</param>
    /// <returns>A success message or an appropriate HTTP status code.</returns>
    /// <response code="200">All sessions revoked successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User does not have permission to revoke all sessions for another user.</response>
    /// <response code="404">Target user not found.</response>
    [HttpDelete("{userId}/sessions")]
    [EnableRateLimiting(RateLimits.FixedWindow.ADMIN)]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeAllSessionsAdmin([BindRequired, FromRoute] string userId)
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

            if (!await UserManager.HasPermissionAsync(user, CustomPermissions.Account.Delete.SessionsOther))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            CustomUser? targetUser = await UserStore.FindUserByIdAsync(userId);
            if (targetUser == null)
                return JsonResult(HttpStatusCode.NotFound, "Target user not found");

            if (!await UserManager.HasHigherRoleThanAsync(user, targetUser))
                return JsonResult(HttpStatusCode.Forbidden, "You do not have permission to manage this user.");

            await _dbContext.ClearUserLoginsAsync(targetUser.Id, true);
            return JsonResult(HttpStatusCode.OK, "All sessions revoked successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while revoking all sessions of the target user.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    #endregion
}
