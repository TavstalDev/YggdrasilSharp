using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tavstal.YggdrasilSharp.Controllers.Misc;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;

namespace Tavstal.YggdrasilSharp.Tests.Controllers.Misc;

/// <summary>
/// Unit tests for <see cref="FilesController"/> covering file retrieval behavior:
/// <br/>- returning cached files,
/// <br/>- conditional GET (ETag / If-None-Match),
/// <br/>- handling invalid model state,
/// <br/>- missing file scenarios (404 and 500 cases).
/// <br/>Tests use an in-memory database and the shared TestHelper memory cache to avoid disk IO where possible.
/// </summary>
public class FilesControllerTests : ControllerTestBase
{
    private readonly IRepository<FileData> _fileDataRepo;
    private readonly Mock<ILogger<FilesController>> _loggerMock = new();
    private readonly FilesController _controller;

    /// <summary>
    /// Initializes the test fixture:
    /// <br/>- creates an in-memory DB context,
    /// <br/>- uses the shared TestHelper memory cache,
    /// <br/>- constructs a FilesController with fake logger and test settings,
    /// <br/>- attaches a DefaultHttpContext configured with a test IP, User-Agent and Host.
    /// </summary>
    /// <param name="testOutputHelper">XUnit-provided output helper for logging.</param>
    public FilesControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _fileDataRepo = new Repository<FileData>(_dbContext);
        _controller = new FilesController(_loggerMock.Object, _userStore, _fileDataRepo, _memoryCacheService, _settings);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }

    /// <summary>
    /// Success: when the memory cache contains the file bytes for the provided hash,
    /// the controller should return a FileContentResult with the same content-type and bytes,
    /// and the response should include the expected ETag header.
    /// </summary>
    [Fact(DisplayName = "Success: Returns file from cache")]
    public async Task ReturnsFileFromCache_WhenCacheContainsBytes()
    {
        string hash = "mock-hash";
        byte[] bytes = "mock-file-bytes"u8.ToArray();
        string contentType = "image/png";
        _memoryCacheService.SetValue($"file:{hash}", (bytes, contentType), TimeSpan.FromDays(1));

        IActionResult result = await _controller.GetFile(hash);

        result.Should().BeOfType<FileContentResult>();
        var fileResult = result as FileContentResult;
        fileResult!.ContentType.Should().Be(contentType);
        fileResult.FileContents.Should().Equal(bytes);

        var etag = _controllerHttpContext.Response.Headers.ETag.ToString();
        etag.Should().Be('"' + hash + '"');
        _testOutputHelper.WriteLine("Result: " + fileResult.ContentType);
    }

    /// <summary>
    /// Success: when the request contains an If-None-Match header that matches the generated ETag,
    /// the controller should return a 304 Not Modified (StatusCodeResult) and set the ETag header.
    /// </summary>
    [Fact(DisplayName = "Success: Returns 304 Not Modified when If-None-Match matches ETag")]
    public async Task ReturnsNotModified_WhenIfNoneMatchMatches()
    {
        string hash = "mock-hash";
        byte[] bytes = "mock-file-bytes"u8.ToArray();
        string contentType = "image/png";
        _memoryCacheService.SetValue($"file:{hash}", (bytes, contentType), TimeSpan.FromDays(1));

        // Set If-None-Match header to match the ETag that would be generated for this file
        _controllerHttpContext.Request.Headers.IfNoneMatch = '"' + hash + '"';

        IActionResult result = await _controller.GetFile(hash);

        result.Should().BeOfType<StatusCodeResult>();
        var status = result as StatusCodeResult;
        status!.StatusCode.Should().Be(304);

        var etag = _controllerHttpContext.Response.Headers.ETag.ToString();
        etag.Should().Be('"' + hash + '"');
        _testOutputHelper.WriteLine("Returned 304 with ETag: " + etag);
    }

    /// <summary>
    /// Failure: when ModelState is invalid (e.g. missing required route parameter),
    /// the controller should return a 400 Bad Request (ObjectResult).
    /// </summary>
    [Fact(DisplayName = "Failure: Model state is invalid")]
    public async Task ReturnsBadRequest_WhenModelStateInvalid()
    {
        _controller.ModelState.AddModelError("hash", "Hash is required.");

        IActionResult result = await _controller.GetFile(null!);

        result.Should().BeOfType<ObjectResult>();
        var obj = result as ObjectResult;
        obj!.StatusCode.Should().Be(400);
        _testOutputHelper.WriteLine("Result: " + obj.Value);
    }

    /// <summary>
    /// Failure: when there is no cache entry and no matching DB record, the controller should return 404 Not Found.
    /// </summary>
    [Fact(DisplayName = "Failure: File is not found")]
    public async Task ReturnsNotFound_WhenNoFileInDbAndNoCache()
    {
        string hash = "no-such-file-hash";
        _memoryCacheService.RemoveValue($"file:{hash}");

        IActionResult result = await _controller.GetFile(hash);

        result.Should().BeOfType<ObjectResult>();
        var obj = result as ObjectResult;
        obj!.StatusCode.Should().Be(404);
        _testOutputHelper.WriteLine("Result: " + obj.Value);
    }

    /// <summary>
    /// Failure: when a DB entry exists but its physical file data is missing (FileData.GetFileData returns null),
    /// the controller should return 500 Internal Server Error.
    /// </summary>
    [Fact(DisplayName = "Failure: DB entry exists but file data is missing")]
    public async Task ReturnsInternalServerError_WhenFileDataMissingOnDb()
    {
        string hash = "db-file-missing";
        _memoryCacheService.RemoveValue($"file:{hash}");

        var fileData = new FileData
        {
            Hash = hash,
            FileName = "non-existing-file.png",
            ContentType = "image/png",
            Type = EFileDataType.SKIN
        };
        await _fileDataRepo.AddAsync(fileData, true, TestContext.Current.CancellationToken);

        IActionResult result = await _controller.GetFile(hash);

        result.Should().BeOfType<ObjectResult>();
        var obj = result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
        _testOutputHelper.WriteLine("Result: " + obj.Value);
    }
}