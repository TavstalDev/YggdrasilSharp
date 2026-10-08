using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Models.RateLimiting.Constants;
using Tavstal.YggdrasilSharp.Models.Responses.Auth;
using Tavstal.YggdrasilSharp.Serialization;
using Tavstal.YggdrasilSharp.Services.Database;

namespace Tavstal.YggdrasilSharp.Controllers.Auth;

/// <summary>
/// Controller for managing two-factor authentication (2FA) operations.
/// </summary>
[ApiController]
[Route("/2fa")]
[Tags("Authentication: 2FA")]
[Authorize(AuthenticationSchemes = "Bearer,Basic")]
[EnableRateLimiting(RateLimits.FixedWindow.AUTH_LOGIN)]
public class TwoFactorController : CustomControllerBase
{
    private readonly CustomSignInManager _signInManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TwoFactorController"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for logging.</param>
    /// <param name="userManager">Custom user manager for user operations.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="appConfiguration">Application settings.</param>
    /// <param name="signInManager">The sign-in manager for handling authentication flows.</param>
    public TwoFactorController(ILogger<TwoFactorController> logger, CustomUserManager userManager, CustomUserStore userStore, AppConfiguration appConfiguration,
        CustomSignInManager signInManager) : base(logger, userManager, userStore, appConfiguration)
    {
        _signInManager = signInManager;
    }

    /// <summary>
    /// Enables two-factor authentication for the authenticated user.
    /// </summary>
    /// <param name="twoFactorCode">The 2FA code provided by the user.</param>
    /// <response code="200">Two-factor authentication enabled successfully.</response>
    /// <response code="401">Unauthorized. User is not authenticated.</response>
    /// <response code="403">Forbidden. Two-factor authentication is already enabled.</response>
    /// <response code="500">Internal server error. An unknown error occurred while processing the request.</response>
    [HttpPatch("enable")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden),
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EnableTwoFactorAuthAsync([BindRequired, StringLength(6)] string twoFactorCode)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return JsonResult(HttpStatusCode.BadRequest, string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (user.TwoFactorEnabled)
                return JsonResult(HttpStatusCode.Forbidden, "Two-factor authentication is already enabled.");

            if (!UserManager.VerifyTwoFactorCode(user, twoFactorCode))
                return JsonResult(HttpStatusCode.Unauthorized, "Invalid two-factor code.");

            user.TwoFactorEnabled = true;
            await UserStore.UpdateUserAsync(user, true);

            return JsonResult(HttpStatusCode.OK, "Two-factor authentication enabled.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to enable 2FA.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Disables two-factor authentication for the authenticated user.
    /// </summary>
    /// <param name="twoFactorCode">The 2FA code provided by the user.</param>
    /// <response code="200">Two-factor authentication disabled successfully.</response>
    /// <response code="401">Unauthorized. User is not authenticated.</response>
    /// <response code="403">Forbidden. Two-factor authentication is not enabled.</response>
    /// <response code="500">Internal server error. An unknown error occurred while processing the request.</response>
    [HttpPatch("disable")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden),
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DisableTwoFactorAuthAsync([BindRequired, StringLength(6)] string twoFactorCode)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return JsonResult(HttpStatusCode.BadRequest, string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!user.TwoFactorEnabled)
                return JsonResult(HttpStatusCode.Forbidden, "Two-factor authentication is not enabled.");

            if (!UserManager.VerifyTwoFactorCode(user, twoFactorCode))
                return JsonResult(HttpStatusCode.Unauthorized, "Invalid two-factor code.");

            user.TwoFactorEnabled = false;
            await UserStore.UpdateUserAsync(user, true);

            return JsonResult(HttpStatusCode.OK, "Two-factor authentication disabled.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to disable 2FA.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Generates a new 2FA secret for the authenticated user.
    /// </summary>
    /// <response code="200">2FA secret generated successfully.</response>
    /// <response code="401">Unauthorized. User is not authenticated.</response>
    /// <response code="403">Forbidden. Two-factor authentication is already enabled.</response>
    /// <response code="500">Internal server error. An unknown error occurred while processing the request.</response>
    [HttpPatch("generate")]
    [JsonResponse(StatusCodes.Status200OK, typeof(TwoFactorSecretResponse)),
     TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden),
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateCodeAsync()
    {
        try
        {
            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (user.TwoFactorEnabled)
                return JsonResult(HttpStatusCode.Forbidden, "Two-factor authentication is already enabled.");

            string rawSecret = await UserManager.GenerateTwoFactorTokenAsync(user);

            return JsonResult(new TwoFactorSecretResponse
            {
                StatusCode = HttpStatusCode.OK,
                UserId = user.Id,
                Email = user.Email,
                Secret = rawSecret,
            }, CustomJsonContext.Default.TwoFactorSecretResponse);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate 2FA secret.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Regenerates recovery codes for the authenticated user.
    /// The user's current password must be supplied in the request body.
    /// </summary>
    /// <param name="password">The user's current password, used to confirm identity before regenerating codes.</param>
    /// <response code="200">Recovery codes regenerated successfully.</response>
    /// <response code="401">Unauthorized. User is not authenticated.</response>
    /// <response code="403">Failed to regenerate recovery codes.</response>
    /// <response code="500">Internal server error. An unknown error occurred while processing the request.</response>
    [HttpPatch("regenerate/recovery")]
    [JsonResponse(StatusCodes.Status200OK, typeof(TwoFactorCodeResponse)),
     TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden),
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegenerateRecoveryCodesAsync([Required, FromBody] string password)
    {
        try
        {
            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await _signInManager.VerifyPasswordAsync(user, password))
                return JsonResult(HttpStatusCode.Forbidden, "Failed to regenerate recovery codes.");

            var recoveryCodes = await UserStore.UserBackupCodes.QueryAsync(x => x.UserId == user.Id);
            foreach (var code in recoveryCodes)
                await UserStore.UserBackupCodes.RemoveAsync(code);
            var newCodes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 6);

            return JsonResult(new TwoFactorCodeResponse
            {
                StatusCode = HttpStatusCode.OK,
                UserId = user.Id,
                Email = user.Email,
                RecoveryCodes = newCodes
            }, CustomJsonContext.Default.TwoFactorCodeResponse);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to regenerate recovery codes.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
}
