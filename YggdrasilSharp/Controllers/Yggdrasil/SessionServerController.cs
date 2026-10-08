using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Database.Server;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Models.Responses.Yggdrasil;
using Tavstal.YggdrasilSharp.Serialization;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using Tavstal.YggdrasilSharp.Utils.Helpers;

namespace Tavstal.YggdrasilSharp.Controllers.Yggdrasil;

/// <summary>
/// Controller for handling Yggdrasil session server-related API requests.
/// </summary>
[ApiController]
[Route("yggdrasil/session/minecraft")] // Client
[Route("yggdrasil/sessionserver/session/minecraft")] // Server
[Tags("Yggdrasil")]
public class SessionServerController : CustomControllerBase
{
    private readonly IRepository<Cape> _capeRepo;
    private readonly IRepository<FileData> _fileDataRepository;
    private readonly IRepository<ServerJoin> _serverJoinRepo;
    private readonly AppConfiguration _appConfiguration;
    private readonly MemoryCacheService _cacheService;
    private static readonly TimeSpan SignedTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan UnsignedTtl = TimeSpan.FromHours(1);
    private static readonly ConcurrentDictionary<string, (SemaphoreSlim @lock, int waits)> _locks = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionServerController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging information.</param>
    /// <param name="userManager">The user manager for handling user-related operations.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="appConfiguration">The application settings.</param>
    /// <param name="serverJoinRepo">Repository for recording server join events.</param>
    /// <param name="fileDataRepository">Repository for managing file data (skins, capes, etc.).</param>
    /// <param name="capeRepo">Repository for managing cape entities.</param>
    /// <param name="cacheService">The memory cache service for caching data.</param>
    public SessionServerController(ILogger<SessionServerController> logger, CustomUserManager userManager, CustomUserStore userStore, AppConfiguration appConfiguration,
        IRepository<ServerJoin> serverJoinRepo, IRepository<FileData> fileDataRepository, IRepository<Cape> capeRepo, MemoryCacheService cacheService)
        : base(logger, userManager, userStore, appConfiguration)
    {
        _fileDataRepository = fileDataRepository;
        _capeRepo = capeRepo;
        _serverJoinRepo = serverJoinRepo;
        _appConfiguration = appConfiguration;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Retrieves the list of blocked servers.
    /// </summary>
    /// <returns>A JSON response containing an empty list of blocked servers.</returns>
    [HttpGet("/yggdrasil/sessionserver/blockedservers")]
    public IActionResult GetBlockedServers()
    {
        string finalJson = JsonSerializer.Serialize(new YigBlockedServersResponse
        {
            BlockedServers = _appConfiguration.Yggdrasil.BlockedServers
        }, CustomJsonContext.Default.YigBlockedServersResponse);
        string etag = ComputeETag(finalJson);
        if (HttpContext.Request.Headers.TryGetValue("If-None-Match", out var incomingEtag))
        {
            if (incomingEtag.ToString().Equals(etag, StringComparison.Ordinal))
            {
                HttpContext.Response.Headers.ETag = etag;
                return CodeResult(HttpStatusCode.NotModified);
            }
        }

        HttpContext.Response.Headers.ETag = etag;
        return JsonResult(finalJson);
    }

    /// <summary>
    /// Handles the join request for a server.
    /// </summary>
    /// <param name="request">The join server request containing the selected profile and access token.</param>
    /// <returns>
    /// A response indicating the result of the join operation.
    /// </returns>
    /// <response code="204">The join operation was successful.</response>
    /// <response code="404">User or session not found.</response>
    /// <response code="401">The access token has expired.</response>
    /// <response code="403">The IP address does not match the request.</response>
    [HttpPost("join")]
    public async Task<IActionResult> Join([Required, FromBody] YigJoinServerRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return YigErrorResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            if (!Guid.TryParse(request.SelectedProfile, out Guid uuid))
                return YigErrorResult(HttpStatusCode.BadRequest, "Invalid uuid was provided.");

            string dashedUuid = uuid.ToString("D");
            CustomUser? user = await UserStore.FindUserByIdAsync(dashedUuid);
            if (user == null)
                return YigErrorResult(HttpStatusCode.NotFound,
                    "User not found for the provided selectedProfile UUID");

            if (!await UserManager.VerifyJwtTokenAsync(request.AccessToken))
                return YigErrorResult(HttpStatusCode.Unauthorized, "Invalid access token");

            string hashedAccessToken = StringChiper.GetEncryptedHash(request.AccessToken, _appConfiguration.Jwt.EncryptionKey);
            UserPlaySession? session = await UserStore.UserPlaySessions.FindAsync(x => x.UserId == user.Id && x.Token == hashedAccessToken);
            if (session == null)
                return YigErrorResult(HttpStatusCode.NotFound,
                    "No active session found for the provided access token");

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (now > session.ExpiresAt)
                return YigErrorResult(HttpStatusCode.Unauthorized, "The access token has expired");

            string host = HttpHelper.GetClientIp(HttpContext) ?? "";
            if (_appConfiguration.Yggdrasil.EnforceIpCheckInJoin && session.UserIp != host)
                return YigErrorResult(HttpStatusCode.Forbidden,
                    "The IP address associated with the access token does not match the IP address of the request");

            await _serverJoinRepo.AddAsync(new ServerJoin
            {
                ServerId = request.ServerId,
                UserId = user.Id,
                UserIp = host,
                CreatedAt = now,
                ExpiresAt = now.AddDays(1)
            }, true);
            return CodeResult(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Unknown error while processing join request for selectedProfile: {SelectedProfile}, serverId: {ServerId}.", request.SelectedProfile, request.ServerId);
            return YigErrorResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Checks if a user has joined a server.
    /// </summary>
    /// <param name="serverId">The ID of the server.</param>
    /// <param name="username">The username of the user.</param>
    /// <param name="ip">The optional IP address of the user.</param>
    /// <returns>
    /// A JSON response containing the user's profile if the join is valid.
    /// </returns>
    /// <response code="200">The user has joined the server.</response>
    /// <response code="404">No matching server join exists. An unknown username returns this same response so the
    /// endpoint cannot be used to enumerate accounts.</response>
    /// <response code="401">The server join has expired.</response>
    /// <response code="403">The username does not match the server join.</response>
    [HttpGet("hasJoined")]
    public async Task<IActionResult> HasJoined([FromQuery] string serverId, [FromQuery, StringLength(16, MinimumLength = 3)] string username, [FromQuery, StringLength(15, MinimumLength = 7)] string? ip)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return YigErrorResult(HttpStatusCode.BadRequest,
                    string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            CustomUser? user = await UserStore.FindUserAsync(x => x.UserName == username);
            if (user == null)
                return YigErrorResult(HttpStatusCode.NotFound, "No matching server join found for the provided serverId and IP address");

            ServerJoin? join = await _serverJoinRepo.FindAsync(x => x.UserId == user.Id && x.ServerId == serverId &&
                                                                    ( x.UserIp == ip ||
                                                                      !_appConfiguration.Yggdrasil.EnforceIpCheckInHasJoined ||
                                                                      (_appConfiguration.Yggdrasil.AllowEmptyJoinedAddress && ip == null)));
            if (join == null)
                return YigErrorResult(HttpStatusCode.NotFound, "No matching server join found for the provided serverId and IP address");

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (now > join.ExpiresAt)
                return YigErrorResult(HttpStatusCode.Unauthorized, "The server join has expired");

            string json = await GetProfileResponseJsonAsync(user);
            return JsonResult(json);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Unknown error while processing hasJoined request for serverId: {ServerId}, username: {Username}.", serverId, username);
            return YigErrorResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Retrieves the profile of a user by UUID.
    /// </summary>
    /// <param name="uuid">The UUID of the user.</param>
    /// <param name="unsigned">Indicates whether the profile should be unsigned.</param>
    /// <returns>
    /// A JSON response containing the user's profile.
    /// </returns>
    /// <response code="200">The profile was retrieved successfully.</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">An error occurred while processing the request.</response>
    [HttpGet("profile/{uuid}")]
    public async Task<IActionResult> GetProfile([BindRequired, FromRoute, StringLength(36, MinimumLength = 32)] string uuid, [FromQuery] bool unsigned = true)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return YigErrorResult(HttpStatusCode.BadRequest, string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }

            if (!Guid.TryParse(uuid, out Guid guid))
                return YigErrorResult(HttpStatusCode.BadRequest, "Invalid uuid was provided.");

            string dashedUuid = guid.ToString("D");
            CustomUser? user = await UserStore.FindUserByIdAsync(dashedUuid);
            if (user == null)
                return YigErrorResult(HttpStatusCode.NotFound, "User not found");

            string json = await GetProfileResponseJsonAsync(user, unsigned);
            string etag = ComputeETag(json);
            if (HttpContext.Request.Headers.TryGetValue("If-None-Match", out var incomingEtag))
            {
                if (incomingEtag.ToString().Equals(etag, StringComparison.Ordinal))
                {
                    HttpContext.Response.Headers.ETag = etag;
                    return CodeResult(HttpStatusCode.NotModified);
                }
            }

            HttpContext.Response.Headers.ETag = etag;
            return JsonResult(json);
        }
        catch (Exception ex)
        {
            Logger.LogCritical("Unknown error while processing profile request for uuid: {Uuid}, unsigned: {Unsigned}. Error: {ErrorMessage}", uuid, unsigned, ex);
            return YigErrorResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Generates or retrieves cached profile JSON for a given user.
    /// The method uses an in-memory cache with different TTLs for signed and unsigned profiles.
    /// A per-user semaphore is used to prevent cache stampedes.
    /// </summary>
    /// <param name="user">The user for whom the profile JSON is being generated.</param>
    /// <param name="unsigned">
    /// A boolean indicating whether the profile should be unsigned.
    /// Defaults to true.
    /// </param>
    /// <returns>A JSON string representing the user's profile.</returns>
    private async Task<string> GetProfileResponseJsonAsync(CustomUser user, bool unsigned = true)
    {
        string key = $"profile:{user.Id}:{(unsigned ? "unsigned" : "signed")}";
        TimeSpan ttl = unsigned ? UnsignedTtl : SignedTtl;

        if (_cacheService.TryGetValue<string>(key, out var cached))
            return cached!;

        var sem = _locks.GetOrAdd(key, _ => (new SemaphoreSlim(1, 1), 1));
        sem.waits++;
        await sem.@lock.WaitAsync();
        try
        {
            // double-check after acquiring lock
            if (_cacheService.TryGetValue<string>(key, out var cachedAfterLock))
                return cachedAfterLock!;
            Dictionary<string, object> textures = new Dictionary<string, object>();
            var skin = await _fileDataRepository.FindAsync(x => x.UserId == user.Id && x.Type == EFileDataType.SKIN);
            if (skin != null)
            {
                textures.Add("SKIN", new Dictionary<string, object>
                {
                    { "url", skin.GetUrl(_appConfiguration.Misc.ApiUrl, true) },
                    {
                        "metadata", new Dictionary<string, object>
                        {
                            {
                                "model", user.GetSkinVariant()
                            }
                        }
                    }
                });
            }

            var userCape = await UserStore.UserCapes.FindAsync(x => x.UserId == user.Id && x.IsSelected);
            if (userCape != null)
            {
                var cape = await _capeRepo.FindAsync(x => x.Id == userCape.CapeId);
                if (cape != null)
                {
                    var capeData = await _fileDataRepository.FindAsync(x => x.Id == cape.FileId);
                    if (capeData != null)
                    {
                        textures.Add("CAPE", new Dictionary<string, object>
                        {
                            { "url", capeData.GetUrl(_appConfiguration.Misc.ApiUrl, true) },
                        });
                    }
                }
            }

            Dictionary<string, object> textureValues = new Dictionary<string, object>
            {
                { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                { "profileId", user.Id },
                { "profileName", user.UserName },
                { "textures", textures }
            };
            if (!unsigned)
                textureValues.Add("signatureRequired", true);

            string jsonString = JsonSerializer.Serialize(textureValues, CustomJsonContext.Default.DictionaryStringObject);
            string base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonString));

            List<Dictionary<string, object>> properties = [];
            Dictionary<string, object> texturesProperty = new Dictionary<string, object>
            {
                { "name", "textures" },
                { "value", base64Value },
            };
            if (!unsigned)
            {
                using var cert = Program.GetCertificate(_appConfiguration.CertificateFingerprint, _appConfiguration.CertificatePassword);
                using var rsa = cert.GetRSAPrivateKey();

                if (rsa != null)
                {
                    byte[] dataToSign = Encoding.UTF8.GetBytes(base64Value);
                    byte[] signatureBytes = rsa.SignData(dataToSign, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

                    texturesProperty.Add("signature", Convert.ToBase64String(signatureBytes));
                }
            }

            properties.Add(texturesProperty);
            Dictionary<string, object> response = new Dictionary<string, object>
            {
                { "id", user.Id },
                { "name", user.UserName },
                { "properties", properties }
            };
            string finalJson = JsonSerializer.Serialize(response, CustomJsonContext.Default.DictionaryStringObject);

            // Cache the result with absolute expiration
            _cacheService.SetValue(key, finalJson, ttl);
            return finalJson;
        }
        finally
        {
            sem.@lock.Release();
            sem.waits--;
            if (sem.waits <= 0)
                _locks.Remove(key, out _);
        }
    }
}
