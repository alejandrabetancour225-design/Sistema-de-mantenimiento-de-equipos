using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Backend.API.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public int ExpiresInMinutes =>
        int.TryParse(_configuration["Jwt:ExpiresInMinutes"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes)
            && minutes > 0
            ? minutes
            : 60;

    public string GenerateToken(Guid userId, string email, string? role, Guid securityStamp)
    {
        var jwt = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AppClaims.SecurityStamp, securityStamp.ToString("N"))
        };

        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(ExpiresInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
