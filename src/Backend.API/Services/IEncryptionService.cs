namespace Backend.API.Services;

public interface IEncryptionService
{
    string Normalize(string value);
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string ComputeLookup(string value);
}