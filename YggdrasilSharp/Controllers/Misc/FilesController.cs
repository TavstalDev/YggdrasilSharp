using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;

namespace Tavstal.YggdrasilSharp.Controllers.Misc;

/// <summary>
/// Controller for managing file retrieval operations.
/// </summary>
[ApiController]
[Route("files")]
public class FilesController : CustomControllerBase
{
    private readonly IRepository<FileData> _fileDataRepo;
    private readonly MemoryCacheService _memoryCache;
    private static readonly TimeSpan CacheSlidingTtl = TimeSpan.FromDays(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="FilesController"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for logging.</param>
    /// <param name="userManager">Custom user manager for user operations.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="appConfiguration">Application settings.</param>
    /// <param name="fileDataRepo">Repository for managing file data.</param>
    /// <param name="memoryCache">Service for caching file data.</param>
    public FilesController(ILogger<FilesController> logger, CustomUserManager userManager, CustomUserStore userStore, AppConfiguration appConfiguration,
        IRepository<FileData> fileDataRepo, MemoryCacheService memoryCache) :
        base(logger, userManager, userStore, appConfiguration)
    {
        _fileDataRepo = fileDataRepo;
        _memoryCache = memoryCache;
    }

    /// <summary>
    /// Retrieves a file by its hash.
    /// </summary>
    /// <param name="hash">The hash of the file to retrieve.</param>
    /// <response code="200">File retrieved successfully.</response>
    /// <response code="304">File not modified since the last request.</response>
    /// <response code="404">File not found.</response>
    /// <response code="500">Failed to retrieve file data.</response>
    [HttpGet("{hash}")]
    [TextResponse(StatusCodes.Status200OK),
     TextResponse(StatusCodes.Status304NotModified),
     TextResponse(StatusCodes.Status404NotFound),
     TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFile([BindRequired, FromRoute, StringLength(64, MinimumLength = 64)] string hash)
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

            string cacheKey = $"file:{hash}";
            byte[]? bytes;
            string contentType;
            if (!_memoryCache.TryGetValue<(byte[], string)>(cacheKey, out var fd))
            {
                var fileData = await _fileDataRepo.FindAsync(x =>
                    x.Hash == hash && (x.Type == EFileDataType.CAPE || x.Type == EFileDataType.SKIN ||
                                       x.Type == EFileDataType.PROFILE_PICTURE || x.Type == EFileDataType.NEWS_BANNER));
                if (fileData == null)
                    return JsonResult(HttpStatusCode.NotFound, "File not found.");
                bytes = fileData.GetFileData();
                if (bytes == null)
                    return JsonResult(HttpStatusCode.InternalServerError, "Failed to retrieve file data.");
                contentType = fileData.ContentType;
                _memoryCache.SetValue(cacheKey, (bytes, contentType), slidingExpiration: CacheSlidingTtl);
            }
            else
            {
                bytes = fd.Item1;
                contentType = fd.Item2;
            }

            string etag = "\"" + hash + "\"";
            if (HttpContext.Request.Headers.TryGetValue("If-None-Match", out var incomingEtag))
            {
                if (incomingEtag.ToString().Equals(etag, StringComparison.Ordinal))
                {
                    HttpContext.Response.Headers.ETag = etag;
                    HttpContext.Response.Headers.CacheControl = "public,max-age=86400,immutable";
                    return CodeResult(HttpStatusCode.NotModified);
                }
            }

            HttpContext.Response.Headers.ETag = etag;
            HttpContext.Response.Headers.CacheControl = "public,max-age=86400,immutable";
            return File(bytes, contentType);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving file with hash {Hash}", hash);
            return JsonResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
}
