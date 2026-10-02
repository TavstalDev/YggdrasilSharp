using System.Security.Cryptography;
using System.Text;

namespace Tavstal.YggdrasilSharp.Utils.Helpers;

/// <summary>
/// Provides cryptographic helpers for hashing, keyed hashing (HMAC) and authenticated
/// encryption (AES-GCM) of string values.
/// </summary>
/// <remarks>
/// The output produced by <see cref="Encrypt"/> and <see cref="Decrypt"/> is a Base64 string
/// with the layout <c>[Nonce (12 bytes)][Tag (16 bytes)][Ciphertext (variable)]</c>.
/// Encrypting the same plaintext twice produces different results because a fresh random nonce
/// is generated on every call, while decrypting any valid payload always yields the original plaintext.
/// </remarks>
public static class StringChiper
{
    private const int NONCE_SIZE = 12;
    private const int TAG_SIZE = 16;
    
    /// <summary>
    /// Converts a string to its UTF-8 hexadecimal representation. This is an encoding helper and
    /// is not a cryptographic hash; use <see cref="GetEncryptedHash"/> for keyed hashing.
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
    public static string GetEncryptedHash(string input, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hashBytes);
    }

    /// <summary>
    /// Encrypts the given plaintext string using AES-GCM authenticated encryption.
    /// </summary>
    /// <param name="key">The encryption key. Must be 16, 24, or 32 bytes (128, 192, or 256 bits).</param>
    /// <param name="plainText">The plaintext string to encrypt. It is encoded as UTF-8 before encryption.</param>
    /// <returns>
    /// The Base64-encoded payload laid out as <c>[Nonce (12 bytes)][Tag (16 bytes)][Ciphertext]</c>.
    /// Pass this value to <see cref="Decrypt"/> to recover the plaintext.
    /// </returns>
    public static string Encrypt(byte[] key, string plainText)
    {
        if (plainText == null) 
            throw new ArgumentNullException(nameof(plainText));
        
        if (key == null || (key.Length != 16 && key.Length != 24 && key.Length != 32))
            throw new ArgumentException("Key must be 128, 192, or 256 bits.", nameof(key));

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] nonce = new byte[NONCE_SIZE];
        byte[] tag = new byte[TAG_SIZE];
        byte[] cipherBytes = new byte[plainBytes.Length];
        
        // Generate cryptographically secure random nonce
        RandomNumberGenerator.Fill(nonce);

        using (var aesGcm = new AesGcm(key,  TAG_SIZE))
        {
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        // Pack layout: [Nonce (12B)] + [Tag (16B)] + [Ciphertext (Variable)]
        byte[] resultBytes = new byte[12 + 16 + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, resultBytes, 0, 12);
        Buffer.BlockCopy(tag, 0, resultBytes, 12, 16);
        Buffer.BlockCopy(cipherBytes, 0, resultBytes, 28, cipherBytes.Length);

        return Convert.ToBase64String(resultBytes);
    }

    /// <summary>
    /// Extension-method counterpart of <see cref="Encrypt(byte[],string)"/> that lets a plaintext
    /// string be encrypted with instance-like syntax.
    /// </summary>
    /// <param name="plainText">The plaintext string to encrypt. It is encoded as UTF-8 before encryption.</param>
    /// <param name="key">The encryption key. Must be 16, 24, or 32 bytes (128, 192, or 256 bits).</param>
    /// <returns>
    /// The Base64-encoded payload laid out as <c>[Nonce (12 bytes)][Tag (16 bytes)][Ciphertext]</c>.
    /// </returns>
    public static string EncryptSelf(this string plainText, byte[] key) => Encrypt(key, plainText);

    /// <summary>
    /// Decrypts a Base64-encoded AES-GCM payload produced by <see cref="Encrypt(byte[],string)"/>.
    /// </summary>
    /// <param name="key">The decryption key. Must be 16, 24, or 32 bytes and identical to the key used during encryption.</param>
    /// <param name="cipherText">
    /// The Base64-encoded payload laid out as <c>[Nonce (12 bytes)][Tag (16 bytes)][Ciphertext]</c>.
    /// </param>
    /// <returns>The decrypted plaintext decoded from UTF-8.</returns>
    public static string Decrypt(byte[] key, string cipherText)
    {
        if (cipherText == null) throw new ArgumentNullException(nameof(cipherText));
        if (key == null) throw new ArgumentNullException(nameof(key));

        byte[] fullCipherBytes = Convert.FromBase64String(cipherText);

        if (fullCipherBytes.Length < NONCE_SIZE + TAG_SIZE)
            throw new CryptographicException("Invalid ciphertext payload.");

        byte[] nonce = new byte[NONCE_SIZE];
        byte[] tag = new byte[TAG_SIZE];
        int cipherTextLength = fullCipherBytes.Length - NONCE_SIZE - TAG_SIZE;
        byte[] cipherBytes = new byte[cipherTextLength];
        byte[] plainBytes = new byte[cipherTextLength];

        // Extract layout components
        Buffer.BlockCopy(fullCipherBytes, 0, nonce, 0, NONCE_SIZE);
        Buffer.BlockCopy(fullCipherBytes, NONCE_SIZE, tag, 0, TAG_SIZE);
        Buffer.BlockCopy(fullCipherBytes, NONCE_SIZE + TAG_SIZE, cipherBytes, 0, cipherTextLength);

        using (var aesGcm = new AesGcm(key, TAG_SIZE))
        {
            // Throws CryptographicException if tag validation fails (tampered payload)
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }
    
    /// <summary>
    /// Extension-method counterpart of <see cref="Decrypt(byte[],string)"/> that lets an encrypted
    /// payload be decrypted with instance-like syntax.
    /// </summary>
    /// <param name="base64String">
    /// The Base64-encoded payload laid out as <c>[Nonce (12 bytes)][Tag (16 bytes)][Ciphertext]</c>.
    /// </param>
    /// <param name="key">The decryption key. Must be 16, 24, or 32 bytes and identical to the key used during encryption.</param>
    /// <returns>The decrypted plaintext decoded from UTF-8.</returns>
    public static string DecryptSelf(this string base64String, byte[] key) => Decrypt(key, base64String);
}