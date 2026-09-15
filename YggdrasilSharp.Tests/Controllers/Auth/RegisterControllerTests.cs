using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tavstal.YggdrasilSharp.Controllers.Auth;
using Tavstal.YggdrasilSharp.Models.Bodies.Auth;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Tests.Helpers;
using Tavstal.YggdrasilSharp.Tests.Models;
using Tavstal.YggdrasilSharp.Tests.Services;

namespace Tavstal.YggdrasilSharp.Tests.Controllers.Auth;

/// <summary>
/// Test suite for <see cref="RegisterController"/> covering registration and confirmation flows.
/// </summary>
public class RegisterControllerTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly TestHelper _testHelper;
    private readonly FakeEmailService _emailService;
    private readonly IPasswordHasher<CustomUser> _passwordHasher;
    private readonly RegisterController _controller;
    private readonly DefaultHttpContext _controllerHttpContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterControllerTests"/> class and prepares
    /// test dependencies (DB, user manager, email service and settings).
    /// </summary>
    /// <param name="testOutputHelper">xUnit-provided test output helper for logging test information.</param>
    public RegisterControllerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _testHelper = new TestHelper();
        var loggerMock = new Mock<ILogger<RegisterController>>();
        var dbContext = TestHelper.CreateInMemoryDbContext();
        var userStore = TestHelper.CreateCustomUserStore(dbContext);
        var userManager = _testHelper.CreateCustomUserManager(dbContext, userStore);
        _passwordHasher = _testHelper.PasswordHasher;
        _emailService = _testHelper.FakeEmailService;
        var settings = TestHelper.CreateTestSettings();
        var fileDataRepo = new Repository<FileData>(dbContext);
        _controller = new RegisterController(loggerMock.Object, userManager, dbContext, userStore, _passwordHasher, _emailService, fileDataRepo, settings);
        
        _controllerHttpContext = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = IPAddress.Parse(TestHelper.IpAddress)
            }
        };
        _controllerHttpContext.Request.Headers.UserAgent = TestHelper.UserAgent;
        _controllerHttpContext.Request.Host = new HostString("localhost", 5000);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }

    /// <summary>
    /// Tests focused on the registration form submission endpoint.
    /// </summary>
    public class RegisterFormTests : RegisterControllerTests
    {
        public RegisterFormTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: posting a valid registration should return HTTP 201 Created.
        /// </summary>
        [Fact(DisplayName = "Success: Returns 201 Created")]
        public async Task ReturnsCreatedResult()
        {

            IActionResult result = await _controller.RegisterForm(new RegisterRequestBody
            {
                Username = "testuser",
                EmailAddress = "testuser@gmail.com",
                Password = "This%Valid_And#Pass%mock-2026",
            });

            result.Should().BeOfType<ObjectResult>();

            ObjectResult? objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(201);
            _testOutputHelper.WriteLine("Registration Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: registering the same user twice should produce HTTP 409 Conflict on the second attempt.
        /// </summary>
        [Fact(DisplayName = "Failure: Returns 409 Conflict for existing user")]
        public async Task ReturnsBadRequest_ForExistingUser()
        {
            var request = new RegisterRequestBody
            {
                Username = "testuser",
                EmailAddress = "testuser@gmail.com",
                Password = "This%Valid_And#Pass%mock-2026",
            };

            IActionResult result = await _controller.RegisterForm(request);
            result.Should().BeOfType<ObjectResult>();

            ObjectResult? objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(201);
            _testOutputHelper.WriteLine("Registration Result: " + objectResult.Value);

            result = await _controller.RegisterForm(request);
            result.Should().BeOfType<ObjectResult>();

            objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(409);
            _testOutputHelper.WriteLine("Second Registration Result (should fail): " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: submitting an invalid email format should return HTTP 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Returns 400 Bad Request for invalid email")]
        public async Task ReturnsBadRequest_ForInvalidEmail()
        {
            var request = new RegisterRequestBody
            {
                Username = "testuser2",
                EmailAddress = "invalid-email",
                Password = "This%Valid_And#Pass%mock-2026",
            };

            IActionResult result = await _controller.RegisterForm(request);
            result.Should().BeOfType<ObjectResult>();

            ObjectResult? objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Registration Result with invalid email: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: submitting a weak password should return HTTP 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Returns 403 Forbidden for weak password")]
        public async Task ReturnsBadRequest_ForWeakPassword()
        {
            var request = new RegisterRequestBody()
            {
                Username = "testuser3",
                EmailAddress = "testuser3@gmail.com",
                Password = "123456",
            };

            IActionResult result = await _controller.RegisterForm(request);
            result.Should().BeOfType<ObjectResult>();

            ObjectResult? objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Registration Result with weak password: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: missing required fields should produce HTTP 400 Bad Request.
        /// </summary>
        /// <remarks>
        /// This test manually adds a model state error to simulate model validation failures.
        /// </remarks>
        [Fact(DisplayName = "Failure: Returns 400 Bad Request for missing required fields")]
        public async Task ReturnsBadRequest_ForMissingFields()
        {
            var request = new RegisterRequestBody()
            {
                Username = "testuser4",
                EmailAddress = "testuser4@gmail.com",
                Password = null!,
            };

            _controller.ModelState.AddModelError("Password", "The Password field is required.");
            IActionResult result = await _controller.RegisterForm(request);
            result.Should().BeOfType<ObjectResult>();

            ObjectResult? objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Registration Result with missing fields: " + objectResult.Value);
        }
    }

    /// <summary>
    /// Tests for the registration confirmation endpoint.
    /// </summary>
    public class ConfirmRegistrationTests : RegisterControllerTests
    {
        public ConfirmRegistrationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: confirm a newly registered user using the token from the captured email;
        /// expected result is HTTP 200 OK.
        /// </summary>
        [Fact(DisplayName = "Success: Returns 200 OK for valid confirmation")]
        public async Task ReturnsOkResult()
        {
            IActionResult result = await _controller.RegisterForm(new RegisterRequestBody
            {
                Username = "testuser5",
                EmailAddress = "testuser5@gmail.com",
                Password = "This%Valid_And#Pass%mock-2026",
            });

            result.Should().BeOfType<ObjectResult>();

            ObjectResult? objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(201);
            _testOutputHelper.WriteLine("Registration Result: " + objectResult.Value);

            _emailService.SentEmails.Should().HaveCountGreaterThanOrEqualTo(1);
            SentEmail sentEmail = _emailService.SentEmails.First();

            string userId, token;
            if (!string.IsNullOrEmpty(sentEmail.ActionUrl))
            {
                userId = sentEmail.ActionUrl
                    .Substring(sentEmail.ActionUrl.IndexOf("userId=", StringComparison.Ordinal) + 7, 36).Trim();
                token = sentEmail.ActionUrl[(sentEmail.ActionUrl.IndexOf("token=", StringComparison.Ordinal) + 6)..]
                    .Trim();
            }
            else
            {
                userId = sentEmail.Body.Substring(sentEmail.Body.IndexOf("userId=", StringComparison.Ordinal) + 7, 36)
                    .Trim();
                token = sentEmail.Body[(sentEmail.Body.IndexOf("token=", StringComparison.Ordinal) + 6)..].Trim()
                    .Split("<br/><br/>")[0];
            }


            result = await _controller.ConfirmRegistration(new ConfirmRegisterRequestBody
            {
                UserId = userId,
                ConfirmationToken = token
            });

            result.Should().BeOfType<ObjectResult>();
            objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Confirmation Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: confirming an already confirmed user should return HTTP 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Returns 403 Forbidden for already confirmed user")]
        public async Task ReturnsBadRequest_ForAlreadyConfirmedUser()
        {
            IActionResult result = await _controller.RegisterForm(new RegisterRequestBody
            {
                Username = "testuser5",
                EmailAddress = "testuser5@gmail.com",
                Password = "This%Valid_And#Pass%mock-2026",
            });

            result.Should().BeOfType<ObjectResult>();

            ObjectResult? objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(201);
            _testOutputHelper.WriteLine("Registration Result: " + objectResult.Value);

            _emailService.SentEmails.Should().HaveCountGreaterThanOrEqualTo(1);
            SentEmail sentEmail = _emailService.SentEmails.First();

            string userId, token;
            if (!string.IsNullOrEmpty(sentEmail.ActionUrl))
            {
                userId = sentEmail.ActionUrl
                    .Substring(sentEmail.ActionUrl.IndexOf("userId=", StringComparison.Ordinal) + 7, 36).Trim();
                token = sentEmail.ActionUrl[(sentEmail.ActionUrl.IndexOf("token=", StringComparison.Ordinal) + 6)..]
                    .Trim();
            }
            else
            {
                userId = sentEmail.Body.Substring(sentEmail.Body.IndexOf("userId=", StringComparison.Ordinal) + 7, 36)
                    .Trim();
                token = sentEmail.Body[(sentEmail.Body.IndexOf("token=", StringComparison.Ordinal) + 6)..].Trim()
                    .Split("<br/><br/>")[0];
            }

            result = await _controller.ConfirmRegistration(new ConfirmRegisterRequestBody
            {
                UserId = userId,
                ConfirmationToken = token
            });

            result.Should().BeOfType<ObjectResult>();
            objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("First confirmation result: " + objectResult.Value);

            result = await _controller.ConfirmRegistration(new ConfirmRegisterRequestBody
            {
                UserId = userId,
                ConfirmationToken = token
            });

            result.Should().BeOfType<ObjectResult>();
            objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Second confirmation result (should be invalid): " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: providing an invalid token returns HTTP 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Returns 400 Bad Request for invalid token")]
        public async Task ReturnsBadRequest_ForInvalidToken()
        {
            IActionResult result = await _controller.RegisterForm(new RegisterRequestBody
            {
                Username = "testuser5",
                EmailAddress = "testuser5@gmail.com",
                Password = "This%Valid_And#Pass%mock-2026",
            });

            result.Should().BeOfType<ObjectResult>();

            ObjectResult? objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(201);
            _testOutputHelper.WriteLine("Registration Result: " + objectResult.Value);

            _emailService.SentEmails.Should().HaveCountGreaterThanOrEqualTo(1);
            SentEmail sentEmail = _emailService.SentEmails.First();

            var value = !string.IsNullOrEmpty(sentEmail.ActionUrl) ? sentEmail.ActionUrl : sentEmail.Body;
            var userId = value.Substring(value.IndexOf("userId=", StringComparison.Ordinal) + 7, 36).Trim();


            result = await _controller.ConfirmRegistration(new ConfirmRegisterRequestBody
            {
                UserId = userId,
                ConfirmationToken = "invalid-token"
            });

            result.Should().BeOfType<ObjectResult>();
            objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Confirmation Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: confirming a non-existent user returns HTTP 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Returns 400 Bad Request for non-existent user")]
        public async Task ReturnsBadRequest_ForInvalidUser()
        {
            IActionResult result = await _controller.RegisterForm(new RegisterRequestBody
            {
                Username = "testuser5",
                EmailAddress = "testuser5@gmail.com",
                Password = "This%Valid_And#Pass%mock-2026",
            });

            result.Should().BeOfType<ObjectResult>();

            ObjectResult? objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(201);
            _testOutputHelper.WriteLine("Registration Result: " + objectResult.Value);


            result = await _controller.ConfirmRegistration(new ConfirmRegisterRequestBody
            {
                UserId = Guid.NewGuid().ToString(),
                ConfirmationToken = "invalid-token" // We can use any token here since the user doesn't exist
            });

            result.Should().BeOfType<ObjectResult>();
            objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Confirmation Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: missing required confirmation fields should return HTTP 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Returns 400 Bad Request for missing required fields")]
        public async Task ReturnsBadRequest_ForMissingFields()
        {
            _controller.ModelState.AddModelError("UserId", "The UserId field is required.");
            IActionResult result = await _controller.ConfirmRegistration(new ConfirmRegisterRequestBody
            {
                UserId = null!,
                ConfirmationToken = null!
            });

            result.Should().BeOfType<ObjectResult>();

            ObjectResult? objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Confirmation Result with missing fields: " + objectResult.Value);
        }
    }
}