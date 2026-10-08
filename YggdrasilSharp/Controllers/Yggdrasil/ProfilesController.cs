using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Models.Responses.Yggdrasil;
using Tavstal.YggdrasilSharp.Serialization;
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
    /// <param name="userManager">Custom user manager for user operations.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="appConfiguration">Application settings.</param>
    public ProfilesController(ILogger<ProfilesController> logger, CustomUserManager userManager, CustomUserStore userStore, AppConfiguration appConfiguration)
        : base(logger, userManager, userStore, appConfiguration) {}

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
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public async Task<IActionResult> MinecraftProfile([Required, MaxLength(10), FromBody] List<string> names)
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

            // Retrieve users from the database whose usernames match the provided list.
            List<CustomUser> users = (await UserStore.QueryUserAsync(x => names.Contains(x.UserName))).ToList();
            if (users.Count == 0)
                return JsonResult("[]");

            // Prepare the response containing user IDs and usernames.
            List<YigProfileResponse> response = new List<YigProfileResponse>();
            foreach (CustomUser user in users)
            {
                response.Add(new YigProfileResponse
                {
                    Id = user.Id,
                    Name = user.UserName,
                });
            }

            // Return the response as JSON.
            return JsonResult(response, CustomJsonContext.Default.ListYigProfileResponse);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Error retrieving Minecraft profiles for usernames: {Usernames}", string.Join(", ", names));
            return YigErrorResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
}
