using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Bodies.Auth;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Models.Responses.Auth;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.Database;
using RateLimits = Tavstal.YggdrasilSharp.Models.RateLimiting.Constants.RateLimits;
using SignInResult = Tavstal.YggdrasilSharp.Models.Database.SignInResult;

namespace Tavstal.YggdrasilSharp.Controllers.Auth;

/// <summary>
/// Controller responsible for handling login-related authentication endpoints.
/// </summary>
[ApiController]
[Tags("Authentication: Login")]
public class LoginController : CustomControllerBase
{
    private readonly CustomSignInManager _signInManager;
    private readonly MemoryCacheService _memoryCacheService;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging operations.</param>
    /// <param name="signInManager">The sign-in manager for handling authentication flows.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="memoryCacheService">Service for caching launcher data.</param>
    /// <param name="appConfiguration">The application settings.</param>
    public LoginController(ILogger<LoginController> logger, CustomSignInManager signInManager, CustomUserStore userStore, MemoryCacheService memoryCacheService, AppConfiguration appConfiguration) : base(logger, userStore, appConfiguration)
    {
        _signInManager = signInManager;
        _memoryCacheService = memoryCacheService;
    }
    
    /// <summary>
    /// Handles user login requests.
    /// </summary>
    /// <param name="request">The login request body containing user credentials.</param>
    /// <response code="200">Request successful. Returns authentication result and tokens or session info when applicable.</response>
    /// <response code="302">Redirect required (e.g. to 2FA page). Response body includes redirect URL and related info.</response>
    /// <response code="400">Bad request. Missing or invalid input (e.g. missing token or malformed body).</response>
    /// <response code="401">Unauthorized. Authentication failed (invalid credentials, invalid session token or 2FA code).</response>
    /// <response code="403">Forbidden. Access denied (e.g. expired session token, too many attempts, or account restrictions).</response>
    /// <response code="404">Not found. Requested resource (user, session, token) does not exist.</response>
    /// <response code="423">Locked. Account is locked; includes lockout reason and expiration when applicable.</response>
    /// <response code="500">Internal server error. Unexpected error occurred while processing the request.</response>
    [HttpPost("/login")]
    [EnableRateLimiting(RateLimits.FixedWindow.AUTH_LOGIN)]
    [Consumes("application/json")]
    [JsonResponse(StatusCodes.Status200OK, typeof(LoginResponse)), JsonResponse(StatusCodes.Status302Found, typeof(LoginRedirectResponse)), 
     TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), 
     TextResponse(StatusCodes.Status404NotFound), TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LoginAsync([Required, FromBody] LoginRequestBody request)
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

            SignInResult result;
            if (request.Email.Contains('@'))
                result = await _signInManager.EmailSignInAsync(request.Email, request.Password, request.RememberMe, HttpContext);
            else
                result = await _signInManager.UsernameSignInAsync(request.Email, request.Password, request.RememberMe, HttpContext);

            if (!result.Succeeded && !result.RequiresTwoFactor)
                return JsonResult(HttpStatusCode.BadRequest, result.Message ?? "Invalid credentials.");

