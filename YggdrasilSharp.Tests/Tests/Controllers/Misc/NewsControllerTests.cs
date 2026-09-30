using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Tavstal.YggdrasilSharp.Controllers.Misc;
using Tavstal.YggdrasilSharp.Models.Bodies.News;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Responses.News;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using Tavstal.YggdrasilSharp.Tests.Helpers;
using Tavstal.YggdrasilSharp.Tests.Models;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Controllers.Misc;

/// <summary>
/// Tests for <see cref="NewsController"/> instance methods.
/// </summary>
public class NewsControllerTests : ControllerTestBase
{
    private readonly IRepository<News> _newsRepo;
    private readonly IRepository<FileData> _fileDataRepo;
    private readonly Mock<ILogger<NewsController>> _loggerMock = new();
    private readonly NewsController _controller;

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="testOutputHelper">XUnit test output helper forwarded to the base class.</param>
    public NewsControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _newsRepo = new Repository<News>(_dbContext);
        _fileDataRepo = new Repository<FileData>(_dbContext);
        _controller = new NewsController(_loggerMock.Object, _userManager, _userStore, _newsRepo, _fileDataRepo, _memoryCacheService, AppConfiguration);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }

    /// <summary>
    /// Tests for the GetNews() endpoint which returns all news entries.
    /// </summary>
    public class GetNewsTests : NewsControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public GetNewsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success scenario:
        /// Creates a user, inserts a news item with an associated banner file and asserts
        /// that <see cref="NewsController.GetNews"/> returns a non-empty JSON content result
        /// that can be deserialized into a list of <see cref="NewsResponseBody"/>.
        /// </summary>
        [Fact(DisplayName = "Success: Returns existing news with banner URL")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller);

            using var stream = TestHelper.CreateTestImage(600, 100);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            stream.Position = 0;

            FileData fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.png",
                ContentType = "image/png",
                Type = EFileDataType.NEWS_BANNER
            }, true, TestContext.Current.CancellationToken);
            fd.SaveFile(stream);

            await _newsRepo.AddAsync(new News
            {
                Title = "Test Title",
                Content = "Test Content",
                BannerId = fd.Id,
                CreatedAt = DateTimeOffset.UtcNow
            }, true, TestContext.Current.CancellationToken);
            
            var result = await _controller.GetNews();
            
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult!.Content.Should().NotBeNull();
            var list = JsonConvert.DeserializeObject<List<NewsResponseBody>>(contentResult.Content);
            list.Should().NotBeNull();
            _testOutputHelper.WriteLine($"Response: {contentResult.Content}");
            
            var files = await _fileDataRepo.QueryAsync(x => x.Hash == fileHash && x.Type == EFileDataType.NEWS_BANNER, TestContext.Current.CancellationToken);
            foreach (var f in files)
                f.DeleteFile();
        }
        
        /// <summary>
        /// Success scenario:
        /// Ensures that when there are no news entries the endpoint still returns a valid,
        /// empty JSON list and a content result.
        /// </summary>
        [Fact(DisplayName = "Success: Returns empty list when no news exists")]
        public async Task ReturnsOk_WithEmptyList()
        {
            await CreateUserAsync(_controller);
            
            var result = await _controller.GetNews();
            
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult!.Content.Should().NotBeNull();
            var list = JsonConvert.DeserializeObject<List<NewsResponseBody>>(contentResult.Content);
            list.Should().NotBeNull();
            _testOutputHelper.WriteLine($"Response: {contentResult.Content}");
        }
    }

    /// <summary>
    /// Tests for the GetLatestNews() endpoint which returns the most recent news items.
    /// </summary>
    public class GetLatestNewsTests : NewsControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public GetLatestNewsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success scenario:
        /// Inserts a news item with a banner and asserts the controller returns JSON content
        /// containing the news as <see cref="NewsResponseBody"/> list.
        /// </summary>
        [Fact(DisplayName = "Success: Returns existing news with banner URL")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller);

            using var stream = TestHelper.CreateTestImage(600, 100);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            stream.Position = 0;

            FileData fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.png",
                ContentType = "image/png",
                Type = EFileDataType.NEWS_BANNER
            }, true, TestContext.Current.CancellationToken);
            fd.SaveFile(stream);

            await _newsRepo.AddAsync(new News
            {
                Title = "Test Title",
                Content = "Test Content",
                BannerId = fd.Id,
                CreatedAt = DateTimeOffset.UtcNow
            }, true, TestContext.Current.CancellationToken);
            
            var result = await _controller.GetLatestNews();
            
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult!.Content.Should().NotBeNull();
            var list = JsonConvert.DeserializeObject<List<NewsResponseBody>>(contentResult.Content);
            list.Should().NotBeNull();
            _testOutputHelper.WriteLine($"Response: {contentResult.Content}");
            
            var files = await _fileDataRepo.QueryAsync(x => x.Hash == fileHash && x.Type == EFileDataType.NEWS_BANNER, TestContext.Current.CancellationToken);
            foreach (var f in files)
                f.DeleteFile();
        }
        
        /// <summary>
        /// Success scenario:
        /// Validates that the endpoint returns an empty but valid JSON array when there are no news items.
        /// </summary>
        [Fact(DisplayName = "Success: Returns empty list when no news exists")]
        public async Task ReturnsOk_WithEmptyList()
        {
            await CreateUserAsync(_controller);
            
            var result = await _controller.GetLatestNews();
            
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult!.Content.Should().NotBeNull();
            var list = JsonConvert.DeserializeObject<List<NewsResponseBody>>(contentResult.Content);
            list.Should().NotBeNull();
            _testOutputHelper.WriteLine($"Response: {contentResult.Content}");
        }
    }

    /// <summary>
    /// Tests for the GetNewsById(int id) endpoint which returns a single news item by id.
    /// </summary>
    public class GetNewsByIdTests : NewsControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public GetNewsByIdTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success scenario:
        /// Adds a news entry with a banner file then calls GetNewsById and asserts that a
        /// <see cref="NewsResponseBody"/> JSON object is returned inside a <see cref="ContentResult"/>.
        /// </summary>
        [Fact(DisplayName = "Success: Returns existing news with banner URL")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller);

            using var stream = TestHelper.CreateTestImage(600, 100);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            stream.Position = 0;

            FileData fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.png",
                ContentType = "image/png",
                Type = EFileDataType.NEWS_BANNER
            }, true, TestContext.Current.CancellationToken);
            fd.SaveFile(stream);

            var news = await _newsRepo.AddAsync(new News
            {
                Title = "Test Title",
                Content = "Test Content",
                BannerId = fd.Id,
                CreatedAt = DateTimeOffset.UtcNow
            }, true, TestContext.Current.CancellationToken);
            
            var result = await _controller.GetNewsById(news.Id);
            
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult!.Content.Should().NotBeNull();
            var list = JsonConvert.DeserializeObject<NewsResponseBody>(contentResult.Content);
            list.Should().NotBeNull();
            _testOutputHelper.WriteLine($"Response: {contentResult.Content}");
            
            var files = await _fileDataRepo.QueryAsync(x => x.Hash == fileHash && x.Type == EFileDataType.NEWS_BANNER, TestContext.Current.CancellationToken);
            foreach (var f in files)
                f.DeleteFile();
        }
        
        /// <summary>
        /// Failure scenario:
        /// When there is no news item for the provided id, the controller should return a 404 NotFound
        /// wrapped in an <see cref="ObjectResult"/>.
        /// </summary>
        [Fact(DisplayName = "Failure: Returns NotFound when news does not exist")]
        public async Task ReturnsNotFound_WhenNewsDoesNotExist()
        {
            await CreateUserAsync(_controller);
            
            var result = await _controller.GetNewsById(5);
            
            TestHelper.TestResponse(result, HttpStatusCode.NotFound);
        }
    }

    /// <summary>
    /// Tests for creating news via CreateNews(NewsCreateRequestBody).
    /// Covers success path and common failure cases (permissions and invalid file type).
    /// </summary>
    public class CreateNewsTests : NewsControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public CreateNewsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success scenario:
        /// Creates a user with permissions, provides a valid PNG banner file, calls CreateNews
        /// and asserts a 201 Created response in an <see cref="ObjectResult"/>.
        /// </summary>
        [Fact(DisplayName = "Success: Creates news with banner")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller);
            
            using var stream = TestHelper.CreateTestImage(600, 100);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            
            var result = await _controller.CreateNews(new NewsCreateRequestBody
            {
                Title = "Test News",
                Content = "This is a test news item.",
                Banner = file
            });
            
            TestHelper.TestResponse(result, HttpStatusCode.Created);
            
            // Clean-up
            stream.Position = 0;
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            var files = await _fileDataRepo.QueryAsync(x => x.Hash == fileHash && x.Type == EFileDataType.NEWS_BANNER, TestContext.Current.CancellationToken);
            foreach (var f in files)
                f.DeleteFile();
        }

        /// <summary>
        /// Failure scenario:
        /// When the current user lacks the required permissions to create news, the call should return 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden_WhenUserLacksPermissions()
        {
            await CreateUserAsync(_controller, givePermissions: false);

            using var stream = TestHelper.CreateTestImage(600, 100);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            var result = await _controller.CreateNews(new NewsCreateRequestBody
            {
                Title = "Test News",
                Content = "This is a test news item.",
                Banner = file
            });

            TestHelper.TestResponse(result, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// Failure scenario:
        /// When an invalid banner file type (non-PNG) is provided the controller should respond with 400 Bad Request.
        /// </summary>
        [Fact(DisplayName = "Failure: Invalid banner file type")]
        public async Task ReturnsBadRequest_WhenBannerFileTypeIsInvalid()
        {
            await CreateUserAsync(_controller);

            using var stream = TestHelper.CreateTestImage(600, 100);
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "test.jpg")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpg"
            };

            var result = await _controller.CreateNews(new NewsCreateRequestBody
            {
                Title = "Test News",
                Content = "This is a test news item.",
                Banner = file
            });

            TestHelper.TestResponse(result, HttpStatusCode.BadRequest);
        }
    }
    
    /// <summary>
    /// Tests for the UpdateNews(int id, NewsUpdateRequestBody) endpoint which updates existing news entries.
    /// </summary>
    public class UpdateNewsTests : NewsControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public UpdateNewsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success scenario:
        /// Prepares an existing news item and asserts that updating the title returns HTTP 200 OK.
        /// </summary>
        [Fact(DisplayName = "Success: Updates news with new title")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller);

            using var stream = TestHelper.CreateTestImage(600, 100);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            FileData fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.png",
                ContentType = "image/png",
                Type = EFileDataType.NEWS_BANNER
            }, true, TestContext.Current.CancellationToken);
            fd.SaveFile(stream);

            try
            {
                var news = await _newsRepo.AddAsync(new News
                {
                    Title = "Test title",
                    Content = "This is a test news item.",
                    BannerId = fd.Id,
                    CreatedAt = DateTimeOffset.UtcNow
                }, true, TestContext.Current.CancellationToken);

                var result = await _controller.UpdateNews(news.Id, new NewsUpdateRequestBody
                {
                    Title = "Updated Title"
                });
                
                TestHelper.TestResponse(result, HttpStatusCode.OK);
            }
            finally
            {
                fd.DeleteFile();
            }
        }

        /// <summary>
        /// Failure scenario:
        /// Asserts that an unauthorized user (no permissions) cannot update a news item and receives 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden_WhenUserLacksPermissions()
        {
            await CreateUserAsync(_controller, givePermissions: false);

            using var stream = TestHelper.CreateTestImage(600, 100);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            FileData fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.png",
                ContentType = "image/png",
                Type = EFileDataType.NEWS_BANNER
            }, true, TestContext.Current.CancellationToken);
            fd.SaveFile(stream);

            try
            {
                var news = await _newsRepo.AddAsync(new News
                {
                    Title = "Test title",
                    Content = "This is a test news item.",
                    BannerId = fd.Id,
                    CreatedAt = DateTimeOffset.UtcNow
                }, true, TestContext.Current.CancellationToken);

                var result = await _controller.UpdateNews(news.Id, new NewsUpdateRequestBody
                {
                    Title = "Updated Title"
                });
              
                TestHelper.TestResponse(result, HttpStatusCode.Forbidden);
            }
            finally
            {
                fd.DeleteFile();
            }
        }

        /// <summary>
        /// Failure scenario:
        /// When the requested news id does not exist the controller should return 404 NotFound.
        /// </summary>
        [Fact(DisplayName = "Failure: News not found")]
        public async Task ReturnsNotFound_WhenNewsDoesNotExist()
        {
            await CreateUserAsync(_controller);
            
            var result = await _controller.UpdateNews(1, new NewsUpdateRequestBody
            {
                Title = "Updated Title"
            });
            
            TestHelper.TestResponse(result, HttpStatusCode.NotFound);
        }
    }
    
    /// <summary>
    /// Tests for deleting news via DeleteNews(int id).
    /// </summary>
    public class DeleteNewsTests : NewsControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
        public DeleteNewsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success scenario:
        /// Asserts deleting existing news returns HTTP 200 OK.
        /// </summary>
        [Fact(DisplayName = "Success: Deletes existing news")]
        public async Task ReturnsOk()
        {
            await CreateUserAsync(_controller);

            using var stream = TestHelper.CreateTestImage(600, 100);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            FileData fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.png",
                ContentType = "image/png",
                Type = EFileDataType.NEWS_BANNER
            }, true, TestContext.Current.CancellationToken);
            fd.SaveFile(stream);

            var news = await _newsRepo.AddAsync(new News
            {
                Title = "Test title",
                Content = "This is a test news item.",
                BannerId = fd.Id,
                CreatedAt = DateTimeOffset.UtcNow
            }, true, TestContext.Current.CancellationToken);
            
            var result = await _controller.DeleteNews(news.Id);
            
            TestHelper.TestResponse(result, HttpStatusCode.OK);
        }

        /// <summary>
        /// Failure scenario:
        /// When the current user lacks the delete permission, the call should return 403 Forbidden.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden_WhenUserLacksPermissions()
        {
            await CreateUserAsync(_controller, givePermissions: false);

            using var stream = TestHelper.CreateTestImage(600, 100);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            FileData fd = await _fileDataRepo.AddAsync(new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.png",
                ContentType = "image/png",
                Type = EFileDataType.NEWS_BANNER
            }, true, TestContext.Current.CancellationToken);
            fd.SaveFile(stream);

            try
            {
                var news = await _newsRepo.AddAsync(new News
                {
                    Title = "Test title",
                    Content = "This is a test news item.",
                    BannerId = fd.Id,
                    CreatedAt = DateTimeOffset.UtcNow
                }, true, TestContext.Current.CancellationToken);

                var result = await _controller.DeleteNews(news.Id);
                
                TestHelper.TestResponse(result, HttpStatusCode.Forbidden);
            }
            finally
            {
                fd.DeleteFile();
            }
        }

        /// <summary>
        /// Failure scenario:
        /// When the news id does not exist, DeleteNews should return a 404 NotFound.
        /// </summary>
        [Fact(DisplayName = "Failure: News not found")]
        public async Task ReturnsNotFound_WhenNewsDoesNotExist()
        {
            await CreateUserAsync(_controller);
            
            var result = await _controller.DeleteNews(1);
            TestHelper.TestResponse(result, HttpStatusCode.NotFound);
        }
    }
}