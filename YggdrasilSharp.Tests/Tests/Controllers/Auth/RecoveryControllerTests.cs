using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tavstal.YggdrasilSharp.Controllers.Auth;
using Tavstal.YggdrasilSharp.Models.Bodies.Auth;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Tests.Helpers;
using Tavstal.YggdrasilSharp.Tests.Models;
using Tavstal.YggdrasilSharp.Utils.Helpers;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Controllers.Auth;

/// <summary>
/// Tests for <see cref="RecoveryController"/> covering password and two-factor recovery flows.
/// </summary>
public class RecoveryControllerTests : ControllerTestBase
{
    private readonly Mock<ILogger<RecoveryController>> _loggerMock = new();
    private readonly RecoveryController _controller;

    /// <summary>
    /// Initializes shared test fixtures:
    /// <br/>- creates in-memory DB context,
    /// <br/>- creates a test UserManager,
    /// <br/>- prepares a fake email service and memory cache service,
    /// <br/>- constructs a <see cref="RecoveryController"/> instance,
    /// <br/>- sets up a default <see cref="DefaultHttpContext"/> (IP, User-Agent, Host),
    /// <br/>- prepares a default <see cref="CustomUser"/> object used by tests.
    /// </summary>
    /// <param name="testOutputHelper">XUnit output helper passed by the test framework.</param>
    public RecoveryControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        // Controller expects: (logger, dbContext, userStore, passwordHasher, emailService, memoryCacheService, settings)
        _controller = new RecoveryController(_loggerMock.Object, _dbContext, _userStore, _passwordHasher,
            _fakeEmailService, _memoryCacheService, _settings);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }

    /// <summary>
    /// Tests for the recovery request endpoint (password recovery request).
    /// These verify successful email sending, missing user, unconfirmed email and rate-limit checks.
    /// </summary>
    public class RequestRecoveryTests : RecoveryControllerTests
    {
        public RequestRecoveryTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: when the user exists and email is confirmed, a recovery email should be enqueued/sent.
        /// Expected result: <see cref="ObjectResult"/> with HTTP status 201 Created.
        /// </summary>
        [Fact(DisplayName = "Success: Send recovery email")]
        public async Task ReturnsOk()
        {
            _userMock.EmailConfirmed = true;
            await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);
            IActionResult result = await _controller.RequestRecoveryAsync(_userMock.Email);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(201);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: requesting recovery for a non-existent email returns NotFound.
        /// Expected result: <see cref="ObjectResult"/> with HTTP status 404 Not Found.
        /// </summary>
        [Fact(DisplayName = "Failure: User not found")]
        public async Task ReturnsNotFound_WhenUserMissing()
        {
            IActionResult result = await _controller.RequestRecoveryAsync("noone@example.com");
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: the user exists but their email is not confirmed — recovery should be forbidden.
        /// Expected result: <see cref="ObjectResult"/> with HTTP status 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Email not confirmed")]
        public async Task ReturnsForbidden_WhenEmailNotConfirmed()
        {
             _userMock.EmailConfirmed = false;
             await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);

            IActionResult result = await _controller.RequestRecoveryAsync(_userMock.Email);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: user has requested recovery recently and is rate-limited.
        /// The memory cache is prepopulated to simulate a recent request, which should cause a 403.
        /// Expected result: <see cref="ObjectResult"/> with HTTP status 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Already requested recovery recently")]
        public async Task ReturnsForbidden_WhenRequestTooFrequent()
        {
            var user = await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);
            var memoryService = _memoryCacheService;
            string fingerprint = TestHelper.GetFingerprint(user.Id);
            string cacheKey = $"recovery:{fingerprint}:password:token";
            memoryService.SetValue(cacheKey, "existing-token", TimeSpan.FromMinutes(15));

            IActionResult result = await _controller.RequestRecoveryAsync(_userMock.Email);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }

    /// <summary>
    /// Tests for the password recovery execution endpoint.
    /// These cover a successful password reset, invalid token, expired token, and attempts limit.
    /// </summary>
    public class RecoverPasswordTests : RecoveryControllerTests
    {
        public RecoverPasswordTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: valid recovery token present in cache and attempts under limit should reset the password.
        /// Expected result: <see cref="ObjectResult"/> with HTTP status 200 OK.
        /// </summary>
        [Fact(DisplayName = "Success: Reset password")]
        public async Task ReturnsOk()
        {
            var user = await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);
            string token = TokenHelper.GenerateRecoverySessionToken();
            string fingerprint = TestHelper.GetFingerprint(user.Id);
            _memoryCacheService.SetValue($"recovery:{fingerprint}:password:token", token, TimeSpan.FromMinutes(15));
            _memoryCacheService.SetValue($"recovery:{fingerprint}:password:attempt", 0, TimeSpan.FromMinutes(15));

            IActionResult result = await _controller.RecoverPasswordAsync(new RecoverPasswordRequestBody
            {
                Email = _userMock.Email,
                RecoveryToken = token,
                NewPassword = "NewPass-dasPW#2026",
                LogoutEverywhere = true
            });

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: provided recovery token is invalid for the user.
        /// Expected result: <see cref="ObjectResult"/> with HTTP status 404 Not Found (token/user mismatch).
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid recovery token")]
        public async Task ReturnsUnauthorized_ForInvalidToken()
        {
            await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);
            IActionResult result = await _controller.RecoverPasswordAsync(new RecoverPasswordRequestBody
            {
                Email = _userMock.Email,
                RecoveryToken = "wrong-token",
                NewPassword = "NewPass-dasPW#2026",
                LogoutEverywhere = false
            });

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: recovery token has expired (not present in cache or expired).
        /// Expected result: <see cref="ObjectResult"/> with HTTP status 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Expired recovery token")]
        public async Task ReturnsBadRequest_WhenTokenExpired()
        {
            await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);
            string token = TokenHelper.GenerateRecoverySessionToken();
            
            IActionResult result = await _controller.RecoverPasswordAsync(new RecoverPasswordRequestBody
            {
                Email = _userMock.Email,
                RecoveryToken = token,
                NewPassword = "NewPass-dasPW#2026",
                LogoutEverywhere = false
            });

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: too many recovery attempts were made for this fingerprint; the endpoint should return forbidden.
        /// The test pre-populates the attempts counter in the cache to simulate this.
        /// Expected result: <see cref="ObjectResult"/> with HTTP status 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Too many attempts")]
        public async Task ReturnsForbidden_WhenTooManyAttempts()
        {
            var user = await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);
            string token = TokenHelper.GenerateRecoverySessionToken();
            string fingerprint = TestHelper.GetFingerprint(user.Id);
            _memoryCacheService.SetValue($"recovery:{fingerprint}:password:token", token, TimeSpan.FromMinutes(15));
            _memoryCacheService.SetValue($"recovery:{fingerprint}:password:attempt", 4, TimeSpan.FromMinutes(15));
            
            IActionResult result = await _controller.RecoverPasswordAsync(new RecoverPasswordRequestBody
            {
                Email = _userMock.Email,
                RecoveryToken = token,
                NewPassword = "NewPass-dasPW#2026",
                LogoutEverywhere = false
            });

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }

    /// <summary>
    /// Tests for requesting two-factor authentication recovery (TFA backup codes / email flow).
    /// Ensures email sending, missing user and rate-limit behavior.
    /// </summary>
    public class RequestTwoFactorTests : RecoveryControllerTests
    {
        public RequestTwoFactorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) {}
        
        /// <summary>
        /// Success case: when user exists and email is confirmed, a TFA recovery email should be sent.
        /// Expected: <see cref="ObjectResult"/> with HTTP 201 Created.
        /// </summary>
        [Fact(DisplayName = "Success: Send recovery email")]
        public async Task ReturnsOk()
        {
            _userMock.EmailConfirmed = true;
             await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);
            IActionResult result = await _controller.RequestTFARecoveryAsync(_userMock.Email);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(201);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: requesting TFA recovery for an unknown email returns 404 Not Found.
        /// </summary>
        [Fact(DisplayName = "Failure: User not found")]
        public async Task ReturnsNotFound_WhenUserMissing()
        {
            IActionResult result = await _controller.RequestTFARecoveryAsync("noone@example.com");
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: user's email not confirmed — TFA recovery should be forbidden.
        /// Expected: HTTP 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Email not confirmed")]
        public async Task ReturnsForbidden_WhenEmailNotConfirmed()
        {
             _userMock.EmailConfirmed = false;
             await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);

            IActionResult result = await _controller.RequestTFARecoveryAsync(_userMock.Email);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: TFA recovery was requested recently and is rate-limited.
        /// Simulates the cache containing an existing token, expecting HTTP 403.
        /// </summary>
        [Fact(DisplayName = "Failure: Already requested recovery recently")]
        public async Task ReturnsForbidden_WhenRequestTooFrequent()
        {
            var user = await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);
            var memoryService = _memoryCacheService;
            string fingerprint = TestHelper.GetFingerprint(user.Id);
            string cacheKey = $"recovery:{fingerprint}:tfa:token";
            memoryService.SetValue(cacheKey, "existing-token", TimeSpan.FromMinutes(15));

            IActionResult result = await _controller.RequestTFARecoveryAsync(_userMock.Email);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Tests covering performing two-factor recovery (using backup codes or token-based flows).
    /// Verifies valid backup code flow, invalid code, too many attempts and user-not-found.
    /// </summary>
    public class RecoverTwoFactorTests : RecoveryControllerTests
    {
        public RecoverTwoFactorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: user has 2FA enabled and a valid backup code stored in DB.
        /// The test places the expected recovery token into cache and ensures the call returns 200 OK.
        /// </summary>
        [Fact(DisplayName = "Success: Recover 2FA with valid backup code")]
        public async Task ReturnsOk_WhenValidBackupCode()
        {
            _userMock.TwoFactorEnabled = true;
            var user = await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);
            await _userManager.GenerateTwoFactorTokenAsync(user);
            string backup = "backup-code-123";
            await _userStore.UserBackupCodes.AddAsync(new UserBackupCode
            {
                UserId = user.Id,
                HashedCode = StringChiper.GetEncryptedHash(backup, _settings.Jwt.EncryptionKey),
                CreateAt =  DateTime.UtcNow,
            }, true, TestContext.Current.CancellationToken);
            string fingerprint = TestHelper.GetFingerprint(_userMock.Id);
            string token = TokenHelper.GenerateRecoverySessionToken();
            _memoryCacheService.SetValue($"recovery:{fingerprint}:tfa:token", token, TimeSpan.FromMinutes(15));
            _memoryCacheService.SetValue($"recovery:{fingerprint}:tfa:attempt", 0, TimeSpan.FromMinutes(15));

            IActionResult result = await _controller.RecoverTwoFactorAsync(new RecoverTwoFactorRequestBody
            {
                Email = _userMock.Email,
                BackupCode = backup,
                RecoveryToken = token,
                LogoutEverywhere = true
            });

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: backup code provided by user is invalid — expect HTTP 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid backup code")]
        public async Task ReturnsUnauthorized_ForInvalidBackupCode()
        {
            _userMock.TwoFactorEnabled = true;
            var user = await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);
            await _userManager.GenerateTwoFactorTokenAsync(user);

            string fingerprint = TestHelper.GetFingerprint(_userMock.Id);
            string token = TokenHelper.GenerateRecoverySessionToken();
            _memoryCacheService.SetValue($"recovery:{fingerprint}:tfa:token", token, TimeSpan.FromMinutes(15));
            _memoryCacheService.SetValue($"recovery:{fingerprint}:tfa:attempt", 0, TimeSpan.FromMinutes(15));

            IActionResult result = await _controller.RecoverTwoFactorAsync(new RecoverTwoFactorRequestBody
            {
                Email = _userMock.Email,
                BackupCode = "wrong-backup",
                RecoveryToken = token,
                LogoutEverywhere = false
            });

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: too many attempts have been made for TFA recovery and endpoint returns forbidden.
        /// The test simulates this by pre-populating the attempts counter in cache.
        /// </summary>
        [Fact(DisplayName = "Failure: Too many recovery attempts")]
        public async Task ReturnsForbidden_WhenTooManyAttempts()
        {
            _userMock.TwoFactorEnabled = true;
            var user = await _userStore.AddUserAsync(_userMock, true, TestContext.Current.CancellationToken);
            await _userManager.GenerateTwoFactorTokenAsync(user);
            
            string fingerprint = TestHelper.GetFingerprint(_userMock.Id);
            string token = TokenHelper.GenerateRecoverySessionToken();
            _memoryCacheService.SetValue($"recovery:{fingerprint}:tfa:token", token, TimeSpan.FromMinutes(15));
            _memoryCacheService.SetValue($"recovery:{fingerprint}:tfa:attempt", 4, TimeSpan.FromMinutes(15));

            IActionResult result = await _controller.RecoverTwoFactorAsync(new RecoverTwoFactorRequestBody
            {
                Email = _userMock.Email,
                RecoveryToken = "",
                BackupCode = "anything",
                LogoutEverywhere = false
            });

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: attempting TFA recovery for a non-existing user results in NotFound.
        /// Expected result: <see cref="ObjectResult"/> with HTTP status 404 Not Found.
        /// </summary>
        [Fact(DisplayName = "Failure: User not found")]
        public async Task ReturnsNotFound_WhenUserMissing()
        {
            IActionResult result = await _controller.RecoverTwoFactorAsync(new RecoverTwoFactorRequestBody
            {
                Email = "noone@example.com",
                RecoveryToken = "",
                BackupCode = "any",
                LogoutEverywhere = false
            });

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
}

