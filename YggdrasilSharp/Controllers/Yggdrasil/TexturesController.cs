using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;

namespace Tavstal.YggdrasilSharp.Controllers.Yggdrasil;

/// <summary>
/// Controller for handling Yggdrasil texture-related API requests.
/// </summary>
[ApiController]
[Route("yggdrasil/textures")]
[Tags("Yggdrasil")]
public class TexturesController : CustomControllerBase
{
    private readonly IRepository<FileData> _fileDataRepo;
    private readonly MemoryCacheService _memoryCache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="TexturesController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging information.</param>
    /// <param name="userStore">The <see cref="CustomUserStore"/> used by the base controller for user operations.</param>
    /// <param name="fileDataRepo">Repository for <see cref="FileData"/> entities.</param>
    /// <param name="memoryCache">The memory cache service for caching texture data.</param>
    /// <param name="appConfiguration">Application settings.</param>
    public TexturesController(ILogger<TexturesController> logger, CustomUserStore userStore, IRepository<FileData> fileDataRepo,
        MemoryCacheService memoryCache, AppConfiguration appConfiguration) : base(logger, userStore, appConfiguration)
    {
        _fileDataRepo = fileDataRepo;
        _memoryCache = memoryCache;
    }
    
    /// <summary>
    /// Retrieves texture data based on the provided hash.
    /// </summary>
    /// <param name="hash">The hash of the texture to retrieve.</param>
    /// <returns>
    /// A file response containing the texture data if found, or an appropriate error response.
    /// </returns>
    /// <response code="200">Returns the texture file.</response>
    /// <response code="304">Texture not modified (ETag matches).</response>
    /// <response code="404">Texture not found.</response>
    /// <response code="500">Failed to retrieve texture data.</response>
    [HttpGet("{hash}")]
    public async Task<IActionResult> GetTexture([BindRequired, FromRoute, MinLength(64), MaxLength(64)] string hash)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return CodeResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            string cacheKey = $"file:{hash}";
            byte[]? bytes;
            string contentType;
            if (!_memoryCache.TryGetValue<(byte[], string)>(cacheKey, out var fd))
            {
                var fileData = await _fileDataRepo.FindAsync(x =>
                    x.Hash == hash && (x.Type == EFileDataType.SKIN || x.Type == EFileDataType.CAPE));
                if (fileData == null)
                    return CodeResult(HttpStatusCode.NotFound, "Texture not found.");
                bytes = fileData.GetFileData();
                if (bytes == null)
                    return CodeResult(HttpStatusCode.InternalServerError, "Failed to retrieve texture data.");
                contentType = fileData.ContentType;
                _memoryCache.SetValue(cacheKey, (bytes, contentType), CacheTtl);
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
            Logger.LogError(ex, "Error retrieving texture with hash {Hash}", hash);
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
}
