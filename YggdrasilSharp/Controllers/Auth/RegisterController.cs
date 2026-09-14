using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkiaSharp;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Bodies.Auth;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using Tavstal.YggdrasilSharp.Utils.Extensions;
using Tavstal.YggdrasilSharp.Utils.Helpers;

namespace Tavstal.YggdrasilSharp.Controllers.Auth;

/// <summary>
/// Controller for handling user registration and email confirmation.
/// </summary>
[ApiController]
[Route("/register")]
[Tags("Authentication: Registration")]
public class RegisterController : CustomControllerBase
{
    private readonly CustomUserManager _userManager;
    private readonly CustomDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly IRepository<FileData> _fileDataRepo;
    private readonly IPasswordHasher<CustomUser> _passwordHasher;
    private readonly Settings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterController"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for logging.</param>
    /// <param name="userManager">Custom user manager for user operations.</param>
    /// <param name="dbContext">Database context for accessing user data.</param>
    /// <param name="userStore">The user store for accessing user data.</param>
    /// <param name="passwordHasher">The password hasher for securely hashing user passwords during registration.</param>
    /// <param name="emailService">Service for sending emails.</param>
    /// <param name="fileDataRepo">Repository for managing file data, such as user avatars.</param>
    /// <param name="settings">Application settings.</param>
    public RegisterController(ILogger<RegisterController> logger, CustomUserManager userManager, CustomDbContext dbContext, CustomUserStore userStore, IPasswordHasher<CustomUser> passwordHasher, IEmailService emailService, IRepository<FileData> fileDataRepo, Settings settings) : base(logger, userStore, settings)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _fileDataRepo = fileDataRepo;
        _settings = settings;
    }

    /// <summary>
    /// Registers a new user using a multipart/form-data request.
    /// </summary>
    /// <param name="request">The registration request body containing user details and an optional avatar file.</param>
    /// <response code="201">User registered successfully.</response>
    /// <response code="400">Bad request. Invalid input data.</response>
    /// <response code="403">Forbidden. Password is compromised.</response>
    /// <response code="409">Conflict. User already exists.</response>
    /// <response code="500">Internal server error. An unknown error occurred while processing the request.</response>
    [HttpPost("")]
    [EnableRateLimiting(RateLimits.AUTH_REGISTER)]
    [Consumes("multipart/form-data")]
    [TextResponse(StatusCodes.Status201Created), TextResponse(StatusCodes.Status400BadRequest),
     TextResponse(StatusCodes.Status403Forbidden),
     TextResponse(StatusCodes.Status409Conflict), TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterForm([Required, FromForm] RegisterRequestBody request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return CodeResult(HttpStatusCode.BadRequest, string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }
            
            if (await _userManager.IsCompromisedPasswordAsync(request.Password)) 
                return CodeResult(HttpStatusCode.Forbidden, "Password is compromised.");
            
            if (!request.EmailAddress.IsValidEmail())
                return CodeResult(HttpStatusCode.BadRequest, "Invalid email address.");
                
            var normalizedEmail = request.EmailAddress.Normalize();
            var normalizedUsername = request.Username.Normalize();
            CustomUser? user = await UserStore.FindUserAsync(x => x.NormalizedEmail == normalizedEmail || x.NormalizedUserName == normalizedUsername);
            if (user != null)
                return CodeResult(HttpStatusCode.Conflict, "User already exists.");
            
            FileData? avatarData = null;
            if (request.Avatar is { Length: > 0 })
            {
                await using var stream = request.Avatar.OpenReadStream();
                using var sha256 = SHA256.Create();
                byte[] hashBytes = await sha256.ComputeHashAsync(stream);
                string fileHash = Convert.ToHexStringLower(hashBytes);
                stream.Position = 0;
                
                try 
                {
                    // Check Format and Dimensions using ImageSharp
                    using var codec = SKCodec.Create(stream);
                    if (codec.EncodedFormat != SKEncodedImageFormat.Png)
                        return CodeResult(HttpStatusCode.BadRequest, "Invalid image format (not a real PNG).");

                    stream.Position = 0;
                }
                catch (Exception)
                {
                    Logger.LogError($"Failed to upload avatar file: {fileHash}");
                    return CodeResult(HttpStatusCode.BadRequest, "Invalid image format.");
                }
                
                avatarData = await _fileDataRepo.AddAsync(new FileData
                {
                    Hash = fileHash,
                    FileName = $"{Guid.NewGuid():N}.png",
                    ContentType = "image/png",
                    Type = EFileDataType.PROFILE_PICTURE
                }, true);
                avatarData.SaveFile(stream);
            }

            var newUser = new CustomUser(request.Username, normalizedUsername, request.EmailAddress, normalizedEmail,
                "", ESkinType.WIDE,
                null, string.Empty, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            
            // Hash the password
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, request.Password);
            newUser.SecurityStamp = Guid.NewGuid().ToString();
            
            user = await UserStore.AddUserAsync(newUser, true);
            if (avatarData != null)
            {
                avatarData.UserId = user.Id;
                await _fileDataRepo.UpdateAsync(avatarData);
            }
            
            // Add the user to the default role
            var defaultRole = await UserStore.Roles.FindAsync(x => x.NormalizedName == "DEFAULT");
            if (defaultRole != null)
            {
                await UserStore.UserRoles.AddAsync(new CustomUserRole { UserId = user.Id, RoleId = defaultRole.Id, });
            }
            // Add customer role claim to the user
            await UserStore.UserClaims.AddAsync(new CustomUserClaim { UserId = user.Id, ClaimType = ClaimTypes.Role, ClaimValue = "default" });
            // Save changes
            await _dbContext.SaveChangesAsync();
            
            // Send registration confirmation email
            try
            {
                await SendConfirmEmail(user);
            }
            catch (Exception eex)
            {
                Logger.LogCritical("Failed to send confirmation email: {Message}", eex);
            }

            return CodeResult(HttpStatusCode.Created, "User registered successfully");
        }
        catch (Exception ex)
        {
            Logger.LogCritical("Error during registration: {Message}", ex);
            return CodeResult(HttpStatusCode.InternalServerError, "Unexpected error occurred");
        }
    }
    
    /// <summary>
    /// Confirms a user's registration using a confirmation token.
    /// </summary>
    /// <param name="request">The confirmation request body containing user ID and token.</param>
    /// <response code="200">User confirmed successfully.</response>
    /// <response code="400">Bad request. Invalid confirmation token.</response>
    /// <response code="403">Forbidden. User is already confirmed.</response>
    /// <response code="404">Not found. User does not exist.</response>
    /// <response code="500">Internal server error. An unknown error occurred while processing the request.</response>
    [HttpPatch("confirm")]
    [EnableRateLimiting(RateLimits.AUTH_REGISTER)]
    [Consumes("application/json")]
    [TextResponse(StatusCodes.Status200OK), TextResponse(StatusCodes.Status400BadRequest),
     TextResponse(StatusCodes.Status403Forbidden),
     TextResponse(StatusCodes.Status404NotFound), TextResponse(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmRegistration([Required, FromBody] ConfirmRegisterRequestBody request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return CodeResult(HttpStatusCode.BadRequest, string.IsNullOrEmpty(errorMessages) ? "Invalid input data." : errorMessages);
            }
            
            // Find the user by ID
            CustomUser? user = await UserStore.FindUserByIdAsync(request.UserId);
            if (user == null)
                return CodeResult(HttpStatusCode.BadRequest, "User does not exist.");

            // Check if the user's email is already confirmed
            if (user.EmailConfirmed)
                return CodeResult(HttpStatusCode.Forbidden, "The user is already confirmed.");

            // Validate the confirmation token
            var confirmationToken = await UserStore.UserTokens.FindAsync(x => x.UserId == user.Id && x.Name == "EmailConfirmationToken");
            if (confirmationToken == null)
                return CodeResult(HttpStatusCode.BadRequest, "Invalid confirmation token");

            if (confirmationToken.Value != request.ConfirmationToken)
                return CodeResult(HttpStatusCode.BadRequest, "Invalid confirmation token");
            
            await UserStore.UserTokens.RemoveAsync(confirmationToken);
            user.EmailConfirmed = true;
            await UserStore.UpdateUserAsync(user);
            await _dbContext.SaveChangesAsync();

            // Send a confirmation email to the user
            await _emailService.SendEmailAsync(user.Email, user.UserName, "Account Confirmation",
                "Your account has been confirmed<br/>Thank you for confirming your account. You can now log in.");

            return CodeResult(HttpStatusCode.OK, "User confirmed successfully");
        }
        catch (Exception ex)
        {
            // Log critical errors and return an internal server error response
            Logger.LogCritical("Error during email confirmation: {Message}", ex);
            return CodeResult(HttpStatusCode.InternalServerError, "Unexpected error occurred");
        }
    }

    /// <summary>
    /// Sends a confirmation email to the user with a confirmation token.
    /// </summary>
    /// <param name="user">The user to send the confirmation email to.</param>
    private async Task SendConfirmEmail(CustomUser user)
    {
        if (string.IsNullOrEmpty(user.Email))
            return;
        
        var confirmationToken = await UserStore.UserTokens.FindAsync(x => x.UserId == user.Id && x.Name == "EmailConfirmationToken");
        if (confirmationToken != null)
            await UserStore.UserTokens.RemoveAsync(confirmationToken, true);

        confirmationToken = await UserStore.UserTokens.AddAsync(new CustomUserToken
        {
            Name = "EmailConfirmationToken",
            LoginProvider = "MesterMC",
            Value = TokenHelper.GenerateAccountConfirmationToken(),
            CreateDate = DateTimeOffset.UtcNow,
            UserId = user.Id
        }, true);
        
        var uriBuilder = new UriBuilder(new Uri(Settings.Misc.WebsiteUrl))
        {
            Path = "/register/confirm",
            Query = $"userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(confirmationToken.Value!)}"
        };
        string confirmationLink = uriBuilder.ToString();
        await _emailService.SendEmailAsync(user.Email, user.UserName, "Registration Confirmation",
            $"Confirm your account by clicking the button below, or by copying and pasting the following link into your browser: {confirmationLink}<br/><br/>", 
            confirmationLink, 
            "Confirm Account");
    }
}