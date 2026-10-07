using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Tavstal.YggdrasilSharp.Controllers.Yggdrasil;
using Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Models.Responses.Yggdrasil;
using Tavstal.YggdrasilSharp.Tests.Helpers;
using Tavstal.YggdrasilSharp.Tests.Models;
using Tavstal.YggdrasilSharp.Utils.Helpers;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Controllers.Yggdrasil;

/// <summary>
/// Unit tests for <see cref="AuthController"/> covering the legacy Yggdrasil authentication endpoints:
/// <br/>- authenticate (token issuance, agent enforcement, 2FA rejection, credential failures),
/// <br/>- refresh (token rotation and session lookup failures),
/// <br/>- validate (token checks),
/// <br/>- invalidate (token revocation),
/// <br/>- signout (global play session revocation).
/// <br/>Every endpoint is gated behind the <c>EnableLegacyAuth</c> configuration flag, so the tests
/// enable it up-front and add dedicated tests for the disabled state.
/// </summary>
public class AuthControllerTests : ControllerTestBase
{
    private readonly Mock<ILogger<AuthController>> _loggerMock = new();
    private readonly AuthController _controller;

    /// <summary>
    /// Initializes the test fixture:
    /// <br/>- builds a sign-in manager from the shared in-memory store,
    /// <br/>- constructs an <see cref="AuthController"/> with fake logger and test settings,
    /// <br/>- attaches the in-memory <see cref="Microsoft.AspNetCore.Http.HttpContext"/>,
    /// <br/>- enables the legacy Yggdrasil endpoints so the actions under test do real work.
    /// </summary>
    /// <param name="testOutputHelper">XUnit-provided output helper for logging.</param>
    public AuthControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        var signInManager = _testHelper.CreateSignInManager(_userStore, _userManager, AppConfiguration);
        _controller = new AuthController(_loggerMock.Object, _userManager, _userStore, AppConfiguration, signInManager);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
        AppConfiguration.Yggdrasil.EnableLegacyAuth = true;
    }

    /// <summary>
    /// Tests for the authenticate endpoint which issues a Yggdrasil access token.
    /// </summary>
    public class AuthenticateTests : AuthControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public AuthenticateTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: valid credentials return the access token together with the selected profile,
        /// and the play session is bound to the client token sent by the client.
        /// </summary>
        [Fact(DisplayName = "Success: Authenticate with valid credentials")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller);
            string clientToken = Guid.NewGuid().ToString();

            IActionResult result = await _controller.Authenticate(new YigLoginRequest
            {
                Agent = new YigAgent { Name = "Minecraft", Version = 1 },
                Username = user.UserName,
                Password = _passwordMock,
                ClientToken = clientToken
            });

            var response = Deserialize<YigLoginResponse>(GetContent(result));
            response.AccessToken.Should().NotBeNullOrEmpty();
            response.ClientToken.Should().Be(clientToken);
            response.SelectedProfile.Id.Should().Be(user.Id);
            response.SelectedProfile.Name.Should().Be(user.UserName);
            response.AvailableProfiles.Should().ContainSingle(p => p.Id == user.Id);
            response.User.Should().NotBeNull();
            response.User!.Id.Should().Be(user.Id);
            _testOutputHelper.WriteLine("Result: " + GetContent(result));
        }

        /// <summary>
        /// Success case: the response carries the raw access token while the database only stores its
        /// keyed hash, so a database leak cannot be replayed as a credential.
        /// </summary>
        [Fact(DisplayName = "Success: Authenticate returns the raw token and stores only its hash")]
        public async Task ReturnsRawTokenAndStoresOnlyItsHash()
        {
            var user = await CreateUserAsync(_controller);

            IActionResult result = await _controller.Authenticate(new YigLoginRequest
            {
                Agent = new YigAgent { Name = "Minecraft", Version = 1 },
                Username = user.UserName,
                Password = _passwordMock
            });

            var response = Deserialize<YigLoginResponse>(GetContent(result));
            response.AccessToken.Should().NotBeNullOrEmpty();

            var sessions = await _userStore.UserPlaySessions.QueryAsync(x => x.UserId == user.Id, TestContext.Current.CancellationToken);
            var session = sessions.Should().ContainSingle().Subject;
            session.Token.Should().Be(TestHelper.HashToken(response.AccessToken, AppConfiguration));
            session.Token.Should().NotBe(response.AccessToken, "the raw play session token must never be persisted");
            (await _userManager.VerifyJwtTokenAsync(response.AccessToken)).Should().BeTrue();
        }

        /// <summary>
        /// Failure case: when ModelState is invalid (e.g. a missing required body field),
        /// the controller should return a 400 Bad Request with a Yggdrasil error payload.
        /// </summary>
        [Fact(DisplayName = "Failure: Model state is invalid")]
        public async Task ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("username", "The Username field is required.");

            IActionResult result = await _controller.Authenticate(new YigLoginRequest
            {
                Agent = new YigAgent { Name = "Minecraft", Version = 1 },
                Username = null!,
                Password = _passwordMock
            });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.BadRequest));
            error.ErrorMessage.Should().Be("The Username field is required.");
        }

        /// <summary>
        /// Failure case: when the legacy Yggdrasil endpoints are switched off by configuration,
        /// the controller should refuse the request with 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Legacy authentication is disabled")]
        public async Task ReturnsForbidden_WhenLegacyAuthDisabled()
        {
            AppConfiguration.Yggdrasil.EnableLegacyAuth = false;

            IActionResult result = await _controller.Authenticate(new YigLoginRequest
            {
                Agent = new YigAgent { Name = "Minecraft", Version = 1 },
                Username = _userMock.UserName,
                Password = _passwordMock
            });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.Forbidden));
            error.ErrorMessage.Should().Be("Legacy authentication is disabled on this server.");
        }

        /// <summary>
        /// Failure case: when agent enforcement is enabled and the request announces a different agent
        /// than the configured one, the controller should reject the request with 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Enforced agent does not match")]
        public async Task ReturnsBadRequest_WhenAgentDoesNotMatch()
        {
            AppConfiguration.Yggdrasil.EnforceAgent = true;
            AppConfiguration.Yggdrasil.Agent.Name = "Minecraft";
            AppConfiguration.Yggdrasil.Agent.Version = 1;

            IActionResult result = await _controller.Authenticate(new YigLoginRequest
            {
                Agent = new YigAgent { Name = "SomeOtherClient", Version = 99 },
                Username = _userMock.UserName,
                Password = _passwordMock
            });

            TestHelper.TestResponse(result, HttpStatusCode.BadRequest, "Invalid request.", _testOutputHelper);
        }

        /// <summary>
        /// Failure case: two-factor accounts cannot complete the legacy flow, so the controller answers
        /// with 424 Failed Dependency explaining that 2FA is unsupported on Yggdrasil.
        /// </summary>
        [Fact(DisplayName = "Failure: Two-factor authentication is required")]
        public async Task ReturnsFailedDependency_WhenTwoFactorRequired()
        {
            _userMock.TwoFactorEnabled = true;
            var user = await CreateUserAsync(_controller);
            await _userManager.GenerateTwoFactorTokenAsync(user);

            IActionResult result = await _controller.Authenticate(new YigLoginRequest
            {
                Agent = new YigAgent { Name = "Minecraft", Version = 1 },
                Username = user.UserName,
                Password = _passwordMock
            });

            TestHelper.TestResponse(result, HttpStatusCode.FailedDependency,
                "Two factor authentication is required. It is unsupported on yggdrasil.", _testOutputHelper);
        }

        /// <summary>
        /// Failure case: unknown username or wrong password should return 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid credentials")]
        public async Task ReturnsBadRequest_WhenCredentialsInvalid()
        {
            await CreateUserAsync(_controller);

            IActionResult result = await _controller.Authenticate(new YigLoginRequest
            {
                Agent = new YigAgent { Name = "Minecraft", Version = 1 },
                Username = _userMock.UserName,
                Password = "This%Valid_And#Pass%mock-2027"
            });

            TestHelper.TestResponse(result, HttpStatusCode.BadRequest, "Invalid credentials.", _testOutputHelper);
        }

        /// <summary>
        /// Failure case: a username that does not exist should return 400 Bad Request,
        /// indistinguishable from a wrong password.
        /// </summary>
        [Fact(DisplayName = "Failure: Unknown user")]
        public async Task ReturnsBadRequest_WhenUserDoesNotExist()
        {
            IActionResult result = await _controller.Authenticate(new YigLoginRequest
            {
                Agent = new YigAgent { Name = "Minecraft", Version = 1 },
                Username = "NoSuchUser",
                Password = _passwordMock
            });

            TestHelper.TestResponse(result, HttpStatusCode.BadRequest, "Invalid credentials.", _testOutputHelper);
        }
    }

    /// <summary>
    /// Tests for the refresh endpoint which rotates an access token of an active play session.
    /// </summary>
    public class RefreshTests : AuthControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public RefreshTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: refreshing a valid session issues a new raw access token, stores only its hash,
        /// keeps the session bound to the same client token and revokes the previous session.
        /// </summary>
        [Fact(DisplayName = "Success: Refresh issues a new token and revokes the old session")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller);
            string clientToken = Guid.NewGuid().ToString();
            string oldRawToken = TokenHelper.GenerateToken();
            var oldSession = await AddPlaySessionAsync(user, oldRawToken, clientToken);

            IActionResult result = await _controller.Refresh(new YigRefreshRequest
            {
                AccessToken = oldRawToken,
                ClientToken = clientToken,
                SelectedProfile = new YigProfileBody { Id = user.Id, Name = user.UserName }
            });

            var response = Deserialize<YigLoginResponse>(GetContent(result));
            response.AccessToken.Should().NotBeNullOrEmpty();
            response.AccessToken.Should().NotBe(oldRawToken, "refresh must rotate the access token");
            response.ClientToken.Should().Be(clientToken);
            response.SelectedProfile.Id.Should().Be(user.Id);
            _testOutputHelper.WriteLine("Result: " + GetContent(result));

            var sessions = await _userStore.UserPlaySessions.QueryAsync(x => x.UserId == user.Id, TestContext.Current.CancellationToken);
            var session = sessions.Should().ContainSingle().Subject;
            session.Token.Should().Be(TestHelper.HashToken(response.AccessToken, AppConfiguration));
            session.Token.Should().NotBe(response.AccessToken, "the raw play session token must never be persisted");
            session.ClientId.Should().Be(clientToken);

            (await _userStore.UserPlaySessions.FindAsync(x => x.Id == oldSession.Id, TestContext.Current.CancellationToken))
                .Should().BeNull("the refreshed session must revoke the previous one");
        }

        /// <summary>
        /// Failure case: when ModelState is invalid the controller should return 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Model state is invalid")]
        public async Task ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("accessToken", "The AccessToken field is required.");

            IActionResult result = await _controller.Refresh(new YigRefreshRequest
            {
                AccessToken = null!,
                SelectedProfile = new YigProfileBody { Id = Guid.NewGuid().ToString(), Name = "testuser" }
            });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.BadRequest));
            error.ErrorMessage.Should().Be("The AccessToken field is required.");
        }

        /// <summary>
        /// Failure case: when the legacy Yggdrasil endpoints are switched off by configuration,
        /// the controller should refuse the request with 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Legacy authentication is disabled")]
        public async Task ReturnsForbidden_WhenLegacyAuthDisabled()
        {
            AppConfiguration.Yggdrasil.EnableLegacyAuth = false;

            IActionResult result = await _controller.Refresh(new YigRefreshRequest
            {
                AccessToken = TokenHelper.GenerateToken(),
                SelectedProfile = new YigProfileBody { Id = Guid.NewGuid().ToString(), Name = "testuser" }
            });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.Forbidden));
            error.ErrorMessage.Should().Be("Legacy authentication is disabled on this server.");
        }

        /// <summary>
        /// Failure case: an access token that matches no play session should be rejected with 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Unknown access token")]
        public async Task ReturnsForbidden_WhenTokenIsUnknown()
        {
            await CreateUserAsync(_controller);

            IActionResult result = await _controller.Refresh(new YigRefreshRequest
            {
                AccessToken = TokenHelper.GenerateToken(),
                ClientToken = Guid.NewGuid().ToString(),
                SelectedProfile = new YigProfileBody { Id = _userMock.Id, Name = _userMock.UserName }
            });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.Forbidden));
            error.ErrorMessage.Should().Be("Not allowed.");
        }

        /// <summary>
        /// Failure case: a valid access token presented together with a different client token than the
        /// one the session was created with must be rejected with 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Client token does not match")]
        public async Task ReturnsForbidden_WhenClientTokenDoesNotMatch()
        {
            var user = await CreateUserAsync(_controller);
            string rawToken = TokenHelper.GenerateToken();
            await AddPlaySessionAsync(user, rawToken, Guid.NewGuid().ToString());

            IActionResult result = await _controller.Refresh(new YigRefreshRequest
            {
                AccessToken = rawToken,
                ClientToken = Guid.NewGuid().ToString(),
                SelectedProfile = new YigProfileBody { Id = user.Id, Name = user.UserName }
            });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.Forbidden));
            error.ErrorMessage.Should().Be("Not allowed.");
        }

        /// <summary>
        /// Failure case: an expired play session cannot be refreshed, so the controller answers 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Expired session")]
        public async Task ReturnsForbidden_WhenSessionExpired()
        {
            var user = await CreateUserAsync(_controller);
            string clientToken = Guid.NewGuid().ToString();
            await AddPlaySessionAsync(user, TokenHelper.GenerateToken(), clientToken, DateTimeOffset.UtcNow.AddMinutes(-30));

            IActionResult result = await _controller.Refresh(new YigRefreshRequest
            {
                AccessToken = TokenHelper.GenerateToken(),
                ClientToken = clientToken,
                SelectedProfile = new YigProfileBody { Id = user.Id, Name = user.UserName }
            });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.Forbidden));
            error.ErrorMessage.Should().Be("Not allowed.");
        }

        /// <summary>
        /// Failure case: a play session whose owner no longer exists must not yield a refreshed token.
        /// </summary>
        [Fact(DisplayName = "Failure: Session owner does not exist")]
        public async Task ReturnsForbidden_WhenUserDoesNotExist()
        {
            string clientToken = Guid.NewGuid().ToString();
            string rawToken = TokenHelper.GenerateToken();
            await AddPlaySessionAsync(new CustomUser { Id = Guid.NewGuid().ToString() }, rawToken, clientToken);

            IActionResult result = await _controller.Refresh(new YigRefreshRequest
            {
                AccessToken = rawToken,
                ClientToken = clientToken,
                SelectedProfile = new YigProfileBody { Id = Guid.NewGuid().ToString(), Name = "ghost" }
            });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.Forbidden));
            error.ErrorMessage.Should().Be("Not allowed.");
        }
    }

    /// <summary>
    /// Tests for the validate endpoint which checks whether an access token is still usable.
    /// </summary>
    public class ValidateTests : AuthControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public ValidateTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: a play session matching both the access token and the client token is valid,
        /// so the controller returns 204 No Content.
        /// </summary>
        [Fact(DisplayName = "Success: Valid access token returns 204")]
        public async Task ReturnsNoContent_WhenTokenIsValid()
        {
            var user = await CreateUserAsync(_controller);
            string clientToken = Guid.NewGuid().ToString();
            string rawToken = TokenHelper.GenerateToken();
            await AddPlaySessionAsync(user, rawToken, clientToken);

            IActionResult result = await _controller.Validate(new YigValidateRequest
            {
                AccessToken = rawToken,
                ClientToken = clientToken
            });

            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult!.StatusCode.Should().Be(204);
        }

        /// <summary>
        /// Failure case: when ModelState is invalid the controller should return 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Model state is invalid")]
        public async Task ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("accessToken", "The AccessToken field is required.");

            IActionResult result = await _controller.Validate(new YigValidateRequest { AccessToken = null! });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.BadRequest));
            error.ErrorMessage.Should().Be("The AccessToken field is required.");
        }

        /// <summary>
        /// Failure case: when the legacy Yggdrasil endpoints are switched off by configuration,
        /// the controller should refuse the request with 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Legacy authentication is disabled")]
        public async Task ReturnsForbidden_WhenLegacyAuthDisabled()
        {
            AppConfiguration.Yggdrasil.EnableLegacyAuth = false;

            IActionResult result = await _controller.Validate(new YigValidateRequest
            {
                AccessToken = TokenHelper.GenerateToken()
            });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.Forbidden));
            error.ErrorMessage.Should().Be("Legacy authentication is disabled on this server.");
        }

        /// <summary>
        /// Failure case: an access token that matches no play session should be rejected with 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Unknown access token")]
        public async Task ReturnsForbidden_WhenTokenIsUnknown()
        {
            await CreateUserAsync(_controller);

            IActionResult result = await _controller.Validate(new YigValidateRequest
            {
                AccessToken = TokenHelper.GenerateToken(),
                ClientToken = Guid.NewGuid().ToString()
            });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.Forbidden));
            error.ErrorMessage.Should().Be("Not allowed.");
        }

        /// <summary>
        /// Failure case: a valid access token paired with a different client token than the session was
        /// created with is not considered valid, so the controller answers 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Client token does not match")]
        public async Task ReturnsForbidden_WhenClientTokenDoesNotMatch()
        {
            var user = await CreateUserAsync(_controller);
            string rawToken = TokenHelper.GenerateToken();
            await AddPlaySessionAsync(user, rawToken, Guid.NewGuid().ToString());

            IActionResult result = await _controller.Validate(new YigValidateRequest
            {
                AccessToken = rawToken,
                ClientToken = Guid.NewGuid().ToString()
            });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.Forbidden));
            error.ErrorMessage.Should().Be("Not allowed.");
        }
    }

    /// <summary>
    /// Tests for the invalidate endpoint which revokes an access token.
    /// </summary>
    public class InvalidateTests : AuthControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public InvalidateTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: invalidating a valid access token returns 204 No Content and removes the
        /// underlying play session from the store.
        /// </summary>
        [Fact(DisplayName = "Success: Invalidate revokes the session")]
        public async Task ReturnsNoContent_WhenTokenIsValid()
        {
            var user = await CreateUserAsync(_controller);
            string clientToken = Guid.NewGuid().ToString();
            string rawToken = TokenHelper.GenerateToken();
            var session = await AddPlaySessionAsync(user, rawToken, clientToken);

            IActionResult result = await _controller.Invalidate(new YigInvalidateRequest
            {
                AccessToken = rawToken,
                ClientToken = clientToken
            });

            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult!.StatusCode.Should().Be(204);

            (await _userStore.UserPlaySessions.FindAsync(x => x.Id == session.Id, TestContext.Current.CancellationToken))
                .Should().BeNull("invalidate should revoke the play session");
            var remaining = await _userStore.UserPlaySessions.QueryAsync(x => x.UserId == user.Id, TestContext.Current.CancellationToken);
            remaining.Should().BeEmpty();
        }

        /// <summary>
        /// Failure case: when ModelState is invalid the controller should return 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Model state is invalid")]
        public async Task ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("accessToken", "The AccessToken field is required.");

            IActionResult result = await _controller.Invalidate(new YigInvalidateRequest { AccessToken = null! });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.BadRequest));
            error.ErrorMessage.Should().Be("The AccessToken field is required.");
        }

        /// <summary>
        /// Failure case: when the legacy Yggdrasil endpoints are switched off by configuration,
        /// the controller should refuse the request with 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Legacy authentication is disabled")]
        public async Task ReturnsForbidden_WhenLegacyAuthDisabled()
        {
            AppConfiguration.Yggdrasil.EnableLegacyAuth = false;

            IActionResult result = await _controller.Invalidate(new YigInvalidateRequest
            {
                AccessToken = TokenHelper.GenerateToken()
            });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.Forbidden));
            error.ErrorMessage.Should().Be("Legacy authentication is disabled on this server.");
        }

        /// <summary>
        /// Failure case: an access token that matches no play session should be rejected with 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Unknown access token")]
        public async Task ReturnsForbidden_WhenTokenIsUnknown()
        {
            await CreateUserAsync(_controller);

            IActionResult result = await _controller.Invalidate(new YigInvalidateRequest
            {
                AccessToken = TokenHelper.GenerateToken(),
                ClientToken = Guid.NewGuid().ToString()
            });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.Forbidden));
            error.ErrorMessage.Should().Be("Not allowed.");
        }
    }

    /// <summary>
    /// Tests for the signout endpoint which revokes every play session of the signed-in account.
    /// </summary>
    public class SignoutTests : AuthControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public SignoutTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: signing out with valid credentials returns 204 No Content and removes every
        /// play session of that user while leaving the sessions of other users untouched.
        /// </summary>
        [Fact(DisplayName = "Success: Signout revokes all sessions of the user")]
        public async Task ReturnsNoContent_WhenCredentialsValid()
        {
            var user = await CreateUserAsync(_controller);
            var otherUser = await CreateUserAsync(_controller, _userMock2);
            await AddPlaySessionAsync(user, TokenHelper.GenerateToken());
            await AddPlaySessionAsync(user, TokenHelper.GenerateToken());
            await AddPlaySessionAsync(otherUser, TokenHelper.GenerateToken());

            IActionResult result = await _controller.Signout(new YigSignoutRequest
            {
                Username = user.UserName,
                Password = _passwordMock
            });

            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult!.StatusCode.Should().Be(204);

            var remaining = await _userStore.UserPlaySessions.QueryAsync(x => x.UserId == user.Id, TestContext.Current.CancellationToken);
            remaining.Should().BeEmpty("signout should revoke every play session of the user");
            var otherRemaining = await _userStore.UserPlaySessions.QueryAsync(x => x.UserId == otherUser.Id, TestContext.Current.CancellationToken);
            otherRemaining.Should().HaveCount(1, "signout must not revoke sessions of other users");
        }

        /// <summary>
        /// Failure case: when ModelState is invalid the controller should return 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Model state is invalid")]
        public async Task ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("username", "The Username field is required.");

            IActionResult result = await _controller.Signout(new YigSignoutRequest
            {
                Username = null!,
                Password = _passwordMock
            });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.BadRequest));
            error.ErrorMessage.Should().Be("The Username field is required.");
        }

        /// <summary>
        /// Failure case: when the legacy Yggdrasil endpoints are switched off by configuration,
        /// the controller should refuse the request with 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Legacy authentication is disabled")]
        public async Task ReturnsForbidden_WhenLegacyAuthDisabled()
        {
            AppConfiguration.Yggdrasil.EnableLegacyAuth = false;

            IActionResult result = await _controller.Signout(new YigSignoutRequest
            {
                Username = _userMock.UserName,
                Password = _passwordMock
            });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.Forbidden));
            error.ErrorMessage.Should().Be("Legacy authentication is disabled on this server.");
        }

        /// <summary>
        /// Failure case: signing out with wrong credentials returns 401 Unauthorized and must not
        /// revoke any session.
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid credentials")]
        public async Task ReturnsUnauthorized_WhenCredentialsInvalid()
        {
            var user = await CreateUserAsync(_controller);
            await AddPlaySessionAsync(user, TokenHelper.GenerateToken());

            IActionResult result = await _controller.Signout(new YigSignoutRequest
            {
                Username = user.UserName,
                Password = "This%Valid_And#Pass%mock-2027"
            });

            var error = DeserializeYigError(GetContent(result));
            error.Error.Should().Be(nameof(HttpStatusCode.Unauthorized));
            error.ErrorMessage.Should().Be("Invalid credentials.");

            var remaining = await _userStore.UserPlaySessions.QueryAsync(x => x.UserId == user.Id, TestContext.Current.CancellationToken);
            remaining.Should().HaveCount(1, "a rejected signout must not revoke anything");
        }
    }

    /// <summary>
    /// Stores a play session for the given user using the keyed hash of the raw access token,
    /// which is the form the controller matches against the store.
    /// </summary>
    /// <param name="user">The user the play session belongs to.</param>
    /// <param name="rawToken">The raw access token as it would be handed to a client.</param>
    /// <param name="clientToken">The client token the session is bound to.</param>
    /// <param name="expiresAt">When the session expires; defaults to 30 minutes from now.</param>
    /// <returns>The stored play session.</returns>
    private async Task<UserPlaySession> AddPlaySessionAsync(CustomUser user, string rawToken, string? clientToken = null, DateTimeOffset? expiresAt = null)
    {
        return await _userStore.UserPlaySessions.AddAsync(new UserPlaySession
        {
            UserId = user.Id,
            UserIp = TestHelper.IpAddress,
            ClientId = clientToken,
            Token = TestHelper.HashToken(rawToken, AppConfiguration),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(30),
        }, true, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Returns the raw JSON body of a <see cref="ContentResult"/>, failing the test when it is missing.
    /// </summary>
    /// <param name="result">The action result returned by the controller under test.</param>
    /// <returns>The raw response body.</returns>
    private static string GetContent(IActionResult result)
    {
        result.Should().BeOfType<ContentResult>();
        var contentResult = result as ContentResult;
        contentResult.Should().NotBeNull();
        contentResult!.Content.Should().NotBeNullOrEmpty();
        return contentResult.Content!;
    }

    /// <summary>
    /// Deserializes a Yggdrasil error body, failing the test when the payload is missing.
    /// </summary>
    /// <param name="content">The raw JSON body of the response.</param>
    /// <returns>The deserialized error response.</returns>
    private YigErrorResponse DeserializeYigError(string content)
    {
        var errorResponse = JsonConvert.DeserializeObject<YigErrorResponse>(content);
        errorResponse.Should().NotBeNull();
        _testOutputHelper.WriteLine($"Result: \n{errorResponse!.Error}\n{errorResponse.ErrorMessage}\n{errorResponse.Cause}");
        return errorResponse;
    }

    /// <summary>
    /// Deserializes a controller response body, failing the test when the payload is missing.
    /// </summary>
    /// <typeparam name="T">The expected response type.</typeparam>
    /// <param name="content">The raw JSON body of the response.</param>
    /// <returns>The deserialized response.</returns>
    private T Deserialize<T>(string content)
    {
        var deserialized = JsonConvert.DeserializeObject<T>(content);
        deserialized.Should().NotBeNull();
        return deserialized!;
    }
}
