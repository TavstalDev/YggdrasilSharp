using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using SkiaSharp;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Bodies.News;
using Tavstal.YggdrasilSharp.Models.Claims;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Responses.News;
using Tavstal.YggdrasilSharp.Serialization;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using Tavstal.YggdrasilSharp.Utils.Helpers;
using RateLimits = Tavstal.YggdrasilSharp.Models.RateLimiting.Constants.RateLimits;

namespace Tavstal.YggdrasilSharp.Controllers.Misc;

/// <summary>
/// Controller for managing news articles, including retrieving, creating, updating, and deleting news.
/// </summary>
[ApiController]
[Route("/news")]
public class NewsController : CustomControllerBase
{
    private readonly IRepository<News> _newsRepo;
    private readonly IRepository<FileData> _fileDataRepo;
    private readonly AppConfiguration _appConfiguration;
    private readonly MemoryCacheService _cacheService;
    private readonly TimeSpan CacheTTL = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Initializes a new instance of the <see cref="NewsController"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for logging.</param>
    /// <param name="userManager">Custom user manager for user operations.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="appConfiguration">Application settings.</param>
    /// <param name="newsRepo">Repository for managing news entities.</param>
    /// <param name="fileDataRepo">Repository for managing file data (news banners).</param>
    /// <param name="cacheService">Service for caching news data.</param>
    public NewsController(ILogger<NewsController> logger, CustomUserManager userManager, CustomUserStore userStore, AppConfiguration appConfiguration,
        IRepository<News> newsRepo, IRepository<FileData> fileDataRepo, MemoryCacheService cacheService)
        : base(logger, userManager, userStore, appConfiguration)
    {
        _newsRepo = newsRepo;
        _fileDataRepo = fileDataRepo;
        _appConfiguration = appConfiguration;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Retrieves all news articles.
    /// </summary>
    /// <response code="200">Returns a list of all news articles.</response>
    [HttpGet]
    [JsonResponse(typeof(List<NewsResponseBody>))]
    public async Task<IActionResult> GetNews()
    {
        try
        {
            if (_cacheService.TryGetValue("news:all", out List<NewsResponseBody>? cachedNews) && cachedNews != null)
            {
                Response.Headers.CacheControl =
                    "public,max-age=3600,immutable";
                return JsonResult(cachedNews, CustomJsonContext.Default.ListNewsResponseBody);
            }


            var news = await _newsRepo.QueryAsync(null);
            List<NewsResponseBody> newsResponse = [];
            foreach (News n in news)
            {
                string bannerUrl = string.Empty;
                FileData? fd =
                    await _fileDataRepo.FindAsync(x => x.Id == n.BannerId && x.Type == EFileDataType.NEWS_BANNER);
                if (fd != null)
                    bannerUrl = fd.GetUrl(_appConfiguration.Misc.ApiUrl);
                newsResponse.Add(new NewsResponseBody
                {
                    Title = n.Title,
                    Content = n.Content,
                    BannerUrl = bannerUrl
                });
            }

            _cacheService.SetValue("news:all", newsResponse, CacheTTL);
            Response.Headers.CacheControl =
                "public,max-age=3600,immutable";
            return JsonResult(newsResponse, CustomJsonContext.Default.ListNewsResponseBody);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Failed to retrieve news articles.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Retrieves the latest news articles.
    /// </summary>
    /// <response code="200">Returns the latest news articles.</response>
    [HttpGet("latest")]
    [JsonResponse(typeof(List<NewsResponseBody>))]
    public async Task<IActionResult> GetLatestNews([FromQuery] int count = 5)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return JsonResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            if (count < 1)
                count = 1;
            else if (count > 20)
                count = 20;

            if (_cacheService.TryGetValue("news:latest", out List<NewsResponseBody>? cachedNews) && cachedNews != null)
            {
                Response.Headers.CacheControl =
                    "public,max-age=3600,immutable";
                return JsonResult(cachedNews, CustomJsonContext.Default.ListNewsResponseBody);
            }

            var news = await _newsRepo.QueryAsync(null);
            news = news.OrderByDescending(x => x.CreatedAt);
            news = news.Take(count);
            List<NewsResponseBody> newsResponse = [];
            foreach (News n in news)
            {
                string bannerUrl = string.Empty;
                FileData? fd =
                    await _fileDataRepo.FindAsync(x => x.Id == n.BannerId && x.Type == EFileDataType.NEWS_BANNER);
                if (fd != null)
                    bannerUrl = fd.GetUrl(_appConfiguration.Misc.ApiUrl);
                newsResponse.Add(new NewsResponseBody
                {
                    Title = n.Title,
                    Content = n.Content,
                    BannerUrl = bannerUrl
                });
            }

            _cacheService.SetValue("news:latest", newsResponse, CacheTTL);
            Response.Headers.CacheControl =
                "public,max-age=3600,immutable";
            return JsonResult(newsResponse, CustomJsonContext.Default.ListNewsResponseBody);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Failed to retrieve latest news articles.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Retrieves a specific news article by its ID.
    /// </summary>
    /// <param name="id">The ID of the news article.</param>
    /// <response code="200">Returns the news article.</response>
    /// <response code="404">News article not found.</response>
    [HttpGet("{id}")]
    [JsonResponse(typeof(NewsResponseBody)), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNewsById([BindRequired, FromRoute] ulong id)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return JsonResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            if (_cacheService.TryGetValue($"news:{id}", out NewsResponseBody? cachedNews) && cachedNews != null)
            {
                Response.Headers.CacheControl =
                    "public,max-age=3600,immutable";
                return JsonResult(cachedNews, CustomJsonContext.Default.NewsResponseBody);
            }

            News? news = await _newsRepo.FindByIdAsync(id);
            if (news == null)
                return JsonResult(HttpStatusCode.NotFound, "News article not found.");

            string bannerUrl = string.Empty;
            FileData? fd =
                await _fileDataRepo.FindAsync(x => x.Id == news.BannerId && x.Type == EFileDataType.NEWS_BANNER);
            if (fd != null)
                bannerUrl = fd.GetUrl(_appConfiguration.Misc.ApiUrl);

            var responseBody = new NewsResponseBody
            {
                Title = news.Title,
                Content = news.Content,
                BannerUrl = bannerUrl
            };
            _cacheService.SetValue($"news:{id}", responseBody, CacheTTL);
            Response.Headers.CacheControl =
                "public,max-age=3600,immutable";
            return JsonResult(responseBody, CustomJsonContext.Default.NewsResponseBody);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Failed to retrieve news article.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    #region Admin endpoints
    /// <summary>
    /// Creates a new news article.
    /// </summary>
    /// <param name="requestBody">The request body containing news details.</param>
    /// <response code="200">News article created successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized. User is not authenticated.</response>
    /// <response code="403">Forbidden. Insufficient permissions.</response>
    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer,Basic")]
    [EnableRateLimiting(RateLimits.FixedWindow.ADMIN)]
    [Consumes("multipart/form-data")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status400BadRequest), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateNews([Required, FromForm] NewsCreateRequestBody requestBody)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return JsonResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            var user = await GetCurrentUserAsync();
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await UserManager.HasPermissionAsync(user, CustomPermissions.News.Create))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            if (requestBody.Banner.Length > 1024 * 500)
                return JsonResult(HttpStatusCode.BadRequest, "File size exceeds the 500 KB limit.");

            if (!requestBody.Banner.FileName.EndsWith(".png"))
                return JsonResult(HttpStatusCode.BadRequest, "Only PNG files are allowed.");

            await using var stream = requestBody.Banner.OpenReadStream();
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream);
            string fileHash = Convert.ToHexStringLower(hashBytes);
            stream.Position = 0;

            if (!await SkiaHelper.IsValidFormatAsync(stream, SKEncodedImageFormat.Png, Logger))
                return JsonResult(HttpStatusCode.BadRequest, "Invalid image format (not a real PNG).");

            FileData fd = new FileData
            {
                Hash = fileHash,
                FileName = $"{Guid.NewGuid():N}.png",
                ContentType = "image/png",
                Type = EFileDataType.NEWS_BANNER
            };
            var uploadResult = await fd.SaveFileAsync(stream);
            if (!uploadResult.Success)
                return JsonResult(uploadResult.StatusCode, uploadResult.Message);


            fd = await _fileDataRepo.AddAsync(fd, true);

            await _newsRepo.AddAsync(new News
            {
                Title = requestBody.Title,
                Content = requestBody.Content,
                BannerId = fd.Id,
                CreatedAt = DateTimeOffset.UtcNow
            }, true);

            InvalidateNewsCache();
            return JsonResult(HttpStatusCode.Created, "News article created successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Failed to create new article.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Updates an existing news article.
    /// </summary>
    /// <param name="id">The ID of the news article to update.</param>
    /// <param name="requestBody">The request body containing updated news details.</param>
    /// <response code="200">News article updated successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized. User is not authenticated.</response>
    /// <response code="403">Forbidden. Insufficient permissions.</response>
    /// <response code="404">News article not found.</response>
    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer,Basic")]
    [EnableRateLimiting(RateLimits.FixedWindow.ADMIN)]
    [Consumes("multipart/form-data")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status400BadRequest), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNews([BindRequired, FromRoute] ulong id, [Required, FromForm] NewsUpdateRequestBody requestBody)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return JsonResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            var user = await GetCurrentUserAsync();
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await UserManager.HasPermissionAsync(user, CustomPermissions.News.Update))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            News? news = await _newsRepo.FindByIdAsync(id);
            if (news == null)
                return JsonResult(HttpStatusCode.NotFound, "News article not found.");

            if (!string.IsNullOrEmpty(requestBody.Title))
                news.Title = requestBody.Title;

            if (!string.IsNullOrEmpty(requestBody.Content))
                news.Content = requestBody.Content;

            if (requestBody.Banner != null)
            {
                if (requestBody.Banner.Length > 1024 * 500)
                    return JsonResult(HttpStatusCode.BadRequest, "File size exceeds the 500 KB limit.");

                if (!requestBody.Banner.FileName.EndsWith(".png"))
                    return JsonResult(HttpStatusCode.BadRequest, "Only PNG files are allowed.");

                await using var stream = requestBody.Banner.OpenReadStream();
                using var sha256 = SHA256.Create();
                byte[] hashBytes = await sha256.ComputeHashAsync(stream);
                string fileHash = Convert.ToHexStringLower(hashBytes);
                stream.Position = 0;

                if (!await SkiaHelper.IsValidFormatAsync(stream, SKEncodedImageFormat.Png, Logger))
                    return JsonResult(HttpStatusCode.BadRequest, "Invalid image format (not a real PNG).");

                FileData? existingBanner =
                    await _fileDataRepo.FindAsync(x =>
                        x.Id == news.BannerId && x.Type == EFileDataType.NEWS_BANNER);
                if (existingBanner != null)
                {
                    existingBanner.DeleteFile();
                    await _fileDataRepo.RemoveAsync(existingBanner, true);
                }

                FileData fd = new FileData
                {
                    Hash = fileHash,
                    FileName = $"{Guid.NewGuid():N}.png",
                    ContentType = "image/png",
                    Type = EFileDataType.NEWS_BANNER
                };
                var uploadResult = await fd.SaveFileAsync(stream);
                if (!uploadResult.Success)
                    return JsonResult(uploadResult.StatusCode, uploadResult.Message);

                fd = await _fileDataRepo.AddAsync(fd, true);
                news.BannerId = fd.Id;
            }

            await _newsRepo.UpdateAsync(news, true);
            InvalidateNewsCache(id);
            return JsonResult(HttpStatusCode.OK, "News article updated successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Failed to update news article.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Deletes a news article by its ID.
    /// </summary>
    /// <param name="id">The ID of the news article to delete.</param>
    /// <response code="200">News article deleted successfully.</response>
    /// <response code="401">Unauthorized. User is not authenticated.</response>
    /// <response code="403">Forbidden. Insufficient permissions.</response>
    /// <response code="404">News article not found.</response>
    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer,Basic")]
    [EnableRateLimiting(RateLimits.FixedWindow.ADMIN)]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status401Unauthorized), TextResponse(StatusCodes.Status403Forbidden), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNews([BindRequired, FromRoute] ulong id)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return JsonResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            var user = await GetCurrentUserAsync();
            if (user == null)
                return JsonResult(HttpStatusCode.Unauthorized, "User not authenticated");

            if (!await UserManager.HasPermissionAsync(user, CustomPermissions.News.Delete))
                return JsonResult(HttpStatusCode.Forbidden, "Permission denied.");

            News? news = await _newsRepo.FindByIdAsync(id);
            if (news == null)
                return JsonResult(HttpStatusCode.NotFound, "News article not found.");

            FileData? file =
                await _fileDataRepo.FindAsync(x => x.Id == news.BannerId && x.Type == EFileDataType.NEWS_BANNER);
            if (file != null)
            {
                file.DeleteFile();
                await _fileDataRepo.RemoveAsync(file);
            }

            await _newsRepo.RemoveAsync(news, true);
            InvalidateNewsCache(id);
            return JsonResult(HttpStatusCode.OK, "News article deleted successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Failed to delete news article.");
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    #endregion

    /// <summary>
    /// Evicts all cached news responses, optionally including the cached entry for a specific article.
    /// </summary>
    /// <param name="id">The news article id whose individual cache entry should also be evicted.</param>
    private void InvalidateNewsCache(ulong? id = null)
    {
        _cacheService.RemoveValue("news:all");
        _cacheService.RemoveValue("news:latest");
        if (id.HasValue)
            _cacheService.RemoveValue($"news:{id.Value}");
    }
}
