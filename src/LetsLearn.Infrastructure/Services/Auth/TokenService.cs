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

namespace LetsLearn.Infrastructure.Services.Auth
{
    public class TokenService
    {
        private const string AccessTokenCookie = "ACCESS_TOKEN";
        private const string RefreshTokenCookie = "REFRESH_TOKEN";
        private const string AuthPrefix = "Bearer_";

        private const int AccessTokenExpireSeconds = 360;              // 6 phút
        private const int RefreshTokenExpireSeconds = 3600 * 24 * 7;   // 7 ngày

        private readonly string _issuer;
        private readonly byte[] _secretBytes;

        public TokenService(IConfiguration config)
        {
            _issuer = config["Jwt:Issuer"] ?? "auth0";
            var secretKey = config["Jwt:Secret"] ?? "your-super-secret-key";
            _secretBytes = Encoding.UTF8.GetBytes(secretKey);
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

        public void SetTokenCookies(HttpContext context, string accessToken, string refreshToken)
        {
            context.Response.Cookies.Append(AccessTokenCookie, AuthPrefix + accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Path = "/",
                Expires = DateTime.UtcNow.AddSeconds(AccessTokenExpireSeconds)
            });

            context.Response.Cookies.Append(RefreshTokenCookie, AuthPrefix + refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Path = "/",
                Expires = DateTime.UtcNow.AddSeconds(RefreshTokenExpireSeconds)
            });
        }

        public void RemoveAllTokens(HttpContext context)
        {
            context.Response.Cookies.Delete(AccessTokenCookie);
            context.Response.Cookies.Delete(RefreshTokenCookie);
        }

        public string? GetToken(HttpContext context, bool isAccessToken)
        {
            var cookieName = isAccessToken ? AccessTokenCookie : RefreshTokenCookie;
            if (!context.Request.Cookies.TryGetValue(cookieName, out var cookieValue))
                throw new Exception("No authorization cookie!");

            if (!cookieValue.StartsWith(AuthPrefix))
                throw new Exception("Authorization cookie must start with '" + AuthPrefix + "'.");

            return cookieValue.Substring(AuthPrefix.Length);
        }

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
    }
}
