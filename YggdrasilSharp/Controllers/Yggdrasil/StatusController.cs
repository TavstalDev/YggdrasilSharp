using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.Database;

namespace Tavstal.YggdrasilSharp.Controllers.Yggdrasil;

/// <summary>
/// Controller for handling Yggdrasil status-related API requests.
/// </summary>
[ApiController]
[Route("yggdrasil")]
[Tags("Yggdrasil")]
public class StatusController : CustomControllerBase
{
    private readonly MemoryCacheService _cacheService;
    private readonly Settings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging information.</param>
    /// <param name="userStore">The <see cref="CustomUserStore"/> used by the base controller for user operations.</param>
    /// <param name="cacheService">Service for caching data in memory.</param>
    /// <param name="settings">The application settings.</param>
    public StatusController(ILogger<StatusController> logger, CustomUserStore userStore, MemoryCacheService cacheService, Settings settings) : base(logger, userStore, settings)
    {
        _cacheService = cacheService;
        _settings = settings;
    }
    
    /// <summary>
    /// Retrieves the root status information, including skin domains, public key signature, and metadata.
    /// </summary>
    /// <returns>
    /// A JSON response containing the root status information.
    /// </returns>
    /// <response code="200">Returns the root status information.</response>
    /// <response code="500">Failed to load the RSA private key from the certificate.</response>
    [HttpGet] 
    public IActionResult Root()
    {
        try
        {
            var cert = Program.GetCertificate(_settings.CertificateFingerprint, _settings.CertificatePassword);
            var rsa = cert.GetRSAPrivateKey();
            if (rsa == null)
                return CodeResult(HttpStatusCode.InternalServerError,
                    "Failed to load RSA private key from certificate");

            string signature = rsa.ExportSubjectPublicKeyInfoPem();
            return JsonResult(new
            {
                skinDomains = _settings.Yggdrasil.SkinDomains,
                signaturePublickey = signature,
                meta = new Dictionary<string, object>
                {
                    { "serverName", _settings.Yggdrasil.ServerName },
                    { "implementationVersion", _settings.Yggdrasil.ImplementationVersion },
                    { "feature.non_email_login", true },
                    { "implementationName", _settings.Yggdrasil.ImplementationName }
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving yggdrasil status");
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Retrieves the current status of users, tokens, and pending authentications.
    /// </summary>
    /// <returns>
    /// A JSON response containing the status counts.
    /// </returns>
    /// <response code="200">Returns the status counts.</response>
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        if (_cacheService.TryGetValue("yggdrasil_status", out string? cachedResult) && !string.IsNullOrEmpty(cachedResult))
            return JsonResult(cachedResult);
        
        try
        {
            var users = await UserStore.QueryUserAsync(null);
            var tokens = await UserStore.UserTokens.QueryAsync(null);
            var result = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "user.count", users.Count() },
                { "token.count", tokens.Count() },
                { "pendingAuthentication.count", 0 } // Not implemented
            }, Formatting.Indented);
            _cacheService.SetValue("yggdrasil_status", result, TimeSpan.FromMinutes(5));
            return JsonResult(result);
        }
        catch (Exception ex)
        {
           Logger.LogCritical(ex, "Error retrieving yggdrasil status counts");
           return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    
    /// <summary>
    /// Retrieves the public keys for profile verification.
    /// </summary>
    /// <returns>
    /// A JSON response containing an empty list of profile keys.
    /// </returns>
    /// <response code="200">Returns an empty list of profile keys.</response>
    [HttpGet("publickeys")]
    [HttpGet("minecraftservices/publickeys")]
    public IActionResult GetPublicKeys()
    {
        return JsonResult(new { profileKeys = Array.Empty<object>() });
    }
}