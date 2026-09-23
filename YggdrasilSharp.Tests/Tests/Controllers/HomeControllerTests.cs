using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tavstal.YggdrasilSharp.Controllers;
using Tavstal.YggdrasilSharp.Tests.Helpers;
using Tavstal.YggdrasilSharp.Tests.Models;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="HomeController"/>.
/// </summary>
public class HomeControllerTests : ControllerTestBase
{
    /// <summary>
    /// Creates a new instance of <see cref="HomeControllerTests"/>.
    /// </summary>
    /// <param name="testOutputHelper">
    /// xUnit-provided test output helper; useful for writing trace information for failing tests.
    /// </param>
    public HomeControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) {}

    /// <summary>
    /// Verifies that the <see cref="HomeController.Index"/> action returns an HTTP 200 response
    /// with the message "The API is running." as the response value.
    /// </summary>
    [Fact(DisplayName = "Success: The API is running.")]
    public void Index_ReturnsOkWithMessage()
    {
        var loggerMock = new Mock<ILogger<HomeController>>();
        var controller = new HomeController(loggerMock.Object, _userStore, TestHelper.CreateTestSettings());
        
        IActionResult result = controller.Index();
        
        result.Should().BeOfType<ObjectResult>();

        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(200);
        objectResult.Value.Should().Be("The API is running.");
        _testOutputHelper.WriteLine("Index action returned: {0}", objectResult.Value);
    }
}
