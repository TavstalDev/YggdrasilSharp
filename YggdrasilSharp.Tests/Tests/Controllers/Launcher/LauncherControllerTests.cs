using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tavstal.YggdrasilSharp.Controllers.Launcher;
using Tavstal.YggdrasilSharp.Models.Bodies.Launcher;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Database.Launcher;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using Tavstal.YggdrasilSharp.Tests.Helpers;
using Tavstal.YggdrasilSharp.Tests.Models;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Controllers.Launcher;

/// <summary>
/// Unit tests for the LauncherController.
/// </summary>
public class LauncherControllerTests : ControllerTestBase
{
    private readonly IRepository<LauncherVersion> _launcherVersionRepo;
    private readonly IRepository<LauncherVersionData> _launcherVersionDataRepo;
    private readonly IRepository<FileData> _fileDataRepo;
    private readonly Mock<ILogger<LauncherController>> _loggerMock = new();
    private readonly LauncherController _controller;

    /// <summary>
    /// Initializes a new instance of the LauncherControllerTests class.
    /// </summary>
    /// <param name="testOutputHelper">Helper for test output.</param>
    public LauncherControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
            _launcherVersionRepo = new Repository<LauncherVersion>(_dbContext);
            _launcherVersionDataRepo = new Repository<LauncherVersionData>(_dbContext);
            _fileDataRepo = new Repository<FileData>(_dbContext);
            _controller = new LauncherController(_loggerMock.Object, _userManager, _userStore, AntiVirusService, AppConfiguration, _launcherVersionRepo, _launcherVersionDataRepo, _fileDataRepo);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }

    /// <summary>
    /// Tests for the GetLauncherVersion endpoint.
    /// </summary>
    public class GetLauncherVersionTests : LauncherControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public GetLauncherVersionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Verifies that the endpoint returns a list of versions successfully.
        /// </summary>
        [Fact(DisplayName = "Success: Returns a list of versions")]
        public async Task ReturnsOk()
        {
            await _launcherVersionRepo.AddAsync(new LauncherVersion
            {
                Version = "1.0.0",
                Changelog = "Initial release",
                VersionType = EVersionType.RELEASE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }, true, TestContext.Current.CancellationToken);

            var result = await _controller.GetLauncherVersions();
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + contentResult.Content);
        }

        /// <summary>
        /// Verifies that the endpoint returns 404 when no versions exist.
        /// </summary>
        [Fact(DisplayName = "Failure: No version exists")]
        public async Task ReturnsNotFound()
        {
            var result = await _controller.GetLauncherVersions();
            TestHelper.TestResponse(result, HttpStatusCode.NotFound, "No launcher versions found.");
        }
    }

    /// <summary>
    /// Tests for the GetLatestLauncherVersion endpoint.
    /// </summary>
    public class GetLatestLauncherVersionTests : LauncherControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public GetLatestLauncherVersionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Verifies that the endpoint returns the latest version successfully.
        /// </summary>
        [Fact(DisplayName = "Success: Returns the latest version")]
        public async Task ReturnsOk()
        {
            await _launcherVersionRepo.AddAsync(new LauncherVersion
            {
                Version = "1.0.0",
                Changelog = "Initial release",
                VersionType = EVersionType.RELEASE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }, true, TestContext.Current.CancellationToken);

            var result = await _controller.GetLatestLauncherVersion();
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + contentResult.Content);
        }

        /// <summary>
        /// Verifies that the endpoint returns 404 when no versions exist.
        /// </summary>
        [Fact(DisplayName = "Failure: No version exists")]
        public async Task ReturnsNotFound()
        {
            var result = await _controller.GetLatestLauncherVersion();
            TestHelper.TestResponse(result, HttpStatusCode.NotFound, "No launcher versions found.");
        }
    }

    /// <summary>
    /// Tests for the GetLauncherVersionDetails endpoint.
    /// </summary>
    public class GetLauncherVersionDetailsTests : LauncherControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public GetLauncherVersionDetailsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Verifies that the endpoint returns version details successfully.
        /// </summary>
        [Fact(DisplayName = "Success: Returns version details")]
        public async Task ReturnsOk()
        {
            var version = await _launcherVersionRepo.AddAsync(new LauncherVersion
            {
                Version = "1.0.0",
                Changelog = "Initial release",
                VersionType = EVersionType.RELEASE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }, true, TestContext.Current.CancellationToken);

            byte[] bytes = "Hello world"u8.ToArray();
            using var stream = new MemoryStream(bytes);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);


            var fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.zip",
                ContentType = "application/zip",
                Type = EFileDataType.LAUNCHER,
            }, true, TestContext.Current.CancellationToken);

            await _launcherVersionDataRepo.AddAsync(new LauncherVersionData
            {
                VersionId = version.Id,
                FileId = fd.Id,
                Os = ELauncherOs.WINDOWS,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }, true, TestContext.Current.CancellationToken);

            var result = await _controller.GetLauncherVersionDetails(version.Id);
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.Should().NotBeNull();
            _testOutputHelper.WriteLine("Result: " + contentResult.Content);
        }

        /// <summary>
        /// Verifies that the endpoint returns 404 when the version does not exist.
        /// </summary>
        [Fact(DisplayName = "Failure: No version exists")]
        public async Task ReturnsNotFound()
        {
            var result = await _controller.GetLauncherVersionDetails(1);
            TestHelper.TestResponse(result, HttpStatusCode.NotFound, "Launcher version not found.");
        }
    }

    /// <summary>
    /// Unit tests for the DownloadLauncherVersion functionality in the LauncherController.
    /// </summary>
    public class DownloadLauncherVersionTests : LauncherControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public DownloadLauncherVersionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Verifies that the endpoint successfully returns a file for a valid launcher version.
        /// </summary>
        [Fact(DisplayName = "Success: Returns file")]
        public async Task ReturnsOk()
        {
            var version = await _launcherVersionRepo.AddAsync(new LauncherVersion
            {
                Version = "1.0.0",
                Changelog = "Initial release",
                VersionType = EVersionType.RELEASE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }, true, TestContext.Current.CancellationToken);

            byte[] bytes;
            using (var zipStream = new MemoryStream())
            {
                await using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    var entry = archive.CreateEntry("test.txt");
                    await using var entryStream = await entry.OpenAsync(TestContext.Current.CancellationToken);
                    await using var writer = new StreamWriter(entryStream);
                    await writer.WriteAsync("Hello world");
                }
                bytes = zipStream.ToArray();
            }

            using var stream = new MemoryStream(bytes);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            stream.Position = 0;

            var fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.zip",
                ContentType = "application/zip",
                Type = EFileDataType.LAUNCHER,
            }, true, TestContext.Current.CancellationToken);
            var fr = await fd.SaveFileAsync(stream);
            fr.Success.Should().BeTrue();

            try
            {
                await _launcherVersionDataRepo.AddAsync(new LauncherVersionData
                {
                    VersionId = version.Id,
                    FileId = fd.Id,
                    Os = ELauncherOs.WINDOWS,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }, true, TestContext.Current.CancellationToken);

                var result = await _controller.DownloadLauncherVersion(version.Id, ELauncherOs.WINDOWS);
                if (result is ObjectResult objectResult)
                    _testOutputHelper.WriteLine("UNEXPECTED RESULT: " + objectResult.Value);

                result.Should().BeOfType<FileContentResult>();
                var fileContentResult = result as FileContentResult;
                fileContentResult.Should().NotBeNull();
                _testOutputHelper.WriteLine("Result: " + fileContentResult.ContentType);
            }
            finally
            {
                fd.DeleteFile();
            }
        }

        /// <summary>
        /// Verifies that the endpoint returns a 404 status when the launcher version does not exist.
        /// </summary>
        [Fact(DisplayName = "Failure: No version exists")]
        public async Task ReturnsNotFound_WhenVersionDoesNotExist()
        {
            var result = await _controller.DownloadLauncherVersion(999, ELauncherOs.WINDOWS);
            TestHelper.TestResponse(result, HttpStatusCode.NotFound, "Launcher version not found.");
        }

        /// <summary>
        /// Verifies that the endpoint returns a 404 status when the launcher version file does not exist.
        /// </summary>
        [Fact(DisplayName = "Failure: No version file exists")]
        public async Task ReturnsNotFound_WhenVersionDataDoesNotExist()
        {
            var version = await _launcherVersionRepo.AddAsync(new LauncherVersion
            {
                Version = "1.0.0",
                Changelog = "Initial release",
                VersionType = EVersionType.RELEASE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }, true, TestContext.Current.CancellationToken);

            var result = await _controller.DownloadLauncherVersion(version.Id, ELauncherOs.WINDOWS);
            TestHelper.TestResponse(result, HttpStatusCode.NotFound, "Launcher version data not found.");
        }
    }

    /// <summary>
    /// Unit tests for the CreateLauncherVersion functionality in the LauncherController.
    /// </summary>
    public class CreateLauncherVersionTests : LauncherControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public CreateLauncherVersionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Verifies that the endpoint successfully creates a launcher version.
        /// </summary>
        [Fact(DisplayName = "Success: Create launcher version")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller);
            var result = await _controller.CreateLauncherVersion(new CreateLauncherVersionRequest
            {
                Version = "1.0.0",
                VersionType = EVersionType.RELEASE,
                Changelog = "Release 1.0"
            });
            TestHelper.TestResponse(result, HttpStatusCode.OK, "Launcher version created successfully.");
        }

        /// <summary>
        /// Verifies that the endpoint returns a 400 status when a duplicate launcher version is created.
        /// </summary>
        [Fact(DisplayName = "Failure: Duplicate launcher version")]
        public async Task ReturnsBadRequest_WhenDuplicate()
        {
            await CreateUserAsync(_controller);

            await _launcherVersionRepo.AddAsync(new LauncherVersion
            {
                Version = "1.0.0",
                Changelog = "Initial",
                VersionType = EVersionType.RELEASE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, true, TestContext.Current.CancellationToken);

            var result = await _controller.CreateLauncherVersion(new CreateLauncherVersionRequest
            {
                Version = "1.0.0",
                VersionType = EVersionType.RELEASE,
                Changelog = "Duplicate"
            });
            TestHelper.TestResponse(result, HttpStatusCode.BadRequest, "A launcher version with the same version number already exists.");
        }

        /// <summary>
        /// Verifies that the endpoint returns a 401 status when no user is authenticated.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized_WhenNoUser()
        {
            var result = await _controller.CreateLauncherVersion(new CreateLauncherVersionRequest
            {
                Version = "1.0.0",
                VersionType = EVersionType.RELEASE,
                Changelog = "Initial"
            });
            TestHelper.TestResponse(result, HttpStatusCode.Unauthorized, "User not authenticated");
        }

        /// <summary>
        /// Verifies that the endpoint returns a 403 status when the user lacks sufficient permissions.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden_WhenInsufficientPermissions()
        {
            await CreateUserAsync(_controller, givePermissions: false);
            var result = await _controller.CreateLauncherVersion(new CreateLauncherVersionRequest
            {
                Version = "1.0.0",
                VersionType = EVersionType.RELEASE,
                Changelog = "Initial"
            });
            TestHelper.TestResponse(result, HttpStatusCode.Forbidden, "Permission denied.");
        }
    }

    /// <summary>
    /// Unit tests for the UpdateLauncherVersion functionality in the LauncherController.
    /// </summary>
    public class UpdateLauncherVersionTests : LauncherControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public UpdateLauncherVersionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Verifies that the endpoint successfully updates a launcher version.
        /// </summary>
        [Fact(DisplayName = "Success: Update launcher version")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller);

            var version = await _launcherVersionRepo.AddAsync(new LauncherVersion
            {
                Version = "1.0.0",
                Changelog = "Initial",
                VersionType = EVersionType.RELEASE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, true, TestContext.Current.CancellationToken);

            var result = await _controller.UpdateLauncherVersion(version.Id, new UpdateLauncherVersionRequest
            {
                Version = "1.0.1",
                Changelog = "Patch"
            });
            TestHelper.TestResponse(result, HttpStatusCode.OK, "Launcher version updated successfully.", testOutputHelper: _testOutputHelper);
        }

        /// <summary>
        /// Verifies that the endpoint returns a 404 status when the launcher version is not found.
        /// </summary>
        [Fact(DisplayName = "Failure: Version not found")]
        public async Task ReturnsNotFound_WhenMissing()
        {
            await CreateUserAsync(_controller);

            var result = await _controller.UpdateLauncherVersion(1, new UpdateLauncherVersionRequest
            {
                Version = "1.0.1",
                Changelog = "Patch"
            });
            TestHelper.TestResponse(result, HttpStatusCode.NotFound, "Launcher version not found.");
        }

        /// <summary>
        /// Verifies that the endpoint returns a 400 status when attempting to update to a duplicate version.
        /// </summary>
        [Fact(DisplayName = "Failure: Duplicate version on update")]
        public async Task ReturnsBadRequest_WhenDuplicate()
        {
            await CreateUserAsync(_controller);

            var v1 = await _launcherVersionRepo.AddAsync(new LauncherVersion
            {
                Version = "1.0.0",
                Changelog = "v1",
                VersionType = EVersionType.RELEASE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, true, TestContext.Current.CancellationToken);

            await _launcherVersionRepo.AddAsync(new LauncherVersion
            {
                Version = "1.0.1",
                Changelog = "Patch",
                VersionType = EVersionType.RELEASE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, true, TestContext.Current.CancellationToken);

            var result = await _controller.UpdateLauncherVersion(v1.Id, new UpdateLauncherVersionRequest
            {
                Version = "1.0.1"
            });
            TestHelper.TestResponse(result, HttpStatusCode.BadRequest, "A launcher version with the same version number already exists.");
        }

        /// <summary>
        /// Verifies that the endpoint returns a 401 status when no user is authenticated.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized_WhenNoUser()
        {
            var v1 = await _launcherVersionRepo.AddAsync(new LauncherVersion
            {
                Version = "1.0.0",
                Changelog = "v1",
                VersionType = EVersionType.RELEASE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, true, TestContext.Current.CancellationToken);

            var result = await _controller.UpdateLauncherVersion(v1.Id, new UpdateLauncherVersionRequest
            {
                Version = "1.0.1"
            });
            TestHelper.TestResponse(result, HttpStatusCode.Unauthorized, "User not authenticated");
        }

        /// <summary>
        /// Verifies that the endpoint returns a 403 status when the user lacks sufficient permissions.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden_WhenInsufficientPermissions()
        {
            await CreateUserAsync(_controller, givePermissions: false);

            var v1 = await _launcherVersionRepo.AddAsync(new LauncherVersion
            {
                Version = "1.0.0",
                Changelog = "v1",
                VersionType = EVersionType.RELEASE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, true, TestContext.Current.CancellationToken);

            var result = await _controller.UpdateLauncherVersion(v1.Id, new UpdateLauncherVersionRequest
            {
                Version = "1.0.1"
            });
            TestHelper.TestResponse(result, HttpStatusCode.Forbidden, "Permission denied.");
        }
    }

    /// <summary>
    /// Unit tests for the DeleteLauncherVersion functionality in the LauncherController.
    /// </summary>
    public class DeleteLauncherVersionTests : LauncherControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public DeleteLauncherVersionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Verifies that the endpoint successfully deletes an existing launcher version.
        /// </summary>
        [Fact(DisplayName = "Success: Delete launcher version")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller);

            var version = await _launcherVersionRepo.AddAsync(new LauncherVersion
            {
                Version = "8.0.0",
                Changelog = "to delete",
                VersionType = EVersionType.RELEASE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, true, TestContext.Current.CancellationToken);

            var result = await _controller.DeleteLauncherVersion(version.Id);
            TestHelper.TestResponse(result, HttpStatusCode.OK, "Launcher version deleted successfully.");
        }

        /// <summary>
        /// Verifies that the endpoint returns a 404 status when attempting to delete a non-existing launcher version.
        /// </summary>
        [Fact(DisplayName = "Failure: Delete non-existing version")]
        public async Task ReturnsNotFound_WhenMissing()
        {
            await CreateUserAsync(_controller);

            var result = await _controller.DeleteLauncherVersion(1);
            TestHelper.TestResponse(result, HttpStatusCode.NotFound, "Launcher version not found.");
        }

        /// <summary>
        /// Verifies that the endpoint returns a 403 status when the user lacks sufficient permissions to delete a launcher version.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden_WhenInsufficientPermissions()
        {
            await CreateUserAsync(_controller, givePermissions: false);

            var result = await _controller.DeleteLauncherVersion(1);
            TestHelper.TestResponse(result, HttpStatusCode.Forbidden, "Permission denied.");
        }
    }

    /// <summary>
    /// Unit tests for the AddLauncherVersionData functionality in the LauncherController.
    /// </summary>
    public class AddLauncherVersionDataTests : LauncherControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public AddLauncherVersionDataTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Verifies that the endpoint successfully adds launcher version data.
        /// </summary>
        [Fact(DisplayName = "Success: Add launcher version data")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller);

            var version = await _launcherVersionRepo.AddAsync(new LauncherVersion
            {
                Version = "9.0.0",
                Changelog = "with data",
                VersionType = EVersionType.RELEASE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, true, TestContext.Current.CancellationToken);

            byte[] bytes = "Hello world"u8.ToArray();
            using var stream = new MemoryStream(bytes);
            var formFile = new FormFile(stream, 0, stream.Length, "File", "test.zip")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/zip"
            };

            var request = new CreateLauncherVersionDataRequest
            {
                Os = ELauncherOs.WINDOWS,
                File = formFile
            };

            var result = await _controller.AddLauncherVersionData(version.Id, request);
            TestHelper.TestResponse(result, HttpStatusCode.OK, "Launcher version data added successfully.");
        }

        /// <summary>
        /// Verifies that the endpoint returns a 404 status when the launcher version does not exist.
        /// </summary>
        [Fact(DisplayName = "Failure: Add data to missing version")]
        public async Task ReturnsNotFound_WhenVersionMissing()
        {
            await CreateUserAsync(_controller);

            byte[] bytes = "Hello world"u8.ToArray();
            using var stream = new MemoryStream(bytes);
            var formFile = new FormFile(stream, 0, stream.Length, "File", "test.zip")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/zip"
            };

            var request = new CreateLauncherVersionDataRequest
            {
                Os = ELauncherOs.WINDOWS,
                File = formFile
            };

            var result = await _controller.AddLauncherVersionData(1, request);
            TestHelper.TestResponse(result, HttpStatusCode.NotFound, "Launcher version not found.");
        }

        /// <summary>
        /// Verifies that the endpoint returns a 401 status when no user is authenticated.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized_WhenNoUser()
        {
            byte[] bytes = "Hello world"u8.ToArray();
            using var stream = new MemoryStream(bytes);
            var formFile = new FormFile(stream, 0, stream.Length, "File", "test.zip")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/zip"
            };

            var result = await _controller.AddLauncherVersionData(1, new CreateLauncherVersionDataRequest
            {
                Os = ELauncherOs.WINDOWS,
                File = formFile
            });
            TestHelper.TestResponse(result, HttpStatusCode.Unauthorized, "User not authenticated");
        }

        /// <summary>
        /// Verifies that the endpoint returns a 403 status when the user lacks sufficient permissions.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden_WhenInsufficientPermissions()
        {
            await CreateUserAsync(_controller, givePermissions: false);

            byte[] bytes = "Hello world"u8.ToArray();
            using var stream = new MemoryStream(bytes);
            var formFile = new FormFile(stream, 0, stream.Length, "File", "test.zip")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/zip"
            };

            var result = await _controller.AddLauncherVersionData(1, new CreateLauncherVersionDataRequest
            {
                Os = ELauncherOs.WINDOWS,
                File = formFile
            });
            TestHelper.TestResponse(result, HttpStatusCode.Forbidden, "Permission denied.");
        }
    }

    /// <summary>
    /// Unit tests for the DeleteLauncherVersionData functionality in the LauncherController.
    /// </summary>
    public class DeleteLauncherVersionDataTests : LauncherControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public DeleteLauncherVersionDataTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Verifies that the endpoint successfully deletes launcher version data.
        /// </summary>
        [Fact(DisplayName = "Success: Delete launcher version data")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller);

            var version = await _launcherVersionRepo.AddAsync(new LauncherVersion
            {
                Version = "10.0.0",
                Changelog = "to delete data",
                VersionType = EVersionType.RELEASE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, true, TestContext.Current.CancellationToken);

            byte[] bytes = "Hello world"u8.ToArray();
            using var stream = new MemoryStream(bytes);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);

            var fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.zip",
                ContentType = "application/zip",
                Type = EFileDataType.LAUNCHER
            }, true, TestContext.Current.CancellationToken);
            stream.Position = 0;
            await fd.SaveFileAsync(stream);

            try
            {
                var versionData = await _launcherVersionDataRepo.AddAsync(new LauncherVersionData
                {
                    VersionId = version.Id,
                    FileId = fd.Id,
                    Os = ELauncherOs.WINDOWS,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }, true, TestContext.Current.CancellationToken);

                var result = await _controller.DeleteLauncherVersionData(version.Id, versionData.Id);
                TestHelper.TestResponse(result, HttpStatusCode.OK, "Launcher version data added successfully.");
            }
            finally
            {
                fd.DeleteFile();
            }
        }

        /// <summary>
        /// Verifies that the endpoint returns a 404 status when the launcher version data does not exist.
        /// </summary>
        [Fact(DisplayName = "Failure: Delete missing version data")]
        public async Task ReturnsNotFound_WhenMissing()
        {
            await CreateUserAsync(_controller);

            var result = await _controller.DeleteLauncherVersionData(1, 1);
            TestHelper.TestResponse(result, HttpStatusCode.NotFound, "Launcher version data not found.");
        }

        /// <summary>
        /// Verifies that the endpoint returns a 401 status when no user is authenticated.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized_WhenNoUser()
        {
            var result = await _controller.DeleteLauncherVersionData(1, 1);
            TestHelper.TestResponse(result, HttpStatusCode.Unauthorized, "User not authenticated");
        }

        /// <summary>
        /// Verifies that the endpoint returns a 403 status when the user lacks sufficient permissions.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden_WhenInsufficientPermissions()
        {
            await CreateUserAsync(_controller, givePermissions: false);

            var result = await _controller.DeleteLauncherVersionData(1, 1);
            TestHelper.TestResponse(result, HttpStatusCode.Forbidden, "Permission denied.");
        }
    }
}
