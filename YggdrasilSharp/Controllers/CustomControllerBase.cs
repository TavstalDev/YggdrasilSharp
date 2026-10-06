using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Models.Responses;
using Tavstal.YggdrasilSharp.Models.Responses.Yggdrasil;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Utils.Helpers;

namespace Tavstal.YggdrasilSharp.Controllers;

/// <summary>
/// Base controller class that provides common functionality for derived controllers.
/// </summary>
public abstract class CustomControllerBase : Controller
{
    /// <summary>
    /// Logger instance used for logging within the controller.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    ///  Reference to the application's <see cref="CustomUserManager"/> used to modify user data.
    /// </summary>
    protected readonly CustomUserManager UserManager;

    /// <summary>
    /// Reference to the application's <see cref="CustomUserStore"/> used to query and modify user data.
    /// </summary>
    protected readonly CustomUserStore UserStore;

    /// <summary>
    /// Application settings instance available to derived controllers.
    /// </summary>
    protected readonly AppConfiguration AppConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomControllerBase"/> class.
    /// </summary>
    /// <param name="logger">The logger instance to be used by the controller.</param>
    /// <param name="userManager">The <see cref="CustomUserManager"/> instance for user operations.</param>
    /// <param name="userStore">The <see cref="CustomUserStore"/> instance for accessing user data.</param>
    /// <param name="appConfiguration">The <see cref="AppConfiguration"/> instance containing application configuration used by controllers.</param>
    protected CustomControllerBase(ILogger logger, CustomUserManager userManager, CustomUserStore userStore, AppConfiguration appConfiguration)
    {
        Logger = logger;
        UserManager = userManager;
        UserStore = userStore;
        AppConfiguration = appConfiguration;
    }

    /// <summary>
    /// Gets the user ID of the currently authenticated user.
    /// </summary>
    protected string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Retrieves the current user asynchronously using the user store.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the current user.</returns>
    protected async Task<CustomUser?> GetCurrentUserAsync()
    {
        if (string.IsNullOrEmpty(UserId))
            return null;
        return await UserStore.FindUserByIdAsync(UserId);
    }

    /// <summary>
    /// Returns a JSON response containing a <see cref="YigErrorResponse"/> describing the failure,
    /// using the error format expected by the Yggdrasil API.
    /// </summary>
    /// <param name="code">The HTTP status code to describe the failure with.</param>
    /// <param name="message">The message describing why the request failed.</param>
    /// <param name="details">Optional additional information about the failure.</param>
    /// <returns>An <see cref="IActionResult"/> containing the serialized <see cref="YigErrorResponse"/>.</returns>
    protected IActionResult YigErrorResult(HttpStatusCode code, string message, string? details = null)
        => JsonResult(new YigErrorResponse
        {
            Error = code.ToString(),
            ErrorMessage = message,
            Cause = details
        });

    /// <summary>
    /// Returns an HTTP response with the specified status code and message.
    /// </summary>
    /// <param name="status">The HTTP status code to return.</param>
    /// <param name="message">The message to include in the response.</param>
    /// <returns>An IActionResult representing the HTTP response.</returns>
    protected IActionResult CodeResult(HttpStatusCode status, string? message = null)
    {
        if (string.IsNullOrEmpty(message))
            return StatusCode((int)status);
        return StatusCode((int)status, message);
    }

    /// <summary>
    /// Returns a JSON response containing an <see cref="ErrorResponse"/> describing the failure.
    /// </summary>
    /// <param name="code">The HTTP status code to describe the failure with.</param>
    /// <param name="message">The message describing why the request failed.</param>
    /// <param name="details">Optional additional information about the failure.</param>
    /// <returns>An <see cref="IActionResult"/> containing the serialized <see cref="ErrorResponse"/>.</returns>
    protected IActionResult JsonResult(HttpStatusCode code, string message, string? details = null)
        => JsonResult(new ErrorResponse
        {
            StatusCode = code,
            Message = message,
            Details = details
        });

    /// <summary>
    /// Returns a JSON response with the specified object serialized to JSON.
    /// This method intelligently handles different input types to optimize serialization.
    /// </summary>
    /// <param name="obj">
    /// The object to serialize and include in the response. Supported types include:
    /// <list type="bullet">
    /// <item><description><see cref="string"/> - Raw JSON string (returned as-is)</description></item>
    /// <item><description>Any other object - Serialized using <see cref="JsonHelper.SerializeJson"/></description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// An <see cref="IActionResult"/> containing the JSON-formatted response with content type "application/json".
    /// </returns>
    protected IActionResult JsonResult(object obj)
    {
        switch (obj)
        {
            case string str:
                return Content(str, "application/json");
            default:
               return Content(JsonHelper.SerializeJson(obj), "application/json");
        }
    }

    /// <summary>
    /// Computes a stable ETag (Entity Tag) for the given JSON string.
    /// The ETag is generated by computing the SHA1 hash of the JSON string
    /// and encoding it in Base64 format, enclosed in double quotes.
    /// </summary>
    /// <param name="json">The JSON string for which the ETag is to be computed.</param>
    /// <returns>A string representing the computed ETag.</returns>
    protected static string ComputeETag(string json)
    {
        // ETag is not security-sensitive, SHA1 is acceptable for performance reasons.
        using var sha1 = SHA1.Create();
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = sha1.ComputeHash(bytes);
        return "\"" + Convert.ToBase64String(hash) + "\"";
    }
}
