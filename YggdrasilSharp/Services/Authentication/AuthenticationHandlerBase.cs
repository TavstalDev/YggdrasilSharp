using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services.Database;

namespace Tavstal.YggdrasilSharp.Services.Authentication;

/// <summary>
/// Abstract base class for all authentication handlers (Bearer, Basic, Cookie).
/// Provides common functionality for user validation, lockout management, and claims creation.
/// </summary>
public abstract class AuthenticationHandlerBase : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// The custom user manager used to query and update user data from the database.
    /// </summary>
    protected readonly CustomUserManager _userManager;

    /// <summary>
    /// A grouped set of repositories for user-related entities (users, roles, claims, tokens, etc.).
    /// </summary>
    protected readonly CustomUserStore _userStore;
    
    /// <summary>
    /// Initializes a new instance of the AuthenticationHandlerBase class.
    /// </summary>
    /// <param name="options">The options monitor for authentication scheme configuration.</param>
    /// <param name="logger">The logger factory for creating loggers.</param>
    /// <param name="encoder">The URL encoder for encoding values.</param>
    /// <param name="userManager">The custom user manager for database access.</param>
    /// <param name="userStore">The grouped repository holder that provides access to user-related repositories.</param>
    protected AuthenticationHandlerBase(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        CustomUserManager userManager,
        CustomUserStore userStore)
        : base(options, logger, encoder)
    {
        _userManager = userManager;
        _userStore = userStore;
    }

    /// <summary>
    /// Verifies user validity, manages lockout status, and creates an authentication ticket.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method performs the following checks in order:
    /// <br/>1. Validates user exists (returns fail if null).
    /// <br/>2. Checks if user is locked out and lockout period is still active (returns fail if so).
    /// <br/>3. Clears expired lockout status if lockout period has elapsed.
    /// <br/>4. Creates standard claims (Name, NameIdentifier, Email).
    /// <br/>5. Generates and returns a successful authentication ticket.
    /// </para>
    /// <para>
    /// Lockout expiration is automatically cleared by updating the user in the database, allowing
    /// the user to authenticate again on their next attempt after the lockout period expires.
    /// </para>
    /// </remarks>
    /// <param name="user">The user object to process. If null, authentication fails.</param>
    /// <returns>
    /// An AuthenticateResult indicating success with a valid ticket and claims, or failure
    /// with an appropriate error message.
    /// </returns>
    protected async Task<AuthenticateResult> HandleUserAsync(CustomUser? user)
    {
        if (user == null)
            return AuthenticateResult.Fail("Failed to authenticate user with provided authentication information.");

        if (user.LockoutEnabled)
        {
            if (user.LockoutEnd > DateTimeOffset.UtcNow)
                return AuthenticateResult.Fail(user.LockoutReason ?? "User is locked out.");

            user.LockoutEnabled = false;
            await _userStore.UpdateUserAsync(user, true);
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
