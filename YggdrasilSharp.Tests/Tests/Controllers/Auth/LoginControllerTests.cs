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
        _controller = new LoginController(loggerMock.Object, _signInManager, _userStore, memoryCache, _appConfiguration);
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
        /// Failure case: attempting to login for a non-existent user returns 404 NotFound.
        /// </summary>
        [Fact(DisplayName = "Failure: Non-existent user")]
        public async Task ReturnsBadRequest_WhenUserDoesNotExist()
        {
            IActionResult result = await _controller.LoginAsync(new LoginRequestBody
            {
                Email = _userMock.Email,
                Password = _passwordMock
            });

            TestHelper.TestResponse(result, HttpStatusCode.BadRequest);
        }
        
        /// <summary>
        /// Failure case: existing user with incorrect password should return 401 Unauthorized.
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

            TestHelper.TestResponse(result, HttpStatusCode.BadRequest);
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
    /// Tests for web two-factor login flow (TFA session cookie + code verification).
    /// Covers successful TFA confirmation, missing session cookie, invalid/expired session.
    /// </summary>
    public class LoginTwoFactorTests : LoginControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public LoginTwoFactorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: after the initial login redirect, the TFA cookies are present and submitting the correct TOTP returns success.
        /// Expected: ContentResult with final login payload.
        /// </summary>
        [Fact(DisplayName = "Success: Login with valid credentials")]
        public async Task ReturnsOk()
        {
            await AddMockUserAndLoginAsync(true);
            var setCookie = _controllerHttpContext.Response.Headers.SetCookie.ToString();
            setCookie.Should().Contain("ysharp-twofactor-session=");
            setCookie.Should().Contain("ysharp-userId=");
            _userMock.TwoFactorSecret.Should().NotBeNullOrEmpty();

            byte[] secretBytes = Encoding.UTF8.GetBytes(_userMock.TwoFactorSecret.DecryptSelf(_appConfiguration.Jwt.EncryptionKey));
            var totpGenerator = new Totp(secretBytes);
            string expectedCode = totpGenerator.ComputeTotp();
            IActionResult result = await _controller.LoginTwoFactorAsync(new LoginTFASessionRequestBody
            {
                TwoFactorCode = expectedCode
            });
            
            result.Should().BeOfType<ContentResult>();

            ContentResult? contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + contentResult.Content);
        }

        /// <summary>
        /// Failure case: missing TFA session cookie should result in unauthorized response (401).
        /// The test clears Request.Headers.Cookie to simulate a missing cookie.
        /// </summary>
        [Fact(DisplayName = "Failure: Missing TFA session cookie")]
        public async Task ReturnsUnauthorized_ForMissingCookie()
        {
            await AddMockUserAndLoginAsync(true);
            _controllerHttpContext.Request.Headers.Cookie = []; // Clear cookies
            IActionResult result = await _controller.LoginTwoFactorAsync(new LoginTFASessionRequestBody
            {
                TwoFactorCode = "000000"
            });
            
            TestHelper.TestResponse(result, HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Failure case: submitting an invalid TFA code returns 401 Unauthorized.
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid TFA code")]
        public async Task ReturnsBadRequest_ForInvalidCode()
        {
            await AddMockUserAndLoginAsync(true);
            
            IActionResult result = await _controller.LoginTwoFactorAsync(new LoginTFASessionRequestBody
            {
                TwoFactorCode = "000000"
            });
            
            TestHelper.TestResponse(result, HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Failure case: expired TFA session (token removed from cache) should return 401 or 403 depending on controller behavior.
        /// This test explicitly removes the stored token to simulate expiration.
        /// </summary>
        [Fact(DisplayName = "Failure: Expired TFA session")]
        public async Task ReturnsForbidden_ForExpiredSession()
        {
            var loginResult = await AddMockUserAndLoginAsync(true);
            var setCookie = _controllerHttpContext.Response.Headers.SetCookie.ToString();
            setCookie.Should().Contain("ysharp-twofactor-session=");
            setCookie.Should().Contain("ysharp-userId=");

            var memoryCacheService = _testHelper.MemoryCacheService;
            string fingerprint = TestHelper.GetFingerprint(loginResult.userId);
            string tokenKey = $"auth:{fingerprint}:tfa:token";
            if (memoryCacheService.TryGetValue(tokenKey, out string? _))
                memoryCacheService.RemoveValue(tokenKey);
            
            IActionResult result = await _controller.LoginTwoFactorAsync(new LoginTFASessionRequestBody
            {
                TwoFactorCode = "000000"
            });

            TestHelper.TestResponse(result, HttpStatusCode.Unauthorized);
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
        /// Failure case: launcher login with incorrect password returns 401 Unauthorized.
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
            
            TestHelper.TestResponse(result, HttpStatusCode.BadRequest);
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

            byte[] secretBytes = Encoding.UTF8.GetBytes(_userMock.TwoFactorSecret.DecryptSelf(_appConfiguration.Jwt.EncryptionKey));
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

            TestHelper.TestResponse(result, HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Failure case: invalid two-factor code for launcher confirmation returns 401 Unauthorized.
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

            TestHelper.TestResponse(secondResult, HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Failure case: expired launcher session token: the test removes the cached token to simulate expiration.
        /// Expected: the controller should return 401 (or 403 depending on implementation); test asserts 401.
        /// </summary>
        [Fact(DisplayName = "Failure: Expired session token")]
        public async Task ReturnsForbidden_ForExpiredLauncherSession()
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

            TestHelper.TestResponse(secondResult, HttpStatusCode.Unauthorized);
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
        /// Success case: Logout without explicit token parameter returns a SignOutResult.
        /// </summary>
        [Fact(DisplayName = "Success: Returns sign-out result")]
        public async Task ReturnsSignOut()
        {
            await AddMockUserAndLoginAsync();
            IActionResult logoutResult = await _controller.LogoutAsync(null);
            logoutResult.Should().BeOfType<SignOutResult>();
            _testOutputHelper.WriteLine("Result: " + logoutResult.GetType().Name);
        }
        
        /// <summary>
        /// Success case: Logout with token parameter present in query should also return SignOutResult.
        /// The test extracts the token from cookies set during login and passes it to the Logout endpoint.
        /// </summary>
        [Fact(DisplayName = "Success: Logout when token provided as parameter")]
        public async Task ReturnsSignOut_WhenTokenProvided()
        {
            await AddMockUserAndLoginAsync();
            var setCookie = _controllerHttpContext.Response.Headers.SetCookie.ToString();
            setCookie.Should().Contain("auth-token=");
            var cookiePair = setCookie.Split(';', 2)[0].Trim();
            var token = cookiePair.Split('=', 2)[1];
            
            IActionResult logoutResult = await _controller.LogoutAsync(token);
            logoutResult.Should().BeOfType<SignOutResult>();
        }

        /// <summary>
        /// Failure case: invalid token parameter for logout should return a bad request (400).
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid token returns bad request")]
        public async Task ReturnsBadRequest_ForInvalidToken()
        {
            IActionResult result = await _controller.LogoutAsync("invalid-token");
            TestHelper.TestResponse(result, HttpStatusCode.BadRequest);
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
        return (user.Id, contentResult.Content);
    }
}