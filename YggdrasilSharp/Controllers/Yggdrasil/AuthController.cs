using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;
using Tavstal.YggdrasilSharp.Services.Database;

namespace Tavstal.YggdrasilSharp.Controllers.Yggdrasil;

[ApiController]
[Route("yggdrasil")]
[Route("yggdrasil/authserver")]
[Tags("Yggdrasil")]
public class AuthController  : CustomControllerBase
{
    public AuthController(ILogger<ProfilesController> logger, CustomUserStore userStore, AppConfiguration appConfiguration) : base(logger, userStore, appConfiguration) {}
    
    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate([Required, FromBody] YigLoginRequest request)
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

            if (!AppConfiguration.Yggdrasil.EnableLegacyAuth)
                return YigErrorResult(HttpStatusCode.Forbidden, "Legacy authentication is disabled on this server.");
            
            // TODO
            
            return JsonResult("{}");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Unexpected error occurred while processing the request.");
            return YigErrorResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([Required, FromBody] YigRefreshRequest request)
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

            if (!AppConfiguration.Yggdrasil.EnableLegacyAuth)
                return YigErrorResult(HttpStatusCode.Forbidden, "Legacy authentication is disabled on this server.");
            
            // TODO
            
            return JsonResult("{}");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Unexpected error occurred while processing the request.");
            return YigErrorResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    
    [HttpPost("validate")]
    public async Task<IActionResult> Validate([Required, FromBody] YigValidateRequest request)
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

            if (!AppConfiguration.Yggdrasil.EnableLegacyAuth)
                return YigErrorResult(HttpStatusCode.Forbidden, "Legacy authentication is disabled on this server.");
            
            // TODO
            
            return CodeResult(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Unexpected error occurred while processing the request.");
            return YigErrorResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    
    [HttpPost("invalidate")]
    public async Task<IActionResult> Invalidate([Required, FromBody] YigInvalidateRequest request)
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

            if (!AppConfiguration.Yggdrasil.EnableLegacyAuth)
                return YigErrorResult(HttpStatusCode.Forbidden, "Legacy authentication is disabled on this server.");
            
            // TODO
            
            return CodeResult(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Unexpected error occurred while processing the request.");
            return YigErrorResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
    
    [HttpPost("signout")]
    public async Task<IActionResult> Signout([Required, FromBody] YigSignoutRequest request)
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

            if (!AppConfiguration.Yggdrasil.EnableLegacyAuth)
                return YigErrorResult(HttpStatusCode.Forbidden, "Legacy authentication is disabled on this server.");

            // TODO
            
            return CodeResult(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Unexpected error occurred while processing the request.");
            return YigErrorResult(HttpStatusCode.InternalServerError, Program.IsDevelopment ? ex.ToString() : "An unknown error occurred while processing the request.");
        }
    }
}