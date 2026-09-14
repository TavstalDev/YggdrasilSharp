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
    }
}