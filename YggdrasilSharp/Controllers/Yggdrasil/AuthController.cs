using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Models.RateLimiting.Constants;
using Tavstal.YggdrasilSharp.Models.Responses.Yggdrasil;
using Tavstal.YggdrasilSharp.Services.Database;

namespace Tavstal.YggdrasilSharp.Controllers.Yggdrasil;

/// <summary>
/// Controller for handling legacy Yggdrasil authentication API requests.
/// Every endpoint is gated behind the <c>Yggdrasil:EnableLegacyAuth</c> configuration key.
/// </summary>
[ApiController]
[Route("yggdrasil")]
[Route("yggdrasil/authserver")]
[EnableRateLimiting(RateLimits.FixedWindow.AUTH_LOGIN)]
[Tags("Yggdrasil")]
public class AuthController  : CustomControllerBase
{
    private readonly CustomSignInManager _signInManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging information.</param>
    /// <param name="signInManager">The sign-in manager for handling authentication flows.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="appConfiguration">Application settings.</param>
    public AuthController(ILogger<ProfilesController> logger, CustomSignInManager signInManager, CustomUserStore userStore,
        AppConfiguration appConfiguration) : base(logger, userStore, appConfiguration)
    {
        _signInManager = signInManager;
    }

    /// <summary>
    /// Authenticates a user and issues an Yggdrasil access token.
    /// </summary>
    /// <param name="request">The login request containing the credentials and client agent.</param>
    /// <returns>An <see cref="IActionResult"/> containing the authentication result.</returns>
    /// <response code="400">The request body is invalid.</response>
    /// <response code="403">Legacy authentication is disabled on this server.</response>
    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate([Required, FromBody] YigLoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return YigErrorResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            if (!AppConfiguration.Yggdrasil.EnableLegacyAuth)
                return YigErrorResult(HttpStatusCode.Forbidden, "Legacy authentication is disabled on this server.");

            if (AppConfiguration.Yggdrasil.EnforceAgent && (request.Agent.Name != AppConfiguration.Yggdrasil.Agent.Name || request.Agent.Version != AppConfiguration.Yggdrasil.Agent.Version))
                return JsonResult(HttpStatusCode.BadRequest, "Invalid request.");

            LauncherSignInResult result = await _signInManager.LauncherSignInAsync(request.Username, request.Password, request.ClientToken, HttpContext);
            if (result.RequiresTwoFactor)
                return JsonResult(HttpStatusCode.FailedDependency, "Two factor authentication is required. It is unsupported on yggdrasil.");

            if (!result.Succeeded || result.User == null)
                return JsonResult(HttpStatusCode.BadRequest, result.Message ?? "Invalid credentials.");

            YigProfileBody profile = new YigProfileBody
            {
                Id = result.User!.Id,
                Name = result.User.UserName
            };

            YigLoginResponse response = new YigLoginResponse
            {
                AccessToken = result.UserPlaySession!.Token!,
                ClientToken = request.ClientToken ?? string.Empty,
                SelectedProfile = profile,
                AvailableProfiles = [ profile ]
            };

            if (!request.RequestUser)
            {
                response.User = new YigUser
                {
                    Id = profile.Id,
                    Username = result.User.Email,
                    Properties = []
                };
            }

            return JsonResult(response);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Unexpected error occurred while processing the request.");
            return YigErrorResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Refreshes an access token for an active Yggdrasil session.
    /// </summary>
    /// <param name="request">The refresh request containing the access token and client token.</param>
    /// <returns>An <see cref="IActionResult"/> containing the refreshed token.</returns>
    /// <response code="400">The request body is invalid.</response>
    /// <response code="403">Legacy authentication is disabled on this server.</response>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([Required, FromBody] YigRefreshRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return YigErrorResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            if (!AppConfiguration.Yggdrasil.EnableLegacyAuth)
                return YigErrorResult(HttpStatusCode.Forbidden, "Legacy authentication is disabled on this server.");

            UserPlaySession? playSession = await UserStore.UserPlaySessions.FindAsync(x => x.Token == request.AccessToken && x.ClientId == request.ClientToken);
            if (playSession == null)
                return YigErrorResult(HttpStatusCode.Forbidden, "Not allowed.");

            UserPlaySession? newSession = await _signInManager.RefreshPlaySessionToken(playSession.Token);
            if (newSession == null)
                return YigErrorResult(HttpStatusCode.Forbidden, "Not allowed.");

            var user = await UserStore.FindUserAsync(x => x.Id == newSession.UserId);
            if (user == null)
                return YigErrorResult(HttpStatusCode.Forbidden, "Not allowed.");

            await UserStore.UserPlaySessions.RemoveAsync(playSession, true);

            YigProfileBody profile = new YigProfileBody
            {
                Id = user.Id,
                Name = user.UserName
            };

            YigLoginResponse response = new YigLoginResponse
            {
                AccessToken = newSession.Token,
                ClientToken = request.ClientToken ?? string.Empty,
                SelectedProfile = profile,
                AvailableProfiles = [ profile ]
            };

            if (!request.RequestUser)
            {
                response.User = new YigUser
                {
                    Id = profile.Id,
                    Username = user.Email,
                    Properties = []
                };
            }
            return JsonResult(response);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Unexpected error occurred while processing the request.");
            return YigErrorResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Validates whether an access token is still usable for a session.
    /// </summary>
    /// <param name="request">The validation request containing the access token.</param>
    /// <returns>An <see cref="IActionResult"/> returning 204 when the token is valid.</returns>
    /// <response code="400">The request body is invalid.</response>
    /// <response code="403">Legacy authentication is disabled on this server.</response>
    [HttpPost("validate")]
    public async Task<IActionResult> Validate([Required, FromBody] YigValidateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return YigErrorResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            if (!AppConfiguration.Yggdrasil.EnableLegacyAuth)
                return YigErrorResult(HttpStatusCode.Forbidden, "Legacy authentication is disabled on this server.");

            UserPlaySession? playSession = await UserStore.UserPlaySessions.FindAsync(x => x.Token == request.AccessToken && x.ClientId == request.ClientToken);
            if (playSession == null)
                return YigErrorResult(HttpStatusCode.Forbidden, "Not allowed.");

            return CodeResult(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Unexpected error occurred while processing the request.");
            return YigErrorResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Invalidates an access token so it can no longer be used.
    /// </summary>
    /// <param name="request">The invalidation request containing the access token.</param>
    /// <returns>An <see cref="IActionResult"/> returning 204 when the token was invalidated.</returns>
    /// <response code="400">The request body is invalid.</response>
    /// <response code="403">Legacy authentication is disabled on this server.</response>
    [HttpPost("invalidate")]
    public async Task<IActionResult> Invalidate([Required, FromBody] YigInvalidateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return YigErrorResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            if (!AppConfiguration.Yggdrasil.EnableLegacyAuth)
                return YigErrorResult(HttpStatusCode.Forbidden, "Legacy authentication is disabled on this server.");

            UserPlaySession? playSession = await UserStore.UserPlaySessions.FindAsync(x => x.Token == request.AccessToken && x.ClientId == request.ClientToken);
            if (playSession == null)
                return YigErrorResult(HttpStatusCode.Forbidden, "Not allowed.");

            await UserStore.UserPlaySessions.RemoveAsync(playSession, true);

            return CodeResult(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Unexpected error occurred while processing the request.");
            return YigErrorResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Signs the user out of all active sessions.
    /// </summary>
    /// <param name="request">The sign-out request containing the access token and username.</param>
    /// <returns>An <see cref="IActionResult"/> returning 204 when the sign-out completed.</returns>
    /// <response code="400">The request body is invalid.</response>
    /// <response code="403">Legacy authentication is disabled on this server.</response>
    [HttpPost("signout")]
    public async Task<IActionResult> Signout([Required, FromBody] YigSignoutRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return YigErrorResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            if (!AppConfiguration.Yggdrasil.EnableLegacyAuth)
                return YigErrorResult(HttpStatusCode.Forbidden, "Legacy authentication is disabled on this server.");

            LauncherSignInResult result = await _signInManager.LauncherSignInAsync(request.Username, request.Password, null, HttpContext);
            if (!result.Succeeded || result.User == null)
                return YigErrorResult(HttpStatusCode.Unauthorized, "Invalid credentials.");

            var logins = await UserStore.UserPlaySessions.QueryAsync(x => x.UserId == result.User.Id);
            await UserStore.UserPlaySessions.RemoveRangeAsync(logins, true);

            return CodeResult(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Unexpected error occurred while processing the request.");
            return YigErrorResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
}
