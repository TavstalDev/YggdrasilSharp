using System.Text;
using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Config;

/// <summary>
/// Represents the JWT (JSON Web Token) authentication settings loaded from the configuration.
/// </summary>
public class JwtConfig
{
    /// <summary>
    /// Gets or sets the symmetric key used for AES-GCM authenticated encryption of stored data and for
    /// signing/encrypting JWTs.
    /// </summary>
    [JsonIgnore]
    public byte[] EncryptionKey { get; set; }
    
    /// <summary>
    /// Gets or sets the symmetric key used to encrypt and decrypt stored two-factor (TOTP) secrets
    /// with AES-GCM. It is not used to sign tokens.
    /// </summary>
    [JsonIgnore]
    public byte[] TwoFactorEncryptionKey { get; set; }
    
    /// <summary>
    /// Gets or sets the symmetric key used to HMAC-sign machine (device) fingerprints, so a previously
    /// issued fingerprint can be recomputed and compared later.
    /// </summary>
    [JsonIgnore]
    public byte[] FingerprintKey { get; set; }
    
    /// <summary>
    /// Gets or sets the JWT issuer.
    /// </summary>
    [JsonPropertyName("Issuer")]
    public string Issuer { get; set; }

    /// <summary>
    /// Gets or sets the JWT audience.
    /// </summary>
    [JsonPropertyName("Audience")]
    public string Audience { get; set; }
    
    /// <summary>
    /// Gets or sets the clock skew tolerance for JWT token validation.
    /// </summary>
    [JsonPropertyName("ClockSkew")]
    public TimeSpan ClockSkew { get; set; }
    
    /// <summary>
    /// Maximum number of failed authentication attempts allowed before a user is locked out.
    /// </summary>
    [JsonPropertyName("LockoutMaxAttempts")]
    public int LockoutMaxAttempts { get; set; }
    
    /// <summary>
    /// Duration of the lockout applied when the user exceeds the allowed failed authentication attempts.
    /// </summary>
    [JsonPropertyName("LockoutDuration")]
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
    /// <param name="signingKey">The symmetric key used to HMAC-sign machine fingerprints. Falls back to <paramref name="encryptionKey"/> when omitted.</param>
    /// <param name="twoFactorEncryptionKey">The symmetric key used to encrypt stored two-factor secrets. Falls back to <paramref name="encryptionKey"/> when omitted.</param>
    public JwtConfig(byte[] encryptionKey, string issuer, string audience, TimeSpan clockSkew, int lockoutMaxAttempts, TimeSpan lockoutDuration, byte[]? signingKey = null, byte[]? twoFactorEncryptionKey = null)
    {
        EncryptionKey = encryptionKey;
        FingerprintKey = signingKey ?? encryptionKey;
        TwoFactorEncryptionKey = twoFactorEncryptionKey ?? encryptionKey;
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
    /// <exception cref="ArgumentNullException">Thrown if a required JWT configuration value is missing.</exception>
    public JwtConfig(IConfiguration configuration)
    {
        string key = AppConfiguration.GetString(configuration, Constants.EnvironmentKeys.JwtEncryptionKey);
        EncryptionKey = Encoding.UTF8.GetBytes(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        key = AppConfiguration.GetString(configuration, Constants.EnvironmentKeys.FingerprintSigningKey);
        FingerprintKey = Encoding.UTF8.GetBytes(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        key = AppConfiguration.GetString(configuration, Constants.EnvironmentKeys.TwoFactorEncryptionKey);
        TwoFactorEncryptionKey = Encoding.UTF8.GetBytes(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        Issuer = AppConfiguration.GetString(configuration, Constants.ConfigurationKeys.JwtIssuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(Issuer);
        Audience = AppConfiguration.GetString(configuration, Constants.ConfigurationKeys.JwtAudience);
        ArgumentException.ThrowIfNullOrWhiteSpace(Audience);
        ClockSkew = TimeSpan.FromSeconds(configuration.GetValue(Constants.ConfigurationKeys.JwtClockSkew, 5));
        LockoutMaxAttempts = configuration.GetValue(Constants.ConfigurationKeys.JwtLockoutMaxAttempts, 5);
        LockoutDuration = TimeSpan.FromSeconds(configuration.GetValue(Constants.ConfigurationKeys.JwtLockoutDuration, 900)); 
    }
}
