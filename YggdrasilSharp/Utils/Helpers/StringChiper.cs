using System.Security.Cryptography;
using System.Text;

namespace Tavstal.YggdrasilSharp.Utils.Helpers;

/// <summary>
/// Utility class providing cryptographic operations for string hashing and authentication.
/// </summary>
public static class StringChiper
{
    /// <summary>
    /// Converts a string to its UTF-8 hexadecimal representation.
    /// </summary>
    /// <param name="input">The input string to convert to hexadecimal.</param>
    /// <returns>The hexadecimal representation of the UTF-8 encoded input string in lowercase.</returns>
    public static string GetHash(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToHexStringLower(inputBytes);
    }
    
    /// <summary>
    /// Computes an HMAC-SHA256 keyed hash of the input string.
    /// </summary>
    /// <param name="input">The plaintext string to hash.</param>
    /// <param name="key">The secret key for HMAC computation. Should be stored securely and treated as a credential.</param>
    /// <returns>A hexadecimal string (lowercase) representation of the computed HMAC-SHA256 hash.</returns>
    public static string GetEncryptedHash(string input, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hashBytes);
    }

    /// <summary>
    /// Encrypts the given plaintext string using the specified key.
    /// </summary>
    /// <param name="key">The encryption key.</param>
    /// <param name="plainText">The plaintext string to encrypt.</param>
    /// <returns>The encrypted string in Base64 format.</returns>
    public static string Encrypt(string key, string plainText)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.GenerateIV();

        using ICryptoTransform encryptor = aes.CreateEncryptor();

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] cipherText = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        byte[] combined = new byte[aes.IV.Length + cipherText.Length];
        Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherText, 0, combined, aes.IV.Length, cipherText.Length);

        return Convert.ToBase64String(combined);
    }

    /// <summary>
    /// Encrypts the given plaintext string using the specified key (extension method).
    /// </summary>
    /// <param name="plainText">The plaintext string to encrypt.</param>
    /// <param name="key">The encryption key.</param>
    /// <returns>The encrypted string in Base64 format.</returns>
    public static string EncryptSelf(this string plainText, string key) => Encrypt(key, plainText);

    /// <summary>
    /// Decrypts a Base64-encoded AES-encrypted string using the specified key.
    /// </summary>
    /// <param name="key">The decryption key as a string. Must be the same key used during encryption.</param>
    /// <param name="base64String">The Base64-encoded encrypted string, containing the IV (first 16 bytes) followed by the ciphertext.</param>
    /// <returns>The decrypted plaintext string in UTF-8 format.</returns>
    public static string Decrypt(string key, string base64String)
    {
        byte[] buffer = Convert.FromBase64String(base64String);
        byte[] iv = new byte[16];
        byte[] cipherText = new byte[buffer.Length - 16];
        Buffer.BlockCopy(buffer, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(buffer, iv.Length, cipherText, 0, cipherText.Length);
        
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = iv;
        using ICryptoTransform decryptor = aes.CreateDecryptor();
        byte[] plainBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
    
    /// <summary>
    /// Decrypts a Base64-encoded AES-encrypted string using the specified key.
    /// </summary>
    /// <param name="key">The decryption key as a string. Must be the same key used during encryption.</param>
    /// <param name="base64String">The Base64-encoded encrypted string, containing the IV (first 16 bytes) followed by the ciphertext.</param>
    /// <returns>The decrypted plaintext string in UTF-8 format.</returns>
    public static string DecryptSelf(this string base64String, string key) => Decrypt(key, base64String);
}