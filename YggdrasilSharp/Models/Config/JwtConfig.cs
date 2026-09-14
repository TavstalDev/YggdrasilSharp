namespace Tavstal.YggdrasilSharp.Models.Config;

/// <summary>
/// Represents the JWT (JSON Web Token) authentication settings loaded from the configuration.
/// </summary>
public class JwtConfig
{
    /// <summary>
    /// Gets or sets the encryption key used for JWT.
    /// </summary>
    public string EncryptionKey { get; set; }

    /// <summary>
    /// Gets or sets the JWT issuer.
    /// </summary>
    public string Issuer { get; set; }

    /// <summary>
    /// Gets or sets the JWT audience.
    /// </summary>
    public string Audience { get; set; }
    
    /// <summary>
    /// Gets or sets the clock skew tolerance for JWT token validation.
    /// </summary>
    public TimeSpan ClockSkew { get; set; }
    
    /// <summary>
    /// Maximum number of failed authentication attempts allowed before a user is locked out.
    /// </summary>
    public int LockoutMaxAttempts { get; set; }
    
    /// <summary>
    /// Duration of the lockout applied when the user exceeds the allowed failed authentication attempts.
    /// </summary>
    public TimeSpan LockoutDuration { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtConfig"/> class with explicit values.
    /// </summary>
    /// <param name="encryptionKey">The symmetric key used for JWT signing/encryption.</param>
    /// <param name="issuer">The JWT issuer value.</param>
    /// <param name="audience">The JWT audience value.</param>
    /// <param name="clockSkew">The clock skew tolerance for JWT token validation.</param>
    /// <param name="lockoutMaxAttempts">The amount of failed authentication attempts allowed before a user is locked out.</param>
    /// <param name="lockoutDuration">The duration of the lockout applied when the user exceeds the allowed failed authentication attempts.</param>
    public JwtConfig(string encryptionKey, string issuer, string audience, TimeSpan clockSkew, int lockoutMaxAttempts, TimeSpan lockoutDuration)
    {
        EncryptionKey = encryptionKey;
        Issuer = issuer;
        Audience = audience;
        ClockSkew = clockSkew;
        LockoutMaxAttempts = lockoutMaxAttempts;
        LockoutDuration = lockoutDuration;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtConfig"/> class and loads settings from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration to load settings from.</param>
    /// <exception cref="InvalidOperationException">Thrown if a required JWT configuration value is missing.</exception>
    public JwtConfig(IConfiguration configuration)
    {
        EncryptionKey = Settings.GetString(configuration, Constants.EnvironmentKeys.JwtEncryptionKey);
        if (string.IsNullOrWhiteSpace(EncryptionKey) || EncryptionKey.Length < 32)
            throw new ArgumentException("Encryption key must be at least 32 characters.", nameof(EncryptionKey));
        
        Issuer = Settings.GetString(configuration, Constants.ConfigurationKeys.JwtIssuer);
        Audience = Settings.GetString(configuration, Constants.ConfigurationKeys.JwtAudience);
        ClockSkew = TimeSpan.FromSeconds(configuration.GetValue(Constants.ConfigurationKeys.JwtClockSkew, 5));
        LockoutMaxAttempts = configuration.GetValue(Constants.ConfigurationKeys.JwtLockoutMaxAttempts, 5);
        LockoutDuration = TimeSpan.FromSeconds(configuration.GetValue(Constants.ConfigurationKeys.JwtLockoutDuration, 900)); 
    }
}