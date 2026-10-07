namespace Backend.API.Services;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hashedPassword);

    // true si el hash usa menos iteraciones que las actuales y conviene regenerarlo.
    bool NeedsRehash(string hashedPassword);
}
