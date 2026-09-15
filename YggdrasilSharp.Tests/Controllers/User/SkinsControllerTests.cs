using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tavstal.YggdrasilSharp.Controllers.User;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using Tavstal.YggdrasilSharp.Tests.Helpers;

namespace Tavstal.YggdrasilSharp.Tests.Controllers.User;

/// <summary>
/// Tests for <see cref="SkinsController"/>.
/// </summary>
public class SkinsControllerTests : ControllerTestBase
{
    private readonly IRepository<FileData> _fileDataRepo;
    private readonly Mock<ILogger<SkinsController>> _loggerMock = new();
    private readonly SkinsController _controller;
    
    /// <summary>
    /// Initializes a new instance of <see cref="SkinsControllerTests"/>.
    /// Sets up the controller instance with a mock logger, the custom user manager from the base test class,
    /// the test database context and settings. Also configures a ControllerContext with an HttpContext prepared by the base.
    /// </summary>
    /// <param name="testOutputHelper">XUnit test output helper used by the base class.</param>
    public SkinsControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _fileDataRepo = new Repository<FileData>(_dbContext);
        _controller = new SkinsController(_loggerMock.Object, _userManager, _userStore, _fileDataRepo, _settings);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }
    
    /// <summary>
    /// Tests for getting the current authenticated user's skin.
    /// </summary>
    public class GetSkinTests : SkinsControllerTests
    {
        public GetSkinTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: verifies that when an authenticated user has a skin file saved in the DB and filesystem,
        /// <see cref="SkinsController.GetSkin"/> returns a <see cref="FileStreamResult"/> with the image content type.
        /// </summary>
        [Fact(DisplayName = "Success: Get existing skin")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller);
            using var stream = TestHelper.CreateTestImage(64, 64);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            
            var fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = "skin.png",
                ContentType = "image/png",
                UserId = user.Id,
                Type = EFileDataType.SKIN,
            }, true, TestContext.Current.CancellationToken);
            fd.SaveFile(stream);
            var result = await _controller.GetSkin();
            
            result.Should().BeOfType<FileStreamResult>();
            var fileStreamResult = result as FileStreamResult;
            fileStreamResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + fileStreamResult.ContentType);
            
            fd.DeleteFile();
        }
        
        /// <summary>
        /// Failure case: when no user is authenticated, <see cref="SkinsController.GetSkin"/> should return an unauthorized result.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized()
        {
            var result = await _controller.GetSkin();
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: when an authenticated user has no skin, <see cref="SkinsController.GetSkin"/> should return NotFound (404).
        /// </summary>
        [Fact(DisplayName = "Failure: No skin found")]
        public async Task ReturnsNotFound()
        {
            await CreateUserAsync(_controller);
            var result = await _controller.GetSkin();
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Tests for uploading a skin for the current authenticated user.
    /// </summary>
    public class UploadSkinTests : SkinsControllerTests
    {
        public UploadSkinTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: verifies that uploading a valid PNG skin for an authenticated user returns 200 OK.
        /// </summary>
        [Fact(DisplayName = "Success: Upload skin")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller);
            using var stream = TestHelper.CreateTestImage(64, 64);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            
            var result = await _controller.UploadSkin(file);
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);

            var files = await _fileDataRepo.QueryAsync(null, TestContext.Current.CancellationToken);
            foreach (var f in files)
                f.DeleteFile();
        }
        
        /// <summary>
        /// Failure case: when not authenticated, an upload attempt returns 401 Unauthorized.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized()
        {
            using var stream = TestHelper.CreateTestImage(64, 64);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var result = await _controller.UploadSkin(file);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: when the uploaded file exceeds allowed size, the controller should return 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: File too large")]
        public async Task ReturnsBadRequest_WhenFileTooLarge()
        {
            await CreateUserAsync(_controller);
            using var stream = new MemoryStream(new byte[1024 * 512]);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var result = await _controller.UploadSkin(file);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: when uploaded image dimensions are invalid for a skin (e.g. non-square or wrong height),
        /// the controller should return 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid dimensions")]
        public async Task ReturnsBadRequest_WhenInvalidDimensions()
        {
            await CreateUserAsync(_controller);
            using var stream = TestHelper.CreateTestImage(64, 65);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var result = await _controller.UploadSkin(file);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Tests for deleting the current authenticated user's skin.
    /// </summary>
    public class DeleteSkinTests : SkinsControllerTests
    {
        public DeleteSkinTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: when a user has a skin, DeleteSkin should remove it and return 200 OK.
        /// </summary>
        [Fact(DisplayName = "Success: Delete skin")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller);
            using var stream = TestHelper.CreateTestImage(64, 64);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            
            var fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = "skin.png",
                ContentType = "image/png",
                UserId = user.Id,
                Type = EFileDataType.SKIN,
            }, true, TestContext.Current.CancellationToken);
            fd.SaveFile(stream);
            var result = await _controller.DeleteSkin();
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
            
            fd.DeleteFile();
        }
        
        /// <summary>
        /// Failure case: unauthenticated delete attempts should return 401 Unauthorized.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized()
        {
            var result = await _controller.DeleteSkin();
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: when the user has no skin, delete should return 404 Not Found.
        /// </summary>
        [Fact(DisplayName = "Failure: No skin found")]
        public async Task ReturnsNotFound()
        {
            await CreateUserAsync(_controller);
            var result = await _controller.DeleteSkin();
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Admin tests for retrieving another user's skin (admin route).
    /// </summary>
    public class GetSkinAdminTests : SkinsControllerTests
    {
        public GetSkinAdminTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: admin with appropriate permissions can retrieve another user's skin and receives a FileStreamResult.
        /// </summary>
        [Fact(DisplayName = "Success: Get existing skin")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            using var stream = TestHelper.CreateTestImage(64, 64);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            
            var fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = "skin.png",
                ContentType = "image/png",
                UserId = user.Id,
                Type = EFileDataType.SKIN,
            }, true, TestContext.Current.CancellationToken);
            fd.SaveFile(stream);
            var result = await _controller.GetSkinAdmin(user.Id);
            
            result.Should().BeOfType<FileStreamResult>();
            var fileStreamResult = result as FileStreamResult;
            fileStreamResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + fileStreamResult.ContentType);
            
            fd.DeleteFile();
        }
        
        /// <summary>
        /// Failure case: if the caller lacks admin permissions, GetSkinAdmin should return 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: No permissions")]
        public async Task ReturnsUnauthorized()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller, givePermissions: false);
            var result = await _controller.GetSkinAdmin(user.Id);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: when the target user has no skin, GetSkinAdmin should return 404 Not Found.
        /// </summary>
        [Fact(DisplayName = "Failure: No skin found")]
        public async Task ReturnsNotFound()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            var result = await _controller.GetSkinAdmin(user.Id);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Admin tests for uploading a skin for another user (admin route).
    /// </summary>
    public class UploadSkinAdminTests : SkinsControllerTests
    {
        public UploadSkinAdminTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: admin with permissions uploads a skin for another user and receives 200 OK.
        /// </summary>
        [Fact(DisplayName = "Success: Upload skin")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller, _userMock2, givePermissions: false);
            await CreateUserAsync(_controller);
            using var stream = TestHelper.CreateTestImage(64, 64);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            
            var result = await _controller.UploadSkinAdmin(user.Id, file);
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);

            var files = await _fileDataRepo.QueryAsync(null, TestContext.Current.CancellationToken);
            foreach (var f in files)
                f.DeleteFile();
        }
        
        /// <summary>
        /// Failure case: an admin without sufficient permissions should receive 403 Forbidden when attempting to upload for another user.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden()
        {
            var user = await CreateUserAsync(_controller, _userMock2, givePermissions: false);
            await CreateUserAsync(_controller, givePermissions: false);
            using var stream = TestHelper.CreateTestImage(64, 64);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var result = await _controller.UploadSkinAdmin(user.Id, file);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: when uploaded file exceeds allowed size for admin upload, controller should return 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: File too large")]
        public async Task ReturnsBadRequest_WhenFileTooLarge()
        {
            var user =  await CreateUserAsync(_controller, _userMock2, givePermissions: false);
            await CreateUserAsync(_controller);
            using var stream = new MemoryStream(new byte[1024 * 512]);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var result = await _controller.UploadSkinAdmin(user.Id, file);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: when uploaded image dimensions are invalid for a skin, admin upload should return 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid dimensions")]
        public async Task ReturnsBadRequest_WhenInvalidDimensions()
        {
            var user = await CreateUserAsync(_controller, _userMock2, givePermissions: false);
            await CreateUserAsync(_controller);
            using var stream = TestHelper.CreateTestImage(64, 65);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var result = await _controller.UploadSkinAdmin(user.Id, file);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Admin tests for deleting another user's skin.
    /// </summary>
    public class DeleteSkinAdminTests : SkinsControllerTests
    {
        public DeleteSkinAdminTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: admin deletes another user's skin and receives 200 OK.
        /// </summary>
        [Fact(DisplayName = "Success: Delete skin")]
        public async Task ReturnsOk()
        {
            var user =  await CreateUserAsync(_controller, _userMock2, givePermissions: false);
            await CreateUserAsync(_controller);
            using var stream = TestHelper.CreateTestImage(64, 64);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            
            var fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = "skin.png",
                ContentType = "image/png",
                UserId = user.Id,
                Type = EFileDataType.SKIN,
            }, true, TestContext.Current.CancellationToken);
            fd.SaveFile(stream);
            var result = await _controller.DeleteSkinAdmin(user.Id);
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
            
            fd.DeleteFile();
        }
        
        /// <summary>
        /// Failure case: when admin lacks deletion permission, return 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden()
        {
            var user = await CreateUserAsync(_controller, _userMock2, givePermissions: false);
            await CreateUserAsync(_controller, givePermissions: false);
            var result = await _controller.DeleteSkinAdmin(user.Id);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }

        /// <summary>
        /// Failure case: when the target user has no skin, admin delete should return 404 Not Found.
        /// </summary>
        [Fact(DisplayName = "Failure: No skin found")]
        public async Task ReturnsNotFound()
        {
            var user = await CreateUserAsync(_controller, _userMock2, givePermissions: false);
            await CreateUserAsync(_controller);
            var result = await _controller.DeleteSkinAdmin(user.Id);
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
}