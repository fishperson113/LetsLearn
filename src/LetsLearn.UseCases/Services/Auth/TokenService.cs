using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LetsLearn.Core.Entities;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using LetsLearn.UseCases.ServiceInterfaces;

namespace LetsLearn.UseCases.Services.Auth
{
    public class TokenService : ITokenService
    {
        private readonly int AccessTokenExpireSeconds;             
        private readonly int RefreshTokenExpireSeconds;   

        private readonly string _issuer;
        private readonly byte[] _secretBytes;

        public TokenService(IConfiguration config)
        {
            _issuer = config["Jwt:Issuer"] ?? throw new ArgumentException("Jwt:Issuer is required");
            var secretKey = config["Jwt:Secret"] ?? throw new ArgumentException("Jwt:Secret is required");
            _secretBytes = Encoding.UTF8.GetBytes(secretKey);
            AccessTokenExpireSeconds = int.Parse(config["Jwt:AccessTokenExpireSeconds"] ?? "3600");
            RefreshTokenExpireSeconds = int.Parse(config["Jwt:RefreshTokenExpireSeconds"] ?? "604800");
        }

        public string CreateToken(Guid userId, string role, bool isAccessToken)
        {
            var claims = new List<Claim>
            {
                new Claim("userID", userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("typ", isAccessToken ? "access" : "refresh") 
            };

            var credentials = new SigningCredentials(new SymmetricSecurityKey(_secretBytes), SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddSeconds(isAccessToken ? AccessTokenExpireSeconds : RefreshTokenExpireSeconds);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                claims: claims,
                expires: expires,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string CreateAccessToken(Guid userId, string role)
            => CreateToken(userId, role, true);

        public string CreateRefreshToken(Guid userId, string role)
            => CreateToken(userId, role, false);

        public ClaimsPrincipal ValidateToken(string token, bool isAccessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = false,
                IssuerSigningKey = new SymmetricSecurityKey(_secretBytes),
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            return tokenHandler.ValidateToken(token, validationParameters, out _);
        }

        public int GetRefreshTokenExpireSeconds()
        {
            return RefreshTokenExpireSeconds;
        }
    }
}
