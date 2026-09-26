using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tavstal.YggdrasilSharp.Controllers.User;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using Tavstal.YggdrasilSharp.Tests.Helpers;
using Tavstal.YggdrasilSharp.Tests.Models;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Controllers.User;

/// <summary>
/// Unit tests for <see cref="PublicUserController"/>.
/// Exercises retrieval of public user information and public avatar endpoints.
/// </summary>
public class PublicUserControllerTests : ControllerTestBase
{
    private readonly IRepository<FileData> _fileDataRepo;
    private readonly Mock<ILogger<PublicUserController>> _loggerMock = new();
    private readonly PublicUserController _controller;

    /// <summary>
    /// Initializes the test fixture and constructs a <see cref="PublicUserController"/>
    /// wired with the test HttpContext, user manager and in-memory DB context provided by the base class.
    /// </summary>
    /// <param name="testOutputHelper">XUnit test output helper for logging test information.</param>
    public PublicUserControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _fileDataRepo = new Repository<FileData>(_dbContext);
        _controller = new PublicUserController(_loggerMock.Object, _userStore, _fileDataRepo, AppConfiguration);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }

    /// <summary>
    /// Tests for the <see cref="PublicUserController.GetUserInfo"/> endpoint.
    /// </summary>
    public class GetUserInfoTests : PublicUserControllerTests
    {
        public GetUserInfoTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: when a user exists in the test database the controller should
        /// return a <see cref="ContentResult"/> containing the serialized public user info.
        /// </summary>
        [Fact(DisplayName = "Success: Returns user info")]
        public async Task ReturnsOk()
        {
            var user =await CreateUserAsync(_controller);
            
            IActionResult result = await _controller.GetUserInfo(user.Id);
            
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            _testOutputHelper.WriteLine(contentResult.Content!);
        }
        
        /// <summary>
        /// Failure case: when the requested user id does not exist the controller should
        /// return an <see cref="ObjectResult"/> with HTTP status 404 (Not Found).
        /// </summary>
        [Fact(DisplayName = "Failure: User not found")]
        public async Task ReturnsNotFound()
        {
            IActionResult result = await _controller.GetUserInfo("nonexistent-id");
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Tests for the <see cref="PublicUserController.GetAvatar"/> endpoint.
    /// </summary>
    public class GetAvatarTests : PublicUserControllerTests
    {
        public GetAvatarTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: when a user has an avatar stored in FileData the controller should
        /// return a <see cref="FileStreamResult"/> containing the avatar stream and correct content type.
        /// </summary>
        [Fact(DisplayName = "Success: Returns avatar")]
        public async Task ReturnsOk()
        {
            var user =await CreateUserAsync(_controller);
            using var stream = TestHelper.CreateTestImage(128, 128);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);

            var fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                ContentType = "image/png",
                FileName = $"{Guid.NewGuid():N}.png",
                Type = EFileDataType.PROFILE_PICTURE,
                UserId = user.Id
            }, true, TestContext.Current.CancellationToken);
            fd.SaveFile(stream);

            try
            {
                IActionResult result = await _controller.GetAvatar(user.Id);

                result.Should().BeOfType<FileStreamResult>();
                var fileStreamResult = result as FileStreamResult;
                fileStreamResult.Should().NotBeNull();
                _testOutputHelper.WriteLine("Result: " + fileStreamResult.ContentType);
            }
            finally
            {
                fd.DeleteFile();
            }
        }
        
        /// <summary>
        /// Failure case: requesting an avatar for a non-existent user id should return
        /// an <see cref="ObjectResult"/> with HTTP status 404 (Not Found).
        /// </summary>
        [Fact(DisplayName = "Failure: User not found")]
        public async Task ReturnsNotFound_WhenUserNotFound()
        {
            IActionResult result = await _controller.GetAvatar("no-such-user");
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: when the user exists but no avatar is associated, the controller should
        /// return an <see cref="ObjectResult"/> with HTTP status 404 (Not Found).
        /// </summary>
        [Fact(DisplayName = "Failure: User has no avatar")]
        public async Task ReturnsNotFound_WhenAvatarNotFound()
        {
            var user = await CreateUserAsync(_controller);

            IActionResult result = await _controller.GetAvatar(user.Id);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
}