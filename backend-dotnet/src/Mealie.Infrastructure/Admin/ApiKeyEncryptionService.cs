using System.Security.Cryptography;
using System.Text;

namespace Mealie.Infrastructure.Admin;

/// <summary>
///     Provides AES-256-CBC encryption/decryption for AI provider API keys.
///     The encryption key is derived from <c>AppSettings.Secret</c> via SHA-256.
/// </summary>
public interface IApiKeyEncryptionService
{
    /// <summary>Encrypts a plaintext API key. Returns null if input is null or empty.</summary>
    string? Encrypt(string? plaintext);

    /// <summary>Decrypts an encrypted API key. Returns null if input is null or empty.</summary>
    string? Decrypt(string? ciphertext);

    /// <summary>
    ///     Returns a masked preview like "sk-...••••1234" for display purposes.
    ///     Never returns the real key.
    /// </summary>
    string? Mask(string? ciphertext);
}

public class ApiKeyEncryptionService : IApiKeyEncryptionService
{
    private readonly byte[] _key;

    public ApiKeyEncryptionService(string secret)
    {
        // Derive a 32-byte AES-256 key from the application secret
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    public string? Encrypt(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return null;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertextBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        // Prepend IV to ciphertext, then base64-encode the whole thing
        var result = new byte[aes.IV.Length + ciphertextBytes.Length];
        aes.IV.CopyTo(result, 0);
        ciphertextBytes.CopyTo(result, aes.IV.Length);

        return Convert.ToBase64String(result);
    }

    public string? Decrypt(string? ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext)) return null;

        try
        {
            var combined = Convert.FromBase64String(ciphertext);

            using var aes = Aes.Create();
            aes.Key = _key;

            // First 16 bytes are the IV
            var iv = combined[..16];
            var ciphertextBytes = combined[16..];

            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor();
            var plaintextBytes = decryptor.TransformFinalBlock(ciphertextBytes, 0, ciphertextBytes.Length);
            return Encoding.UTF8.GetString(plaintextBytes);
        }
        catch
        {
            return null;
        }
    }

    public string? Mask(string? ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext)) return null;

        var plaintext = Decrypt(ciphertext);
        if (string.IsNullOrEmpty(plaintext)) return null;

        // Return "sk-...••••<last4>" or just "••••<last4>" for short keys
        if (plaintext.Length <= 4)
        {
            return new string('•', plaintext.Length);
        }

        var prefix = plaintext.Length > 10 ? plaintext[..Math.Min(6, plaintext.Length - 4)] + "..." : "";
        var suffix = plaintext[^4..];
        return $"{prefix}••••{suffix}";
    }
}
