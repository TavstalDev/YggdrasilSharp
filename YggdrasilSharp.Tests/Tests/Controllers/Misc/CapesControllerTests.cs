using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tavstal.YggdrasilSharp.Controllers.Misc;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using Tavstal.YggdrasilSharp.Tests.Helpers;
using Tavstal.YggdrasilSharp.Tests.Models;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Controllers.Misc;

/// <summary>
/// Unit tests for the <see cref="CapesController"/> controller.
/// This test fixture sets up an in-memory <see cref="CustomDbContext"/>, a test <see cref="CustomUserManager"/>,
/// and configures an application Startup instance to provide the upload directory used by <see cref="FileData"/>.
/// </summary>
public class CapesControllerTests : ControllerTestBase
{
    private readonly IRepository<Cape> _capeRepo;
    private readonly IRepository<FileData> _fileDataRepo;
    private readonly Mock<ILogger<CapesController>> _loggerMock = new();
    private readonly CapesController _controller;

    /// <summary>
    /// Initializes the test fixture. Creates an in-memory database, custom user manager,
    /// controller instance and initializes Startup with a temporary upload directory
    /// so that <see cref="FileData.SaveFileAsync"/> can write files during tests.
    /// </summary>
    /// <param name="testOutputHelper">xUnit output helper injected by the test runner.</param>
    public CapesControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _capeRepo = new Repository<Cape>(_dbContext);
        _fileDataRepo = new Repository<FileData>(_dbContext);
        _controller = new CapesController(_loggerMock.Object, _userManager, _userStore, AntiVirusService, AppConfiguration, _dbContext, _capeRepo, _fileDataRepo);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }

    /// <summary>
    /// Tests related to uploading capes via <see cref="CapesController.UploadCape(IFormFile)"/>.
    /// </summary>
    public class UploadCapeTests : CapesControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public UploadCapeTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: verifies that a valid PNG cape image (64x64) uploaded by an authorized user
        /// returns status 200 OK and that a corresponding file/cape record is created.
        /// </summary>
        [Fact(DisplayName = "Success: Upload Cape")]
        public async Task ReturnsOK()
        {
            string fileHash = string.Empty;
            try
            {
                await CreateUserAsync(_controller);
                using var stream = TestHelper.CreateTestImage(64, 64);

                IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/png"
                };
                
                stream.Position = 0;
                using var sha256 = SHA256.Create();
                byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
                fileHash = Convert.ToHexStringLower(hashBytes);

                var result = await _controller.UploadCape(file);
                TestHelper.TestResponse(result, HttpStatusCode.OK, "Cape uploaded successfully");
            }
            finally
            {
                // Clean-up
                var files = await _fileDataRepo.QueryAsync(x => x.Hash == fileHash && x.Type == EFileDataType.CAPE, TestContext.Current.CancellationToken);
                foreach (var file in files)
                    file.DeleteFile();
            }
        }

        /// <summary>
        /// Failure case: uploading a non-image (text file) should result in a 400 Bad Request response.
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid File Format")]
        public async Task ReturnsBadRequest_ForInvalidFileFormat()
        {
            await CreateUserAsync(_controller);
            using var stream = new MemoryStream("This is not an image"u8.ToArray());
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.txt")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };
            
            var result = await _controller.UploadCape(file);
            TestHelper.TestResponse(result, HttpStatusCode.BadRequest, "Only PNG files are allowed.");
        }
        
        /// <summary>
        /// Failure case: uploading an image with invalid dimensions (not 64x64) should result in a 400 Bad Request response.
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid image dimension")]
        public async Task ReturnsBadRequest_ForInvalidImageDimension()
        {
            await CreateUserAsync(_controller);
            using var stream = TestHelper.CreateTestImage(64, 65);

            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            var result = await _controller.UploadCape(file);
            TestHelper.TestResponse(result, HttpStatusCode.BadRequest, "Invalid image format or dimensions. Expected dimensions: 64x32, 64x64, 512x256, or 512x512.");
        }

        /// <summary>
        /// Failure case: uploading a file that exceeds the size limit should return 400 Bad Request.
        /// This test uses a stream of 500 KB + 1 byte to exceed the configured limit in the controller.
        /// </summary>
        [Fact(DisplayName = "Failure: File Size Exceeds Limit")]
        public async Task ReturnsBadRequest_ForFileSizeExceedsLimit()
        {
            await CreateUserAsync(_controller);
            using var stream = new MemoryStream(new byte[1024 * 512]);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            
            var result = await _controller.UploadCape(file);
            TestHelper.TestResponse(result, HttpStatusCode.BadRequest, "File size exceeds the 500 KB limit.");
        }

        /// <summary>
        /// Failure case: uploading a cape with duplicate content should return 400 Bad Request on the second upload.
        /// The first upload should succeed (200 OK) and the second should be rejected as duplicate.
        /// </summary>
        [Fact(DisplayName = "Failure: Duplicate Cape Content")]
        public async Task ReturnsBadRequest_ForDuplicateCapeContent()
        {
            string fileHash = string.Empty;
            try
            {
                await CreateUserAsync(_controller);
                using var stream = TestHelper.CreateTestImage(64, 64);

                IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/png"
                };
                
                stream.Position = 0;
                using var sha256 = SHA256.Create();
                byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
                fileHash = Convert.ToHexStringLower(hashBytes);

                var result = await _controller.UploadCape(file);
                TestHelper.TestResponse(result, HttpStatusCode.OK, "Cape uploaded successfully");
                
                
                result = await _controller.UploadCape(file);
                TestHelper.TestResponse(result, HttpStatusCode.BadRequest, "Cape with the same content already exists.");
            }
            finally
            {
                // Clean-up
                var files = await _fileDataRepo.QueryAsync(x => x.Hash == fileHash && x.Type == EFileDataType.CAPE, TestContext.Current.CancellationToken);
                foreach (var file in files)
                    file.DeleteFile();
            }
        }

        /// <summary>
        /// Failure case: an authenticated user that lacks the required permission should receive 403 Forbidden.
        /// This test creates a user but does not grant admin/permission roles.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized Access")]
        public async Task ReturnsForbidden_WhenUserLacksPermissions()
        {
            await CreateUserAsync(_controller, givePermissions: false);
            using var stream = TestHelper.CreateTestImage(64, 64);

            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            var result = await _controller.UploadCape(file);
            TestHelper.TestResponse(result, HttpStatusCode.Forbidden, "Permission denied.");
        }
    }

    /// <summary>
    /// Tests related to deleting capes via <see cref="CapesController.DeleteCape(ulong)"/>.
    /// </summary>
    public class DeleteCapeTests : CapesControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public DeleteCapeTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: ensures an authorized admin user can delete an existing cape.
        /// The test inserts a cape and file into the in-memory database and writes the file to disk,
        /// then calls the controller delete action and asserts a 200 OK result.
        /// </summary>
        [Fact(DisplayName = "Success: Delete Cape")]
        public async Task ReturnsOK()
        {
            await CreateUserAsync(_controller);
            
            // Add cape mock
            using var stream = TestHelper.CreateTestImage(64, 64);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            
            FileData fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.png",
                ContentType = "image/png",
                Type = EFileDataType.CAPE,
            }, true, TestContext.Current.CancellationToken);
            await fd.SaveFileAsync(stream);
            Cape cape = await _capeRepo.AddAsync(new Cape
            {
                Name = "test",
                FileId = fd.Id,
                IsPublic = true
            }, true, TestContext.Current.CancellationToken);
            
            var result = await _controller.DeleteCape(cape.Id);
            
            TestHelper.TestResponse(result, HttpStatusCode.OK, "Cape deleted successfully");
        }
        
        /// <summary>
        /// Failure case: a user without sufficient permissions should receive 403 Forbidden when attempting to delete a cape.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized Access")]
        public async Task ReturnsForbidden_WhenUserLacksPermissions()
        {
            await CreateUserAsync(_controller, givePermissions: false);
            
            var result = await _controller.DeleteCape(123);
            
            TestHelper.TestResponse(result, HttpStatusCode.Forbidden, "Permission denied.");
        }

        /// <summary>
        /// Failure case: attempting to delete a non-existent cape should return 404 Not Found.
        /// </summary>
        [Fact(DisplayName = "Failure: Cape not found")]
        public async Task ReturnsNotFound()
        {
            await CreateUserAsync(_controller);
            
            var result = await _controller.DeleteCape(123);
            
            TestHelper.TestResponse(result, HttpStatusCode.NotFound, "Cape not found");
        }
    }
}