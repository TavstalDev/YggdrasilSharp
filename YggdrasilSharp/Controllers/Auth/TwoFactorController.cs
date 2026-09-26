using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services.Database;

namespace Tavstal.YggdrasilSharp.Controllers.Auth;

/// <summary>
/// Controller for managing two-factor authentication (2FA) operations.
/// </summary>
[ApiController]
[Route("/2fa")]
[Tags("Authentication: 2FA")]
public class TwoFactorController : CustomControllerBase {
    
    private readonly CustomUserManager _userManager;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TwoFactorController"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for logging.</param>
    /// <param name="userManager">Custom user manager for user operations.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="appConfiguration">Application settings.</param>
    public TwoFactorController(ILogger<TwoFactorController> logger, CustomUserManager userManager, CustomUserStore userStore, AppConfiguration appConfiguration) : base(logger, userStore, appConfiguration)
    {
        _userManager = userManager;
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

                return CodeResult(HttpStatusCode.BadRequest, string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }
            
            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return CodeResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (user.TwoFactorEnabled)
                return CodeResult(HttpStatusCode.Forbidden, "Two-factor authentication is already enabled.");
            
            if (!_userManager.VerifyTwoFactorCode(user, twoFactorCode))
                return CodeResult(HttpStatusCode.Unauthorized, "Invalid two-factor code.");
            
            user.TwoFactorEnabled = true;
            await UserStore.UpdateUserAsync(user, true);
            
            return CodeResult(HttpStatusCode.OK, "Two-factor authentication enabled.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to enable 2FA.");
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
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

                return CodeResult(HttpStatusCode.BadRequest, string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }
            
            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return CodeResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!user.TwoFactorEnabled)
                return CodeResult(HttpStatusCode.Forbidden, "Two-factor authentication is not enabled.");
            
            if (!_userManager.VerifyTwoFactorCode(user, twoFactorCode))
                return CodeResult(HttpStatusCode.Unauthorized, "Invalid two-factor code.");
            
            user.TwoFactorEnabled = false;
            await UserStore.UpdateUserAsync(user, true);
            
            return CodeResult(HttpStatusCode.OK, "Two-factor authentication disabled.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to disable 2FA.");
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
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
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), 
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateCodeAsync()
    {
        try
        {
            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return CodeResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (user.TwoFactorEnabled)
                return CodeResult(HttpStatusCode.Forbidden, "Two-factor authentication is already enabled.");

            string rawSecret = await _userManager.GenerateTwoFactorTokenAsync(user);
            
            return JsonResult(new
            {
                statusCode = HttpStatusCode.OK,
                userId = user.Id,
                email = user.Email,
                secret = rawSecret,
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate 2FA secret.");
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    
    /// <summary>
    /// Regenerates recovery codes for the authenticated user.
    /// </summary>
    /// <response code="200">Recovery codes regenerated successfully.</response>
    /// <response code="401">Unauthorized. User is not authenticated.</response>
    /// <response code="403">Forbidden. Two-factor authentication is not enabled.</response>
    /// <response code="500">Internal server error. An unknown error occurred while processing the request.</response>
    [HttpPatch("regenerate/recovery")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), 
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegenerateRecoveryCodesAsync()
    {
        try
        {
            CustomUser? user = await GetCurrentUserAsync();
            if (user == null)
                return CodeResult(HttpStatusCode.Unauthorized, "User not authenticated");

            var recoveryCodes = await UserStore.UserBackupCodes.QueryAsync(x => x.UserId == user.Id);
            foreach (var code in recoveryCodes) 
                await UserStore.UserBackupCodes.RemoveAsync(code);
            var newCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 6);
            
            return JsonResult(new
            {
                statusCode = HttpStatusCode.OK,
                userId = user.Id,
                email = user.Email,
                recoveryCodes = newCodes
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to regenerate recovery codes.");
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
}