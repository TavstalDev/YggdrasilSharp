using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OtpNet;
using Tavstal.YggdrasilSharp.Controllers.Auth;
using Tavstal.YggdrasilSharp.Tests.Helpers;
using Xunit;

namespace Tavstal.YggdrasilSharp.Tests.Controllers.Auth;

/// <summary>
/// Unit tests for <see cref="TwoFactorController"/> covering enabling/disabling 2FA,
/// generation of codes and regenerating recovery codes. Tests use an in-memory DB
/// and a fake email service provided by <see cref="TestHelper"/>.
/// </summary>
public class TwoFactorControllerTests : ControllerTestBase
{
    private readonly Mock<ILogger<TwoFactorController>> _loggerMock = new();
    private readonly TwoFactorController _controller;
        
    /// <summary>
    /// Initializes a new instance of the <see cref="TwoFactorControllerTests"/> test class.
    /// </summary>
    /// <param name="testOutputHelper">
    /// xUnit's <see cref="Xunit.Abstractions.ITestOutputHelper"/> provided by the test runner.
    /// This is forwarded to the base test class (via <c>base(testOutputHelper)</c>) to enable logging in the shared fixture.
    /// </param>
        public TwoFactorControllerTests(ITestOutputHelper testOutputHelper) :  base(testOutputHelper)
        {
            // Controller now expects (logger, userManager, userStore, settings)
            _controller = new TwoFactorController(_loggerMock.Object, _userManager, _userStore, _settings);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }

    /// <summary>
    /// Tests for enabling two-factor authentication.
    /// </summary>
    public class EnableTwoFactorTests : TwoFactorControllerTests
    {
        public EnableTwoFactorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: when the user supplies a correct TOTP code for the secret stored on the user,
        /// the controller should enable 2FA and return HTTP 200.
        /// </summary>
        [Fact(DisplayName = "Success: Enable 2FA")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller, _userMock);
            var secret = await _userManager.GenerateTwoFactorTokenAsync(user);

            byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
            var totp = new Totp(secretBytes);
            string code = totp.ComputeTotp();
            
            IActionResult result = await _controller.EnableTwoFactorAuthAsync(code);
            
