using System.Security.Claims;
using System.Text;
using InventoryApp.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace InventoryApp.Services
{
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiresMinutes { get; set; } = 480;
    }

    public class TokenService
    {
        private readonly JwtSettings _settings;

        public TokenService(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
        }

        public (string Token, DateTime ExpiresAt) CreateToken(User user)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiresMinutes);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim("displayName", user.DisplayName),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                }),
                Expires = expiresAt,
                Issuer = _settings.Issuer,
                Audience = _settings.Audience,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            };

            var token = new JsonWebTokenHandler().CreateToken(descriptor);
            return (token, expiresAt);
        }
    }
}