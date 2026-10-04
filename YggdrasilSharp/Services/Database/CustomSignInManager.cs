using Microsoft.AspNetCore.Identity;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Utils.Helpers;
using SignInResult = Tavstal.YggdrasilSharp.Models.Database.SignInResult;

namespace Tavstal.YggdrasilSharp.Services.Database;

/// <summary>
/// Responsible for handling sign-in related flows for users.
/// </summary>
public class CustomSignInManager
{
    private readonly CustomUserStore _userStore;
    private readonly CustomUserManager _userManager;
    private readonly IPasswordHasher<CustomUser> _passwordHasher;
    private readonly MemoryCacheService _memoryCacheService;
    private readonly AppConfiguration _appConfiguration;
    private readonly TimeSpan _regularLogin = TimeSpan.FromHours(1);
    private readonly TimeSpan _rememberMeLogin = TimeSpan.FromDays(7);

    /// <summary>
    /// Creates a new instance of <see cref="CustomSignInManager"/>.
    /// </summary>
    /// <param name="userStore">Data store for user and related entities.</param>
    /// <param name="userManager">Helper manager for user-specific operations.</param>
    /// <param name="passwordHasher">Password hasher used to verify and rehash passwords.</param>
    /// <param name="memoryCacheService">In-memory cache for temporary values (TFA tokens, attempts, etc).</param>
    /// <param name="appConfiguration">Application settings (lockout thresholds, durations).</param>
    public CustomSignInManager(CustomUserStore userStore, CustomUserManager userManager, IPasswordHasher<CustomUser> passwordHasher, MemoryCacheService memoryCacheService, AppConfiguration appConfiguration)
    {
         _userStore = userStore;
         _userManager = userManager;
         _passwordHasher = passwordHasher;
         _memoryCacheService = memoryCacheService;
         _appConfiguration = appConfiguration;
    }

    /// <summary>
    /// Sign-in using a username and password.
    /// </summary>
    /// <param name="username">Plain username provided by the client.</param>
    /// <param name="password">Plain password provided by the client.</param>
    /// <param name="rememberMe">If true, issue a longer-lived token.</param>
    /// <param name="httpContext">HttpContext to extract client metadata (IP, UA) for login records.</param>
    /// <returns>A <see cref="SignInResult"/> describing the outcome.</returns>
    public async Task<SignInResult> UsernameSignInAsync(string username, string password, bool rememberMe, HttpContext httpContext)
    {
        string normalizedUsername = username.Normalize();
        CustomUser? user = await _userStore.FindUserAsync(x => x.NormalizedUserName == normalizedUsername);
        if (user == null)
            return new SignInResult
            {
                Succeeded = false,
                Message = "Invalid credentials."
            };

        return await SignInAsync(user, password, rememberMe, httpContext);
    }

    /// <summary>
    /// Sign-in using an email address and password.
    /// </summary>
    /// <param name="email">Plain email provided by the client.</param>
    /// <param name="password">Plain password provided by the client.</param>
    /// <param name="rememberMe">If true, issue a longer-lived token.</param>
    /// <param name="httpContext">HttpContext to extract client metadata (IP, UA) for login records.</param>
    /// <returns>A <see cref="SignInResult"/> describing the outcome.</returns>
    public async Task<SignInResult> EmailSignInAsync(string email, string password, bool rememberMe,
        HttpContext httpContext)
    {
        string normalizedEmail = email.Normalize();
        CustomUser? user = await _userStore.FindUserAsync(x => x.NormalizedEmail == normalizedEmail);
        if (user == null)
            return new SignInResult
            {
                Succeeded = false,
                Message = "Invalid credentials."
            };

        return await SignInAsync(user, password, rememberMe, httpContext);
    }

