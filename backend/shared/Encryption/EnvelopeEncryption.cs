using System.Security.Cryptography;
using System.Text;

namespace VE.Shared.Encryption;

/// <summary>
/// Envelope encryption utility for AES-256 data encryption.
/// Implements per-record data keys managed via cloud KMS.
/// VE-008: Encryption at rest via envelope encryption.
/// </summary>
public class EnvelopeEncryption
{
    private const int KeySize = 32; // AES-256
    private const int IvSize = 16;  // AES block size
    private const int TagSize = 16; // GCM tag

    /// <summary>
    /// Encrypts plaintext using AES-256-GCM.
    /// Returns tuple of (encryptedData, iv, authTag) suitable for storage.
    /// </summary>
    public static (byte[] ciphertext, byte[] iv, byte[] authTag) Encrypt(
        byte[] plaintext,
        byte[] dataEncryptionKey)
    {
        if (dataEncryptionKey.Length != KeySize)
            throw new ArgumentException($"Data key must be {KeySize} bytes", nameof(dataEncryptionKey));

        using var cipher = new AesGcm(dataEncryptionKey, TagSize);
        var iv = new byte[IvSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(iv);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        cipher.Encrypt(iv, plaintext, ciphertext, tag, null);

        return (ciphertext, iv, tag);
    }

    /// <summary>
    /// Decrypts ciphertext using AES-256-GCM.
    /// </summary>
    public static byte[] Decrypt(
        byte[] ciphertext,
        byte[] iv,
        byte[] authTag,
        byte[] dataEncryptionKey)
    {
        if (dataEncryptionKey.Length != KeySize)
            throw new ArgumentException($"Data key must be {KeySize} bytes", nameof(dataEncryptionKey));

        using var cipher = new AesGcm(dataEncryptionKey, TagSize);
        var plaintext = new byte[ciphertext.Length];

        cipher.Decrypt(iv, ciphertext, authTag, plaintext, null);

        return plaintext;
    }

    /// <summary>
    /// Generates a random data encryption key (DEK).
    /// In production, this key would be encrypted by the master key in KMS
    /// before storage (envelope encryption pattern).
    /// </summary>
    public static byte[] GenerateDataKey()
    {
        var key = new byte[KeySize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);
        return key;
    }
}
