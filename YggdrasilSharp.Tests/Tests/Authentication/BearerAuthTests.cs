using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services.Authentication;
using Tavstal.YggdrasilSharp.Tests.Helpers;
using Tavstal.YggdrasilSharp.Tests.Models;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Authentication;

/// <summary>
/// Unit tests for <see cref="BearerAuthenticationHandler"/> covering successful authentication
/// and every malformed or unverifiable bearer token it must reject.
/// </summary>
public class BearerAuthTests : TestBase
{
    private readonly AuthenticationScheme _scheme;
    private readonly BearerAuthenticationHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="BearerAuthTests"/> class with a mocked options monitor,
    /// logger factory and URL encoder.
    /// </summary>
    /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
    public BearerAuthTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        var options = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        options
            .Setup(x => x.Get(It.IsAny<string>()))
            .Returns(new AuthenticationSchemeOptions());

        var logger = new Mock<ILogger<BearerAuthenticationHandler>>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<String>())).Returns(logger.Object);

        var encoder = new Mock<UrlEncoder>();
        _scheme = new AuthenticationScheme("Bearer", null, typeof(BearerAuthenticationHandler));
        _handler = new BearerAuthenticationHandler(options.Object, loggerFactory.Object, encoder.Object, _userManager, _userStore);
    }

    /// <summary>
    /// Success: a stored access token belonging to a login of an existing user completes the authentication.
    /// The store holds the keyed hash of the token, while the request presents the raw token.
    /// </summary>
    [Fact(DisplayName = "Success: Authentication was completed.")]
    public async Task SuccessfulAuth()
    {
        var user = await AddMockUserAsync();
        string rawToken = _userManager.CreateJwtToken(TimeSpan.FromDays(1));
        var token = await AddAccessTokenAsync(user, TestHelper.HashToken(rawToken, AppConfiguration));

        var context = GetHttpContext();
        var authorizationHeader = new StringValues($"Bearer {rawToken}");
        context.Request.Headers.Append(HeaderNames.Authorization, authorizationHeader);
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.None.Should().BeFalse();
        result.Failure.Should().BeNull();
    }

    /// <summary>
    /// Failure: the raw token is only accepted when the store holds its hash. A token persisted
    /// unhashed must never authenticate, so a database leak cannot be replayed directly.
    /// </summary>
    [Fact(DisplayName = "Fail: An unhashed stored token cannot be used to authenticate.")]
    public async Task FailureWhenTokenIsStoredUnhashed()
    {
        var user = await AddMockUserAsync();
        string rawToken = _userManager.CreateJwtToken(TimeSpan.FromDays(1));
        await AddAccessTokenAsync(user, rawToken);

        var context = GetHttpContext();
        context.Request.Headers.Append(HeaderNames.Authorization, new StringValues($"Bearer {rawToken}"));
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure.Message.Should().Be("Failed to authenticate user with provided authentication information.");
    }

    /// <summary>
    /// Failure: a token whose login record has expired must not authenticate, even though the
    /// token itself is still within its own lifetime.
    /// </summary>
    [Fact(DisplayName = "Fail: An expired login record cannot authenticate.")]
    public async Task FailureWhenLoginExpired()
    {
        var user = await AddMockUserAsync();
        string rawToken = _userManager.CreateJwtToken(TimeSpan.FromDays(1));
        var token = await AddAccessTokenAsync(user, TestHelper.HashToken(rawToken, AppConfiguration), DateTimeOffset.UtcNow.AddDays(-1));

        var context = GetHttpContext();
        context.Request.Headers.Append(HeaderNames.Authorization, new StringValues($"Bearer {rawToken}"));
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure.Message.Should().Be("Failed to authenticate user with provided authentication information.");
        token.Should().NotBeNull();
    }

    /// <summary>
    /// Stores an access token together with the login record that authorizes it, mirroring what a
    /// successful sign-in persists.
    /// </summary>
    /// <param name="user">The user the token belongs to.</param>
    /// <param name="storedTokenValue">The value persisted for the token, i.e. the token or its hash.</param>
    /// <param name="loginExpireDate">The expiry date of the login record backing the token.</param>
    /// <returns>The persisted access token.</returns>
    private async Task<CustomUserToken> AddAccessTokenAsync(CustomUser user, string storedTokenValue,
        DateTimeOffset? loginExpireDate = null)
    {
        var token = await _userStore.UserTokens.AddAsync(new CustomUserToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            Name = "AccessToken",
            LoginProvider = "Default",
            Value = storedTokenValue,
            CreateDate = DateTimeOffset.UtcNow
        }, true, TestContext.Current.CancellationToken);

        await _userStore.UserLogins.AddAsync(new CustomUserLogin
        {
            Id = 1,
            UserId = user.Id,
            ProviderDisplayName = "Default",
            LoginProvider = "Default",
            ProviderKey = token.Id,
            OperatingSystem = "Windows",
            IPv4Address = "127.0.0.1",
            CreateDate = DateTime.UtcNow,
            ExpireDate = loginExpireDate ?? DateTime.UtcNow.AddDays(1)
        }, true, TestContext.Current.CancellationToken);

        return token;
    }

    /// <summary>
    /// Failure: an empty authorization header results in no authentication result.
    /// </summary>
    [Fact(DisplayName = "Fail: The provided authentication header was empty.")]
    public async Task FailureEmptyAuthHeader()
    {
        var context = GetHttpContext();
        var authorizationHeader = new StringValues(string.Empty);
        context.Request.Headers.Append(HeaderNames.Authorization, authorizationHeader);
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.None.Should().BeTrue();
    }

    /// <summary>
    /// Failure: a header using another scheme results in no authentication result.
    /// </summary>
    [Fact(DisplayName = "Fail: The provided authentication header was not Bearer.")]
    public async Task FailureHeaderNotBearer()
    {
        var context = GetHttpContext();
        var authorizationHeader = new StringValues("Invalid abcd");
        context.Request.Headers.Append(HeaderNames.Authorization, authorizationHeader);
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.None.Should().BeTrue();
    }

    /// <summary>
    /// Failure: a Bearer header without a token fails with an invalid authentication information error.
    /// </summary>
    [Fact(DisplayName = "Fail: The provided Bearer header did not contain a token.")]
    public async Task FailureEmptyBearer()
    {
        var context = GetHttpContext();
        var authorizationHeader = new StringValues("Bearer");
        context.Request.Headers.Append(HeaderNames.Authorization, authorizationHeader);
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.None.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure.Message.Should().Be("Invalid authentication information provided in the request.");
    }

    /// <summary>
    /// Failure: an unknown bearer token fails the authentication.
    /// </summary>
    [Fact(DisplayName = "Fail: The provided bearer token could not be verified.")]
    public async Task FailureTokenCouldNotBeVerified()
    {
        var context = GetHttpContext();
        var authorizationHeader = new StringValues("Bearer abcd");
        context.Request.Headers.Append(HeaderNames.Authorization, authorizationHeader);
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.None.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure.Message.Should().Be("Failed to authenticate user with provided authentication information.");
    }
}
