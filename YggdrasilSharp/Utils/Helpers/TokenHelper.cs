using System.Security.Cryptography;

namespace Tavstal.YggdrasilSharp.Utils.Helpers;

/// <summary>
/// Provides utility methods for generating various types of tokens.
/// </summary>
public static class TokenHelper
{
    /// <summary>
    /// Generates a random string using the specified character set and length.
    /// </summary>
    /// <param name="charSet">The set of characters to use for the random string.</param>
    /// <param name="length">The length of the random string.</param>
    /// <returns>A randomly generated string.</returns>
    public static string GenerateRandomString(string charSet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", int length = 64)
    {
        string key = "";
        for (int i = 0; i < length; i++)
            key += charSet.ElementAt(RandomNumberGenerator.GetInt32(0, charSet.Length - 1));

        return key;
    }

    /// <summary>
    /// Generates a random token with the specified byte length.
    /// </summary>
    /// <param name="tokenByteLength">The length of the token in bytes.</param>
    /// <returns>A base64-encoded string representation of the token.</returns>
    public static string GenerateToken(int tokenByteLength = 48)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(tokenByteLength);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
    
    /// <summary>
    /// Generates an account confirmation token.
    /// </summary>
    /// <returns>An account confirmation token as a base64-encoded string.</returns>
    public static string GenerateAccountConfirmationToken() => GenerateToken();

    /// <summary>
    /// Generates a recovery session token.
    /// </summary>
    /// <returns>A recovery token as a base64-encoded string.</returns>
    public static string GenerateRecoverySessionToken() => GenerateToken();

    /// <summary>
    /// Generates a two-factor session token.
    /// </summary>
    /// <returns>A two-factor session token as a base64-encoded string.</returns>
    public static string GenerateTwoFactorSessionToken() => GenerateToken(32);
}