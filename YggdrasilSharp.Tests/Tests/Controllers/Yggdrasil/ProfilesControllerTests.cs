using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tavstal.YggdrasilSharp.Controllers.Yggdrasil;
using Tavstal.YggdrasilSharp.Tests.Models;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Controllers.Yggdrasil;

/// <summary>
/// Tests for <see cref="ProfilesController"/>.
/// </summary>
public class ProfilesControllerTests : ControllerTestBase
{
    private readonly Mock<ILogger<ProfilesController>> _loggerMock = new();
    private readonly ProfilesController _controller;
    
    /// <summary>
    /// Constructs a new <see cref="ProfilesControllerTests"/> instance.
    /// Sets up the controller with a mock logger, test database and settings, and assigns a test HttpContext.
    /// </summary>
    /// <param name="testOutputHelper">XUnit test output helper forwarded to the base test class.</param>
    public ProfilesControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _controller = new ProfilesController(_loggerMock.Object, _userStore, AppConfiguration);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }

    /// <summary>
    /// Success: verifies that the controller returns a <see cref="ContentResult"/> containing profiles
    /// when valid existing usernames are provided.
    /// </summary>
    [Fact(DisplayName = "Success: Retrieve profiles")]
    public async Task ReturnsOk()
    {
        var user1 = await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);
        var user2 = await _userStore.AddUserAsync(_userMock2, true, TestContext.Current.CancellationToken);
        
        var result = await _controller.MinecraftProfile([user1.UserName, user2.UserName]);
        result.Should().BeOfType<ContentResult>();
        var contentResult = result as ContentResult;
        contentResult.Should().NotBeNull();
        _testOutputHelper.WriteLine("Result: " + contentResult.Content);
    }
    
    /// <summary>
    /// Failure: verifies that the controller returns a 404 <see cref="ObjectResult"/>
    /// when requesting profiles for usernames that do not exist in the database.
    /// </summary>
    [Fact(DisplayName = "Failure: Profiles not found")]
    public async Task ReturnsNotFound()
    {
        var result = await _controller.MinecraftProfile(["user1", "user2"]);
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(404);
        _testOutputHelper.WriteLine("Result: " + objectResult.Value);
    }
}