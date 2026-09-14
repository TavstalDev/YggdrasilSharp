using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services.Database;

namespace Tavstal.YggdrasilSharp.Services.Authentication;

/// <summary>
/// Authentication handler that processes Bearer (JWT or token) authentication from the
/// HTTP Authorization header and resolves the corresponding <see cref="CustomUser"/>.
/// </summary>
public class BearerAuthenticationHandler : AuthenticationHandlerBase
{
    private readonly ILogger _logger;
    
    /// <summary>
    /// Constructs a new <see cref="BearerAuthenticationHandler"/>.
    /// </summary>
    /// <param name="options">The options monitor providing authentication scheme options.</param>
    /// <param name="logger">Logger factory used to create a logger instance for this handler.</param>
    /// <param name="encoder">URL encoder used by the base authentication handler.</param>
    /// <param name="userManager">Custom user manager used to resolve users and validate tokens.</param>
    /// <param name="userStore">The grouped repository holder that provides access to user-related repositories.</param>
    public BearerAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        CustomUserManager userManager,
        CustomUserStore userStore)
        : base(options, logger, encoder, userManager, userStore)
    {
        _logger = logger.CreateLogger<BearerAuthenticationHandler>();
    }
    
    /// <summary>
    /// Attempts to authenticate the current request using a Bearer token.
    /// </summary>
    /// <returns>
    /// An <see cref="AuthenticateResult"/> indicating success with a valid authentication ticket,
    /// failure for invalid tokens/processing errors, or <c>NoResult</c> when the handler does not apply.
    /// </returns>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
                return AuthenticateResult.NoResult();
            
            string headerValue = authHeader.ToString();
            var authenticationHeader = AuthenticationHeaderValue.Parse(headerValue);
            if (!authenticationHeader.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                return AuthenticateResult.NoResult();

            string? token = authenticationHeader.Parameter;
            if (string.IsNullOrEmpty(token) || string.IsNullOrWhiteSpace(token))
                return AuthenticateResult.Fail("Invalid authentication information provided in the request.");
            
            CustomUser? user = await _userManager.VerifyTokenLoginAsync(token);
            return await HandleUserAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("An error occurred while authenticating the user. Error: {ErrorMessage}", ex);
            return AuthenticateResult.Fail("An error occurred while authenticating the user");
        }
    }
}