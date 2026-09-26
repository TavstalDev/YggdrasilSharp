using Tavstal.YggdrasilSharp.Models.Config;

namespace Tavstal.YggdrasilSharp.Models;

/// <summary>
/// Represents the application settings loaded from the configuration.
/// </summary>
/// <remarks>
/// ABOUT SECURITY:<br/>
/// The settings include sensitive information such as the JWT encryption key and email credentials.
/// These values can be retrieved by memory dumping or via reflection.
/// Since this project will not hit production I am not implementing additional security measures to protect these values in memory,
/// but in a production environment you should consider using secure vaults or encrypted configuration providers to safeguard sensitive settings.
/// </remarks>
public class AppConfiguration
{
    /// <summary>
    /// Gets or sets the characters allowed in usernames.
    /// Used to validate new usernames during registration.
    /// </summary>
    public string AllowedUsernameCharacters { get; set; }
    
    /// <summary>
    /// Gets or sets the certificate fingerprint.
    /// </summary>
    public string CertificateFingerprint { get; }
    
    /// <summary>
    /// Gets or sets the password or passphrase for the SSL/TLS certificate.
    /// This value is required when loading certificates from encrypted PFX/PKCS#12 files.
    /// </summary>
    public string CertificatePassword { get; }
    
    /// <summary>
    /// Gets or sets the database configuration.
    /// </summary>
    public DatabaseConfig Database { get; set; }
    
    /// <summary>
    /// Gets or sets the proxy configuration.
    /// </summary>
    public ProxyConfig Proxy { get; set; }
    
    /// <summary>
    /// Gets or sets the Yggdrasil-compatible server configuration.
    /// </summary>
    public YggdrasilConfig Yggdrasil { get; set; }
    
    /// <summary>
    /// Gets or sets the JWT (JSON Web Token) configuration.
    /// </summary>
    public JwtConfig Jwt { get; set; }
    
    /// <summary>
    /// Gets or sets the email (SMTP) configuration.
    /// </summary>
    public EmailConfig Email { get; set; }
    
    /// <summary>
    /// Gets or sets the Swagger/OpenAPI documentation configuration.
    /// </summary>
    public SwaggerConfig Swagger { get; set; }
    
    /// <summary>
    /// Gets or sets miscellaneous runtime settings such as the website and API URLs.
    /// </summary>
    public MiscConfig Misc { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AppConfiguration"/> class and loads settings from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration to load settings from.</param>
    /// <exception cref="ArgumentNullException">Thrown if a required configuration value is missing.</exception>
    public AppConfiguration(IConfiguration configuration)
    {
        AllowedUsernameCharacters = configuration.GetValue<string>(Constants.ConfigurationKeys.AllowedUsernameCharacters) ?? throw new InvalidOperationException(Constants.ConfigurationKeys.AllowedUsernameCharacters);
        CertificateFingerprint = GetString(configuration, Constants.EnvironmentKeys.CertificateFingerprint);
        CertificatePassword = GetString(configuration, Constants.EnvironmentKeys.CertificatePassword);

        Proxy = new ProxyConfig(configuration);
        Database = new  DatabaseConfig(configuration);
        Yggdrasil = new  YggdrasilConfig(configuration);
        Jwt = new  JwtConfig(configuration);
        Email = new  EmailConfig(configuration);
        Swagger = new SwaggerConfig(configuration);
        Misc = new  MiscConfig(configuration);
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AppConfiguration"/> class with explicit values for all settings.
    /// </summary>
    /// <param name="websiteUrl">The public website URL for the server (e.g., "https://example.com").</param>
    /// <param name="apiUrl">The base API URL that clients will use to access the API (e.g., "https://api.example.com").</param>
    /// <param name="encryptionKey">The symmetric key used for JWT signing/encryption.</param>
    /// <param name="issuer">The JWT issuer value.</param>
    /// <param name="audience">The JWT audience value.</param>
    /// <param name="clockSkew">The clock skew tolerance for JWT token validation.</param>
    /// <param name="lockoutMaxAttempts">The amount of failed authentication attempts allowed before a user is locked out.</param>
    /// <param name="lockoutDuration">The duration of the lockout applied when the user exceeds the allowed failed authentication attempts.</param>
    /// <param name="emailProvider">The SMTP server hostname or provider identifier used for sending email.</param>
    /// <param name="emailPort">The SMTP port used to send email (commonly 25, 465, or 587).</param>
    /// <param name="emailAddress">The email address used as the sender for outgoing messages.</param>
    /// <param name="emailPassword">The password or app-specific secret for the <paramref name="emailAddress"/> account.</param>
    /// <param name="skinDomains">An array of allowed domains for serving skins (Yggdrasil skin domains).</param>
    /// <param name="certificateFingerprint">The fingerprint of the TLS certificate used for secure communication (optional, can be empty).</param>
    /// <param name="certificatePassword">The password or passphrase for the TLS certificate (required if using an encrypted PFX/PKCS#12 file).</param>
    /// <param name="serverName">The server name presented by Yggdrasil-compatible endpoints.</param>
    /// <param name="implementationName">The name of the Yggdrasil implementation (metadata shown to clients).</param>
    /// <param name="implementationVersion">The version string of the Yggdrasil implementation.</param>
    internal AppConfiguration(string websiteUrl, string apiUrl, string encryptionKey, string issuer, 
        string audience, TimeSpan clockSkew, int lockoutMaxAttempts, TimeSpan lockoutDuration, string emailProvider, int emailPort, 
        string emailAddress, string emailPassword, string[] skinDomains, string certificateFingerprint, string certificatePassword,
        string serverName, string implementationName, string implementationVersion)
    {
        AllowedUsernameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
        CertificateFingerprint = certificateFingerprint;
        CertificatePassword = certificatePassword;
        Proxy = new ProxyConfig();
        Misc = new MiscConfig(websiteUrl, apiUrl);
        Database = new DatabaseConfig("", "", "");
        Jwt = new JwtConfig(encryptionKey, issuer, audience, clockSkew, lockoutMaxAttempts, lockoutDuration);
        Email = new EmailConfig(emailProvider, emailPort, emailAddress, emailPassword);
        Yggdrasil = new YggdrasilConfig(true, true, true, ["1.1.1.1", "2.2.2.2"], skinDomains, serverName, implementationName,
            implementationVersion);
        Swagger = new SwaggerConfig();
    }

    /// <summary>
    /// Retrieves a required string value from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration to read from.</param>
    /// <param name="key">The configuration key to look up.</param>
    /// <returns>The configuration value associated with the specified <paramref name="key"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the configuration value is missing or null.</exception>
    public static string GetString(IConfiguration configuration, string key) => configuration[key] ?? throw new ArgumentNullException(key);
}