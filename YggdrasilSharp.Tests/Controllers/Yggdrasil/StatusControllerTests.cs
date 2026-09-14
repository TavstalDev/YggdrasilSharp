using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tavstal.YggdrasilSharp.Controllers.Yggdrasil;
using Xunit;

namespace Tavstal.YggdrasilSharp.Tests.Controllers.Yggdrasil;

/// <summary>
/// Unit tests for <see cref="StatusController"/>.
/// </summary>
public class StatusControllerTests : ControllerTestBase
{
    private readonly Mock<ILogger<StatusController>> _loggerMock = new();
    private readonly StatusController _controller;
    
    /// <summary>
    /// Initializes a new instance of <see cref="StatusControllerTests"/>.
    /// Constructs the <see cref="StatusController"/> with the test database, custom user manager, memory cache service, mock logger and test settings.
    /// Also assigns a <see cref="Microsoft.AspNetCore.Mvc.ControllerContext"/> with a test <see cref="Microsoft.AspNetCore.Http.HttpContext"/>.
    /// </summary>
    /// <param name="testOutputHelper">XUnit test output helper forwarded to the base class for logging test output.</param>
    public StatusControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _controller = new StatusController(_loggerMock.Object, _userStore, _memoryCacheService, _settings);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }

    /// <summary>
    /// Tests for the root endpoint which returns server information.
    /// </summary>
    public class RootTests : StatusControllerTests
    {
        public RootTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: calling <c>Root</c> should return a <see cref="ContentResult"/> containing server information.
        /// </summary>
        [Fact(DisplayName = "Success: Get server information")]
        public async Task ReturnsOk()
        {
            var result = _controller.Root();
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + contentResult.Content);
        }
    }
    
    /// <summary>
    /// Tests for the status endpoint which returns the current server status.
    /// </summary>
    public class StatusTests : StatusControllerTests
    {
        public StatusTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: calling <c>Status</c> should return a <see cref="ContentResult"/> describing server status.
        /// </summary>
        [Fact(DisplayName = "Success: Get server status")]
        public async Task ReturnsOk()
        {
            var result = await _controller.Status();
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + contentResult.Content);
        }
    }
    
    /// <summary>
    /// Tests for the public keys endpoint which exposes server public keys.
    /// </summary>
    public class PublicKeysTests : StatusControllerTests
    {
        public PublicKeysTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: calling <c>GetPublicKeys</c> should return a <see cref="ContentResult"/> containing the public keys.
        /// </summary>
        [Fact(DisplayName = "Success: Get public-keys")]
        public async Task ReturnsOk()
        {
            var result = _controller.GetPublicKeys();
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + contentResult.Content);
        }
    }
}