namespace Backend.API.Services;

public interface IEncryptionService
{
    string Normalize(string value);

    // context ata el valor cifrado a su registro y campo (por ejemplo "users.email:{id}").
    // Un valor copiado a otro usuario o a otra columna ya no se puede descifrar.
    string Encrypt(string plaintext, string context);
    string Decrypt(string ciphertext, string context);
    bool TryDecryptWithPrimaryKey(string ciphertext, string context, out string plaintext);

    // true si el valor ya está en el formato actual (v2, con contexto) y cifrado con la clave vigente.
    bool IsCurrentFormat(string ciphertext, string context);

    string ComputeLookup(string value);
    string[] ComputeLookupCandidates(string value);
}

public static class EncryptionContexts
{
    public static string UserEmail(Guid userId) => $"users.email:{userId:D}";
    public static string UserPhone(Guid userId) => $"users.phone:{userId:D}";
}