    /// <summary>
    /// Common sign-in flow shared by username and email sign-ins.
    /// <br/>Steps:
    /// <br/>- Check lockout state and return if locked.
    /// <br/>- Verify password using the configured hasher.
    /// <br/>- On failure: increment failed counter, possibly lock the account.
    /// <br/>- On success with rehash needed: rehash password and update SecurityStamp.
    /// <br/>- If Two-Factor is enabled on the account: generate a short TFA session token and store TFA attempt counter in memory cache.
    /// <br/>- Otherwise: create a user access token, record a user login entry with client metadata (IP, UA), reset failed counters and return success.
    /// </summary>
    /// <param name="user">The user to sign in.</param>
    /// <param name="password">The candidate plain password.</param>
    /// <param name="rememberMe">If true, issues a longer-lived token.</param>
    /// <param name="httpContext">HttpContext for metadata and connection info.</param>
    /// <returns>A <see cref="SignInResult"/> describing the outcome.</returns>
    private async Task<SignInResult> SignInAsync(CustomUser user, string password, bool rememberMe, HttpContext httpContext)
    {
        if (!user.EmailConfirmed)
            return new SignInResult
            {
                Succeeded = false,
                Message = "Email address is not confirmed."
            };

        if (user.LockoutEnabled)
        {
            if (user.LockoutEnd > DateTimeOffset.UtcNow)
                return new SignInResult
                {
                    Succeeded = false,
                    Message = $"Account locked until {user.LockoutEnd:u}. Reason: {user.LockoutReason}"
                };

            user.LockoutEnabled = false;
            user.AccessFailedCount = 0;
            await _userStore.UpdateUserAsync(user, true);
        }

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
                return new SignInResult
                {
                    Succeeded = false,
                    Message = user.LockoutEnabled ? $"Account locked until {user.LockoutEnd:u}. Reason: {user.LockoutReason}" :  "Invalid credentials."
                };
            case PasswordVerificationResult.SuccessRehashNeeded:
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                user.SecurityStamp = Guid.NewGuid().ToString();
                break;
        }

        if (user.TwoFactorEnabled)
        {
            string sessionToken = TokenHelper.GenerateTwoFactorSessionToken();
            string fingerprint = _userManager.GetMachineFingerprint(httpContext, user.Id);
            TimeSpan tokenExpiry = TimeSpan.FromMinutes(5);
            _memoryCacheService.SetValue($"auth:{fingerprint}:tfa:token", sessionToken, tokenExpiry);
            user.AccessFailedCount = 0;
            await _userStore.UpdateUserAsync(user, true);

            return new SignInResult
            {
                Succeeded = false,
                RequiresTwoFactor = true,
                SessionToken = sessionToken,
                TokenExpiresAt = DateTimeOffset.UtcNow.Add(tokenExpiry),
                User = user,
                Message = "Two-factor authentication required."
            };
        }

        var lifeSpan = rememberMe ? _rememberMeLogin : _regularLogin;
        DateTimeOffset expireDate = DateTimeOffset.UtcNow.Add(lifeSpan);
        var userToken = await _userStore.UserTokens.AddAsync(
            new CustomUserToken(
                user.Id,
                "AccessToken",
                _userManager.CreateJwtToken(lifeSpan),
                "Default",
                DateTimeOffset.UtcNow
            ), true);

        string ipv4 = httpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
        string ipv6 = httpContext.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "::1";
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        string operatingSystem = HttpHelper.GetOperatingSystem(userAgent);
        string browser = HttpHelper.GetBrowser(userAgent);
        IpInfo ipInfo = await DatabaseHelper.GetIpInformation(ipv4);

        var userLogin = await _userStore.UserLogins.AddAsync(new CustomUserLogin(user.Id, userToken.Id, "Default",
            "Default", ipv4, ipv6, ipInfo, operatingSystem, browser, DateTimeOffset.UtcNow, expireDate), true);

