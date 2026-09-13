using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Service.Authentication;

namespace Service.BAL.Auth;

public class AuthService(IOptions<AuthOptions> authOptions, IConfiguration configuration) : IAuthService
{
    public string? Authenticate(string username, string password)
    {
        var user = authOptions.Value.Users.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));

        // Fixed-time compare so a wrong-length/wrong-content guess can't be timed against the stored password.
        if (user is null || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes(user.Password)))
        {
            return null;
        }

        var jwtSection = configuration.GetSection("Jwt");
        var jwtKey = jwtSection["Key"]
            ?? throw new InvalidOperationException("Jwt:Key configuration is required.");

        Claim[] claims = [
            new(JwtRegisteredClaimNames.Sub, user.Username),
            .. user.Roles.Select(role => new Claim(ClaimTypes.Role, role))
        ];

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
