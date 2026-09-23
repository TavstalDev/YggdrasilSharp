using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tavstal.YggdrasilSharp.Controllers.Yggdrasil;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using Tavstal.YggdrasilSharp.Tests.Models;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Controllers.Yggdrasil;

/// <summary>
/// Unit tests for <see cref="TexturesController"/>.
/// </summary>
public class TexturesControllerTests : ControllerTestBase
{
    private readonly IRepository<FileData> _fileDataRepo;
    private readonly Mock<ILogger<TexturesController>> _loggerMock = new();
    private readonly TexturesController _controller;
    
    /// <summary>
    /// Initializes a new instance of <see cref="TexturesControllerTests"/>.
    /// Constructs the controller with the mock logger, test database context, memory cache service and test settings.
    /// Also wires the controller's ControllerContext to the test HttpContext provided by <see cref="ControllerTestBase"/>.
    /// </summary>
    /// <param name="testOutputHelper">XUnit output helper forwarded to the base class for logging test output.</param>
    public TexturesControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _fileDataRepo = new Repository<FileData>(_dbContext);
        _controller = new TexturesController(_loggerMock.Object, _userStore, _fileDataRepo, _memoryCacheService, _settings);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }
    
    /// <summary>
    /// Success case: When the memory cache already contains the file bytes and content type for the requested hash.
    /// </summary>
    [Fact(DisplayName = "Success: Returns file from cache")]
    public async Task ReturnsFileFromCache_WhenCacheContainsBytes()
    {
        string hash = "mock-hash";
        byte[] bytes = "mock-file-bytes"u8.ToArray();
        string contentType = "image/png";
        _memoryCacheService.SetValue($"file:{hash}", (bytes, contentType), TimeSpan.FromDays(1));

        IActionResult result = await _controller.GetTexture(hash);

        result.Should().BeOfType<FileContentResult>();
        var fileResult = result as FileContentResult;
        fileResult!.ContentType.Should().Be(contentType);
        fileResult.FileContents.Should().Equal(bytes);

        var etag = _controllerHttpContext.Response.Headers.ETag.ToString();
        etag.Should().Be('"' + hash + '"');
        _testOutputHelper.WriteLine("Result: " + fileResult.ContentType);
    }
    
    /// <summary>
    /// Success case: If the incoming request contains an If-None-Match header that matches the file ETag.
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

        IActionResult result = await _controller.GetTexture(hash);

        result.Should().BeOfType<StatusCodeResult>();
        var status = result as StatusCodeResult;
        status!.StatusCode.Should().Be(304);

        var etag = _controllerHttpContext.Response.Headers.ETag.ToString();
        etag.Should().Be('"' + hash + '"');
        _testOutputHelper.WriteLine("Returned 304 with ETag: " + etag);
    }
    
    /// <summary>
    /// Failure case: When the model state is invalid (e.g. missing or invalid hash).
    /// </summary>
    [Fact(DisplayName = "Failure: Model state is invalid")]
    public async Task ReturnsBadRequest_WhenModelStateInvalid()
    {
        _controller.ModelState.AddModelError("hash", "Hash is required.");

        IActionResult result = await _controller.GetTexture(null!);

        result.Should().BeOfType<ObjectResult>();
        var obj = result as ObjectResult;
        obj!.StatusCode.Should().Be(400);
        _testOutputHelper.WriteLine("Result: " + obj.Value);
    }
    
    /// <summary>
    /// Failure case: When neither the memory cache nor the database contains a file record for the requested hash.
    /// </summary>
    [Fact(DisplayName = "Failure: File is not found")]
    public async Task ReturnsNotFound_WhenNoFileInDbAndNoCache()
    {
        string hash = "no-such-file-hash";
        _memoryCacheService.RemoveValue($"file:{hash}");

        IActionResult result = await _controller.GetTexture(hash);

        result.Should().BeOfType<ObjectResult>();
        var obj = result as ObjectResult;
        obj!.StatusCode.Should().Be(404);
        _testOutputHelper.WriteLine("Result: " + obj.Value);
    }
    
    /// <summary>
    /// Failure case: When the database contains a FileData record for the requested hash but the actual file bytes are missing on disk/storage.
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

        IActionResult result = await _controller.GetTexture(hash);

        result.Should().BeOfType<ObjectResult>();
        var obj = result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
        _testOutputHelper.WriteLine("Result: " + obj.Value);
    }
}
