using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services.Database;

namespace Tavstal.YggdrasilSharp.Services.Authentication;

/// <summary>
/// Authentication handler that processes HTTP Basic authentication.
/// </summary>
public class BasicAuthenticationHandler : AuthenticationHandlerBase
{
    private readonly ILogger _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BasicAuthenticationHandler"/> class.
    /// </summary>
    /// <param name="options">Authentication scheme options monitor (provided by the framework).</param>
    /// <param name="logger">Logger factory used to create a logger for this handler.</param>
    /// <param name="encoder">URL encoder passed to the base authentication handler.</param>
    /// <param name="userManager">Custom user manager used to validate username/password pairs and access the database.</param>
    /// <param name="userStore">The grouped repository holder that provides access to user-related repositories.</param>
    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        CustomUserManager userManager,
        CustomUserStore userStore)
        : base(options, logger, encoder, userManager, userStore)
    {
        _logger = logger.CreateLogger<BasicAuthenticationHandler>();
    }
    
    /// <summary>
    /// Attempts to authenticate the current request using HTTP Basic authentication.
    /// </summary>
    /// <returns>
    /// An <see cref="AuthenticateResult"/> indicating success with a valid authentication ticket,
    /// failure for processing errors, or <c>NoResult</c> when the handler does not apply.
    /// </returns>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
                return AuthenticateResult.NoResult();
            
            string headerValue = authHeader.ToString();
            var authenticationHeader = AuthenticationHeaderValue.Parse(headerValue);
            if (!authenticationHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
                return AuthenticateResult.NoResult();
            
            string? base64Str = authenticationHeader.Parameter;
            if (string.IsNullOrEmpty(base64Str) || string.IsNullOrWhiteSpace(base64Str))
                return AuthenticateResult.Fail("Invalid authentication header.");

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64Str);
            }
            catch (Exception)
            {
                return AuthenticateResult.Fail("The header parameter is not encoded correctly.");
            }
            
            string value = Encoding.UTF8.GetString(bytes);
            if (!value.Contains(':'))
                return AuthenticateResult.Fail("Invalid authentication header format. Expected 'username:password'.");
            
            string[] parts = value.Split(':', 2);
            CustomUser? user = await _userManager.VerifyPasswordAsync(parts[0], parts[1]);
            
            return await HandleUserAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("An error occurred while authenticating the user. Error: {ErrorMessage}", ex);
            return AuthenticateResult.Fail("An error occurred while authenticating the user");
        }
    }
}