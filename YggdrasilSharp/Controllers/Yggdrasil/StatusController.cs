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
    private readonly AppConfiguration _appConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging information.</param>
    /// <param name="userStore">The <see cref="CustomUserStore"/> used by the base controller for user operations.</param>
    /// <param name="cacheService">Service for caching data in memory.</param>
    /// <param name="appConfiguration">The application settings.</param>
    public StatusController(ILogger<StatusController> logger, CustomUserStore userStore, MemoryCacheService cacheService, AppConfiguration appConfiguration) : base(logger, userStore, appConfiguration)
    {
        _cacheService = cacheService;
        _appConfiguration = appConfiguration;
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
            var cert = Program.GetCertificate(_appConfiguration.CertificateFingerprint, _appConfiguration.CertificatePassword);
            var rsa = cert.GetRSAPrivateKey();
            if (rsa == null)
                return YigErrorResult(HttpStatusCode.InternalServerError,
                    "Failed to load RSA private key from certificate");

            string signature = rsa.ExportSubjectPublicKeyInfoPem();
            return JsonResult(new
            {
                skinDomains = _appConfiguration.Yggdrasil.SkinDomains,
                signaturePublickey = signature,
                meta = new Dictionary<string, object>
                {
                    { "serverName", _appConfiguration.Yggdrasil.ServerName },
                    { "implementationName", _appConfiguration.Yggdrasil.ImplementationName },
                    { "implementationVersion", _appConfiguration.Yggdrasil.ImplementationVersion },
                    { "feature.non_email_login", _appConfiguration.Yggdrasil.AllowProfileNameLogin },
                    { "feature.username_check", _appConfiguration.Yggdrasil.EnforceUsernameCheck },
                    { "feature.enable_profile_key", _appConfiguration.Yggdrasil.EnableProfileKey},
                    { "feature.legacy_skin_api", false },
                    { "meta.links.homepage", _appConfiguration.Yggdrasil.HomepageUrl ?? "" },
                    { "meta.links.register", _appConfiguration.Yggdrasil.RegisterUrl ?? "" }
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving yggdrasil status");
            return YigErrorResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
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