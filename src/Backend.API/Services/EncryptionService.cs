using System.Security.Cryptography;
using System.Text;

namespace Backend.API.Services;

public class EncryptionService : IEncryptionService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("Backend.API.Encryption.v1");

    private readonly KeyMaterial _primary;
    private readonly KeyMaterial[] _previous;

    public EncryptionService(IConfiguration configuration)
    {
        _primary = BuildKeyMaterial(
            configuration["Encryption:Key"],
            "Encryption:Key",
            "Encryption__Key");

        var previousKeys = configuration.GetSection("Encryption:PreviousKeys").Get<string[]>() ?? [];
        _previous = previousKeys
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select((value, index) => BuildKeyMaterial(
                value,
                $"Encryption:PreviousKeys[{index}]",
                "Encryption__PreviousKeys__0"))
            .ToArray();
    }

    public string Normalize(string value) => value.Trim().ToLowerInvariant();

    public string Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_primary.AesKey, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        var result = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, result, NonceSize + TagSize, cipher.Length);
        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertext)
    {
        if (TryDecrypt(_primary, ciphertext, out var plaintext))
        {
            return plaintext;
        }

        foreach (var key in _previous)
        {
            if (TryDecrypt(key, ciphertext, out plaintext))
            {
                return plaintext;
            }
        }

        throw new CryptographicException(
            "No se pudo descifrar el valor con Encryption:Key ni con Encryption:PreviousKeys. "
            + "Verifica que la clave vigente esté configurada antes de quitar Encryption:PreviousKeys.");
    }

    public bool TryDecryptWithPrimaryKey(string ciphertext, out string plaintext)
        => TryDecrypt(_primary, ciphertext, out plaintext);

    public string ComputeLookup(string value) => ComputeLookupWith(_primary, value);

    public string[] ComputeLookupCandidates(string value)
    {
        var candidates = new string[_previous.Length + 1];
        candidates[0] = ComputeLookupWith(_primary, value);
        for (var i = 0; i < _previous.Length; i++)
        {
            candidates[i + 1] = ComputeLookupWith(_previous[i], value);
        }

        return candidates;
    }

    private static string ComputeLookupWith(KeyMaterial key, string value)
    {
        var normalized = NormalizeCore(value);
        var hash = HMACSHA256.HashData(key.HmacKey, Encoding.UTF8.GetBytes(normalized));
        return Convert.ToBase64String(hash);
    }

    private static string NormalizeCore(string value) => value.Trim().ToLowerInvariant();

    private static bool TryDecrypt(KeyMaterial key, string ciphertext, out string plaintext)
    {
        plaintext = string.Empty;
        if (string.IsNullOrEmpty(ciphertext))
        {
            return false;
        }

        try
        {
            var data = Convert.FromBase64String(ciphertext);
            if (data.Length < NonceSize + TagSize)
            {
                return false;
            }

            var nonce = data[..NonceSize];
            var tag = data[NonceSize..(NonceSize + TagSize)];
            var cipher = data[(NonceSize + TagSize)..];
            var plain = new byte[cipher.Length];

            using var aes = new AesGcm(key.AesKey, TagSize);
            aes.Decrypt(nonce, cipher, tag, plain);
            plaintext = Encoding.UTF8.GetString(plain);
            return true;
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or ArgumentException)
        {
            return false;
        }
    }

    private static KeyMaterial BuildKeyMaterial(string? value, string settingName, string envVarExample)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException(
                $"{settingName} no está configurado. En desarrollo: dotnet user-secrets set \"{settingName}\" <clave-base64-32-bytes>; en producción: variable de entorno {envVarExample}.");
        }

        byte[] masterKey;
        try
        {
            masterKey = Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException($"{settingName} debe estar codificada en Base64.");
        }

        if (masterKey.Length < 32)
        {
            throw new InvalidOperationException($"{settingName} debe tener al menos 32 bytes (256 bits).");
        }

        return new KeyMaterial(masterKey);
    }

    private sealed class KeyMaterial
    {
        public byte[] AesKey { get; }
        public byte[] HmacKey { get; }

        public KeyMaterial(byte[] masterKey)
        {
            AesKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, 32, Salt, Encoding.UTF8.GetBytes("aes"));
            HmacKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, 32, Salt, Encoding.UTF8.GetBytes("hmac"));
        }
    }
}
