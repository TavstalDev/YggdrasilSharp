using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;
using Tavstal.YggdrasilSharp.Services.Database;

namespace Tavstal.YggdrasilSharp.Controllers.Yggdrasil;

/// <summary>
/// Controller for handling legacy Yggdrasil authentication API requests.
/// Every endpoint is gated behind the <c>Yggdrasil:EnableLegacyAuth</c> configuration key.
/// </summary>
[ApiController]
[Route("yggdrasil")]
[Route("yggdrasil/authserver")]
[Tags("Yggdrasil")]
public class AuthController  : CustomControllerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging information.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="appConfiguration">Application settings.</param>
    public AuthController(ILogger<ProfilesController> logger, CustomUserStore userStore, AppConfiguration appConfiguration) : base(logger, userStore, appConfiguration) {}
    
    /// <summary>
    /// Authenticates a user and issues a Yggdrasil access token.
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
            
            // TODO
            
            return JsonResult("{}");
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
            
            // TODO
            
            return JsonResult("{}");
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
            
            // TODO
            
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
            
            // TODO
            
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
    /// <param name="request">The sign out request containing the access token and username.</param>
    /// <returns>An <see cref="IActionResult"/> returning 204 when the sign out completed.</returns>
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

            // TODO
            
            return CodeResult(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Unexpected error occurred while processing the request.");
            return YigErrorResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
}