        user.AccessFailedCount = 0;
        user.LockoutEnabled = false;
        await _userStore.UpdateUserAsync(user, true);
        return new SignInResult
        {
            Succeeded = true,
            User = user,
            UserLogin = userLogin,
            UserToken = userToken,
            Message = "Sign-in successful."
        };
    }

    /// <summary>
    /// Complete a two-factor sign-in flow for a user that previously initiated TFA.
    /// </summary>
    /// <param name="user">The user who is attempting to complete TFA.</param>
    /// <param name="code">The TOTP/verification code submitted by the user.</param>
    /// <param name="rememberMe">If true, issue a longer-lived token on success.</param>
    /// <param name="httpContext">HttpContext for metadata and connection info.</param>
    /// <returns>A <see cref="SignInResult"/> describing the outcome.</returns>
    public async Task<SignInResult> TwoFactorSignInAsync(CustomUser user, string code, bool rememberMe, HttpContext httpContext)
    {
        if (!user.TwoFactorEnabled)
            return new SignInResult
            {
                Succeeded = false,
                Message = "Two-factor authentication is not enabled for this account."
            };

        if (!user.EmailConfirmed)
            return new SignInResult
            {
                Succeeded = false,
                Message = "Email address is not confirmed."
            };

        if (user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow)
            return new SignInResult
            {
                Succeeded = false,
                Message = $"Account locked until {user.LockoutEnd:u}. Reason: {user.LockoutReason}"
            };

        string fingerprint = _userManager.GetMachineFingerprint(httpContext, user.Id);
        string tokenKey = $"auth:{fingerprint}:tfa:token";
        if (!_memoryCacheService.TryGetValue<string>(tokenKey, out _))
            return new SignInResult
            {
                Succeeded = false,
                RequiresTwoFactor = false,
                Message = "Session token expired."
            };

        if (!_userManager.VerifyTwoFactorCode(user, code))
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount > _appConfiguration.Jwt.LockoutMaxAttempts)
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.UtcNow.Add(_appConfiguration.Jwt.LockoutDuration);
                user.LockoutReason = "Too many failed two-factor attempts.";
            }
            await _userStore.UpdateUserAsync(user, true);
            return new SignInResult
            {
                Succeeded = false,
                RequiresTwoFactor = false,
                Message = "Invalid two-factor code."
            };
        }


        var lifeSpan = rememberMe ? _rememberMeLogin : _regularLogin;
        DateTimeOffset expireDate = DateTimeOffset.UtcNow.Add(lifeSpan);
        var userToken = await _userStore.UserTokens.AddAsync(
            new CustomUserToken(
                user.Id,
                "AccessToken",
                _userManager.CreateJwtToken(lifeSpan),
                "Default",
                DateTimeOffset.UtcNow
            ), true);

        string ipv4 = httpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
        string ipv6 = httpContext.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "::1";
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        string operatingSystem = HttpHelper.GetOperatingSystem(userAgent);
        string browser = HttpHelper.GetBrowser(userAgent);
        IpInfo ipInfo = await DatabaseHelper.GetIpInformation(ipv4);

        var userLogin = await _userStore.UserLogins.AddAsync(new CustomUserLogin(user.Id, userToken.Id, "Default",
            "Default", ipv4, ipv6, ipInfo, operatingSystem, browser, DateTimeOffset.UtcNow, expireDate), true);

        user.AccessFailedCount = 0;
        user.LockoutEnabled = false;
        await _userStore.UpdateUserAsync(user, true);

        _memoryCacheService.RemoveValue(tokenKey);

        return new SignInResult
        {
            Succeeded = true,
            User = user,
            UserLogin = userLogin,
            UserToken = userToken,
            Message = "Sign-in successful."
        };
    }


    /// <summary>
    /// Sign in specifically for the desktop launcher flow (returns a play session token).
    /// This flow is similar to <see cref="SignInAsync"/> but creates a shorter-lived play session record
    /// used by the native launcher to start a game session.
    /// </summary>
    /// <param name="username">Username provided by the launcher.</param>
    /// <param name="password">Password provided by the launcher.</param>
    /// <param name="clientId">Client id provided by the launcher.</param>
    /// <param name="httpContext">HttpContext used to extract host for play session recording.</param>
    /// <returns>A <see cref="LauncherSignInResult"/> indicating success/failure and containing the play session record.</returns>
    public async Task<LauncherSignInResult> LauncherSignInAsync(string username, string password, string? clientId, HttpContext httpContext)
    {
        string normalizedUsername = username.Normalize();
        CustomUser? user = await _userStore.FindUserAsync(x => x.NormalizedUserName == normalizedUsername);
        if (user == null)
            return new LauncherSignInResult
            {
                Succeeded = false,
                Message = "Invalid credentials."
            };

        if (!user.EmailConfirmed)
            return new LauncherSignInResult
            {
                Succeeded = false,
                Message = "Email address is not confirmed."
            };

        if (user.LockoutEnabled)
        {
            if (user.LockoutEnd > DateTimeOffset.UtcNow)
                return new LauncherSignInResult
                {
                    Succeeded = false,
                    Message = $"Account locked until {user.LockoutEnd:u}. Reason: {user.LockoutReason}"
                };

            user.LockoutEnabled = false;
            user.AccessFailedCount = 0;
            await _userStore.UpdateUserAsync(user, true);
        }

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
                return new LauncherSignInResult
                {
                    Succeeded = false,
                    Message = user.LockoutEnabled ? $"Account locked until {user.LockoutEnd:u}. Reason: {user.LockoutReason}" :  "Invalid credentials."
                };
            case PasswordVerificationResult.SuccessRehashNeeded:
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                user.SecurityStamp = Guid.NewGuid().ToString();
                break;
        }

        if (user.TwoFactorEnabled)
        {
            string sessionToken = TokenHelper.GenerateTwoFactorSessionToken();
            string fingerprint = _userManager.GetMachineFingerprint(httpContext, user.Id);
            TimeSpan tokenExpiry = TimeSpan.FromMinutes(5);
            _memoryCacheService.SetValue($"auth:{fingerprint}:tfa-launcher:token", sessionToken, tokenExpiry);
            user.AccessFailedCount = 0;
            await _userStore.UpdateUserAsync(user, true);

            return new LauncherSignInResult
            {
                Succeeded = false,
                RequiresTwoFactor = true,
                SessionToken = sessionToken,
                TokenExpiresAt = DateTimeOffset.UtcNow.Add(tokenExpiry),
                User = user,
                Message = "Two-factor authentication required."
            };
        }

        string host = HttpHelper.GetClientIp(httpContext) ?? "";
        var userPlaySession = await _userStore.UserPlaySessions.AddAsync(new UserPlaySession
        {
            UserId = user.Id,
            UserIp = host,
            ClientId = clientId,
            Token = _userManager.CreateJwtToken(TimeSpan.FromHours(_appConfiguration.Yggdrasil.TokenTtlHours)),
            CreatedAt =  DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(_appConfiguration.Yggdrasil.TokenTtlHours)
        }, true);

        user.AccessFailedCount = 0;
        user.LockoutEnabled = false;
        await _userStore.UpdateUserAsync(user, true);
        await CheckPlaySessionsAsync(user, _appConfiguration.Yggdrasil.MaxActiveTokensPerUser);

        return new LauncherSignInResult
        {
            Succeeded = true,
            User = user,
            UserPlaySession = userPlaySession,
            Message = "Sign-in successful."
        };
    }

    /// <summary>
    /// Complete two-factor authentication for the launcher play session flow.
    /// This mirrors <see cref="TwoFactorSignInAsync"/> but uses the launcher-specific cache keys and returns a launcher result type.
    /// </summary>
    /// <param name="user">The user who is completing TFA.</param>
    /// <param name="code">The verification code supplied by the client.</param>
    /// <param name="clientId">Client id provided by the launcher.</param>
    /// <param name="httpContext">HttpContext for host extraction.</param>
    /// <returns>A <see cref="LauncherSignInResult"/>.</returns>
    public async Task<LauncherSignInResult> LauncherTwoFactorSignInAsync(CustomUser user, string code, string? clientId, HttpContext httpContext)
    {
        if (!user.TwoFactorEnabled)
            return new LauncherSignInResult
            {
                Succeeded = false,
                Message = "Two-factor authentication is not enabled for this account."
            };

        if (user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow)
            return new LauncherSignInResult
            {
                Succeeded = false,
                Message = $"Account locked until {user.LockoutEnd:u}. Reason: {user.LockoutReason}"
            };

        string fingerprint = _userManager.GetMachineFingerprint(httpContext, user.Id);
        string tokenKey = $"auth:{fingerprint}:tfa-launcher:token";
        if (!_memoryCacheService.TryGetValue<string>(tokenKey, out _))
            return new LauncherSignInResult
            {
                Succeeded = false,
                RequiresTwoFactor = false,
                Message = "Session token expired."
            };

        if (!_userManager.VerifyTwoFactorCode(user, code))
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount > _appConfiguration.Jwt.LockoutMaxAttempts)
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.UtcNow.Add(_appConfiguration.Jwt.LockoutDuration);
                user.LockoutReason = "Too many failed two-factor attempts.";
            }
            await _userStore.UpdateUserAsync(user, true);
            return new LauncherSignInResult
            {
                Succeeded = false,
                RequiresTwoFactor = false,
                Message = "Invalid two-factor code."
            };
        }

        user.AccessFailedCount = 0;
        user.LockoutEnabled = false;
        await _userStore.UpdateUserAsync(user, true);

        string host = HttpHelper.GetClientIp(httpContext) ?? "";
        var userPlaySession = await _userStore.UserPlaySessions.AddAsync(new UserPlaySession
        {
            UserId = user.Id,
            UserIp = host,
            ClientId = clientId,
            Token = _userManager.CreateJwtToken(TimeSpan.FromHours(_appConfiguration.Yggdrasil.TokenTtlHours)),
            CreatedAt =  DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(_appConfiguration.Yggdrasil.TokenTtlHours)
        }, true);

        _memoryCacheService.RemoveValue(tokenKey);

        await CheckPlaySessionsAsync(user, _appConfiguration.Yggdrasil.MaxActiveTokensPerUser);

        return new LauncherSignInResult
        {
            Succeeded = true,
            User = user,
            UserPlaySession = userPlaySession,
            Message = "Sign-in successful."
        };
    }

    /// <summary>
    /// Remove an access token and its associated login record.
    /// </summary>
    /// <param name="token">The access token value to revoke.</param>
    /// <returns>True if token was found and removed, false otherwise.</returns>
    public async Task<bool> SignOutAsync(string token)
    {
        CustomUserToken? userToken = await _userStore.UserTokens.FindAsync(x => x.Value == token);
        if (userToken == null)
            return false;

        CustomUserLogin? userLogin = await _userStore.UserLogins.FindAsync(x => x.ProviderKey == userToken.Id);
        if (userLogin != null)
            await _userStore.UserLogins.RemoveAsync(userLogin, true);
        await _userStore.UserTokens.RemoveAsync(userToken, true);
        return true;
    }

    /// <summary>
    /// Remove a launcher play session based on the play session token.
    /// </summary>
    /// <param name="playSessionToken">The play session token to revoke.</param>
    /// <returns>True if the session was found and removed, false otherwise.</returns>
    public async Task<bool> LauncherSignOutAsync(string playSessionToken)
    {
        UserPlaySession? playSession = await _userStore.UserPlaySessions.FindAsync(x => x.Token == playSessionToken);
        if (playSession == null)
            return false;

        await _userStore.UserPlaySessions.RemoveAsync(playSession);
        return true;
    }

    /// <summary>
    /// Exchanges an active play session token for a freshly issued one, carrying over the session's user, IP and client.
    /// The previous session token is not revoked by this operation.
    /// </summary>
    /// <param name="token">The play session token to refresh.</param>
    /// <returns>The newly created play session, or null when the token is unknown or expired.</returns>
    public async Task<UserPlaySession?> RefreshPlaySessionToken(string token)
    {
        UserPlaySession? playSession = await _userStore.UserPlaySessions.FindAsync(x => x.Token == token);
        if (playSession == null)
            return null;

        if (playSession.ExpiresAt < DateTimeOffset.UtcNow)
            return null;

        return await _userStore.UserPlaySessions.AddAsync(new UserPlaySession
        {
            UserId = playSession.UserId,
            UserIp = playSession.UserIp,
            ClientId = playSession.ClientId,
            Token = _userManager.CreateJwtToken(TimeSpan.FromHours(_appConfiguration.Yggdrasil.TokenTtlHours)),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(_appConfiguration.Yggdrasil.TokenTtlHours),
        }, true);
    }

    private async Task CheckPlaySessionsAsync(CustomUser user, int toKeep = 10)
    {
        var sessions = await _userStore.UserPlaySessions.QueryAsync(x => x.UserId == user.Id);
        List<UserPlaySession> sessionsToRemove = [];
        int shouldKeepCount = 0;
        foreach (var session in sessions.OrderByDescending(x => x.CreatedAt))
        {
            if (session.ExpiresAt < DateTimeOffset.UtcNow)
            {
                sessionsToRemove.Add(session);
                continue;
            }

            if (shouldKeepCount >= toKeep)
            {
                sessionsToRemove.Add(session);
                continue;
            }

            shouldKeepCount++;
        }

        if (sessionsToRemove.Count > 0)
            await _userStore.UserPlaySessions.RemoveRangeAsync(sessionsToRemove, true);
    }
}