            if (result.RequiresTwoFactor)
            {
                var user = result.User!;
                string sessionToken = result.SessionToken!;
                var expiry = result.TokenExpiresAt;
                Response.Cookies.Append("ysharp-userId", user.Id, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // only over HTTPS
                    SameSite = SameSiteMode.None, // required for cross-origin
                    Expires = expiry
                });
                Response.Cookies.Append("ysharp-twofactor-session", sessionToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // only over HTTPS
                    SameSite = SameSiteMode.None, // required for cross-origin
                    Expires = expiry
                });
                
                var uriBuilder = new UriBuilder(new Uri(AppConfiguration.Misc.WebsiteUrl))
                {
                    Path = "/2fa",
                    Query = $"rememberMe={Uri.EscapeDataString(request.RememberMe.ToString())}"
                };

                return JsonResult(new LoginRedirectResponse
                {
                    StatusCode = HttpStatusCode.Redirect,
                    Message = "Redirect to 2FA page",
                    Email = user.Email,
                    Url = uriBuilder.ToString()
                });
            }
            
            if (!result.Succeeded)
                return JsonResult(HttpStatusCode.BadRequest, result.Message ?? "Invalid credentials.");

            var userToken = result.UserToken!;
            var userLogin = result.UserLogin!;

            return JsonResult(new LoginResponse
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Login successful.",
                UserId = userToken.UserId,
                Expires = userLogin.ExpireDate.ToString(CultureInfo.InvariantCulture)
            });
        }
        catch (Exception ex)
        {
            Logger.LogCritical("Error during login: {Message}", ex.Message);
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    
    /// <summary>
    /// Handles two-factor authentication (2FA) login requests.
    /// </summary>
    /// <param name="request">The 2FA session request body containing the session token and 2FA code.</param>
    /// <response code="200">Request successful. Returns authentication result and tokens.</response>
    /// <response code="401">Unauthorized. Invalid or missing session cookie, session secret, or 2FA code.</response>
    /// <response code="403">Forbidden. Session token expired or too many failed attempts.</response>
    /// <response code="404">Not found. User associated with the session token does not exist.</response>
    /// <response code="423">Locked. User account is locked; includes lockout reason and expiration.</response>
    /// <response code="500">Internal server error. Unexpected error occurred while processing the request.</response>
    [HttpPatch("/login/2fa")]
    [EnableRateLimiting(RateLimits.FixedWindow.AUTH_LOGIN)]
    [Consumes("application/json")]
    [JsonResponse(StatusCodes.Status200OK, typeof(LoginResponse)), TextResponse(StatusCodes.Status401Unauthorized),
     TextResponse(StatusCodes.Status403Forbidden), TextResponse(StatusCodes.Status404NotFound),
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LoginTwoFactorAsync([Required, FromBody] LoginTFASessionRequestBody request)
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
            
            if (!Request.Cookies.TryGetValue("ysharp-twofactor-session", out var sessionCookie)  || string.IsNullOrEmpty(sessionCookie))
                return JsonResult(HttpStatusCode.Unauthorized, "Invalid or missing session cookie.");
            
            if (!Request.Cookies.TryGetValue("ysharp-userId", out var userIdCookie) || string.IsNullOrEmpty(userIdCookie))
                return JsonResult(HttpStatusCode.Unauthorized, "Invalid or missing userId cookie.");
            
            string fingerprint = GetMachineFingerprint(userIdCookie);
            string tokenKey = $"auth:{fingerprint}:tfa:token";
            if (!_memoryCacheService.TryGetValue(tokenKey, out string? cachedSessionToken) || string.IsNullOrEmpty(cachedSessionToken) || cachedSessionToken != sessionCookie)
                return JsonResult(HttpStatusCode.Unauthorized, "Invalid or expired session token.");

            CustomUser? user = await UserStore.FindUserByIdAsync(userIdCookie);
            if (user == null)
                return JsonResult(HttpStatusCode.NotFound, "User not found.");

            var result = await _signInManager.TwoFactorSignInAsync(user, request.TwoFactorCode, request.RememberMe, HttpContext);
            if (!result.Succeeded)
                return JsonResult(HttpStatusCode.BadRequest, result.Message ?? "Invalid two-factor code.");
            
            var userToken = result.UserToken!;
            var userLogin = result.UserLogin!;
            
            return JsonResult(new LoginResponse
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Login successful.",
                UserId = userToken.UserId,
                Token = userToken.Value!,
                Expires = userLogin.ExpireDate.ToString(CultureInfo.InvariantCulture)
            });
        }
        catch (Exception ex)
        {
            Logger.LogCritical("Error during login: {Message}", ex.Message);
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    
    /// <summary>
    /// Handles launcher login requests.
    /// </summary>
    /// <param name="request">The launcher login request body containing username, password, and optional 2FA code.</param>
    /// <response code="200">Request successful. Returns user session token and expiration details.</response>
    /// <response code="302">Redirect required for two-factor authentication. Includes session token and redirect URL.</response>
    /// <response code="401">Unauthorized. Invalid credentials or two-factor authentication code.</response>
    /// <response code="403">Forbidden. Account is locked or too many failed attempts.</response>
    /// <response code="404">Not found. User does not exist.</response>
    /// <response code="500">Internal server error. Unexpected error occurred while processing the request.</response>
    [HttpPost("/login/launcher")]
    [EnableRateLimiting(RateLimits.FixedWindow.AUTH_LOGIN)]
    [Consumes("application/json")]
    [JsonResponse(StatusCodes.Status200OK, typeof(LoginResponse)), JsonResponse(StatusCodes.Status302Found, typeof(LoginLauncherRedirectResponse)), 
     TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), 
     TextResponse(StatusCodes.Status404NotFound), TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LoginLauncherAsync([Required, FromBody] LauncherLoginRequestBody request)
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
            
            LauncherSignInResult result = await _signInManager.LauncherSignInAsync(request.Username, request.Password, HttpContext);

            if (!result.Succeeded && !result.RequiresTwoFactor)
                return JsonResult(HttpStatusCode.BadRequest, result.Message ?? "Invalid credentials.");

            if (result.RequiresTwoFactor)
            {
                var user = result.User!;
                string sessionToken = result.SessionToken!;
                return JsonResult(new LoginLauncherRedirectResponse
                {
                    StatusCode = HttpStatusCode.Redirect,
                    Message = "Redirect to 2FA",
                    UserId = user.Id,
                    Token = sessionToken,
                    Url = $"{AppConfiguration.Misc.ApiUrl}/login/launcher/2fa"
                });
            }
            
            if (!result.Succeeded)
                return JsonResult(HttpStatusCode.BadRequest, result.Message ?? "Invalid credentials.");
            
            var userPlaySession = result.UserPlaySession!;
            return JsonResult(new LoginResponse
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Login successful",
                UserId = userPlaySession.UserId,
                Token = userPlaySession.Token,
                Expires = userPlaySession.ExpiresAt.ToString(CultureInfo.InvariantCulture)
            });
        }
        catch (Exception ex)
        {
            Logger.LogCritical("Error during login: {Message}", ex.Message);
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Handles two-factor authentication (2FA) login requests for the launcher.
    /// </summary>
    /// <param name="request">The 2FA session request body containing the session token and 2FA code.</param>
    /// <response code="200">Request successful. Returns user session token and expiration details.</response>
    /// <response code="401">Unauthorized. Invalid session token, session secret, or 2FA code.</response>
    /// <response code="403">Forbidden. Session token expired or too many failed attempts.</response>
    /// <response code="404">Not found. User associated with the session token does not exist.</response>
    /// <response code="423">Locked. User account is locked; includes lockout reason and expiration.</response>
    /// <response code="500">Internal server error. Unexpected error occurred while processing the request.</response>
    [HttpPatch("/login/launcher/2fa")]
    [EnableRateLimiting(RateLimits.FixedWindow.AUTH_LOGIN)]
    [Consumes("application/json")]
    [JsonResponse(StatusCodes.Status200OK, typeof(LoginResponse)), TextResponse(StatusCodes.Status401Unauthorized),
     TextResponse(StatusCodes.Status403Forbidden), TextResponse(StatusCodes.Status404NotFound),
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LoginLauncherTwoFactorAsync([Required, FromBody] LauncherLoginTFASessionRequestBody request)
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
            
            string fingerprint = GetMachineFingerprint(request.UserId);
            string tokenKey = $"auth:{fingerprint}:tfa-launcher:token";
            if (!_memoryCacheService.TryGetValue(tokenKey, out string? cachedSessionToken) || string.IsNullOrEmpty(cachedSessionToken) || cachedSessionToken != request.SessionToken)
                return JsonResult(HttpStatusCode.Unauthorized, "Invalid or expired session token.");

            CustomUser? user = await UserStore.FindUserByIdAsync(request.UserId);
            if (user == null)
                return JsonResult(HttpStatusCode.NotFound, "User not found.");

            var result = await _signInManager.LauncherTwoFactorSignInAsync(user, request.TwoFactorCode, HttpContext);
            if (!result.Succeeded)
                return JsonResult(HttpStatusCode.BadRequest, result.Message ?? "Invalid two-factor code.");

            var userPlaySession = result.UserPlaySession!;
            return JsonResult(new LoginResponse
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Login successful",
                UserId = userPlaySession.UserId,
                Token = userPlaySession.Token,
                Expires = userPlaySession.ExpiresAt.ToString(CultureInfo.InvariantCulture)
            });
        }
        catch (Exception ex)
        {
            Logger.LogCritical("Error during login: {Message}", ex.Message);
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }


    /// <summary>
    /// Logs out the user by invalidating their session token.
    /// </summary>
    /// <param name="token">
    /// The session token to be invalidated. If not provided, the method attempts to retrieve it 
    /// from the "Authorization" header.
    /// </param>
    /// <response code="200">Logout successful. The user session is terminated.</response>
    /// <response code="400">Bad request. The provided token is invalid or missing.</response>
    /// <response code="500">Internal server error. An unexpected error occurred during logout.</response>
    [HttpPost("/logout")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status400BadRequest),
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LogoutAsync([MinLength(48), MaxLength(48)] string? token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                if (Request.Headers.TryGetValue("Authorization", out var authHeader))
                {
                    string headerValue = authHeader.ToString();
                    var authenticationHeader = AuthenticationHeaderValue.Parse(headerValue);
                    if (authenticationHeader.Scheme == "Bearer")
                        token = authenticationHeader.Parameter;
                }
            }
            
            if (string.IsNullOrEmpty(token))
                return JsonResult(HttpStatusCode.BadRequest, "Invalid token.");
            
            if (!await _signInManager.SignOutAsync(token))
                return JsonResult(HttpStatusCode.BadRequest, "Invalid token.");
            
            return SignOut();
        }
        catch (Exception ex)
        {
            Logger.LogCritical("Error during logout: {Message}", ex.Message);
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
}