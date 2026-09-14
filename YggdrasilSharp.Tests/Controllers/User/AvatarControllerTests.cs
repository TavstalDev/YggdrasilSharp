using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SkiaSharp;
using Tavstal.YggdrasilSharp.Controllers.User;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using Xunit;

namespace Tavstal.YggdrasilSharp.Tests.Controllers.User;

/// <summary>
/// Test fixture for <see cref="AvatarController"/>. Inherits from <see cref="ControllerTestBase"/>
/// which provides an in-memory database, mocked user manager, memory cache service and helper utilities
/// used across controller tests.
/// </summary>
public class AvatarControllerTests : ControllerTestBase
{
    private readonly IRepository<FileData> _fileDataRepo;
    private readonly Mock<ILogger<AvatarController>> _loggerMock = new();
    private readonly AvatarController _controller;
    
    /// <summary>
    /// Constructs the test fixture and configures the controller with the test HttpContext
    /// provided by <see cref="ControllerTestBase"/>.
    /// </summary>
    /// <param name="testOutputHelper">XUnit test output helper for logging test information.</param>
    public AvatarControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _fileDataRepo = new Repository<FileData>(_dbContext);
        _controller = new AvatarController(_loggerMock.Object, _userManager, _userStore, _fileDataRepo, _memoryCacheService, _settings);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }

    /// <summary>
    /// Tests for the GetAvatar endpoint of <see cref="AvatarController"/>.
    /// </summary>
    public class GetAvatarTests : AvatarControllerTests
    {
        public GetAvatarTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: when the avatar is present in the memory cache the controller should
        /// return a <see cref="FileContentResult"/> containing the avatar bytes and content type.
        /// </summary>
        [Fact(DisplayName = "Success: Avatar exists")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller);
            string cacheKey = $"avatar:{user.Id}";
            using var stream = CreateTestImage(256, 256);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            
            _memoryCacheService.SetValue(cacheKey, (stream.ToArray(), "image/png", fileHash));
            
            IActionResult result = await _controller.GetAvatar();

            result.Should().BeOfType<FileContentResult>();
            var fileContentResult = result as FileContentResult;
            fileContentResult.Should().NotBeNull();
        }
        
        /// <summary>
        /// Failure case: when the request is unauthenticated the controller should return
        /// an ObjectResult with status code 401 (Unauthorized).
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized()
        {
            IActionResult result = await _controller.GetAvatar();

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: when the user exists but no avatar is found the controller should
        /// return an ObjectResult with status code 404 (Not Found).
        /// </summary>
        [Fact(DisplayName = "Failure: No avatar exists")]
        public async Task ReturnsNotFound()
        {
            await CreateUserAsync(_controller);
            
            IActionResult result = await _controller.GetAvatar();

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Tests for the UploadAvatar endpoint of <see cref="AvatarController"/>.
    /// </summary>
    public class UploadAvatarTests : AvatarControllerTests
    {
        public UploadAvatarTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: when a valid PNG file is uploaded by an authenticated user the controller
        /// should return a 200 OK ObjectResult.
        /// </summary>
        [Fact(DisplayName = "Success: Upload avatar")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller);
            using var stream = CreateTestImage(256, 256);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            
            IActionResult result = await _controller.UploadAvatar(file);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);

            var fds = await _fileDataRepo.QueryAsync(null, TestContext.Current.CancellationToken);
            foreach (var fd in fds)
                fd.DeleteFile();
        }
        
        /// <summary>
        /// Failure case: when the request model is invalid (for example the file is missing)
        /// the controller should return a 400 Bad Request ObjectResult.
        /// </summary>
        [Fact(DisplayName = "Failure: Missing file")]
        public async Task ReturnsBadRequest_MissingFile()
        {
            _controller.ModelState.AddModelError("file", "File is required.");
            IActionResult result = await _controller.UploadAvatar(null!);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: the file exceeds the allowed size (512 KB in this test),
        /// the controller should return a 400 Bad Request ObjectResult.
        /// </summary>
        [Fact(DisplayName = "Failure: File too large")]
        public async Task ReturnsBadRequsest_FileTooLarge()
        {
            await CreateUserAsync(_controller);
            using var stream = new MemoryStream(new byte[1024 * 512]);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            
            IActionResult result = await _controller.UploadAvatar(file);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: when the uploaded file has an invalid extension/type the controller should
        /// return a 400 Bad Request ObjectResult.
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid file type")]
        public async Task ReturnsBadRequest_InvalidFileType()
        {
            await CreateUserAsync(_controller);
            using var stream = CreateTestImage(256, 256);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.jpg")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            
            IActionResult result = await _controller.UploadAvatar(file);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: when no user is authenticated the controller should return
        /// a 401 Unauthorized ObjectResult for upload attempts.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized()
        {
            using var stream = CreateTestImage(256, 256);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.jpg")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            
            IActionResult result = await _controller.UploadAvatar(file);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Tests for the DeleteAvatar endpoint of <see cref="AvatarController"/>.
    /// </summary>
    public class DeleteAvatarTests : AvatarControllerTests
    {
        public DeleteAvatarTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: when a FileData representing a profile picture exists for the user,
        /// calling DeleteAvatar should return 200 OK.
        /// </summary>
        [Fact(DisplayName = "Success: Delete avatar")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller);
            
            using var stream = CreateTestImage(256, 256);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);

            await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                ContentType = "image/png",
                FileName = "test.png",
                Type = EFileDataType.PROFILE_PICTURE,
                UserId = user.Id
            }, true, TestContext.Current.CancellationToken);
            
            IActionResult result = await _controller.DeleteAvatar();

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: there is no avatar to delete for the current user, the controller should return 404 Not Found.
        /// </summary>
        [Fact(DisplayName = "Failure: No avatar to delete")]
        public async Task ReturnsNotFound()
        {
            await CreateUserAsync(_controller);
            IActionResult result = await _controller.DeleteAvatar();

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: when no user is authenticated the controller should return 401 Unauthorized for delete attempts.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized()
        {
            IActionResult result = await _controller.DeleteAvatar();

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Admin-level upload tests for <see cref="AvatarController"/>
    /// </summary>
    public class UploadAvatarAdminTests : AvatarControllerTests
    {
        public UploadAvatarAdminTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: an admin uploads an avatar for another user, expected 200 OK.
        /// </summary>
        [Fact(DisplayName = "Success: Upload avatar")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            using var stream = CreateTestImage(256, 256);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            
            IActionResult result = await _controller.UploadAvatarAdmin(user.Id, file);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);

            var fds = await _fileDataRepo.QueryAsync(null, TestContext.Current.CancellationToken);
            foreach (var fd in fds)
                fd.DeleteFile();
        }
        
        /// <summary>
        /// Failure case: missing file parameter for admin upload should return 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Missing file")]
        public async Task ReturnsBadRequest_MissingFile()
        {
            var user =  await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            _controller.ModelState.AddModelError("file", "File is required.");
            IActionResult result = await _controller.UploadAvatarAdmin(user.Id, null!);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: file too large for admin upload should return 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: File too large")]
        public async Task ReturnsBadRequsest_FileTooLarge()
        {
            var user =  await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            using var stream = new MemoryStream(new byte[1024 * 512]);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            
            IActionResult result = await _controller.UploadAvatarAdmin(user.Id, file);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: invalid file type for admin upload should return 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid file type")]
        public async Task ReturnsBadRequest_InvalidFileType()
        {
            var user =  await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            using var stream = CreateTestImage(256, 256);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.jpg")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            
            IActionResult result = await _controller.UploadAvatarAdmin(user.Id, file);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: authenticated user does not have admin permissions, expected 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden()
        {
            var user =  await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller, givePermissions: false);
            using var stream = CreateTestImage(256, 256);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.jpg")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            
            IActionResult result = await _controller.UploadAvatarAdmin(user.Id, file);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Admin-level deletion tests for <see cref="AvatarController"/>
    /// </summary>
    public class DeleteAvatarAdminTests : AvatarControllerTests
    {
        public DeleteAvatarAdminTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: admin deletes another user's avatar; expected 200 OK.
        /// </summary>
        [Fact(DisplayName = "Success: Delete avatar")]
        public async Task ReturnsOk()
        {
            var user =    await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            
            using var stream = CreateTestImage(256, 256);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);

            await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                ContentType = "image/png",
                FileName = "test.png",
                Type = EFileDataType.PROFILE_PICTURE,
                UserId = user.Id
            }, true, TestContext.Current.CancellationToken);
            
            IActionResult result = await _controller.DeleteAvatarAdmin(user.Id);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: admin attempts to delete an avatar which doesn't exist; expected 404 Not Found.
        /// </summary>
        [Fact(DisplayName = "Failure: No avatar to delete")]
        public async Task ReturnsNotFound()
        {
            var user =   await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            IActionResult result = await _controller.DeleteAvatarAdmin(user.Id);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: authenticated user lacks permission to perform admin deletion; expected 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller, givePermissions: false);
            IActionResult result = await _controller.DeleteAvatarAdmin(user.Id);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Utility: creates and returns an in-memory PNG image stream of the specified width and height.
    /// The returned <see cref="MemoryStream"/> is positioned at 0 and ready for reading.
    /// </summary>
    /// <param name="width">Width in pixels for the generated image.</param>
    /// <param name="height">Height in pixels for the generated image.</param>
    /// <returns>A <see cref="MemoryStream"/> containing a PNG image.</returns>
    private MemoryStream CreateTestImage(int width, int height)
    {
        var stream = new MemoryStream();
        using var bitmap = new SKBitmap(width, height);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(stream);
        stream.Position = 0; // Reset for reading
        return stream;
    }
}