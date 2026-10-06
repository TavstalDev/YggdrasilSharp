using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OtpNet;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Utils.Helpers;
// ReSharper disable UnusedMember.Local

#pragma warning disable CS9113 // Parameter is unread.

namespace Tavstal.YggdrasilSharp.Services.Database;

/// <summary>
/// Initializes a new instance of the <see cref="CustomUserManager"/> class.
/// </summary>
public class CustomUserManager
{
    private readonly CustomUserStore _userStore;
    private readonly IPasswordHasher<CustomUser> _passwordHasher;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CustomUserManager> _logger;
    private readonly CustomDbContext _context;
    private readonly MemoryCacheService _memoryCacheService;
    private readonly AppConfiguration _appConfiguration;
    private readonly HttpClient _client;
    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler = new();
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly TimeSpan CompPassTTL = TimeSpan.FromHours(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomUserManager"/> class.
    /// </summary>
    /// <param name="userStore">The user store used to read and persist users, roles and claims.</param>
    /// <param name="passwordHasher">The hasher used to verify and rehash user passwords.</param>
    /// <param name="httpClientFactory">The factory used to create outbound HTTP clients.</param>
    /// <param name="logger">The logger used to report failures.</param>
    /// <param name="context">The database context used for direct persistence operations.</param>
    /// <param name="memoryCacheService">The in-memory cache used for short-lived data.</param>
    /// <param name="appConfiguration">The application configuration (JWT settings, keys, lockout policy).</param>
    public CustomUserManager(CustomUserStore userStore, IPasswordHasher<CustomUser> passwordHasher, IHttpClientFactory httpClientFactory,
        ILogger<CustomUserManager> logger, CustomDbContext context, MemoryCacheService memoryCacheService,
        AppConfiguration appConfiguration)
    {
        _userStore = userStore;
        _passwordHasher = passwordHasher;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _context = context;
        _memoryCacheService = memoryCacheService;
        _appConfiguration = appConfiguration;
        _client = new HttpClient();
        _client.Timeout = TimeSpan.FromSeconds(10);

        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _appConfiguration.Jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = _appConfiguration.Jwt.Audience,

            ValidateLifetime = true,
            ClockSkew = _appConfiguration.Jwt.ClockSkew,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_appConfiguration.Jwt.EncryptionKey),
        };
    }

    #region Roles & Claims

    #region Roles
    /// <summary>
    /// Checks if the user has a specific role.
    /// </summary>
    /// <param name="user">The user to check.</param>
    /// <param name="roleName">The role name to check.</param>
    /// <returns>True if the user has the role, otherwise false.</returns>
    public async Task<bool> HasRoleAsync(CustomUser user, string roleName)
    {
        var role = await _userStore.Roles.FindAsync(x => x.NormalizedName == roleName.Normalize());
        if (role == null)
            return false;
        return await _userStore.UserRoles.ExistsAsync(x => x.RoleId == role.Id && x.UserId == user.Id);
    }

    /// <summary>
    /// Checks if the user claims principal has a specific role.
    /// </summary>
    /// <param name="userClaims">The user claims principal to check.</param>
    /// <param name="roleName">The role name to check.</param>
    /// <returns>True if the user claims principal has the role, otherwise false.</returns>
    public async Task<bool> HasRoleAsync(ClaimsPrincipal userClaims, string roleName)
    {
        if (!userClaims.HasClaim(x => x.Type == "userId"))
            return false;

        var userClaim = userClaims.Claims.FirstOrDefault(x => x.Type == "userId");
        if (userClaim == null)
            return false;

        string userid = userClaim.Value;
        CustomUser? user = await _userStore.FindUserByIdAsync(userid);
        if (user == null)
            return false;

        var role = await _userStore.Roles.FindAsync(x => x.NormalizedName == roleName.Normalize());
        if (role == null)
            return false;
        return await _userStore.UserRoles.ExistsAsync(x => x.RoleId == role.Id && x.UserId == user.Id);
    }

    /// <summary>
    /// Checks if the user has a specific role.
    /// </summary>
    /// <param name="user">The user to check.</param>
    /// <param name="role">The role to check.</param>
    /// <returns>True if the user has the role, otherwise false.</returns>
    public async Task<bool> HasRoleAsync(CustomUser user, CustomRole role)
    {
        return await _userStore.UserRoles.ExistsAsync(x => x.RoleId == role.Id && x.UserId == user.Id);
    }

    /// <summary>
    /// Checks if the user claims principal has a specific role.
    /// </summary>
    /// <param name="userClaims">The user claims principal to check.</param>
    /// <param name="role">The role to check.</param>
    /// <returns>True if the user claims principal has the role, otherwise false.</returns>
    public async Task<bool> HasRoleAsync(ClaimsPrincipal userClaims, CustomRole role)
    {
        if (!userClaims.HasClaim(x => x.Type == "userId"))
            return false;

        var userClaim = userClaims.Claims.FirstOrDefault(x => x.Type == "userId");
        if (userClaim == null)
            return false;

        string userid = userClaim.Value;
        CustomUser? user = await _userStore.FindUserByIdAsync(userid);
        if (user == null)
            return false;

        return await _userStore.UserRoles.ExistsAsync(x => x.RoleId == role.Id && x.UserId == user.Id);
    }

    /// <summary>
    /// Checks if the user has all specified roles.
    /// </summary>
    /// <param name="user">The user to check.</param>
    /// <param name="rolenames">The list of role names to check.</param>
    /// <returns>True if the user has all roles, otherwise false.</returns>
    public async Task<bool> HasAllRoleAsync(CustomUser user, List<string> rolenames)
    {
        foreach (var role in rolenames)
        {
            var r = await _userStore.Roles.FindAsync(x => x.Name.ToLower() == role.ToLower());
            if (r == null)
                return false;
            if (!await _userStore.UserRoles.ExistsAsync(x => x.UserId == user.Id && x.RoleId == r.Id))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the user has any of the specified roles.
    /// </summary>
    /// <param name="user">The user to check.</param>
    /// <param name="rolenames">The list of role names to check.</param>
    /// <returns>True if the user has any role, otherwise false.</returns>
    public async Task<bool> HasAnyRoleAsync(CustomUser user, List<string> rolenames)
    {
        foreach (var role in rolenames)
        {
            var r = await _userStore.Roles.FindAsync(x => x.Name.ToLower() == role.ToLower());
            if (r != null)
            {
                if (await _userStore.UserRoles.ExistsAsync(x => x.UserId == user.Id && x.RoleId == r.Id))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the users with a specific role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>A list of users with the role.</returns>
    public async Task<List<CustomUser>> GetUsersByRoleAsync(CustomRole role)
    {
        List<CustomUser> users = [];
        foreach (var userRole in await _userStore.UserRoles.QueryAsync(x => x.RoleId == role.Id))
        {
            CustomUser? user = await _userStore.FindUserByIdAsync(userRole.UserId);
            if (user == null)
                continue;

            if ((await GetUserCustomRolesAsync(user.Id)).Any(x => x.Level > role.Level))
                continue;

            users.Add(user);
        }

        return users;
    }


    /// <summary>
    /// Checks if the caller has a higher role than the target.
    /// </summary>
    /// <param name="caller">The caller to check.</param>
    /// <param name="target">The target to check.</param>
    /// <returns>True if the caller has a higher role, otherwise false.</returns>
    public async Task<bool> HasHigherRoleThanAsync(CustomUser caller, CustomUser target)
    {
        if (caller.Id == target.Id)
            return true;

        return (await GetUserHighestRoleAsync(caller.Id)).Level > (await GetUserHighestRoleAsync(target.Id)).Level;
    }

    /// <summary>
    /// Gets the custom roles for a user.
    /// </summary>
    /// <param name="userid">The user ID to get the custom roles for.</param>
    /// <returns>A list of custom roles.</returns>
    private async Task<List<CustomRole>> GetUserCustomRolesAsync(string userid)
    {
        var userRoles = await _userStore.UserRoles.QueryAsync(x => x.UserId == userid);
        List<CustomRole> roles = [];
        foreach (var role in userRoles)
        {
            var r = await _userStore.Roles.FindAsync(x => x.Id == role.RoleId);
            if (r != null)
                roles.Add(r);
        }
        return roles;
    }

    /// <summary>
    /// Gets the highest role for a user.
    /// </summary>
    /// <param name="userid">The user ID to get the highest role for.</param>
    /// <returns>The highest role.</returns>
    public async Task<CustomRole> GetUserHighestRoleAsync(string userid)
    {
        var userRoles = await _userStore.UserRoles.QueryAsync(x => x.UserId == userid);
        List<CustomRole> roles = [];
        foreach (var role in userRoles)
        {
            var r = await _userStore.Roles.FindAsync(x => x.Id == role.RoleId);
            if (r != null)
                roles.Add(r);
        }
        return roles.OrderByDescending(x => x.Level).ElementAt(0);
    }
    #endregion
    #region Claims
    /// <summary>
    /// Gets the role claims for a user.
    /// </summary>
    /// <param name="userid">The user ID to get the role claims for.</param>
    /// <returns>A list of role claims.</returns>
    private async Task<List<IdentityRoleClaim<string>>> GetUserRoleClaimsAsync(string userid)
    {
        List<IdentityRoleClaim<string>> claims = [];
        List<CustomRole> roles = await GetUserCustomRolesAsync(userid);
        foreach (var role in roles.OrderByDescending(x => x.Level))
        {
            claims.AddRange(await  _userStore.RoleClaims.QueryAsync(x => x.RoleId == role.Id));
        }
        return claims;
    }

    /// <summary>
    /// Sets a user claim.
    /// </summary>
    /// <param name="user">The user to set the claim for.</param>
    /// <param name="claim">The claim to set.</param>
    /// <param name="value">The claim value to set.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetUserClaimAsync(CustomUser user, string claim, string value)
    {
        var localClaim = await _userStore.UserClaims.FindAsync(x => x.UserId == user.Id && x.ClaimType == claim);
        if (localClaim == null)
        {
            await _userStore.UserClaims.AddAsync(new CustomUserClaim
            {
                UserId = user.Id,
                ClaimType = claim,
                ClaimValue = value
            }, true);
            return;
        }

        localClaim.ClaimValue = value;
        await _userStore.UserClaims.UpdateAsync(localClaim, true);
    }
    #endregion

    /// <summary>
    /// Checks if the user has a specific permission.
    /// </summary>
    /// <param name="userid">The user ID to check.</param>
    /// <param name="claim">The claim to check.</param>
    /// <param name="value">The claim value to check.</param>
    /// <returns>True if the user has the permission, otherwise false.</returns>
    public async Task<bool> HasPermissionAsync(string userid, string claim, string value = "true")
    {
        var roleClaim = await _userStore.UserClaims.FindAsync(x => x.UserId == userid && x.ClaimType == claim && x.ClaimValue == value);
        if (roleClaim != null)
            return true;

        List<CustomRole> roles = await GetUserCustomRolesAsync(userid);
        foreach (var role in roles.OrderByDescending(x => x.Level))
        {
            if (await _userStore.RoleClaims.ExistsAsync(x => x.RoleId == role.Id && x.ClaimType == claim && x.ClaimValue == value))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if the user has a specific permission.
    /// </summary>
    /// <param name="user">The user to check.</param>
    /// <param name="claim">The claim to check.</param>
    /// <param name="value">The claim value to check.</param>
    /// <returns>True if the user has the permission, otherwise false.</returns>
    public async Task<bool> HasPermissionAsync(CustomUser user, string claim, string value = "true")
    {
        return await HasPermissionAsync(user.Id, claim, value);
    }
    #endregion

    #region Authentication
    /// <summary>
    /// Checks if a password has been compromised using the Pwned Passwords API.
    /// </summary>
    /// <param name="password">The password to check.</param>
    /// <returns>
    /// A boolean value indicating whether the password has been compromised (true) or not (false).
    /// </returns>
    public async Task<bool> IsCompromisedPasswordAsync(string password)
    {
        using var sha1 = SHA1.Create();
        byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
        string hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();

        string prefix = hashString.Substring(0, 5);
        string suffix = hashString.Substring(5);

        try
        {
            string cacheKey = $"pwned:{prefix}";
            if (!_memoryCacheService.TryGetValue(cacheKey, out string? response))
            {
                response = await _client.GetStringAsync($"https://api.pwnedpasswords.com/range/{prefix}");
                if (!string.IsNullOrEmpty(cacheKey))
                    _memoryCacheService.SetValue(cacheKey, response, CompPassTTL);
            }
            return !string.IsNullOrEmpty(response) && response.Contains(suffix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured during compromised password check.");
            return false;
        }
    }

    /// <summary>
    /// Create a signed JWT access token for a user with the specified duration and optional claims.
    /// </summary>
    /// <param name="duration">The lifetime of the token (how long it will be valid).</param>
    /// <param name="claims">Optional <see cref="ClaimsIdentity"/> to include additional claims in the token payload.</param>
    /// <returns>A serialized JWT as a string.</returns>
    public string CreateJwtToken(TimeSpan duration, ClaimsIdentity? claims = null)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(
            _appConfiguration.Jwt.Issuer,
            _appConfiguration.Jwt.Audience,
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.Add(duration),
            DateTime.UtcNow,
            new SigningCredentials(
                new SymmetricSecurityKey(_appConfiguration.Jwt.EncryptionKey),
                SecurityAlgorithms.HmacSha256)
            );
        return handler.WriteToken(token);
    }

    /// <summary>
    /// Verify the username (email or username) + password combination and return the <see cref="CustomUser"/> if valid.
    /// </summary>
    /// <param name="username">Username or email provided by the client (will be normalized internally).</param>
    /// <param name="password">Plaintext password to verify.</param>
    /// <returns>
    /// The matched <see cref="CustomUser"/> on success; otherwise <c>null</c> when verification failed.
    /// </returns>
    public async Task<CustomUser?> VerifyPasswordAsync(string username, string password)
    {
        string normalizedUsername = username.Normalize();
        CustomUser? user = await _userStore.FindUserAsync(x => x.NormalizedEmail == normalizedUsername || x.NormalizedUserName == normalizedUsername);
        if (user == null)
            return null;

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        switch (result)
        {
            case PasswordVerificationResult.Failed:
                user.AccessFailedCount++;
                if (user.AccessFailedCount > _appConfiguration.Jwt.LockoutMaxAttempts)
                {
                    user.LockoutEnabled = true;
                    user.LockoutEnd = DateTimeOffset.UtcNow.Add(_appConfiguration.Jwt.LockoutDuration);
                    user.LockoutReason = "Too many failed login attempts.";
                }
                await _userStore.UpdateUserAsync(user, true);
                return null;
            case PasswordVerificationResult.SuccessRehashNeeded:
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                user.SecurityStamp = Guid.NewGuid().ToString();
                await _userStore.UpdateUserAsync(user, true);
                break;
        }

        return user;
    }

    /// <summary>
    /// Verify a JWT-based login: validate the JWT, ensure the token exists in the user token store,
    /// and confirm an associated, unexpired login record exists.
    /// </summary>
    /// <param name="token">The JWT token value received from the client.</param>
    /// <returns>The associated <see cref="CustomUser"/> if verification succeeds; otherwise <c>null</c>.</returns>
    public async Task<CustomUser?> VerifyTokenLoginAsync(string token)
    {
        if (!await VerifyJwtTokenAsync(token))
            return null;

        string hashedToken = StringChiper.GetEncryptedHash(token, _appConfiguration.Jwt.EncryptionKey);
        CustomUserToken? userToken = await _userStore.UserTokens.FindAsync(x => x.Value == hashedToken);
        if (userToken == null)
            return null;

        CustomUserLogin? userLogin = await _userStore.UserLogins.FindAsync(x => x.UserId == userToken.UserId && x.ProviderKey == userToken.Id && x.ExpireDate > DateTimeOffset.UtcNow);
        if (userLogin == null)
            return null;

        return await _userStore.FindUserByIdAsync(userToken.UserId);
    }

    /// <summary>
    /// Validate a JWT token's signature, lifetime and configured claims (issuer, audience).
    /// </summary>
    /// <param name="token">The serialized JWT token.</param>
    /// <returns><c>true</c> if token is valid according to the configured <c>TokenValidationParameters</c>; otherwise <c>false</c>.</returns>
    public async Task<bool> VerifyJwtTokenAsync(string token)
    {
        try
        {
            var result = await _jwtSecurityTokenHandler.ValidateTokenAsync(token, _tokenValidationParameters);
            if (result == null || !result.IsValid)
                return false;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while verifying the token.");
            return false;
        }
    }

    /// <summary>
    /// Produce a machine-specific fingerprint for tying short-lived sessions (e.g. TFA flows) to a client.
    /// </summary>
    /// <param name="httpContext">The current <see cref="HttpRequest"/> to inspect client headers and connection info.</param>
    /// <param name="userId">The user id used as part of the fingerprint seed.</param>
    /// <returns>A keyed hash of client traits that can be used as a fingerprint string.</returns>
    public string GetMachineFingerprint(HttpContext httpContext, string userId)
    {
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        // Combine traits and hash them
        var rawData = string.Concat(userId, "-", userAgent);
        return StringChiper.GetEncryptedHash(rawData, _appConfiguration.Jwt.FingerprintKey);
    }
    #endregion

    #region TwoFactor Authentication
    /// <summary>
    /// Generate and persist a new two-factor secret for the user.
    /// </summary>
    /// <param name="user">The user who will receive the TOTP secret.</param>
    /// <returns>
    /// The generated (plaintext) secret token (caller must present this to the user's authenticator app as a QR or manual code).
    /// </returns>
    public async Task<string> GenerateTwoFactorTokenAsync(CustomUser user)
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        string token = Encoding.UTF8.GetString(key);
        user.TwoFactorSecret = token.EncryptSelf(_appConfiguration.Jwt.TwoFactorEncryptionKey);
        user.SecurityStamp  = Guid.NewGuid().ToString();
        await _userStore.UpdateUserAsync(user, true);
        return token;
    }

    /// <summary>
    /// Verify a TOTP code against the stored (encrypted) TOTP secret for a user.
    /// </summary>
    /// <param name="user">The user whose TOTP secret is stored.</param>
    /// <param name="code">The TOTP code provided by the user to verify.</param>
    /// <returns><c>true</c> if the code is valid within the allowed verification window; otherwise <c>false</c>.</returns>
    public bool VerifyTwoFactorCode(CustomUser user, string code)
    {
        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            return false;

        string decryptedSecret = user.TwoFactorSecret.DecryptSelf(_appConfiguration.Jwt.TwoFactorEncryptionKey);
        var totp = new Totp(Encoding.UTF8.GetBytes(decryptedSecret));
        return totp.VerifyTotp(code, out _, new VerificationWindow(1));
    }

    /// <summary>
    /// Generate a fresh set of one-time recovery codes for the user and persist them as hashed values.
    /// </summary>
    /// <param name="user">The user for whom to create recovery codes.</param>
    /// <param name="number">The number of recovery codes to generate.</param>
    /// <returns>A collection of plaintext recovery codes (display these to the user once); or <c>null</c> on failure.</returns>
    public async Task<IEnumerable<string>?> GenerateNewTwoFactorRecoveryCodesAsync(CustomUser user, int number)
    {
        var existingCodes = await _userStore.UserBackupCodes.QueryAsync(x => x.UserId == user.Id);
        foreach (var code in existingCodes)
            await _userStore.UserBackupCodes.RemoveAsync(code);
        await _context.SaveChangesAsync();
        List<string> newCodes = [];
        for (int i = 0; i < number; i++)
        {
            string code = TokenHelper.GenerateRandomString(length: 10);
            newCodes.Add(code);
            await _userStore.UserBackupCodes.AddAsync(new UserBackupCode
            {
                UserId = user.Id,
                HashedCode = StringChiper.GetEncryptedHash(code, _appConfiguration.Jwt.TwoFactorEncryptionKey),
                CreateAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();
        return newCodes;
    }
    #endregion
}
