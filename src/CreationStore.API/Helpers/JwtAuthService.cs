using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CreationStore.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace CreationStore.API.Helpers
{
    public class JwtAuthService
    {
        private readonly string? _key;
        private readonly string? _issuer;
        private readonly string? _audience;
        private readonly int _expiresInMinutes;

        public JwtAuthService(IConfiguration configuration)
        {
            _key = configuration["Jwt:Key"];
            _issuer = configuration["Jwt:Issuer"];
            _audience = configuration["Jwt:Audience"];

            var expiresInMinutesText = configuration["Jwt:ExpiresInMinutes"];

            _expiresInMinutes = string.IsNullOrWhiteSpace(expiresInMinutesText)
                ? 60
                : int.Parse(expiresInMinutesText);
        }

        public string GenerateToken(
            User user,
            List<string> roles,
            out DateTime expiresAt
        )
        {
            if (string.IsNullOrWhiteSpace(_key))
            {
                throw new Exception("JWT Key is missing");
            }

            var key = Encoding.UTF8.GetBytes(_key);

            expiresAt = DateTime.UtcNow.AddMinutes(_expiresInMinutes);

            var claims = new List<Claim>
            {
                // Lưu UserId vào token
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),

                // Lưu Username vào token
                new Claim(ClaimTypes.Name, user.Username),

                // Mã định danh riêng cho token
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                // Thời gian tạo token
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256
            );

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                SigningCredentials = credentials,
                Issuer = _issuer,
                Audience = _audience
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}