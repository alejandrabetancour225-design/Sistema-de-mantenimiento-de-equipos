namespace Backend.API.Services;

public interface ITokenService
{
    string GenerateToken(Guid userId, string email, string? role);
}