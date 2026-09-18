using System.Security.Cryptography;
using System.Text;

namespace Backend.API.Services;

public class EncryptionService : IEncryptionService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("Backend.API.Encryption.v1");

    private readonly byte[] _aesKey;
    private readonly byte[] _hmacKey;

    public EncryptionService(IConfiguration configuration)
    {
        var master = configuration["Encryption:Key"];
        if (string.IsNullOrEmpty(master))
        {
            throw new InvalidOperationException(
                "Encryption:Key no está configurado. En desarrollo: dotnet user-secrets set \"Encryption:Key\" <clave-base64-32-bytes>; en producción: variable de entorno Encryption__Key.");
        }

        byte[] masterKey;
        try
        {
            masterKey = Convert.FromBase64String(master);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Encryption:Key debe estar codificada en Base64.");
        }

        if (masterKey.Length < 32)
        {
            throw new InvalidOperationException("Encryption:Key debe tener al menos 32 bytes (256 bits).");
        }

        _aesKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, 32, Salt, Encoding.UTF8.GetBytes("aes"));
        _hmacKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, 32, Salt, Encoding.UTF8.GetBytes("hmac"));
    }

    public string Normalize(string value) => value.Trim().ToLowerInvariant();

    public string Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_aesKey, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        var result = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, result, NonceSize + TagSize, cipher.Length);
        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertext)
    {
        var data = Convert.FromBase64String(ciphertext);
        var nonce = data[..NonceSize];
        var tag = data[NonceSize..(NonceSize + TagSize)];
        var cipher = data[(NonceSize + TagSize)..];
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(_aesKey, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    public string ComputeLookup(string value)
    {
        var normalized = Normalize(value);
        var hash = HMACSHA256.HashData(_hmacKey, Encoding.UTF8.GetBytes(normalized));
        return Convert.ToBase64String(hash);
    }
}