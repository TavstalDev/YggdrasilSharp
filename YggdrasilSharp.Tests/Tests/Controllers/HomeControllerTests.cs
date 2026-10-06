using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tavstal.YggdrasilSharp.Controllers;
using Tavstal.YggdrasilSharp.Tests.Models;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="HomeController"/>.
/// </summary>
public class HomeControllerTests : ControllerTestBase
{
    private readonly HomeController _controller;

    /// <summary>
    /// Creates a new instance of <see cref="HomeControllerTests"/>.
    /// Constructs the <see cref="HomeController"/> with the test user store, mock logger and test settings.
    /// Also assigns a <see cref="Microsoft.AspNetCore.Mvc.ControllerContext"/> with a test <see cref="Microsoft.AspNetCore.Http.HttpContext"/>,
    /// which <c>Index</c> requires in order to write the <c>X-Authlib-Injector-API-Location</c> response header.
    /// </summary>
    /// <param name="testOutputHelper">
    /// xUnit-provided test output helper; useful for writing trace information for failing tests.
    /// </param>
    public HomeControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        var loggerMock = new Mock<ILogger<HomeController>>();
        _controller = new HomeController(loggerMock.Object, _userManager, _userStore, AppConfiguration);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }

    /// <summary>
    /// Verifies that the <see cref="HomeController.Index"/> action returns an HTTP 200 response
    /// with the message "The API is running." as the response value.
    /// </summary>
    [Fact(DisplayName = "Success: The API is running.")]
    public void Index_ReturnsOkWithMessage()
    {
        IActionResult result = _controller.Index();

        result.Should().BeOfType<ObjectResult>();

        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(200);
        objectResult.Value.Should().Be("The API is running.");
        _controllerHttpContext.Response.Headers["X-Authlib-Injector-API-Location"].ToString().Should().Be("/yggdrasil/");
        _testOutputHelper.WriteLine("Index action returned: {0}", objectResult.Value);
    }
}
