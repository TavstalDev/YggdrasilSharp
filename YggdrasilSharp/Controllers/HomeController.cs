using System.Net;
using Microsoft.AspNetCore.Mvc;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Services.Database;

namespace Tavstal.YggdrasilSharp.Controllers;

/// <summary>
/// Controller responsible for handling requests to the home endpoint.
/// </summary>
[ApiController]
public class HomeController : CustomControllerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HomeController"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for logging.</param>
    /// <param name="userManager">Custom user manager for user operations.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="appConfiguration">Application settings.</param>
    public HomeController(ILogger<HomeController> logger, CustomUserManager userManager, CustomUserStore userStore, AppConfiguration appConfiguration)
        : base(logger, userManager, userStore, appConfiguration) { }

    /// <summary>
    /// Handles the root endpoint ("/") and returns an HTTP 200 OK response.
    /// This endpoint is ignored in the API documentation.
    /// </summary>
    /// <returns>An IActionResult representing the HTTP 200 OK response.</returns>
    [Route("/"), ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Index()
    {
        Response.Headers.TryAdd("X-Authlib-Injector-API-Location", "/yggdrasil/");
        return CodeResult(HttpStatusCode.OK, "The API is running.");
    }
}
