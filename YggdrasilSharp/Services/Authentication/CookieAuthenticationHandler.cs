using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services.Database;

namespace Tavstal.YggdrasilSharp.Services.Authentication;

/// <summary>
/// Authentication handler that processes session-based authentication using an HTTP cookie named "auth-token".
/// </summary>
public class CookieAuthenticationHandler : AuthenticationHandlerBase
{
    private readonly ILogger _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CookieAuthenticationHandler"/> class.
    /// </summary>
    /// <param name="options">Options monitor for authentication scheme configuration (provided by the framework).</param>
    /// <param name="logger">Logger factory used to create a logger for this handler.</param>
    /// <param name="encoder">URL encoder used by the base authentication handler.</param>
    /// <param name="userManager">Custom user manager used to resolve users and validate session tokens.</param>
    /// <param name="userStore">The grouped repository holder that provides access to user-related repositories.</param>
    public CookieAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        CustomUserManager userManager,
        CustomUserStore userStore)
        : base(options, logger, encoder, userManager, userStore)
    {
        _logger = logger.CreateLogger<CookieAuthenticationHandler>();
    }
    
    /// <summary>
    /// Attempts to authenticate the current request using the session cookie named "auth-token".
    /// </summary>
    /// <returns>
    /// An <see cref="AuthenticateResult"/> representing success, no-result (not applicable), or failure.
    /// </returns>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            if (!Request.Cookies.TryGetValue("auth-token", out var authCookie))
                return AuthenticateResult.NoResult();
            
            if (string.IsNullOrEmpty(authCookie) || string.IsNullOrWhiteSpace(authCookie))
                return AuthenticateResult.Fail("Invalid authentication cookie.");
            
            CustomUser? user = await _userManager.VerifyTokenLoginAsync(authCookie);
            return await HandleUserAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("An error occurred while authenticating the user. Error: {ErrorMessage}", ex);
            return AuthenticateResult.Fail("An error occurred while authenticating the user");
        }
    }
}