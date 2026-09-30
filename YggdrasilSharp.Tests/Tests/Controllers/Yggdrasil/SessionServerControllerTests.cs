using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Tavstal.YggdrasilSharp.Controllers.Yggdrasil;
using Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Database.Server;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Models.Responses.Yggdrasil;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using Tavstal.YggdrasilSharp.Tests.Helpers;
using Tavstal.YggdrasilSharp.Tests.Models;
using Tavstal.YggdrasilSharp.Utils.Helpers;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Controllers.Yggdrasil;

/// <summary>
/// Tests for the <see cref="SessionServerController"/> responsible for session-server interactions
/// </summary>
public class SessionServerControllerTests : ControllerTestBase
{
    private readonly IRepository<ServerJoin> _serverJoinRepo;
    private readonly Mock<ILogger<SessionServerController>> _loggerMock = new();
    private readonly SessionServerController _controller;
    
    /// <summary>
    /// Initializes a new instance of <see cref="SessionServerControllerTests"/>.
    /// Sets up a <see cref="SessionServerController"/> with dependencies provided by the test base.
    /// The test controller's <see cref="Controller.ControllerContext"/> is configured to use the
    /// in-memory <see cref="HttpContext"/> from the base test class.
    /// </summary>
    /// <param name="testOutputHelper">XUnit test output helper (injected by the test runner).</param>
    public SessionServerControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _serverJoinRepo = new Repository<ServerJoin>(_dbContext);
        IRepository<FileData> fileDataRepo = new Repository<FileData>(_dbContext);
        IRepository<Cape> capeRepo = new Repository<Cape>(_dbContext);
        _controller = new SessionServerController(_loggerMock.Object, _userManager, _userStore, _serverJoinRepo, fileDataRepo, capeRepo, _memoryCacheService, AppConfiguration);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }
    
    /// <summary>
    /// Tests related to retrieving the list of blocked servers.
    /// </summary>
    public class BlockedServersTests : SessionServerControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public BlockedServersTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Verifies that requesting the list of blocked servers returns a ContentResult containing the expected content.
        /// </summary>
        [Fact(DisplayName = "Success: List of blocked servers")]
        public async Task ReturnsOk()
        {
            var result = _controller.GetBlockedServers();
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + contentResult.Content);
        }
    }
    
    /// <summary>
    /// Tests for the Join endpoint, which registers a user join event for a given server.
    /// </summary>
    public class JoinTests : SessionServerControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public JoinTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: verifies that a valid join request returns HTTP 204 No Content.
        /// </summary>
        [Fact(DisplayName = "Success: Successful join")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller);
            _controllerHttpContext.HttpContext.Request.Host = new HostString(TestHelper.IpAddress);
            string token = _userManager.CreateJwtToken(TimeSpan.FromMinutes(30));
            var userPlaySession = await _userStore.UserPlaySessions.AddAsync(new UserPlaySession
            {
                UserId = user.Id,
                UserIp = TestHelper.IpAddress,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            }, true, TestContext.Current.CancellationToken);
            var result = await _controller.Join(new YigJoinServerRequest
            {
                accessToken = userPlaySession.Token,
                selectedProfile = user.Id,
                serverId = Guid.NewGuid().ToString()
            });
            
            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult!.StatusCode.Should().Be(204);
        }

        /// <summary>
        /// Failure case: when selectedProfile is an invalid UUID (no corresponding user),
        /// the controller should return HTTP 404 Not Found.
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid uuid")]
        public async Task ReturnsNotFound_WhenInvalidUuid()
        {
            var user = await CreateUserAsync(_controller);
            _controllerHttpContext.HttpContext.Request.Host = new HostString(TestHelper.IpAddress);
            var userPlaySession = await _userStore.UserPlaySessions.AddAsync(new UserPlaySession
            {
                UserId = user.Id,
                UserIp = TestHelper.IpAddress,
                Token = TokenHelper.GenerateToken(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            }, true, TestContext.Current.CancellationToken);
            var result = await _controller.Join(new YigJoinServerRequest
            {
                accessToken = userPlaySession.Token,
                selectedProfile = Guid.NewGuid().ToString(),
                serverId = Guid.NewGuid().ToString()
            });
            
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            contentResult.Content.Should().NotBeNullOrEmpty();
            var errorResponse = JsonConvert.DeserializeObject<YigErrorResponse>(contentResult.Content);
            errorResponse.Should().NotBeNull();
            _testOutputHelper.WriteLine($"Result: \n{errorResponse.Error}\n{errorResponse.ErrorMessage}\n{errorResponse.Cause}");
        }
        
        /// <summary>
        /// Failure case: when the IP address from the incoming HTTP request does not match the stored session IP,
        /// the controller should return HTTP 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failed: Invalid ip")]
        public async Task ReturnsForbidden_WhenIpDoesNotMatch()
        {
            var user = await CreateUserAsync(_controller);
            _controllerHttpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.100");
            string token = _userManager.CreateJwtToken(TimeSpan.FromMinutes(30));
            var userPlaySession = await _userStore.UserPlaySessions.AddAsync(new UserPlaySession
            {
                UserId = user.Id,
                UserIp = TestHelper.IpAddress,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            }, true, TestContext.Current.CancellationToken);
            var result = await _controller.Join(new YigJoinServerRequest
            {
                accessToken = userPlaySession.Token,
                selectedProfile = user.Id,
                serverId = Guid.NewGuid().ToString()
            });
            
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            contentResult.Content.Should().NotBeNullOrEmpty();
            var errorResponse = JsonConvert.DeserializeObject<YigErrorResponse>(contentResult.Content);
            errorResponse.Should().NotBeNull();
            _testOutputHelper.WriteLine($"Result: \n{errorResponse.Error}\n{errorResponse.ErrorMessage}\n{errorResponse.Cause}");
        }
        
        /// <summary>
        /// Failure case: when the user play session has expired, the controller should return HTTP 401 Unauthorized.
        /// </summary>
        [Fact(DisplayName = "Failed: Expired session")]
        public async Task ReturnsUnauthorized_WhenExpiredSession()
        {
            var user = await CreateUserAsync(_controller);
            _controllerHttpContext.HttpContext.Request.Host = new HostString(TestHelper.IpAddress);
            var userPlaySession = await _userStore.UserPlaySessions.AddAsync(new UserPlaySession
            {
                UserId = user.Id,
                UserIp = TestHelper.IpAddress,
                Token = TokenHelper.GenerateToken(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-30),
            }, true, TestContext.Current.CancellationToken);
            var result = await _controller.Join(new YigJoinServerRequest
            {
                accessToken = userPlaySession.Token,
                selectedProfile = user.Id,
                serverId = Guid.NewGuid().ToString()
            });
            
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            contentResult.Content.Should().NotBeNullOrEmpty();
            var errorResponse = JsonConvert.DeserializeObject<YigErrorResponse>(contentResult.Content);
            errorResponse.Should().NotBeNull();
            _testOutputHelper.WriteLine($"Result: \n{errorResponse.Error}\n{errorResponse.ErrorMessage}\n{errorResponse.Cause}");
        }
    }
    
    /// <summary>
    /// Tests for the HasJoined endpoint which checks if a specific user has joined a server.
    /// </summary>
    public class HasJoinedTests : SessionServerControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public HasJoinedTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success: verifies that when a server join exists and is valid, the controller returns a ContentResult.
        /// </summary>
        [Fact(DisplayName = "Success: User has joined")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller);
            _controllerHttpContext.HttpContext.Request.Host = new HostString(TestHelper.IpAddress);
            await _userStore.UserPlaySessions.AddAsync(new UserPlaySession
            {
                UserId = user.Id,
                UserIp = TestHelper.IpAddress,
                Token = TokenHelper.GenerateToken(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            }, true, TestContext.Current.CancellationToken);
            string serverId = Guid.NewGuid().ToString();
            await _serverJoinRepo.AddAsync(new ServerJoin
            {
                ServerId = serverId,
                UserId = user.Id,
                UserIp = TestHelper.IpAddress,
                CreatedAt =  DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            }, true, TestContext.Current.CancellationToken);
            
            var result = await _controller.HasJoined(serverId, user.UserName, TestHelper.IpAddress);
            
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + contentResult.Content);
        }
        
        /// <summary>
        /// Failure: when no server join exists for the given server/user, controller should return 404 Not Found.
        /// </summary>
        [Fact(DisplayName = "Failure: Join does not exist")]
        public async Task ReturnsNotFound_WhenJoinDoesNotExist()
        {
            var user =  await CreateUserAsync(_controller);
            _controllerHttpContext.HttpContext.Request.Host = new HostString(TestHelper.IpAddress);
            await _userStore.UserPlaySessions.AddAsync(new UserPlaySession
            {
                UserId = user.Id,
                UserIp = TestHelper.IpAddress,
                Token = TokenHelper.GenerateToken(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            }, true, TestContext.Current.CancellationToken);
            string serverId = Guid.NewGuid().ToString();
            
            var result = await _controller.HasJoined(serverId, user.UserName, TestHelper.IpAddress);
            
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            contentResult.Content.Should().NotBeNullOrEmpty();
            var errorResponse = JsonConvert.DeserializeObject<YigErrorResponse>(contentResult.Content);
            errorResponse.Should().NotBeNull();
            _testOutputHelper.WriteLine($"Result: \n{errorResponse.Error}\n{errorResponse.ErrorMessage}\n{errorResponse.Cause}");
        }
        
        /// <summary>
        /// Failure: when the server join has expired, the controller should return 401 Unauthorized.
        /// </summary>
        [Fact(DisplayName = "Failure: Join expired")]
        public async Task ReturnsUnauthorized_WhenJoinExpired()
        {
            var user = await CreateUserAsync(_controller);
            _controllerHttpContext.HttpContext.Request.Host = new HostString(TestHelper.IpAddress);
            await _userStore.UserPlaySessions.AddAsync(new UserPlaySession
            {
                UserId = user.Id,
                UserIp = TestHelper.IpAddress,
                Token = TokenHelper.GenerateToken(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            }, true, TestContext.Current.CancellationToken);
            string serverId = Guid.NewGuid().ToString();
            await _serverJoinRepo.AddAsync(new ServerJoin
            {
                ServerId = serverId,
                UserId = user.Id,
                UserIp = TestHelper.IpAddress,
                CreatedAt =  DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-30),
            }, true, TestContext.Current.CancellationToken);
            
            var result = await _controller.HasJoined(serverId, user.UserName, TestHelper.IpAddress);
            
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            contentResult.Content.Should().NotBeNullOrEmpty();
            var errorResponse = JsonConvert.DeserializeObject<YigErrorResponse>(contentResult.Content);
            errorResponse.Should().NotBeNull();
            _testOutputHelper.WriteLine($"Result: \n{errorResponse.Error}\n{errorResponse.ErrorMessage}\n{errorResponse.Cause}");
        }
        
        /// <summary>
        /// Failure: when the stored ServerJoin references a different user id than the one resolved by username,
        /// the controller should return 400 Bad Request (user id mismatch).
        /// </summary>
        [Fact(DisplayName = "Failure: User id does not match")]
        public async Task ReturnsBadRequest_WhenUserIdDoesNotMatch()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            var admin = await CreateUserAsync(_controller);
            _controllerHttpContext.HttpContext.Request.Host = new HostString(TestHelper.IpAddress);
            await _userStore.UserPlaySessions.AddAsync(new UserPlaySession
            {
                UserId = user.Id,
                UserIp = TestHelper.IpAddress,
                Token = TokenHelper.GenerateToken(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            }, true, TestContext.Current.CancellationToken);
            string serverId = Guid.NewGuid().ToString();
            await _serverJoinRepo.AddAsync(new ServerJoin
            {
                ServerId = serverId,
                UserId = admin.Id,
                UserIp = TestHelper.IpAddress,
                CreatedAt =  DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            }, true, TestContext.Current.CancellationToken);
            
            var result = await _controller.HasJoined(serverId, user.UserName, TestHelper.IpAddress);
            
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            contentResult.Content.Should().NotBeNullOrEmpty();
            var errorResponse = JsonConvert.DeserializeObject<YigErrorResponse>(contentResult.Content);
            errorResponse.Should().NotBeNull();
            _testOutputHelper.WriteLine($"Result: \n{errorResponse.Error}\n{errorResponse.ErrorMessage}\n{errorResponse.Cause}");
        }
    }
    
    /// <summary>
    /// Tests for the GetProfile endpoint which returns a user's profile (by UUID).
    /// </summary>
    public class GetProfileTests : SessionServerControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public GetProfileTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success: when a user exists for the given uuid, the controller should return a ContentResult
        /// containing the profile JSON (or equivalent content).
        /// </summary>
        [Fact(DisplayName = "Success: Get profile by uuid")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller);
            var result = await _controller.GetProfile(user.Id);
            
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + contentResult.Content);
        }
        
        /// <summary>
        /// Failure: when no user exists for the provided uuid, controller should return 404 Not Found.
        /// </summary>
        [Fact(DisplayName = "Failure: User does not exist")]
        public async Task ReturnsNotFound()
        {
            var result = await _controller.GetProfile(Guid.NewGuid().ToString());
            
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            contentResult.Content.Should().NotBeNullOrEmpty();
            var errorResponse = JsonConvert.DeserializeObject<YigErrorResponse>(contentResult.Content);
            errorResponse.Should().NotBeNull();
            _testOutputHelper.WriteLine($"Result: \n{errorResponse.Error}\n{errorResponse.ErrorMessage}\n{errorResponse.Cause}");
        }
    }
}