            result.Should().BeOfType<ObjectResult>();
            var obj = result as ObjectResult;
            obj!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + obj.Value);
        }

        /// <summary>
        /// Failure case: when the provided TOTP code is invalid, the controller should return HTTP 401 Unauthorized.
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid code")]
        public async Task ReturnsUnauthorized_ForInvalidCode()
        {
            var user = await CreateUserAsync(_controller, _userMock);
            await _userManager.GenerateTwoFactorTokenAsync(user);

            IActionResult result = await _controller.EnableTwoFactorAuthAsync("000000");
            
            result.Should().BeOfType<ObjectResult>();
            var obj = result as ObjectResult;
            obj!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + obj.Value);
        }

        /// <summary>
        /// Failure case: unauthenticated requests to enable 2FA should be rejected with HTTP 401.
        /// This test does not set an authenticated user on the HttpContext.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthenticated user")]
        public async Task ReturnsUnauthorized_WhenUnauthenticated()
        {
            IActionResult result = await _controller.EnableTwoFactorAuthAsync("000000");
            result.Should().BeOfType<ObjectResult>();
            var obj = result as ObjectResult;
            obj!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + obj.Value);
        }
        
        /// <summary>
        /// Failure case: attempting to enable 2FA when it is already enabled should return HTTP 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: TFA already enabled")]
        public async Task ReturnsForbidden()
        {
            _userMock.TwoFactorEnabled = true;
            var user = await CreateUserAsync(_controller, _userMock);
            await _userManager.GenerateTwoFactorTokenAsync(user);

            IActionResult result = await _controller.EnableTwoFactorAuthAsync("000000");
            
            result.Should().BeOfType<ObjectResult>();
            var obj = result as ObjectResult;
            obj!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + obj.Value);
        }
    }

    /// <summary>
    /// Tests for disabling two-factor authentication.
    /// </summary>
    public class DisableTwoFactorTests : TwoFactorControllerTests
    {
        public DisableTwoFactorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: user with 2FA enabled supplies a valid TOTP code and the controller disables 2FA, returning HTTP 200.
        /// </summary>
        [Fact(DisplayName = "Success: Disable 2FA with valid code")]
        public async Task ReturnsOk()
        {
            _userMock.TwoFactorEnabled = true;
            var user = await CreateUserAsync(_controller, _userMock);
            var secret = await _userManager.GenerateTwoFactorTokenAsync(user);

            byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
            var totp = new Totp(secretBytes);
            string code = totp.ComputeTotp();
            
            IActionResult result = await _controller.DisableTwoFactorAuthAsync(code);
            
            result.Should().BeOfType<ObjectResult>();
            var obj = result as ObjectResult;
            obj!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + obj.Value);
        }

        /// <summary>
        /// Failure case: invalid code when attempting to disable 2FA should return HTTP 401 Unauthorized.
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid code")]
        public async Task ReturnsUnauthorized_ForInvalidCode()
        {
            _userMock.TwoFactorEnabled = true;
            var user = await CreateUserAsync(_controller, _userMock);
            await _userManager.GenerateTwoFactorTokenAsync(user);
            
            IActionResult result = await _controller.DisableTwoFactorAuthAsync("000000");

            result.Should().BeOfType<ObjectResult>();
            var obj = result as ObjectResult;
            obj!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + obj.Value);
        }
        
        /// <summary>
        /// Failure case: unauthenticated requests to disable 2FA should be rejected with HTTP 401.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthenticated user")]
        public async Task ReturnsUnauthorized_WhenUnauthenticated()
        {
            IActionResult result = await _controller.DisableTwoFactorAuthAsync("000000");
            result.Should().BeOfType<ObjectResult>();
            var obj = result as ObjectResult;
            obj!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + obj.Value);
        }
        
        /// <summary>
        /// Failure case: attempting to disable 2FA when it is not enabled should return HTTP 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: TFA not enabled")]
        public async Task ReturnsForbidden()
        {
            _userMock.TwoFactorEnabled = false;
            var user = await CreateUserAsync(_controller, _userMock);
            await _userManager.GenerateTwoFactorTokenAsync(user);
            
            IActionResult result = await _controller.DisableTwoFactorAuthAsync("000000");

            result.Should().BeOfType<ObjectResult>();
            var obj = result as ObjectResult;
            obj!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + obj.Value);
        }
    }
    
    /// <summary>
    /// Tests for generating a new two-factor secret/code for the user.
    /// </summary>
    public class GenerateCodeTests : TwoFactorControllerTests
    {
        public GenerateCodeTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: authenticated user requests a new 2FA secret and receives content with the secret/QR info.
        /// Expected: <see cref="ContentResult"/>.
        /// </summary>
        [Fact(DisplayName = "Success: Generates new 2FA secret")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller, _userMock);
            
            IActionResult result = await _controller.GenerateCodeAsync();
            
            result.Should().BeOfType<ContentResult>();
            var content = (result as ContentResult)!.Content;
            _testOutputHelper.WriteLine("Result: " + content);
        }

        /// <summary>
        /// Failure case: unauthenticated user requesting a new 2FA secret should get HTTP 401 Unauthorized.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized user")]
        public async Task ReturnsUnauthorized()
        {
            IActionResult result = await _controller.GenerateCodeAsync();
            result.Should().BeOfType<ObjectResult>();
            var obj = result as ObjectResult;
            obj!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + obj.Value);
        }
        
        /// <summary>
        /// Failure case: authenticated user who already has 2FA enabled should receive HTTP 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: TFA already enabled")]
        public async Task ReturnsForbidden()
        {
            _userMock.TwoFactorEnabled = true;
            var user = await CreateUserAsync(_controller, _userMock);
            await _userManager.GenerateTwoFactorTokenAsync(user);
            
            IActionResult result = await _controller.GenerateCodeAsync();
            result.Should().BeOfType<ObjectResult>();
            var obj = result as ObjectResult;
            obj!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + obj.Value);
        }
    }

    /// <summary>
    /// Tests for regenerating recovery codes for two-factor authentication.
    /// </summary>
    public class RegenerateRecoveryCodesTests : TwoFactorControllerTests
    {
        public RegenerateRecoveryCodesTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: authenticated user regenerates recovery codes and receives a <see cref="ContentResult"/>.
        /// </summary>
        [Fact(DisplayName = "Success: Regenerate recovery codes")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller, _userMock);
            
            IActionResult result = await _controller.RegenerateRecoveryCodesAsync();
            
            result.Should().BeOfType<ContentResult>();
            var obj = result as ContentResult;
            obj.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + obj.Content);
        }

        /// <summary>
        /// Failure case: unauthenticated users attempting to regenerate recovery codes should receive HTTP 401.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthenticated user")]
        public async Task ReturnsUnauthorized_WhenUnauthenticated()
        {
            IActionResult result = await _controller.RegenerateRecoveryCodesAsync();
            
            result.Should().BeOfType<ObjectResult>();
            var obj = result as ObjectResult;
            obj!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + obj.Value);
        }
    }
}