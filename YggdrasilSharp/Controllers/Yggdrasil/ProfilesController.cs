using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services.Database;

namespace Tavstal.YggdrasilSharp.Controllers.Yggdrasil;

/// <summary>
/// Controller for handling Yggdrasil profile-related API requests.
/// </summary>
[ApiController]
[Route("yggdrasil/api/profiles")]
[Tags("Yggdrasil")]
public class ProfilesController : CustomControllerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfilesController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging information.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="appConfiguration">Application settings.</param>
    public ProfilesController(ILogger<ProfilesController> logger, CustomUserStore userStore, AppConfiguration appConfiguration) : base(logger, userStore, appConfiguration) {}

    /// <summary>
    /// Retrieves Minecraft profiles for the specified list of usernames.
    /// </summary>
    /// <param name="names">A list of usernames to retrieve profiles for.</param>
    /// <returns>
    /// A JSON response containing the profiles of the specified users, or a 404 status code
    /// if no users are found.
    /// </returns>
    /// <response code="200">Returns the profiles of the specified users.</response>
    /// <response code="404">No users found with the provided usernames.</response>
    [HttpPost("minecraft")]
    [JsonResponse(typeof(List<Dictionary<string, string>>)), TextResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MinecraftProfile([Required, FromBody] List<string> names)
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

            // Retrieve users from the database whose usernames match the provided list.
            List<CustomUser> users = (await UserStore.QueryUserAsync(x => names.Contains(x.UserName))).ToList();
            if (users.Count == 0)
                return CodeResult(HttpStatusCode.NotFound, "No users found with the provided usernames.");

            // Prepare the response containing user IDs and usernames.
            List<Dictionary<string, string>> response = new List<Dictionary<string, string>>();
            foreach (CustomUser user in users)
            {
                Dictionary<string, string> userData = new Dictionary<string, string>
                {
                    { "id", user.Id },
                    { "name", user.UserName }
                };
                response.Add(userData);
            }

            // Return the response as JSON.
            return JsonResult(response);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Error retrieving Minecraft profiles for usernames: {Usernames}", string.Join(", ", names));
            return CodeResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
}
