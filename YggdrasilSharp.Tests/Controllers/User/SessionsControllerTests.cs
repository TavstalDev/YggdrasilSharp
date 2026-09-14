using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tavstal.YggdrasilSharp.Controllers.User;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Xunit;

namespace Tavstal.YggdrasilSharp.Tests.Controllers.User;

/// <summary>
/// Tests for <see cref="SessionsController"/>. 
/// </summary>
public class SessionsControllerTests : ControllerTestBase
{
    private readonly Mock<ILogger<SessionsController>> _loggerMock = new();
    private readonly SessionsController _controller;
    
    /// <summary>
    /// Initializes the test fixture and creates a controller instance using the test helpers
    /// provided by <see cref="ControllerTestBase"/>.
    /// </summary>
    /// <param name="testOutputHelper">xUnit test output helper used to capture test logs.</param>
    public SessionsControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _controller = new SessionsController(_loggerMock.Object, _userManager, _dbContext, _userStore, _settings);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }
    
    /// <summary>
    /// Tests related to retrieving the current user's sessions.
    /// </summary>
    public class GetSessionsTests : SessionsControllerTests
    {
        public GetSessionsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: When a user is authenticated and has sessions, the controller should
        /// return a <see cref="ContentResult"/> containing the session list.
        /// </summary>
        [Fact(DisplayName = "Success: Return sessions")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller);
            IActionResult result = await _controller.GetSessions();
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + contentResult.Content);
        }
        
        /// <summary>
        /// Failure case: When no user is authenticated, the controller should return 401 Unauthorized.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized()
        {
            IActionResult result = await _controller.GetSessions();
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Tests for revoking a single session belonging to the current user.
    /// </summary>
    public class RevokeSessionTests : SessionsControllerTests
    {
        public RevokeSessionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: Authenticated user revokes one of their sessions. Expect HTTP 200.
        /// </summary>
        [Fact(DisplayName = "Success: Revoke session")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller);
            var session = await _userStore.UserLogins.AddAsync(new CustomUserLogin
            {
                UserId = user.Id,
                LoginProvider = "TestProvider",
                ProviderKey = "TestKey",
                ProviderDisplayName = "Test Session",
                CreateDate = DateTime.UtcNow,
                ExpireDate = DateTime.UtcNow.AddDays(7),
                OperatingSystem = "Test OS",
                Browser = "Test Browser",
                City = "Test City",
                Country = "Test Country"
            }, true, TestContext.Current.CancellationToken);
            
            
            IActionResult result = await _controller.RevokeSession(session.Id);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: Model validation fails because the sessionId is missing/invalid.
        /// </summary>
        [Fact(DisplayName = "Failure: Missing sessionId")]
        public async Task ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("sessionId", "Invalid session ID");
            
            IActionResult result = await _controller.RevokeSession(0);
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: Unauthenticated request to revoke a session should return HTTP 401 Unauthorized.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized()
        {
            IActionResult result = await _controller.RevokeSession(1);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: Attempt to revoke a non-existent session should return HTTP 404 Not Found.
        /// </summary>
        [Fact(DisplayName = "Failure: Session not found")]
        public async Task ReturnsNotFound()
        {
            await CreateUserAsync(_controller);
            IActionResult result = await _controller.RevokeSession(9999);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Tests for revoking all sessions for the current user.
    /// </summary>
    public class RevokeAllSessionsTests : SessionsControllerTests
    {
        public RevokeAllSessionsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: Authenticated user revokes all their sessions. Expect HTTP 200.
        /// </summary>
        [Fact(DisplayName = "Success: Revoke all sessions")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller);
            IActionResult result = await _controller.RevokeAllSessions();

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: Unauthenticated request to revoke all sessions should return HTTP 401 Unauthorized.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized()
        {
            IActionResult result = await _controller.RevokeAllSessions();

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Tests for admin endpoints that retrieve sessions for another user.
    /// </summary>
    public class GetSessionAdmin : SessionsControllerTests
    {
        public GetSessionAdmin(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: Admin user requests sessions for another user. Expect <see cref="ContentResult"/>.
        /// </summary>
        [Fact(DisplayName = "Success: Return sessions")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            IActionResult result = await _controller.GetSessionsAdmin(user.Id);
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + contentResult.Content);
        }
        
        /// <summary>
        /// Failure case: Caller does not have enough permissions to access another user's sessions.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller, givePermissions: false);
            IActionResult result = await _controller.GetSessionsAdmin(user.Id);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Admin tests for revoking a specific session belonging to another user.
    /// </summary>
    public class RevokeSessionAdmin : SessionsControllerTests
    {
        public RevokeSessionAdmin(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: Admin revokes one session of another user; expect HTTP 200.
        /// </summary>
        [Fact(DisplayName = "Success: Revoke session")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            var session = await _userStore.UserLogins.AddAsync(new CustomUserLogin
            {
                UserId = user.Id,
                LoginProvider = "TestProvider",
                ProviderKey = "TestKey",
                ProviderDisplayName = "Test Session",
                CreateDate = DateTime.UtcNow,
                ExpireDate = DateTime.UtcNow.AddDays(7),
                OperatingSystem = "Test OS",
                Browser = "Test Browser",
                City = "Test City",
                Country = "Test Country"
            }, true, TestContext.Current.CancellationToken);
            
            
            IActionResult result = await _controller.RevokeSessionAdmin(user.Id, session.Id);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: Model state invalid because sessionId is missing; expect HTTP 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Missing sessionId")]
        public async Task ReturnsBadRequest()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            _controller.ModelState.AddModelError("sessionId", "Invalid session ID");
            
            IActionResult result = await _controller.RevokeSessionAdmin(user.Id, 0);
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: Caller does not have admin permissions; expect HTTP 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden()
        {
            var user =  await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller, givePermissions: false);
            IActionResult result = await _controller.RevokeSessionAdmin(user.Id, 1);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: Target session does not exist; expect HTTP 404 Not Found.
        /// </summary>
        [Fact(DisplayName = "Failure: Session not found")]
        public async Task ReturnsNotFound()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            IActionResult result = await _controller.RevokeSessionAdmin(user.Id, 9999);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Admin tests for revoking all sessions of another user.
    /// </summary>
    public class RevokeAllSessionsAdmin : SessionsControllerTests
    {
        public RevokeAllSessionsAdmin(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: Admin revokes all sessions of the specified user; expect HTTP 200.
        /// </summary>
        [Fact(DisplayName = "Success: Revoke all sessions")]
        public async Task ReturnsOk()
        {
            var user =  await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            IActionResult result = await _controller.RevokeAllSessionsAdmin(user.Id);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: Caller lacks admin permissions; expect HTTP 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden()
        {
            var user =  await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller, givePermissions: false);
            IActionResult result = await _controller.RevokeAllSessionsAdmin(user.Id);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
}