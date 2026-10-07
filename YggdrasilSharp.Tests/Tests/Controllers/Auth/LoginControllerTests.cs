using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtpNet;
using Tavstal.YggdrasilSharp.Controllers.Auth;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Bodies.Auth;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Models.Responses;
using Tavstal.YggdrasilSharp.Models.Responses.Auth;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Tests.Helpers;
using Tavstal.YggdrasilSharp.Utils.Helpers;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Controllers.Auth;

/// <summary>
/// Unit tests for <see cref="LoginController"/> covering standard login flows,
/// two-factor flows, launcher-specific login flows and logout behavior.
/// The class sets up an in-memory DB, a test <see cref="LoginController"/> instance and
/// a preconfigured test <see cref="DefaultHttpContext"/> used across tests.
/// </summary>
public class LoginControllerTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly TestHelper _testHelper;
    /// <summary>
    /// The user store used to create the users of the login flows.
    /// </summary>
    protected readonly CustomUserStore _userStore;

    /// <summary>
    /// The user manager wired to the two-factor flows.
    /// </summary>
    protected readonly CustomUserManager _userManager;

    /// <summary>
    /// The sign-in manager wired to the login flows.
    /// </summary>
    protected readonly CustomSignInManager _signInManager;
    private readonly AppConfiguration _appConfiguration;
    private readonly LoginController _controller;
    private readonly DefaultHttpContext _controllerHttpContext;
    private readonly CustomUser _userMock;
    private const string _passwordMock = "This%Valid_And#Pass%mock-2026";

    /// <summary>
    /// Initializes shared fixtures:
    /// <br/>- creates in-memory DB context,
    /// <br/>- builds a test UserManager,
    /// <br/>- constructs the <see cref="LoginController"/> with fake dependencies,
    /// <br/>- prepares a default <see cref="DefaultHttpContext"/> (IP address, User-Agent, Host),
    /// <br/>- prepares a default <see cref="CustomUser"/> object used by tests.
    /// </summary>
    /// <param name="testOutputHelper">XUnit-provided output helper for test logging.</param>
    public LoginControllerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _testHelper = new TestHelper();
        var loggerMock = new Mock<ILogger<LoginController>>();
        var dbContext = TestHelper.CreateInMemoryDbContext();
        _userStore = TestHelper.CreateCustomUserStore(dbContext);
        _userManager = _testHelper.CreateCustomUserManager(dbContext, _userStore);
        _appConfiguration = TestHelper.CreateTestSettings();
        var memoryCache = _testHelper.MemoryCacheService;
        _signInManager = _testHelper.CreateSignInManager(_userStore, _userManager, _appConfiguration);
        _controller = new LoginController(loggerMock.Object, _userManager, _userStore, _appConfiguration, _signInManager, memoryCache);
        _controllerHttpContext = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = IPAddress.Parse(TestHelper.IpAddress)
            }
        };
        // Set User-Agent header
        _controllerHttpContext.Request.Headers.UserAgent = TestHelper.UserAgent;
        _controllerHttpContext.Request.Host = new HostString("localhost", 5000);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };


        _userMock = new CustomUser
        {
            Email = "testuser@gmail.com",
            EmailConfirmed = true,
            NormalizedEmail = "testuser@gmail.com".Normalize(),
            UserName = "testuser",
            NormalizedUserName = "testuser".Normalize(),
            PasswordHash = "",
            CreateDate = DateTimeOffset.UtcNow,
            LastLogin = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow,
            SkinModel = ESkinType.WIDE,
            LockoutEnabled = false,
            LockoutEnd = null,
            LockoutReason = null
        };
        _userMock.PasswordHash = _testHelper.PasswordHasher.HashPassword(_userMock, _passwordMock);
    }

    /// <summary>
    /// Tests for standard web login endpoints
    /// </summary>
    public class LoginTests : LoginControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public LoginTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: login with valid credentials returns a non-null content result.
        /// Expected: ContentResult with login payload.
        /// </summary>
        [Fact(DisplayName = "Success: Login with valid credentials")]
        public async Task ReturnsOk()
        {
            var result = await AddMockUserAndLoginAsync();
            var content = result.content;
            content.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + content);
        }

        /// <summary>
        /// Success case: the response carries the raw access token while the database only stores its
        /// keyed hash, so a database leak cannot be replayed as a bearer token.
        /// </summary>
        [Fact(DisplayName = "Success: Login returns the raw token and stores only its hash")]
        public async Task ReturnsRawTokenAndStoresOnlyItsHash()
        {
            var loginResult = await AddMockUserAndLoginAsync();

            var response = Deserialize<LoginResponse>(loginResult.content);
            response.Token.Should().NotBeNullOrEmpty();

            string storedToken = await GetStoredAccessTokenAsync(loginResult.userId);
            storedToken.Should().Be(TestHelper.HashToken(response.Token!, _appConfiguration));
            storedToken.Should().NotBe(response.Token, "the raw token must never be persisted");
            (await _userManager.VerifyJwtTokenAsync(response.Token!)).Should().BeTrue();
        }

        /// <summary>
        /// Redirect case: login when the user has 2FA enabled should return a redirect/2FA payload.
        /// Expected: ContentResult containing redirect/2FA session info.
        /// </summary>
        [Fact(DisplayName = "Redirect: TFA enabled")]
        public async Task ReturnsRedirect()
        {
            var result = await AddMockUserAndLoginAsync(true);
            var content = result.content;
            content.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + content);
        }

        /// <summary>
        /// Failure case: attempting to login for a non-existent user returns 400 BadRequest.
        /// </summary>
        [Fact(DisplayName = "Failure: Non-existent user")]
        public async Task ReturnsBadRequest_WhenUserDoesNotExist()
        {
            IActionResult result = await _controller.LoginAsync(new LoginRequestBody
            {
                Email = _userMock.Email,
                Password = _passwordMock
            });

            TestHelper.TestResponse(result, HttpStatusCode.BadRequest, "Invalid credentials.");
        }

        /// <summary>
        /// Failure case: existing user with incorrect password should return 400 BadRequest.
        /// </summary>
        [Fact(DisplayName = "Failure: Incorrect password")]
        public async Task ReturnsBadRequest_WhenPasswordIncorrect()
        {
            await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);
            IActionResult result = await _controller.LoginAsync(new LoginRequestBody
            {
                Email = _userMock.Email,
                Password = "This%Valid_And#Pass%mock-2027"
            });

            TestHelper.TestResponse(result, HttpStatusCode.BadRequest, "Invalid credentials.");
        }

        /// <summary>
        /// Failure case: locked out user attempt — verifies controller handles lockout state.
        /// Expected behaviour: login returns a ContentResult (controller may return lockout-specific response).
        /// </summary>
        [Fact(DisplayName = "Failure: Locked out user")]
        public async Task ReturnsBadRequest_WhenUserLockedOut()
        {
            _userMock.LockoutEnabled = true;
            _userMock.LockoutEnd = DateTime.UtcNow.AddDays(30);
            _userMock.LockoutReason = "Too many failed login attempts";
            await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);

            IActionResult result = await _controller.LoginAsync(new LoginRequestBody
            {
                Email = _userMock.Email,
                Password = _passwordMock
            });

            TestHelper.TestResponse(result, HttpStatusCode.BadRequest);
        }
    }

    /// <summary>
    /// Tests for web two-factor login flow (session token + code verification).
    /// Covers successful TFA confirmation, missing session token, invalid/expired session.
    /// </summary>
    public class LoginTwoFactorTests : LoginControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public LoginTwoFactorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: the initial login redirect carries the user id and session token in the body,
        /// and submitting them with the correct TOTP returns success.
        /// Expected: ContentResult with final login payload.
        /// </summary>
        [Fact(DisplayName = "Success: Login with valid credentials")]
        public async Task ReturnsOk()
        {
            var loginResult = await AddMockUserAndLoginAsync(true);
            var redirect = Deserialize<LoginRedirectResponse>(loginResult.content);
            redirect.UserId.Should().NotBeNullOrEmpty();
            redirect.SessionToken.Should().NotBeNullOrEmpty();
            _userMock.TwoFactorSecret.Should().NotBeNullOrEmpty();

            byte[] secretBytes = Encoding.UTF8.GetBytes(_userMock.TwoFactorSecret.DecryptSelf(_appConfiguration.Jwt.TwoFactorEncryptionKey));
            var totpGenerator = new Totp(secretBytes);
            string expectedCode = totpGenerator.ComputeTotp();
            IActionResult result = await _controller.LoginTwoFactorAsync(new LoginTFASessionRequestBody
            {
                UserId = redirect.UserId,
                SessionToken = redirect.SessionToken,
                TwoFactorCode = expectedCode
            });

            result.Should().BeOfType<ContentResult>();

            ContentResult? contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + contentResult.Content);

            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(contentResult.Content!);
            errorResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
            errorResponse.Message.Should().Be("Login successful.");
        }

        /// <summary>
        /// Success case: the token issued once the second factor is confirmed is the raw token, and only
        /// its hash is persisted alongside the login record.
        /// </summary>
        [Fact(DisplayName = "Success: 2FA login returns the raw token and stores only its hash")]
        public async Task ReturnsRawTokenAndStoresOnlyItsHash()
        {
            var loginResult = await AddMockUserAndLoginAsync(true);
            var redirect = Deserialize<LoginRedirectResponse>(loginResult.content);
            _userMock.TwoFactorSecret.Should().NotBeNullOrEmpty();

            byte[] secretBytes = Encoding.UTF8.GetBytes(_userMock.TwoFactorSecret.DecryptSelf(_appConfiguration.Jwt.TwoFactorEncryptionKey));
            string expectedCode = new Totp(secretBytes).ComputeTotp();

            IActionResult result = await _controller.LoginTwoFactorAsync(new LoginTFASessionRequestBody
            {
                UserId = redirect.UserId,
                SessionToken = redirect.SessionToken,
                TwoFactorCode = expectedCode
            });

            var response = Deserialize<LoginResponse>((result as ContentResult)!.Content);
            response.Token.Should().NotBeNullOrEmpty();

            string storedToken = await GetStoredAccessTokenAsync(loginResult.userId);
            storedToken.Should().Be(TestHelper.HashToken(response.Token!, _appConfiguration));
            storedToken.Should().NotBe(response.Token, "the raw token must never be persisted");
        }

        /// <summary>
        /// Failure case: a two-factor request without a user id or session token in the body
        /// should result in unauthorized response (401).
        /// </summary>
        [Fact(DisplayName = "Failure: Missing TFA session")]
        public async Task ReturnsUnauthorized_ForMissingSession()
        {
            await AddMockUserAndLoginAsync(true);
            IActionResult result = await _controller.LoginTwoFactorAsync(new LoginTFASessionRequestBody
            {
                UserId = string.Empty,
                SessionToken = string.Empty,
                TwoFactorCode = "000000"
            });

            TestHelper.TestResponse(result, HttpStatusCode.Unauthorized, "Invalid credentials.");
        }

        /// <summary>
        /// Failure case: submitting an invalid TFA code returns 401 Unauthorized.
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid TFA code")]
        public async Task ReturnsUnauthorized_ForInvalidCode()
        {
            var loginResult = await AddMockUserAndLoginAsync(true);
            var redirect = Deserialize<LoginRedirectResponse>(loginResult.content);

            IActionResult result = await _controller.LoginTwoFactorAsync(new LoginTFASessionRequestBody
            {
                UserId = redirect.UserId,
                SessionToken = redirect.SessionToken,
                TwoFactorCode = "000000"
            });

            TestHelper.TestResponse(result, HttpStatusCode.Unauthorized, "Invalid two-factor code.");
        }

        /// <summary>
        /// Failure case: expired TFA session (token removed from cache) should return 401 Unauthorized.
        /// This test explicitly removes the stored token to simulate expiration.
        /// </summary>
        [Fact(DisplayName = "Failure: Expired TFA session")]
        public async Task ReturnsUnauthorized_ForExpiredSession()
        {
            var loginResult = await AddMockUserAndLoginAsync(true);
            var redirect = Deserialize<LoginRedirectResponse>(loginResult.content);
            redirect.SessionToken.Should().NotBeNullOrEmpty();

            var memoryCacheService = _testHelper.MemoryCacheService;
            string fingerprint = TestHelper.GetFingerprint(loginResult.userId);
            string tokenKey = $"auth:{fingerprint}:tfa:token";
            if (memoryCacheService.TryGetValue(tokenKey, out string? _))
                memoryCacheService.RemoveValue(tokenKey);

            IActionResult result = await _controller.LoginTwoFactorAsync(new LoginTFASessionRequestBody
            {
                UserId = redirect.UserId,
                SessionToken = redirect.SessionToken,
                TwoFactorCode = "000000"
            });

            TestHelper.TestResponse(result, HttpStatusCode.Unauthorized, "Invalid credentials.");
        }

        /// <summary>
        /// Failure case: a valid session token paired with a user id that does not exist.
        /// The cached session token check passes for the unknown id, but the user lookup fails,
        /// so the controller returns 400 BadRequest.
        /// </summary>
        [Fact(DisplayName = "Failure: Unknown user id with valid session")]
        public async Task ReturnsBadRequest_ForUnknownUserId()
        {
            var loginResult = await AddMockUserAndLoginAsync(true);
            var redirect = Deserialize<LoginRedirectResponse>(loginResult.content);
            string sessionToken = redirect.SessionToken;
            sessionToken.Should().NotBeNullOrEmpty();

            const string nonExistentUserId = "11111111-1111-1111-1111-111111111111";
            string tokenKey = $"auth:{TestHelper.GetFingerprint(nonExistentUserId)}:tfa:token";
            _testHelper.MemoryCacheService.SetValue(tokenKey, sessionToken, TimeSpan.FromMinutes(5));

            IActionResult result = await _controller.LoginTwoFactorAsync(new LoginTFASessionRequestBody
            {
                UserId = nonExistentUserId,
                SessionToken = sessionToken,
                TwoFactorCode = "000000"
            });

            TestHelper.TestResponse(result, HttpStatusCode.BadRequest, "Invalid credentials.");
        }
    }

    /// <summary>
    /// Tests for the launcher-specific login endpoints and launcher TFA flow.
    /// The launcher flows use a different endpoint/payload format and session token handling.
    /// </summary>
    public class LoginLauncherTests : LoginControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public LoginLauncherTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: launcher login with valid credentials returns a non-null content payload.
        /// Expected: ContentResult containing launcher-specific token/payload.
        /// </summary>
        [Fact(DisplayName = "Success: Launcher login with valid credentials")]
        public async Task ReturnsOk()
        {
            await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);
            IActionResult result = await _controller.LoginLauncherAsync(new LauncherLoginRequestBody
            {
                Username = _userMock.UserName,
                Password = _passwordMock
            });

            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + contentResult.Content);
        }

        /// <summary>
        /// Success case: the launcher receives the raw play session token while the play session record
        /// only stores its keyed hash.
        /// </summary>
        [Fact(DisplayName = "Success: Launcher login returns the raw token and stores only its hash")]
        public async Task ReturnsRawTokenAndStoresOnlyItsHash()
        {
            var loginResult = await AddMockUserAndLoginLauncherAsync();

            var response = Deserialize<LoginResponse>(loginResult.content);
            response.Token.Should().NotBeNullOrEmpty();

            var sessions = await _userStore.UserPlaySessions.QueryAsync(x => x.UserId == loginResult.userId, TestContext.Current.CancellationToken);
            var session = sessions.Should().ContainSingle().Subject;
            session.Token.Should().Be(TestHelper.HashToken(response.Token!, _appConfiguration));
            session.Token.Should().NotBe(response.Token, "the raw play session token must never be persisted");
        }

        /// <summary>
        /// Redirect case: launcher login when 2FA is enabled returns a redirect/session token.
        /// Expected: ContentResult containing session token used for launcher TFA confirmation.
        /// </summary>
        [Fact(DisplayName = "Redirect: Launcher login with TFA")]
        public async Task ReturnsRedirect_WhenTwoFactorEnabled()
        {
            _userMock.TwoFactorEnabled = true;
            var user = await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);
            await _userManager.GenerateTwoFactorTokenAsync(user);
            IActionResult result = await _controller.LoginLauncherAsync(new LauncherLoginRequestBody
            {
                Username = _userMock.UserName,
                Password = _passwordMock
            });

            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + contentResult.Content);
        }

        /// <summary>
        /// Failure case: launcher login with incorrect password returns 400 BadRequest.
        /// </summary>
        [Fact(DisplayName = "Failure: Incorrect password")]
        public async Task ReturnsBadRequest_ForIncorrectPassword()
        {
            await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);
            IActionResult result = await _controller.LoginLauncherAsync(new LauncherLoginRequestBody
            {
                Username = _userMock.UserName,
                Password = "wrong-password"
            });

            TestHelper.TestResponse(result, HttpStatusCode.BadRequest, "Invalid credentials.");
        }
    }

    /// <summary>
    /// Tests for the launcher two-factor confirmation endpoint.
    /// Covers success, missing/invalid token and expired token cases.
    /// </summary>
    public class LoginTwoFactorLauncherTests : LoginControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public LoginTwoFactorLauncherTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: full launcher 2FA flow — first stage returns a session token,
        /// second stage confirms TOTP and returns final success message.
        /// Expected: second call returns ContentResult containing "Login successful".
        /// </summary>
        [Fact(DisplayName = "Success: Launcher 2FA flow (redirect then confirm)")]
        public async Task ReturnsOk()
        {
            var loginResult = await AddMockUserAndLoginLauncherAsync(true);
            var content = loginResult.content;
            content.Should().NotBeNullOrEmpty();
            var redirect = JsonConvert.DeserializeObject<LoginLauncherRedirectResponse>(content);
            redirect.Should().NotBeNull();
            string sessionToken = redirect.Token!;
            sessionToken.Should().NotBeNullOrEmpty();
            _userMock.TwoFactorSecret.Should().NotBeNullOrEmpty();

            byte[] secretBytes = Encoding.UTF8.GetBytes(_userMock.TwoFactorSecret.DecryptSelf(_appConfiguration.Jwt.TwoFactorEncryptionKey));
            var totpGenerator = new Totp(secretBytes);
            string expectedCode = totpGenerator.ComputeTotp();

            IActionResult secondResult = await _controller.LoginLauncherTwoFactorAsync(
                new LauncherLoginTFASessionRequestBody
                {
                    UserId = loginResult.userId,
                    SessionToken = sessionToken,
                    TwoFactorCode = expectedCode
                });

            secondResult.Should().BeOfType<ContentResult>();
            var secondContent = (secondResult as ContentResult)!.Content;
            secondContent.Should().Contain("Login successful");
            _testOutputHelper.WriteLine("Result: " + secondContent);
        }

        /// <summary>
        /// Success case: confirming the launcher second factor hands out the raw play session token and
        /// stores only its hash, so the launcher token stays recoverable while the database stays useless.
        /// </summary>
        [Fact(DisplayName = "Success: Launcher 2FA returns the raw token and stores only its hash")]
        public async Task ReturnsRawTokenAndStoresOnlyItsHash()
        {
            var loginResult = await AddMockUserAndLoginLauncherAsync(true);
            string sessionToken = Deserialize<LoginLauncherRedirectResponse>(loginResult.content).Token!;
            sessionToken.Should().NotBeNullOrEmpty();
            _userMock.TwoFactorSecret.Should().NotBeNullOrEmpty();

            byte[] secretBytes = Encoding.UTF8.GetBytes(_userMock.TwoFactorSecret.DecryptSelf(_appConfiguration.Jwt.TwoFactorEncryptionKey));
            string expectedCode = new Totp(secretBytes).ComputeTotp();

            IActionResult secondResult = await _controller.LoginLauncherTwoFactorAsync(
                new LauncherLoginTFASessionRequestBody
                {
                    UserId = loginResult.userId,
                    SessionToken = sessionToken,
                    TwoFactorCode = expectedCode
                });

            var response = Deserialize<LoginResponse>((secondResult as ContentResult)!.Content);
            response.Token.Should().NotBeNullOrEmpty();

            var sessions = await _userStore.UserPlaySessions.QueryAsync(x => x.UserId == loginResult.userId, TestContext.Current.CancellationToken);
            var session = sessions.Should().ContainSingle().Subject;
            session.Token.Should().Be(TestHelper.HashToken(response.Token!, _appConfiguration));
            session.Token.Should().NotBe(response.Token, "the raw play session token must never be persisted");
        }

        /// <summary>
        /// Failure case: missing or invalid session token for the launcher flow returns 401 Unauthorized.
        /// The test simulates the missing token by not performing the initial login stage.
        /// </summary>
        [Fact(DisplayName = "Failure: Missing/invalid session token")]
        public async Task ReturnsUnauthorized_ForMissingToken()
        {
            var loginResult = await AddMockUserAndLoginLauncherAsync(true, false);
            IActionResult result = await _controller.LoginLauncherTwoFactorAsync(new LauncherLoginTFASessionRequestBody
            {
                UserId = loginResult.userId,
                SessionToken = "invalid-token",
                TwoFactorCode = "000000"
            });

            TestHelper.TestResponse(result, HttpStatusCode.Unauthorized, "Invalid or expired session token.");
        }

        /// <summary>
        /// Failure case: invalid two-factor code for launcher confirmation returns 400 BadRequest.
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid two-factor code")]
        public async Task ReturnsBadRequest_ForInvalidCode()
        {
            var loginResult = await AddMockUserAndLoginLauncherAsync(true);
            var content = loginResult.content;
            content.Should().NotBeNullOrEmpty();
            var redirect = JsonConvert.DeserializeObject<LoginLauncherRedirectResponse>(content);
            redirect.Should().NotBeNull();
            string sessionToken = redirect.Token!;

            IActionResult secondResult = await _controller.LoginLauncherTwoFactorAsync(
                new LauncherLoginTFASessionRequestBody
                {
                    UserId = loginResult.userId,
                    SessionToken = sessionToken,
                    TwoFactorCode = "000000"
                });

            TestHelper.TestResponse(secondResult, HttpStatusCode.BadRequest, "Invalid two-factor code.");
        }

        /// <summary>
        /// Failure case: expired launcher session token: the test removes the cached token to simulate expiration.
        /// Expected: the controller returns 401 Unauthorized.
        /// </summary>
        [Fact(DisplayName = "Failure: Expired session token")]
        public async Task ReturnsUnauthorized_ForExpiredLauncherSession()
        {
            var loginResult = await AddMockUserAndLoginLauncherAsync(true);
            var content = loginResult.content;
            content.Should().NotBeNullOrEmpty();
            JObject json = JObject.Parse(content);
            string sessionToken = json["token"]?.ToString()!;

            var memoryCacheService = _testHelper.MemoryCacheService;
            string fingerprint = TestHelper.GetFingerprint(loginResult.userId);
            string tokenKey = $"auth:{fingerprint}:tfa-launcher:token";
            if (memoryCacheService.TryGetValue(tokenKey, out string? _))
                memoryCacheService.RemoveValue(tokenKey);

            IActionResult secondResult = await _controller.LoginLauncherTwoFactorAsync(
                new LauncherLoginTFASessionRequestBody
                {
                    UserId = loginResult.userId,
                    SessionToken = sessionToken,
                    TwoFactorCode = "000000"
                });

            TestHelper.TestResponse(secondResult, HttpStatusCode.Unauthorized, "Invalid or expired session token.");
        }
    }

    /// <summary>
    /// Tests for the Logout endpoint verifying both sign-out and invalid token behaviours.
    /// </summary>
    public class LogoutTests : LoginControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public LogoutTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: Logout without an explicit token parameter. The controller falls back to the
        /// "Authorization: Bearer" header, so the test seeds that header with the raw token issued at login.
        /// </summary>
        [Fact(DisplayName = "Success: Returns sign-out result")]
        public async Task ReturnsSignOut()
        {
            var loginResult = await AddMockUserAndLoginAsync();
            string token = await GetAccessTokenAsync(loginResult);
            string storedTokenHash = TestHelper.HashToken(token, _appConfiguration);
            _controllerHttpContext.Request.Headers.Authorization = $"Bearer {token}";

            IActionResult logoutResult = await _controller.LogoutAsync(null);
            logoutResult.Should().BeOfType<SignOutResult>();
            _testOutputHelper.WriteLine("Result: " + logoutResult.GetType().Name);

            (await _userStore.UserTokens.FindAsync(x => x.Value == storedTokenHash, TestContext.Current.CancellationToken))
                .Should().BeNull("logout should revoke the access token");
            (await _userStore.UserLogins.QueryAsync(x => x.UserId == loginResult.userId, TestContext.Current.CancellationToken))
                .Should().BeEmpty("logout should revoke the login record backing the access token");
        }

        /// <summary>
        /// Success case: Logout with the token passed explicitly as a query parameter.
        /// The token is the raw access token issued by the preceding login, read back from the login response.
        /// </summary>
        [Fact(DisplayName = "Success: Logout when token provided as parameter")]
        public async Task ReturnsSignOut_WhenTokenProvided()
        {
            var loginResult = await AddMockUserAndLoginAsync();
            string token = await GetAccessTokenAsync(loginResult);
            string storedTokenHash = TestHelper.HashToken(token, _appConfiguration);

            IActionResult logoutResult = await _controller.LogoutAsync(token);
            logoutResult.Should().BeOfType<SignOutResult>();

            (await _userStore.UserTokens.FindAsync(x => x.Value == storedTokenHash, TestContext.Current.CancellationToken))
                .Should().BeNull("logout should revoke the access token");
        }

        /// <summary>
        /// Failure case: the value persisted in the database is a hash, not a token. Presenting that stored
        /// value to the logout endpoint must be rejected instead of revoking the session, otherwise the hash
        /// would double as a bearer credential.
        /// </summary>
        [Fact(DisplayName = "Failure: Logout with the stored token hash returns bad request")]
        public async Task ReturnsBadRequest_WhenGivenTheStoredTokenHash()
        {
            var loginResult = await AddMockUserAndLoginAsync();
            string storedTokenHash = await GetStoredAccessTokenAsync(loginResult.userId);

            IActionResult logoutResult = await _controller.LogoutAsync(storedTokenHash);

            TestHelper.TestResponse(logoutResult, HttpStatusCode.BadRequest, "Invalid token.");
            (await _userStore.UserTokens.FindAsync(x => x.Value == storedTokenHash, TestContext.Current.CancellationToken))
                .Should().NotBeNull("a rejected logout must not revoke anything");
        }

        /// <summary>
        /// Failure case: invalid token parameter for logout should return a bad request (400).
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid token returns bad request")]
        public async Task ReturnsBadRequest_ForInvalidToken()
        {
            IActionResult result = await _controller.LogoutAsync("invalid-token");
            TestHelper.TestResponse(result, HttpStatusCode.BadRequest, "Invalid token.");
        }
    }

    /// <summary>
    /// Helper that creates the user in DB and performs a web login (standard LoginAsync).
    /// It optionally enables 2FA and optionally performs the login stage (performLogin).
    /// </summary>
    /// <param name="enableTwoFactor">If true the user will have TwoFactorEnabled and a generated TwoFactorSecret.</param>
    /// <param name="performLogin">If false this method only adds the user and returns the user id without performing login.</param>
    /// <returns>Tuple of created user's id and login content (or null if not logged in).</returns>
    private async Task<(string userId, string? content)> AddMockUserAndLoginAsync(bool enableTwoFactor = false, bool performLogin = true)
    {
        _userMock.TwoFactorEnabled = enableTwoFactor;
        var user = await _userStore.AddUserAsync(_userMock, true);
        if (enableTwoFactor)
            await _userManager.GenerateTwoFactorTokenAsync(user);

        if (!performLogin)
            return (user.Id, null);

        IActionResult result = await _controller.LoginAsync(new LoginRequestBody
        {
            Email = _userMock.Email,
            Password = _passwordMock
        });

        result.Should().BeOfType<ContentResult>();
        var contentResult = result as ContentResult;
        contentResult.Should().NotBeNull();

        var setCookies = _controllerHttpContext.Response.Headers.SetCookie;
        if (setCookies.Count > 0)
        {
            var cookiePairs = setCookies
                .Select(c => c?.Split(';')[0].Trim());
            _controllerHttpContext.Request.Headers.Cookie = string.Join("; ", cookiePairs);
        }

        var authHeader = _controllerHttpContext.Response.Headers.Authorization;
        _controllerHttpContext.Request.Headers.Authorization = authHeader;
        return (user.Id, contentResult.Content);
    }

    /// <summary>
    /// Helper that creates the user in DB and performs a launcher login (LoginLauncherAsync).
    /// It optionally enables 2FA and optionally performs the login stage (performLogin).
    /// </summary>
    /// <param name="enableTwoFactor">If true the user will have TwoFactorEnabled and a generated TwoFactorSecret.</param>
    /// <param name="performLogin">If false this method only adds the user and returns the user id without performing login.</param>
    /// <returns>Tuple of created user's id and login content (or null if not logged in).</returns>
    private async Task<(string userId, string? content)> AddMockUserAndLoginLauncherAsync(bool enableTwoFactor = false, bool performLogin = true)
    {
        _userMock.TwoFactorEnabled = enableTwoFactor;
        var user = await _userStore.AddUserAsync(_userMock, true);
        if (enableTwoFactor)
            await _userManager.GenerateTwoFactorTokenAsync(user);

        if (!performLogin)
            return (user.Id, null);

        IActionResult result = await _controller.LoginLauncherAsync(new LauncherLoginRequestBody
        {
            Username = _userMock.UserName,
            Password = _passwordMock
        });

        result.Should().BeOfType<ContentResult>();
        var contentResult = result as ContentResult;
        contentResult.Should().NotBeNull();

        var setCookies = _controllerHttpContext.Response.Headers.SetCookie;
        if (setCookies.Count > 0)
        {
            var cookiePairs = setCookies
                .Select(c => c?.Split(';')[0].Trim());
            _controllerHttpContext.Request.Headers.Cookie = string.Join("; ", cookiePairs);
        }

        var authHeader = _controllerHttpContext.Response.Headers.Authorization;
        _controllerHttpContext.Request.Headers.Authorization = authHeader;
        return (user.Id, contentResult.Content);
    }

    /// <summary>
    /// Reads back the raw access token the login response handed to the client. The database only stores
    /// the hash of that token, so the response body is the only place the raw value can be recovered.
    /// </summary>
    /// <param name="loginResult">The result of a completed login.</param>
    /// <returns>The raw access token issued for the user.</returns>
    private async Task<string> GetAccessTokenAsync((string userId, string? content) loginResult)
    {
        var response = Deserialize<LoginResponse>(loginResult.content);
        string token = response.Token!;
        token.Should().NotBeNullOrEmpty("login should return the raw access token");

        // Sanity check: the value the client received must be the one that was hashed into the store.
        string storedToken = await GetStoredAccessTokenAsync(loginResult.userId);
        storedToken.Should().Be(TestHelper.HashToken(token, _appConfiguration));
        return token;
    }

    /// <summary>
    /// Reads the access token value persisted for the given user. Since tokens are stored hashed, this
    /// value is a keyed hash and must not be used as a bearer credential.
    /// </summary>
    /// <param name="userId">The id of the user that logged in.</param>
    /// <returns>The stored (hashed) access token value.</returns>
    private async Task<string> GetStoredAccessTokenAsync(string userId)
    {
        var tokens = await _userStore.UserTokens.QueryAsync(x => x.UserId == userId && x.Name == "AccessToken");
        var token = tokens.Should().ContainSingle().Subject;
        return token.Value!;
    }

    /// <summary>
    /// Deserializes a controller response body, failing the test when the payload is missing.
    /// </summary>
    /// <typeparam name="T">The expected response type.</typeparam>
    /// <param name="content">The raw JSON body of the response.</param>
    /// <returns>The deserialized response.</returns>
    private T Deserialize<T>(string? content)
    {
        content.Should().NotBeNullOrEmpty("the controller should return a JSON body");
        var deserialized = JsonConvert.DeserializeObject<T>(content!);
        deserialized.Should().NotBeNull();
        return deserialized!;
    }
}
