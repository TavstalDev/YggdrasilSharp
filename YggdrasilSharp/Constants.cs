namespace Tavstal.YggdrasilSharp;

/// <summary>
/// Holds project-wide constant values used for configuration keys and authentication rules.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Configuration key names used to read settings from configuration providers
    /// (appsettings.json, environment variables, .env loader, etc.).
    /// Use these constants when accessing IConfiguration to avoid typo-related bugs.
    /// </summary>
    public static class ConfigurationKeys
    {
        /// <summary>
        /// Configuration key for the port number on which the application's Kestrel server will listen.
        /// Example: 5001 (for HTTP) or 443 (for HTTPS)
        /// </summary>
        public const string ApplicationPort = "Application:Port";
        
        /// <summary>
        /// The configuration key for the public-facing website URL of hosted game servers.
        /// Example: "https://example.com"
        /// </summary>
        public const string RuntimeWebsiteUrl = "Runtime:WebsiteUrl";

        /// <summary>
        /// The configuration key for the API base URL.
        /// Example: "https://api.example.com"
        /// </summary>
        public const string RuntimeApiUrl = "Runtime:ApiUrl";
        
        /// <summary>
        /// Configuration key for the application's upload directory path.
        /// Specifies the directory where uploaded files (e.g., user avatars, documents) are stored.
        /// Example: "wwwroot/uploads"
        /// </summary>
        public const string RuntimeUploadDir = "Runtime:UploadDir";
        
        /// <summary>
        /// Configuration key for the CORS (Cross-Origin Resource Sharing) idle timeout duration.
        /// Example: 3600 (represents 1 hour of idle timeout for CORS sessions)
        /// </summary>
        public const string CorsIdleTimeout = "Cors:IdleTimeout";
        
        /// <summary>
        /// Configuration key for the HTTP status code returned when rate limiting is triggered.
        /// Example: 429 (Too Many Requests)
        /// </summary>
        public const string RateLimitingStatusCode = "RateLimiting:StatusCode";
        
        /// <summary>
        /// Configuration key for the maximum file upload size limit in megabytes.
        /// Example: 100 (allows uploads up to 100 MB)
        /// </summary>
        public const string RateLimitingUploadLimit = "RateLimiting:UploadLimitMegabytes";
        
        /// <summary>
        /// Configuration key for the rate limiting rules' dictionary.
        /// Contains configuration for different rate limit policies (Default, AuthLogin, Upload, etc.).
        /// Each rule specifies PermitLimit, WindowSeconds, and QueueLimit values.
        /// </summary>
        public const string RateLimitingRules = "RateLimiting:Rules";

        /// <summary>
        /// Configuration key indicating whether forwarded headers (X-Forwarded-For, X-Forwarded-Proto)
        /// are processed. Must only be enabled when the application sits behind a trusted reverse proxy,
        /// otherwise clients can spoof their own IP address.
        /// Example: true
        /// </summary>
        public const string ProxyEnabled = "Proxy:Enabled";

        /// <summary>
        /// Configuration key for the number of proxy hops that are allowed to append to the
        /// X-Forwarded-For header. Use 1 for a single reverse proxy, 2 for a CDN in front of a reverse proxy.
        /// Example: 1
        /// </summary>
        public const string ProxyForwardLimit = "Proxy:ForwardLimit";

        /// <summary>
        /// Configuration key containing the exact IP addresses of proxies that are allowed
        /// to send forwarded headers.
        /// Example: [ "127.0.0.1", "::1" ]
        /// </summary>
        public const string ProxyKnownProxies = "Proxy:KnownProxies";

        /// <summary>
        /// Configuration key containing the CIDR ranges of proxies that are allowed to send forwarded headers.
        /// Example: [ "172.18.0.0/16" ]
        /// </summary>
        public const string ProxyKnownNetworks = "Proxy:KnownNetworks";

        /// <summary>
        /// Configuration key containing the host names that are accepted in forwarded headers.
        /// Leave empty to accept any host name sent by an already trusted proxy.
        /// Example: [ "api.example.com" ]
        /// </summary>
        public const string ProxyAllowedHosts = "Proxy:AllowedHosts";

        /// <summary>
        /// Configuration key for the database connection string.
        /// The connection string contains placeholders for $DB_USER and $DB_PASSWORD that are 
        /// replaced at runtime with values from the DatabaseUser and DatabasePassword configuration keys.
        /// Example: "server=localhost;port=3306;database=ysharp;uid=$DB_USER;pwd=$DB_PASSWORD;"
        /// </summary>
        public const string DatabaseConnectionString = "Database:ConnectionString";

        /// <summary>
        /// Configuration key for the database provider type.
        /// Determines which Entity Framework Core database provider to use.
        /// </summary>
        public const string DatabaseProvider = "Database:Provider";

        /// <summary>
        /// Configuration key for the database version.
        /// Used to configure database-specific behavior and compatibility options.
        /// </summary>
        public const string DatabaseVersion = "Database:Version";

        /// <summary>
        /// Configuration key for the JWT token issuer value.
        /// </summary>
        public const string JwtIssuer = "Jwt:Issuer";

        /// <summary>
        /// Configuration key for the JWT token audience value.
        /// </summary>
        public const string JwtAudience = "Jwt:Audience";

        /// <summary>
        /// Configuration key for the JWT token clock skew tolerance value in seconds.
        /// </summary>
        public const string JwtClockSkew = "Jwt:ClockSkew";
        
        /// <summary>
        /// Configuration key that holds the maximum number of failed authentication attempts
        /// before a user is locked out for JWT-based authentication flows.
        /// </summary>
        public const string JwtLockoutMaxAttempts = "Jwt:Lockout:MaxAttempts";
        
        /// <summary>
        /// Configuration key that holds the lockout duration applied when a user has exceeded
        /// the allowed failed authentication attempts in JWT-based authentication flows.
        /// </summary>
        public const string JwtLockoutDuration = "Jwt:Lockout:Duration";
        
        /// <summary>
        /// Configuration key for the email (SMTP) provider hostname.
        /// Example: "smtp.example.com"
        /// </summary>
        public const string EmailProvider = "Email:Provider";

        /// <summary>
        /// Configuration key for the email (SMTP) provider port.
        /// Example: 25, 465, or 587
        /// </summary>
        public const string EmailPort = "Email:Port";
        
        /// <summary>
        /// Configuration key for the SMTP client timeout, in milliseconds.
        /// Example: 12000
        /// </summary>
        public const string EmailTimeout = "Email:Timeout";

        /// <summary>
        /// Configuration key that controls whether the legacy Yggdrasil authentication flow
        /// is enabled alongside the JWT-based flow. Leave disabled unless clients in your
        /// environment still depend on the older authentication endpoints.
        /// Example: false
        /// </summary>
        public const string YggdrasilEnableLegacyAuth = "Yggdrasil:EnableLegacyAuth";

        /// <summary>
        /// Configuration key containing an array/list of allowed skin domains for Yggdrasil
        /// (used when validating or serving player skins).
        /// </summary>
        public const string YggdrasilSkinDomains = "Yggdrasil:SkinDomains";

        /// <summary>
        /// Configuration key for the Yggdrasil-compatible server display name.
        /// </summary>
        public const string YggdrasilServerName = "Yggdrasil:ServerName";

        /// <summary>
        /// Configuration key for the Yggdrasil implementation name (metadata returned to clients).
        /// </summary>
        public const string YggdrasilImplementationName = "Yggdrasil:ImplementationName";

        /// <summary>
        /// Configuration key for the Yggdrasil implementation version string (metadata returned to clients).
        /// </summary>
        public const string YggdrasilImplementationVersion = "Yggdrasil:ImplementationVersion";

        /// <summary>
        /// Configuration key that controls whether the IP address of the request is compared against
        /// the IP address stored on the access token's play session when joining a game server.
        /// Disable this when the API is behind a proxy that cannot report the real client IP,
        /// or when players are expected to change networks (mobile, CGNAT, VPN).
        /// Example: true
        /// </summary>
        public const string YggdrasilEnforceIpCheckInJoin = "Yggdrasil:EnforceIpCheckInJoin";

        /// <summary>
        /// Configuration key that controls whether the IP address reported by the game server
        /// on <c>hasJoined</c> requests is compared against the IP address stored on the server join.
        /// Example: true
        /// </summary>
        public const string YggdrasilEnforceIpCheckInHasJoined = "Yggdrasil:EnforceIpCheckInHasJoined";

        /// <summary>
        /// Configuration key that allows <c>hasJoined</c> requests to omit the <c>ip</c> query parameter,
        /// in which case the most recent valid join for the given server and user is used.
        /// Example: false
        /// </summary>
        public const string YggdrasilAllowEmptyJoinedAddress = "Yggdrasil:AllowEmptyJoinedAddress";

        /// <summary>
        /// Configuration key that controls whether the <c>User-Agent</c> header of session server
        /// requests is validated against the configured agent. When disabled, any agent is accepted.
        /// Example: false
        /// </summary>
        public const string YggdrasilEnforceAgent = "Yggdrasil:EnforceAgent";

        /// <summary>
        /// Configuration key for the name of the user agent accepted by the session server endpoints,
        /// which is the part before the slash in the <c>User-Agent</c> header.
        /// Example: "Minecraft"
        /// </summary>
        public const string YggdrasilAgentName = "Yggdrasil:Agent:Name";

        /// <summary>
        /// Configuration key for the version of the user agent accepted by the session server endpoints,
        /// which is the part after the slash in the <c>User-Agent</c> header.
        /// Example: 1
        /// </summary>
        public const string YggdrasilAgentVersion = "Yggdrasil:Agent:Version";

        /// <summary>
        /// Configuration key containing the list of server identifiers that clients are not allowed to join.
        /// Returned verbatim by the session server's <c>blockedservers</c> endpoint.
        /// Example: [ "an-abusive-server.example.com" ]
        /// </summary>
        public const string YggdrasilBlockedServers = "Yggdrasil:BlockedServers";

        /// <summary>
        /// Configuration key that controls whether the Yggdrasil <c>authenticate</c> and <c>signout</c>
        /// endpoints accept a Minecraft username in addition to an email address as the login identifier.
        /// Also drives the <c>meta.feature.non_email_login</c> flag in the Yggdrasil metadata response.
        /// Example: false
        /// </summary>
        public const string YggdrasilAllowProfileNameLogin = "Yggdrasil:AllowProfileNameLogin";

        /// <summary>
        /// Configuration key for the lifetime of a Yggdrasil access token, in hours.
        /// Applies to sessions issued by the Yggdrasil <c>authenticate</c> and <c>refresh</c> endpoints.
        /// Example: 24
        /// </summary>
        public const string YggdrasilTokenTtlHours = "Yggdrasil:TokenTtlHours";

        /// <summary>
        /// Configuration key for the maximum number of concurrently active Yggdrasil access tokens
        /// retained per user. The oldest token is evicted when the limit is exceeded on
        /// <c>authenticate</c>.
        /// Example: 10
        /// </summary>
        public const string YggdrasilMaxActiveTokensPerUser = "Yggdrasil:MaxActiveTokensPerUser";

        /// <summary>
        /// Configuration key that controls whether the <c>player/certificates</c> endpoint under
        /// <c>minecraftservices</c> is served. Also drives <c>meta.feature.enable_profile_key</c>.
        /// Only enable this once the endpoint is implemented, since declaring the feature flag
        /// promises clients that profile key support exists.
        /// Example: false
        /// </summary>
        public const string YggdrasilEnableProfileKey = "Yggdrasil:EnableProfileKey";

        /// <summary>
        /// Configuration key that controls whether username validation is enforced on registration,
        /// and whether authlib-injector is asked to enforce its own username character checks via
        /// <c>meta.feature.username_check</c>.
        /// Example: true
        /// </summary>
        public const string YggdrasilEnforceUsernameCheck = "Yggdrasil:EnforceUsernameCheck";

        /// <summary>
        /// Configuration key for the site homepage URL advertised as <c>meta.links.homepage</c>
        /// in the Yggdrasil metadata response. Optional; omitted from the response when not set.
        /// Example: "https://example.com"
        /// </summary>
        public const string YggdrasilHomepageUrl = "Yggdrasil:HomepageUrl";

        /// <summary>
        /// Configuration key for the registration page URL advertised as <c>meta.links.register</c>
        /// in the Yggdrasil metadata response. Optional; omit or leave empty when account
        /// registration is disabled.
        /// Example: "https://example.com/register"
        /// </summary>
        public const string YggdrasilRegisterUrl = "Yggdrasil:RegisterUrl";

        /// <summary>
        /// Configuration key containing the allowed characters for usernames.
        /// Used to validate new usernames during registration.
        /// Example: "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_"
        /// </summary>
        public const string AllowedUsernameCharacters = "AllowedUsernameCharacters";
        
        /// <summary>
        /// Configuration key for the name of the Swagger/OpenAPI document.
        /// </summary>
        public const string SwaggerName = "Swagger:Name";

        /// <summary>
        /// Configuration key for the description of the Swagger/OpenAPI document.
        /// </summary>
        public const string SwaggerDescription = "Swagger:Description";

        /// <summary>
        /// Configuration key for the version of the Swagger/OpenAPI document.
        /// </summary>
        public const string SwaggerVersion = "Swagger:Version";

        /// <summary>
        /// Configuration key for the contact name shown in the Swagger/OpenAPI document.
        /// </summary>
        public const string SwaggerContactName = "Swagger:Contact:Name";

        /// <summary>
        /// Configuration key for the contact link (URL) shown in the Swagger/OpenAPI document.
        /// </summary>
        public const string SwaggerContactLink = "Swagger:Contact:Link";

        /// <summary>
        /// Configuration key for the license name shown in the Swagger/OpenAPI document.
        /// </summary>
        public const string SwaggerLicenseName = "Swagger:License:Name";

        /// <summary>
        /// Configuration key for the license link (URL) shown in the Swagger/OpenAPI document.
        /// </summary>
        public const string SwaggerLicenseLink = "Swagger:License:Link";
    }

    /// <summary>
    /// Environment variable key names.
    /// Contains constants for keys that should be loaded from environment variables.
    /// These keys represent sensitive values (credentials, secrets) that are typically injected at runtime 
    /// from .env files or environment variables rather than stored directly in appsettings.json.
    /// </summary>
    public static class EnvironmentKeys
    {
        /// <summary>
        /// Environment/config key for the database username (used to build the DB connection string).
        /// </summary>
        public const string DatabaseUser = "DB_USER";

        /// <summary>
        /// Environment/config key for the database password (used to build the DB connection string).
        /// </summary>
        public const string DatabasePassword = "DB_PASSWORD";

        /// <summary>
        /// Environment/config key for the symmetric key used to sign/encrypt JWTs.
        /// This value must be kept secret.
        /// </summary>
        public const string JwtEncryptionKey = "JWT_ENCRYPTION_KEY";

        /// <summary>
        /// Environment/config key for the symmetric key used to HMAC-sign machine (device) fingerprints.
        /// This value must be kept secret and should differ from <see cref="JwtEncryptionKey"/> so that
        /// the fingerprint signing key is never used as an AES or JWT key.
        /// </summary>
        public const string FingerprintSigningKey = "FINGERPRINT_SIGNING_KEY";

        /// <summary>
        /// Environment/config key for the symmetric key used to encrypt and decrypt stored two-factor
        /// (TOTP) secrets with AES-GCM.
        /// This value must be kept secret and should differ from <see cref="JwtEncryptionKey"/> and
        /// <see cref="FingerprintSigningKey"/>.
        /// </summary>
        public const string TwoFactorEncryptionKey = "TWO_FACTOR_ENCRYPTION_KEY";
        
        /// <summary>
        /// Environment/config key for the email sender address used by the app.
        /// Example: "noreply@example.com"
        /// </summary>
        public const string EmailAddress = "EMAIL_ADDRESS";

        /// <summary>
        /// Environment/config key for the email sender account password / app secret.
        /// </summary>
        public const string EmailPassword = "EMAIL_PASSWORD";
        
        /// <summary>
        /// Configuration key for the SSL/TLS certificate fingerprint.
        /// This value is used to verify the identity of the server certificate during secure connections.
        /// </summary>
        public const string CertificateFingerprint = "CERTIFICATE_FINGERPRINT";
        
        /// <summary>
        /// Environment/config key for the SSL/TLS certificate password or passphrase.
        /// This value is required when loading certificates from encrypted PFX/PKCS#12 files.
        /// </summary>
        public const string CertificatePassword = "CERTIFICATE_PASSWORD";
        
        /// <summary>
        /// Environment/config key for the username of the administrator account seeded on first startup.
        /// Example: "admin"
        /// </summary>
        public const string AdminUsername = "ADMIN_USERNAME";
        
        /// <summary>
        /// Environment/config key for the email address of the administrator account seeded on first startup.
        /// Example: "admin@localhost"
        /// </summary>
        public const string AdminEmail = "ADMIN_EMAIL";
        
        /// <summary>
        /// Environment/config key for the password of the administrator account seeded on first startup.
        /// This value must be kept secret.
        /// Example: "admin"
        /// </summary>
        public const string AdminPassword = "ADMIN_PASSWORD";
    }
}