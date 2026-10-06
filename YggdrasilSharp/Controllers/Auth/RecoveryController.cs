using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Bodies.Auth;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Utils.Helpers;
using RateLimits = Tavstal.YggdrasilSharp.Models.RateLimiting.Constants.RateLimits;

namespace Tavstal.YggdrasilSharp.Controllers.Auth;

/// <summary>
/// Controller for handling account recovery operations, including password and 2FA recovery.
/// </summary>
[ApiController]
[Route("/recovery")]
[Tags("Authentication: Recovery")]
public class RecoveryController : CustomControllerBase
{
    private readonly CustomDbContext _dbContext;
    private readonly IPasswordHasher<CustomUser> _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly MemoryCacheService _memoryCacheService;
    private readonly AppConfiguration _appConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecoveryController"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for logging.</param>
    /// <param name="userManager">Custom user manager for user operations.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="appConfiguration">Application settings.</param>
    /// <param name="dbContext">Database context for accessing user data.</param>
    /// <param name="passwordHasher">The password hasher for securely hashing user passwords during registration.</param>
    /// <param name="emailService">Service for sending emails.</param>
    /// <param name="memoryCacheService">Service for caching launcher data.</param>
    public RecoveryController(ILogger<RecoveryController> logger, CustomUserManager userManager, CustomUserStore userStore, AppConfiguration appConfiguration,
        CustomDbContext dbContext, IPasswordHasher<CustomUser> passwordHasher, IEmailService emailService, MemoryCacheService memoryCacheService)
        : base(logger, userManager, userStore, appConfiguration)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _memoryCacheService = memoryCacheService;
        _appConfiguration = appConfiguration;
    }


    /// <summary>
    /// Handles requests to send a recovery email to the user.
    /// </summary>
    /// <param name="email">The email address of the user requesting recovery.</param>
    /// <response code="201">Recovery email sent successfully.</response>
    /// <response code="401">Unauthorized. No account exists for the provided email address.</response>
    /// <response code="403">Forbidden. Email is not confirmed or recovery request is too frequent.</response>
    /// <response code="500">Internal server error. An unknown error occurred while processing the request.</response>
    [HttpPost("password/request")]
    [EnableRateLimiting(RateLimits.FixedWindow.AUTH_RESET)]
    [TextResponse(StatusCodes.Status201Created), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden),
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RequestRecoveryAsync([BindRequired, EmailAddress] string email)
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

            string normalizedEmail = email.Normalize();
            CustomUser? user = await UserStore.FindUserAsync(x => x.NormalizedEmail == normalizedEmail);
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "Invalid credentials.");

            if (!user.EmailConfirmed)
                return JsonResult(HttpStatusCode.Forbidden, "Email is not confirmed.");

            string fingerprint = UserManager.GetMachineFingerprint(HttpContext, user.Id);
            string recoveryTokenKey = $"recovery:{fingerprint}:password:token";
            string recoveryAttemptKey = $"recovery:{fingerprint}:password:attempt";

            if (_memoryCacheService.TryGetValue<string>(recoveryTokenKey, out _))
                return JsonResult(HttpStatusCode.Forbidden, "You must wait before requesting another recovery email.");

            string recoveryToken = TokenHelper.GenerateRecoverySessionToken();
            _memoryCacheService.SetValue(recoveryTokenKey, recoveryToken, TimeSpan.FromMinutes(15));
            _memoryCacheService.SetValue(recoveryAttemptKey, 0, TimeSpan.FromMinutes(15));


            var uriBuilder = new UriBuilder(new Uri(AppConfiguration.Misc.WebsiteUrl))
            {
                Path = "/reset-password",
                Query = $"recoveryToken={Uri.EscapeDataString(recoveryToken)}"
            };
            string recoveryLink = uriBuilder.ToString();
            await _emailService.SendEmailAsync(user.Email, user.UserName, "Account Recovery",
                $"Click the button below or copy and paste the following link into your browser to recover your account: {recoveryLink}" +
                "<br><br>The link is valid for 15 minutes." +
                "<br><br>If you did not request this recovery email, you can ignore it.",
                recoveryLink,
                "Recover Account");

            return JsonResult(HttpStatusCode.Created, "Recovery email sent successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, ex.Message);
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Handles password recovery requests.
    /// </summary>
    /// <param name="request">The recovery request body containing email, recovery token, and new password.</param>
    /// <response code="200">Password reset successful.</response>
    /// <response code="401">Unauthorized. No account exists for the provided email address, or the recovery token is
    /// missing/expired. All unauthenticated failure modes return this same response so the endpoint cannot be used to
    /// enumerate accounts.</response>
    /// <response code="403">Forbidden. Email is not confirmed or too many recovery attempts.</response>
    /// <response code="500">Internal server error. An unknown error occurred while processing the request.</response>
    [HttpPost("password")]
    [EnableRateLimiting(RateLimits.FixedWindow.AUTH_RESET)]
    [Consumes("application/json")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden),
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RecoverPasswordAsync([Required, FromBody] RecoverPasswordRequestBody request)
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

            string normalizedEmail = request.Email.Normalize();
            CustomUser? user = await UserStore.FindUserAsync(x => x.NormalizedEmail == normalizedEmail);
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "Invalid credentials.");

            if (!user.EmailConfirmed)
                return JsonResult(HttpStatusCode.Forbidden, "Email is not confirmed.");

            string fingerprint = UserManager.GetMachineFingerprint(HttpContext, user.Id);
            string recoveryTokenKey = $"recovery:{fingerprint}:password:token";
            string recoveryAttemptKey = $"recovery:{fingerprint}:password:attempt";

            if (!_memoryCacheService.TryGetValue(recoveryAttemptKey, out int attempts))
                return JsonResult(HttpStatusCode.Unauthorized, "Invalid credentials.");

            if (attempts > 3)
                return JsonResult(HttpStatusCode.Forbidden, "Too many recovery attempts. Please try again later.");

            if (!_memoryCacheService.TryGetValue(recoveryTokenKey, out string? cachedToken) ||
                string.IsNullOrEmpty(cachedToken) || cachedToken != request.RecoveryToken)
            {
                _memoryCacheService.SetValue(recoveryAttemptKey, attempts + 1, TimeSpan.FromMinutes(15));
                return JsonResult(HttpStatusCode.Unauthorized, "Invalid credentials.");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
            user.SecurityStamp = Guid.NewGuid().ToString();
            await UserStore.UpdateUserAsync(user, true);

            _memoryCacheService.RemoveValue(recoveryTokenKey);
            _memoryCacheService.RemoveValue(recoveryAttemptKey);

            if (request.LogoutEverywhere)
                await _dbContext.ClearUserLoginsAsync(user.Id);

            await _dbContext.SaveChangesAsync();
            return JsonResult(HttpStatusCode.OK, "Password reset successful.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, ex.Message);
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Starts a two-factor-auth (2FA) recovery flow by sending a recovery email to the user.
    /// </summary>
    /// <param name="email">The email address of the user requesting 2FA recovery. This value is required.</param>
    /// <response code="201">Recovery email sent successfully.</response>
    /// <response code="401">Unauthorized. No account exists for the provided email address. Deliberately
    /// indistinguishable from a wrong recovery token so the endpoint cannot be used to enumerate accounts.</response>
    /// <response code="403">Forbidden. Email is not confirmed or recovery request is too frequent.</response>
    /// <response code="500">Internal server error. An unknown error occurred while processing the request.</response>
    [HttpPost("2fa/request")]
    [EnableRateLimiting(RateLimits.FixedWindow.AUTH_RESET)]
    [TextResponse(StatusCodes.Status201Created), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden),
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RequestTFARecoveryAsync([BindRequired, EmailAddress] string email)
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

            string normalizedEmail = email.Normalize();
            CustomUser? user = await UserStore.FindUserAsync(x => x.NormalizedEmail == normalizedEmail);
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "Invalid credentials.");

            if (!user.EmailConfirmed)
                return JsonResult(HttpStatusCode.Forbidden, "Email is not confirmed.");

            string fingerprint = UserManager.GetMachineFingerprint(HttpContext, user.Id);
            string recoveryTokenKey = $"recovery:{fingerprint}:tfa:token";
            string recoveryAttemptKey = $"recovery:{fingerprint}:tfa:attempt";

            if (_memoryCacheService.TryGetValue<string>(recoveryTokenKey, out _))
                return JsonResult(HttpStatusCode.Forbidden, "You must wait before requesting another recovery email.");

            string recoveryToken = TokenHelper.GenerateRecoverySessionToken();
            _memoryCacheService.SetValue(recoveryTokenKey, recoveryToken, TimeSpan.FromMinutes(15));
            _memoryCacheService.SetValue(recoveryAttemptKey, 0, TimeSpan.FromMinutes(15));

            var uriBuilder = new UriBuilder(new Uri(AppConfiguration.Misc.WebsiteUrl))
            {
                Path = "/reset-tfa",
                Query = $"recoveryToken={Uri.EscapeDataString(recoveryToken)}"
            };
            string recoveryLink = uriBuilder.ToString();
            await _emailService.SendEmailAsync(user.Email, user.UserName, "Account Recovery",
                $"Click the button below or copy and paste the following link into your browser to recover your account: {recoveryLink}" +
                "<br><br>The link is valid for 15 minutes." +
                "<br><br>If you did not request this recovery email, you can ignore it.",
                recoveryLink,
                "Recover Account");

            return JsonResult(HttpStatusCode.Created, "Recovery email sent successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, ex.Message);
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Handles requests to recover two-factor authentication (2FA) settings.
    /// </summary>
    /// <param name="request">The recovery request body containing email and backup code.</param>
    /// <response code="200">2FA reset successful.</response>
    /// <response code="401">Unauthorized. No account exists for the provided email address, or the recovery token is
    /// missing/expired. All unauthenticated failure modes return this same response so the endpoint cannot be used to
    /// enumerate accounts.</response>
    /// <response code="403">Forbidden. Email is not confirmed or too many recovery attempts.</response>
    /// <response code="500">Internal server error. An unknown error occurred while processing the request.</response>
    [HttpPost("2fa")]
    [EnableRateLimiting(RateLimits.FixedWindow.AUTH_RESET)]
    [Consumes("application/json")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden),
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RecoverTwoFactorAsync([Required, FromBody] RecoverTwoFactorRequestBody request)
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

            string normalizedEmail = request.Email.Normalize();
            CustomUser? user = await UserStore.FindUserAsync(x => x.NormalizedEmail == normalizedEmail);
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "Invalid credentials.");

            string fingerprint = UserManager.GetMachineFingerprint(HttpContext, user.Id);
            string recoveryTokenKey = $"recovery:{fingerprint}:tfa:token";
            string recoveryAttemptKey = $"recovery:{fingerprint}:tfa:attempt";

            if (!_memoryCacheService.TryGetValue(recoveryAttemptKey, out int attempts))
                return JsonResult(HttpStatusCode.Unauthorized, "Invalid credentials.");

            if (attempts > 3)
                return JsonResult(HttpStatusCode.Forbidden, "Too many recovery attempts. Please try again later.");

            if (!_memoryCacheService.TryGetValue(recoveryTokenKey, out string? cachedToken) ||
                string.IsNullOrEmpty(cachedToken) || cachedToken != request.RecoveryToken)
            {
                _memoryCacheService.SetValue(recoveryAttemptKey, attempts + 1);
                return JsonResult(HttpStatusCode.Unauthorized, "Invalid credentials.");
            }

            if (!user.TwoFactorEnabled)
                return JsonResult(HttpStatusCode.BadRequest, "Two-factor authentication is not enabled.");

            string hashedCode = StringChiper.GetEncryptedHash(request.BackupCode, _appConfiguration.Jwt.TwoFactorEncryptionKey);
            var backupCode = await UserStore.UserBackupCodes.FindAsync(x => x.UserId == user.Id && x.HashedCode == hashedCode);
            if (backupCode == null)
                return JsonResult(HttpStatusCode.BadRequest, "Backup code is invalid.");

            if (backupCode.UsedAt != null)
                return JsonResult(HttpStatusCode.BadRequest, "Backup code has already been used.");

            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            user.SecurityStamp = Guid.NewGuid().ToString();
            await UserStore.UpdateUserAsync(user, true);

            backupCode.UsedAt = DateTime.UtcNow;
            await UserStore.UserBackupCodes.UpdateAsync(backupCode, true);

            await _dbContext.ClearUserLoginsAsync(user.Id, true);

            _memoryCacheService.RemoveValue(recoveryTokenKey);
            _memoryCacheService.RemoveValue(recoveryAttemptKey);

            return JsonResult(HttpStatusCode.OK, "2FA reset successful.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, ex.Message);
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
}
