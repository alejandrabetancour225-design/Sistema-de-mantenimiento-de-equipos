using System.Globalization;
using System.Security.Cryptography;

namespace Backend.API.Services;

// PBKDF2-HMAC-SHA256. 600.000 iteraciones (recomendación OWASP vigente).
// Los hashes antiguos (100.000) siguen validando y se regeneran al iniciar sesión.
public class PasswordHasher : IPasswordHasher
{
    public const int CurrentIterations = 600_000;

    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int MinAcceptedIterations = 10_000;
    private const int MaxAcceptedIterations = 5_000_000;
    private const char Separator = '.';

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            CurrentIterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return string.Join(
            Separator,
            CurrentIterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string hashedPassword)
    {
        if (!TryParse(hashedPassword, out var iterations, out var salt, out var hash))
        {
            return false;
        }

        var candidate = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            hash.Length);

        return CryptographicOperations.FixedTimeEquals(candidate, hash);
    }

    public bool NeedsRehash(string hashedPassword)
    {
        return !TryParse(hashedPassword, out var iterations, out _, out _)
            || iterations < CurrentIterations;
    }

    private static bool TryParse(string hashedPassword, out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0;
        salt = [];
        hash = [];

        var parts = hashedPassword.Split(Separator);
        if (parts.Length != 3
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out iterations)
            || iterations is < MinAcceptedIterations or > MaxAcceptedIterations)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[1]);
            hash = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length > 0 && hash.Length > 0;
    }
}
